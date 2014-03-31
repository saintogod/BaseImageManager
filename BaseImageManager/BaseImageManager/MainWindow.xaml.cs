using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

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
        BindingList<BrowserItem> failedTests;

        public ObservableCollection<string> HistoryList;
        object lockobj = new object();
        Bussiness bussiness = new Bussiness();
        readonly int MaxHistoryCount = Properties.Settings.Default.MaxHistory;
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
            failedTests = new BindingList<BrowserItem>();
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
            //MI_QuickAccess.ItemsSource = QuickAccessList;
            //InitConfigFile();
            GetQuickAccessList();
            BuildQuickAccess();
        }
        public void GetQuickAccessList()
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            try
            {
                var qa = config.Sections["QuickAccessSection"] as QuickAccessSection;
                if(qa!= null && qa.QuickAccessArray.Count > 0)
                    foreach (QuickAccessItem item in qa.QuickAccessArray)
                    {
                        QuickAccessList.Add(item);
                    }
            }
            catch (Exception)
            {  }
        }
        private ObservableCollection<QuickAccessItem> InitConfigFile()
        {
            try
            {
                var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                //var group = new ConfigurationSectionGroup();
                //config.SectionGroups.Add("QuickAccessGroup", group);
                var list =new QuickAccessCollectoin();
                list.Add(new QuickAccessItem() { Title = "Hello1", BaseDir = @"D:\Workspace\BSI", SearchRule = "*WAResultImageIndex.xml" });
                list.Add(new QuickAccessItem() { Title = "Hello2", BaseDir = @"D:\Workspace\BSI", SearchRule = "*WAResultImageIndex.xml" });

                config.Sections.Add("QuickAccessSection", new QuickAccessSection() { QuickAccessArray = list });

                config.Save(ConfigurationSaveMode.Modified);
            }
            catch (Exception)
            {
                
                throw;
            }
            return null;
        }
        private void BuildQuickAccess()
        {
            foreach (var item in QuickAccessList)
            {
                var menu = new MenuItem();
                menu.Header = item.Title;
                //TODO a refresh button at the top of the menu
                var refresh = new MenuItem() { Header = "Refresh", IsEnabled = true, ToolTip="Refresh to get the latest files", Tag= item };
                refresh.Click += refresh_Click;

                menu.Items.Add(refresh);
                menu.Items.Add(new Separator());

                GenerateSubmenu(menu, item.Items);
                MI_QuickAccess.Items.Add(menu);
            }

        }

        void refresh_Click(object sender, RoutedEventArgs e)
        {
            var refreshMenu = sender as MenuItem;
            var parent = refreshMenu.Parent as MenuItem;
            for (int i = parent.Items.Count - 1; i > 1; i--)
                parent.Items.RemoveAt(i);
            var item = refreshMenu.Tag as QuickAccessItem;
            GenerateSubmenu(parent, item.Items);
        }

        private void GenerateSubmenu(MenuItem menu, List<string> item)
        {
            if (item.Count == 0)
                menu.Items.Add(new MenuItem() { Header = "Can't find any matched file.", IsEnabled = false });
            else
            {
                foreach (var file in item)
                {
                    var subMenu = new MenuItem() { Header = file, ToolTip = file };
                    subMenu.Click += QuickAccess_Click;
                    menu.Items.Add(subMenu);
                }
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
                HistoryList.Insert(0, filePath);
                if (HistoryList.Count > MaxHistoryCount)
                    HistoryList.RemoveAt(MaxHistoryCount);
                
            }
            Properties.Settings.Default.HistoryFiles.Clear();
            Properties.Settings.Default.HistoryFiles.AddRange(HistoryList.ToArray());
            Properties.Settings.Default.Save();
        }
        private void HistoryItem_Click(object sender, RoutedEventArgs e)
        {
            bussiness.LoadIndexFile(((MenuItem)sender).Header.ToString(), failedTests);
            AddHistoryItem(((MenuItem)sender).Header.ToString());
        }
        private void QuickAccess_Click(object sender, RoutedEventArgs e)
        {
            bussiness.LoadIndexFile(((MenuItem)sender).Header.ToString(), failedTests);
            AddHistoryItem(((MenuItem)sender).Header.ToString());
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
                bussiness.LoadIndexFile(ofd.FileName, failedTests);
                ErrorList.ItemsSource = failedTests;
                AddHistoryItem(ofd.FileName);
            }
        }

        private void ReloadIndexFile_Click(object sender, RoutedEventArgs e)
        {
            bussiness.LoadIndexFile(HistoryList.First(), failedTests);
            ErrorList.ItemsSource = failedTests;
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
            var setting = new Setting(QuickAccessList);
            setting.ShowDialog();
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.HistoryFiles.Clear();
            Properties.Settings.Default.Save();
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

        private void MI_Exit_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
