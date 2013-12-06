using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BaseImageManager
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static OpenFileDialog ofd = new OpenFileDialog();
        string _defaultIndexFile = Properties.Settings.Default.IndexFilePath;
        IEnumerable<BrowserItem> failedTests;
        object lockobj = new object();
        Bussiness bussiness = new Bussiness();
        public string DefaultIndexFilePath {
            get {
                lock (lockobj)
                {
                    if (string.IsNullOrEmpty(_defaultIndexFile))
                        DefaultIndexFilePath = System.IO.Path.Combine(Environment.GetEnvironmentVariable("TEMP"), "WAResultImageIndex.xml");
                    return _defaultIndexFile; 
                }
            }
            set {

                lock (lockobj)
                {
                    _defaultIndexFile = value;
                    Properties.Settings.Default.IndexFilePath = value;
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
            ofd.CheckPathExists = true;
            ofd.CheckFileExists = true;
            ofd.Filter = "TestResultFile(.xml)|*.xml";
            ofd.RestoreDirectory = false;
            ofd.Title = "Open Test Result Index File";
            ofd.InitialDirectory = DefaultIndexFilePath;
            ErrorList.Items.Clear();
            ErrorList.ItemsSource = failedTests;
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
            ExpectPIC.ImageSource = new BitmapImage(new Uri(errorInfo.ExpectedImg, UriKind.Absolute));
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
    }
}
