using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaseImageManager
{
    public sealed class QuickAccessItem : ConfigurationElement
    {
        [ConfigurationProperty("Title", DefaultValue="Quick Access", IsRequired=true)]
        [StringValidator(MinLength=2, MaxLength=30)]
        public string Title { get { return this["Title"] as string; } set { this["Title"] = value; } }
        [ConfigurationProperty("BaseDir", IsRequired=true)]
        public string BaseDir { get { return this["BaseDir"] as string; } set { this["BaseDir"] = value; } }
        [ConfigurationProperty("SearchRule", IsRequired = true)]
        public string SearchRule { get { return this["SearchRule"] as string; } set { this["SearchRule"] = value; } }

        readonly int MaxCount = BaseImageManager.Properties.Settings.Default.MaxQuickAccess;
        public List<string> Items { get { return GetItems(); } }

        public string Name { get { return string.Format("MI_{0}", Title.Replace(' ', '_')); } }
                public QuickAccessItem()
        { }
        public QuickAccessItem(string title, string baseDir, string rule)
        {
            Title = title;
            SearchRule = rule;
            BaseDir = baseDir;
        }
        private List<string> GetItems() 
        {
            if (Directory.Exists(BaseDir))
            {
                return new DirectoryInfo(BaseDir)
                    .EnumerateFiles(SearchRule, SearchOption.AllDirectories)
                    .OrderByDescending(file => file.CreationTimeUtc)
                    .Take(MaxCount)
                    .Select(file => file.Name)
                    .ToList();
            }
            return new List<string>();
        }
    }
     
    [ConfigurationCollection(typeof(QuickAccessItem))]
    public sealed class QuickAccessCollectoin : ConfigurationElementCollection
    {
        protected override ConfigurationElement CreateNewElement()
        {
            return new QuickAccessItem();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return (element as QuickAccessItem).Title;
        }
        public QuickAccessItem this[int idx]
        {
            get
            {
                return BaseGet(idx) as QuickAccessItem;
            }
        }

        new public QuickAccessItem this[string key]
        {
            get
            {
                return BaseGet(key) as QuickAccessItem;
            }
        }
        public int IndexOf(QuickAccessItem item)
        {
            return BaseIndexOf(item);
        }
        public void Add(QuickAccessItem item)
        {
            BaseAdd(item);
        }
        protected override void BaseAdd(ConfigurationElement element)
        {
            base.BaseAdd(element, false);
        }
        public void Remove(QuickAccessItem item)
        {
            if (BaseIndexOf(item) >= 0)
            {
                BaseRemove(item.Title);
            }
        }
        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }
        public void Remove(string title)
        {
            BaseRemove(title);
        }
        public void Clear()
        {
            BaseClear();
        }
    }

    public sealed class QuickAccessSection : ConfigurationSection
    {
        [ConfigurationProperty("QuickAccessArray")]
        public QuickAccessCollectoin QuickAccessArray
        {
            get { return base["QuickAccessArray"] as QuickAccessCollectoin; }
            set { base["QuickAccessArray"] = value; }
        }
    }
}
