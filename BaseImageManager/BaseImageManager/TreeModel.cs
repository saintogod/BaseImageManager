using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseImageManager
{
    public class BrowserItem : INotifyPropertyChanged
    {
        #region Private properties
        private bool? _isChecked;
        public ObservableCollection<ErrorItem> ErrorItems { get; set; }
       
        #endregion
        public BrowserItem()
        {
            ErrorItems = new ObservableCollection<ErrorItem>();
            Id = System.Guid.NewGuid().ToString();
            _isChecked = true;
        }
        public BrowserItem(string header, IEnumerable<ErrorItem> errors) : this()
        {
            Header = header;
            foreach (var item in errors)
            {
                item.Parent = this;
                ErrorItems.Add(item);
            }
        }
        public bool ChangeFromChild { get; set; }
        public string Id { get; set; }

        public string Header { get; set; }

        public bool? IsChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                if (value != _isChecked)
                {
                    if (!ChangeFromChild)
                    {
                        if (_isChecked.HasValue)
                            foreach (var child in ErrorItems)
                                child.IsChecked = value??false;
                        _isChecked = value??false;
                    }
                    else
                    {
                        ChangeFromChild = false;
                        var count = ErrorItems.Count(item => item.IsChecked);
                        _isChecked = count < ErrorItems.Count && count > 0 ? (bool?)null : count == ErrorItems.Count ? true : false;
                    }
                    NotifyPropertyChanged("IsChecked");
                }
            }
        }                
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }

    public class ErrorItem : INotifyPropertyChanged
    {
        #region private properties
        private bool _isChecked;
        #endregion
        public ErrorItem()
        {
            _isChecked = true;
        }
        public ErrorItem(BrowserItem parent)
        {
            _isChecked = parent.IsChecked.HasValue ? (bool)parent.IsChecked : false;
            Parent = parent;
        }
        public string ExpectedImg { get; set; }
        public string CapturedImg { get; set; }
        public bool IsChecked
        {
            get
            {
                return _isChecked;
            }
            set
            {
                if (value != _isChecked)
                {
                    _isChecked = value;
                    NotifyPropertyChanged("IsChecked");
                    if (Parent != null)
                    {
                        Parent.ChangeFromChild = true;
                        Parent.IsChecked = _isChecked;
                    }
                }
            }
        }
        public BrowserItem Parent { get; set; }
        public string Header { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }

    public class QuickAccessItem
    {
        public string Title { get; private set; }
        public string Name { get; private set; }
        public string DirectoryPath { get; private set; }
        public List<string> Items { get; private set; }

        public QuickAccessItem(string title, string directory)
        {
            Title = title;
            Name = string.Format("MI_{0}", title.Replace(' ', '_'));
            DirectoryPath = directory;
            Items = GetItems();
        }
        private List<string> GetItems() 
        {
            //if (!Directory.Exists(DirectoryPath))
            //    return new List<string>();
            //TODO find the newest LIst
            return new List<string>(new[] { @"A:\aset0", @"A:\aset1", @"A:\aset2" });
        }
    }
}
