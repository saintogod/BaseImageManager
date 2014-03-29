using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;

namespace BaseImageManager
{
    /// <summary>
    /// Interaction logic for Setting.xaml
    /// </summary>
    public partial class Setting : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        ObservableCollection<QuickAccessEntry> qaList;
        ObservableCollection<QuickAccessEntry> QuickAccessList
        {
            get {
                if (qaList == null)
                    qaList = new ObservableCollection<QuickAccessEntry>();
                return qaList; }
            set
            {
                if (qaList != value)
                {
                    qaList = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("qalist"));
                }
            }
        }
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            if (null != PropertyChanged)
            {
                PropertyChanged(this, e);
            }
        }
        public Setting(ObservableCollection<QuickAccessItem> qalist)
        {
            InitializeComponent();
            DataContext = this;
            foreach (var item in qalist)
                QuickAccessList.Add(new QuickAccessEntry() { Title=item.Title, BaseDir = item.BaseDir, SearchRule = item.SearchRule });
            dg.ItemsSource = QuickAccessList;
        }

        private void Remove_Click(object sender, RoutedEventArgs e)
        {
            QuickAccessList.Remove((sender as Button).Tag as QuickAccessEntry);
        }
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            try
            {
                var qa = config.Sections["QuickAccessSection"] as QuickAccessSection;
                if (qa != null)
                {
                    if (qa.QuickAccessArray == null)
                        qa.QuickAccessArray = new QuickAccessCollectoin();
                    qa.QuickAccessArray.Clear();
                    foreach (var item in QuickAccessList)
                    {
                        qa.QuickAccessArray.Add(new QuickAccessItem() { Title = item.Title, BaseDir = item.BaseDir, SearchRule = item.SearchRule });
                    }
                    config.Save(ConfigurationSaveMode.Modified);
                }
            }
            catch
            { }
            Properties.Settings.Default.Save();
        }
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            Apply_Click(sender, e);
            this.Close();
        }
    }
    class QuickAccessEntry : INotifyPropertyChanged
    {
        private string _title;
        private string _baseDir;
        private string _searchRule;

        public string Title
        {
            get { return _title; }
            set 
            {
                _title = value;
                NotifyPropertyChanged("Title");
            }
        }

        public string BaseDir
        {
            get { return _baseDir; }
            set
            {
                _baseDir = value;
                NotifyPropertyChanged("BaseDir");
            }
        }


        public string SearchRule
        {
            get { return _searchRule; }
            set
            {
                _searchRule = value;
                NotifyPropertyChanged("SearchRule");
            }
        }
        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Private Helpers

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}