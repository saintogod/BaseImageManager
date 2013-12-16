using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace BaseImageManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static OpenFileDialog ofd = new OpenFileDialog();
        string _baseImageFolder = Properties.Settings.Default.BaseImageFolder;
        string _lastOpenFolder = Properties.Settings.Default.LastOpenFolder;
        IEnumerable<BrowserItem> failedTests;
        public ObservableCollection<string> HistoryList;
        object lockobj = new object();
        Bussiness bussiness = new Bussiness();
        readonly int MaxHistoryCount = 10;
        public string BaseImageFolder
        {
            get
            {
                lock (lockobj)
                {
                    if (string.IsNullOrEmpty(_baseImageFolder))
                        BaseImageFolder = @"C:\AutoTestFiles\WebAdmin\Screenshots";
                    return _baseImageFolder;
                }
            }
            set
            {
                lock (lockobj)
                {
                    _baseImageFolder = value;
                    Properties.Settings.Default.BaseImageFolder = value;
                    Properties.Settings.Default.Save();
                }
            }
        }
        public ObservableCollection<QuickAccessItem> QuickAccessList;
        public string LastOpenFolder
        {
            get
            {
                lock (lockobj)
                {
                    if (string.IsNullOrEmpty(_lastOpenFolder))
                        LastOpenFolder = Environment.GetEnvironmentVariable("TEMP");
                    return _lastOpenFolder;
                }
            }
            set
            {
                lock (lockobj)
                {
                    _lastOpenFolder = value;
                    Properties.Settings.Default.LastOpenFolder = value;
                    Properties.Settings.Default.Save();
                }
            }
        }


        public MainWindow()
        {
            InitializeComponent();
            CustomInit();
        }
        private void CustomInit()
        {
            MI_QuickAccess.Items.Clear();
            MI_QuickAccess.ItemsSource = HistoryList;
            ofd.CheckPathExists = true;
            ofd.CheckFileExists = true;
            ofd.Filter = "TestResultFile(.xml)|*.xml";
            ofd.RestoreDirectory = true;
            ofd.Title = "Open Test Result Index File";
            ofd.InitialDirectory = LastOpenFolder;
            ofd.FileName = "WAResultImageIndex";
            ErrorList.Items.Clear();
            ErrorList.ItemsSource = failedTests;

            HistoryList = new ObservableCollection<string>(Properties.Settings.Default.HistoryFiles.Cast<string>());
            MI_Recent.ItemsSource = HistoryList;

            QuickAccessList = new ObservableCollection<QuickAccessItem>();
            QuickAccessList.Add(new QuickAccessItem("Heelo", "C:\\c"));
            //MI_QuickAccess.ItemsSource = QuickAccessList;
            BuildQuickAccess();
        }
        private void BuildQuickAccess()
        {
            foreach (var item in QuickAccessList)
            {
                var menu = new MenuItem();
                menu.Header = item.Title;
                foreach (var file in item.Items)
                {
                    var subMenu = new MenuItem() { Header = file };
                    subMenu.Click += QuickAccess_Click;
                    menu.Items.Add(subMenu);
                }
                MI_QuickAccess.Items.Add(menu);
            }

        }
        private void AddHistoryItem(string filePath) {
            var curItem = HistoryList.Where(item => item.Equals(filePath, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (curItem != null)
            {
                var index = HistoryList.IndexOf(curItem);
                if (index != 0)
                    HistoryList.Move(index, 0);
            }
            else
            {
                if (HistoryList.Count < MaxHistoryCount)
                    HistoryList.Add(filePath);
                else
                {
                    HistoryList.RemoveAt(MaxHistoryCount - 1);
                    HistoryList.Insert(0,filePath);
                }
            }
            Properties.Settings.Default.HistoryFiles.Clear();
            Properties.Settings.Default.HistoryFiles.AddRange(HistoryList.ToArray());
            Properties.Settings.Default.Save();
        }
        private void HistoryItem_Click(object sender, RoutedEventArgs e)
        {
            //bussiness.LoadIndexFile(((MenuItem)sender).Header.ToString(), out failedTests);
            AddHistoryItem(((MenuItem)sender).Header.ToString());
        }
        private void QuickAccess_Click(object sender, RoutedEventArgs e)
        {
            //bussiness.LoadIndexFile(((MenuItem)sender).Header.ToString(), out failedTests);

        }
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (BrowserItem bi in ErrorList.Items)
                bi.IsChecked = true;
        }

        private void ClearSelection_Click(object sender, RoutedEventArgs e)
        {
            foreach (BrowserItem bi in ErrorList.Items)
                bi.IsChecked = false;
        }

        private void SelectReverse_Click(object sender, RoutedEventArgs e)
        {
            foreach (BrowserItem bi in ErrorList.Items)
                foreach (var item in bi.ErrorItems)
                    item.IsChecked = !item.IsChecked;
        }

        private void LoadIndexFile_Click(object sender, RoutedEventArgs e)
        {
            var result = ofd.ShowDialog();
            if (result.HasValue && result.Value)
            {
                bussiness.LoadIndexFile(ofd.FileName, out failedTests);
                ErrorList.ItemsSource = failedTests;
                AddHistoryItem(ofd.FileName);
            }
        }

        private void ResetIndexFile_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ViewInSVN_Click(object sender, RoutedEventArgs e)
        {
            if (ErrorList.SelectedItem is ErrorItem)
            {
                var errorItem = ErrorList.SelectedItem as ErrorItem;
                bussiness.ViewInSvnDiff(errorItem.CapturedImg, errorItem.ExpectedImg);
            }
        }

        private void ApplyNewImages_Click(object sender, RoutedEventArgs e)
        {
           MessageBox.Show(bussiness.ApplyImages(failedTests), "Update Result");
        }

        private void CommitToSVN_Click(object sender, RoutedEventArgs e)
        {
            bussiness.CommitToSVN(BaseImageFolder);
        }

        private void RevertChanges_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Setting_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {

        }

        private void errItem_MouseUp(object sender, MouseButtonEventArgs e)
        {
            var errorInfo = (sender as StackPanel).Tag as ErrorItem;
            ExpectPIC.ImageSource = LoadImage(errorInfo.ExpectedImg);
            ExpectedPicSizeInfo.Content = string.Format("Width * Height : {0:####} * {1:####} ", ExpectPIC.ImageSource.Width, ExpectPIC.ImageSource.Height);

            MainImage.ImageSource = new BitmapImage(new Uri(errorInfo.CapturedImg, UriKind.Absolute));
            CapturedPicSizeInfo.Content = string.Format("Width * Height : {0:####} * {1:####} ", MainImage.ImageSource.Width, MainImage.ImageSource.Height);

            var diff = System.IO.Path.Combine(new FileInfo(errorInfo.CapturedImg).Directory.FullName, "diff.png");
            if (!File.Exists(diff))
            {
                DiffPIC.ImageSource = new BitmapImage(new Uri(@"pack://application:,,,/BaseImageManager;component/Resources/NotFound.png"));
            }
            else
            {
                DiffPIC.ImageSource = new BitmapImage(new Uri(diff, UriKind.Absolute));
            }
        }
        private BitmapImage LoadImage(string myImageFile)
        {
            BitmapImage myRetVal = null;
            if (myImageFile != null)
            {
                BitmapImage image = new BitmapImage();
                using (FileStream stream = File.OpenRead(myImageFile))
                {
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream;
                    image.EndInit();
                }
                myRetVal = image;
            }
            return myRetVal;
        }
        private void PictureInShow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                if (ErrorList.SelectedItem is ErrorItem)
                {
                    if (CapturedImageCanvas.IsVisible)
                    {
                        bussiness.OpenImage((ErrorList.SelectedItem as ErrorItem).CapturedImg);
                    }
                    else
                    {
                        MainImage.ImageSource = LoadImage((ErrorList.SelectedItem as ErrorItem).CapturedImg);
                        CapturedImageCanvas.Visibility = System.Windows.Visibility.Visible;
                        ExpectedImageCanvas.Visibility = System.Windows.Visibility.Visible;
                    }
                }
            }
        }
        private void SmallImage_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                MainImage.ImageSource = ((sender as Canvas).Background as ImageBrush).ImageSource;
                CapturedImageCanvas.Visibility = System.Windows.Visibility.Hidden;
                ExpectedImageCanvas.Visibility = System.Windows.Visibility.Hidden;
                e.Handled = true;
            }
        }

        private void CopyImage_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetImage(MainImage.ImageSource as BitmapSource);
        }

        private void CopyImagePath_Click(object sender, RoutedEventArgs e)
        {
            var img = MainImage.ImageSource as BitmapSource;

            Clipboard.SetText(img.ToString());
        }

        private void OpenInImageViewer_Click(object sender, RoutedEventArgs e)
        {
            var img = MainImage.ImageSource as BitmapSource;
            bussiness.OpenImage(img.ToString());
        }

        private void MenuItem_MouseEnter(object sender, MouseEventArgs e)
        {

        }
    }
}
