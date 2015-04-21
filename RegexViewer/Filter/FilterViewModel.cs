﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace RegexViewer
{
    public class FilterViewModel : BaseViewModel<FilterFileItem>
    {
        #region Public Constructors

        public FilterViewModel()
        {
            this.TabItems = new ObservableCollection<ITabViewModel<FilterFileItem>>();
            this.TabItems.CollectionChanged += TabItems_CollectionChanged;
            this.ViewManager = new FilterFileManager();
            this.ViewManager.PropertyChanged += ViewManager_PropertyChanged;

            // load tabs from last session
            foreach (FilterFile logProperty in this.ViewManager.OpenFiles(this.Settings.CurrentFilterFiles.ToArray()))
            {
                AddTabItem(logProperty);
            }
        }

        #endregion Public Constructors

        #region Public Properties

        public ObservableCollection<WPFMenuItem> RecentCollection
        {
            get
            {
                return (RecentCollectionBuilder(Settings.RecentFilterFiles));
            }
        }

        public TabControl TabControl { get; set; }

        #endregion Public Properties

        #region Public Methods

        public override void AddTabItem(IFile<FilterFileItem> filterFile)
        {
            if (!this.TabItems.Any(x => String.Compare((string)x.Tag, filterFile.Tag, true) == 0))
            {
                SetStatus("adding tab:" + filterFile.Tag);
                FilterTabViewModel tabItem = new FilterTabViewModel()
                {
                    Name = filterFile.FileName,
                    ContentList = ((FilterFile)filterFile).ContentItems,
                    Tag = filterFile.Tag,
                    Header = filterFile.FileName,
                    Modified = false
                };
                tabItem.PropertyChanged += tabItem_PropertyChanged;
                TabItems.Add(tabItem);

                this.SelectedIndex = this.TabItems.Count - 1;
            }
        }

        public List<FilterFileItem> CleanFilterList(FilterFile filterFile)
        {
            List<FilterFileItem> fileItems = new List<FilterFileItem>();

            // clean up list
            foreach (FilterFileItem fileItem in filterFile.ContentItems.OrderBy(x => x.Index))
            {
                if (!fileItem.Enabled | string.IsNullOrEmpty(fileItem.Filterpattern))
                {
                    continue;
                }

                fileItems.Add(fileItem);
            }

            return fileItems;
        }

        public FilterNeed CompareFilterList(List<FilterFileItem> previousFilterFileItems)
        {
            FilterNeed retval = FilterNeed.Unknown;
            List<FilterFileItem> currentItems = this.FilterList();

            SetStatus("CompareFilterList: enter");

            if (previousFilterFileItems == null && currentItems != null)
            {
                retval = FilterNeed.Filter;
            }
            else if (currentItems == null)
            {
                retval = FilterNeed.ShowAll;
            }
            else if (currentItems.Count == 0)
            {
                retval = FilterNeed.ShowAll;
            }
            else if (currentItems.Count > 0
                && previousFilterFileItems.Count > 0
                && currentItems.Count == previousFilterFileItems.Count)
            {
                int i = 0;
                foreach (FilterFileItem fileItem in previousFilterFileItems.OrderBy(x => x.Index))
                {
                    FilterFileItem currentItem = currentItems[i++];
                    if (currentItem.Enabled != fileItem.Enabled
                        || currentItem.Exclude != fileItem.Exclude
                        || currentItem.Regex != fileItem.Regex
                        || currentItem.Filterpattern != fileItem.Filterpattern
                        || currentItem.CaseSensitive != currentItem.CaseSensitive)
                    {
                        retval = FilterNeed.Filter;

                        break;
                    }
                    else if (currentItem.BackgroundColor != fileItem.BackgroundColor
                        || currentItem.ForegroundColor != fileItem.ForegroundColor)
                    {
                        retval = FilterNeed.ApplyColor;
                        break;
                    }

                    retval = FilterNeed.Current;
                }
            }
            else
            {
                retval = FilterNeed.Filter;
            }

            SetStatus("CompareFilterList:returning:" + retval.ToString());
            return retval;
        }

        public List<FilterFileItem> FilterList()
        {
            List<FilterFileItem> filterFileItems = new List<FilterFileItem>();

            try
            {
                if (SelectedIndex >= 0 && SelectedIndex < this.TabItems.Count)
                {
                    FilterFile filterFile = (FilterFile)CurrentFile();
                    if (filterFile != null)
                    {
                        return CleanFilterList(filterFile);
                    }
                }

                return filterFileItems;
            }
            catch (Exception e)
            {
                SetStatus("Exception:FilterTabItem:" + e.ToString());
                return filterFileItems;
            }
        }

        public override void OpenFile(object sender)
        {
            bool silent = (sender is string && !String.IsNullOrEmpty(sender as string)) ? true : false;
            if (sender is string && !String.IsNullOrEmpty(sender as string))
            {
                silent = true;
            }

            string[] logNames;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.Filter = "Xml Files (*.xml)|*.xml|Tat Files (*.tat)|*.tat|All Files (*.*)|*.*";
            dlg.InitialDirectory = Settings.FilterDirectory ?? "";
            dlg.Multiselect = true;

            Nullable<bool> result = false;

            // Show open file dialog box
            if (silent)
            {
                result = true;
                logNames = new string[1] { (sender as string) };
            }
            else
            {
                result = dlg.ShowDialog();
                logNames = dlg.FileNames;
            }

            if (result != true)
            {
                return;
            }

            foreach (string logName in logNames)
            {
                // Process open file dialog box results
                if (File.Exists(logName))
                {
                    SetStatus(string.Format("opening file:{0}", logName));
                    VerifyAndOpenFile(logName);
                }
            }
        }

        public override void RenameTabItem(string logName)
        {
            // rename tab
            ITabViewModel<FilterFileItem> tabItem = this.TabItems[SelectedIndex];
            Settings.RemoveFilterFile(tabItem.Tag);
            tabItem.Tag = CurrentFile().Tag = logName;
            CurrentFile().FileName = tabItem.Header = tabItem.Name = Path.GetFileName(logName);
            Settings.AddFilterFile(logName);
        }

        public override void SaveFileAs(object sender)
        {
            ITabViewModel<FilterFileItem> tabItem;

            if (sender is TabItem)
            {
                tabItem = (ITabViewModel<FilterFileItem>)(sender as TabItem);
            }
            else
            {
                tabItem = (ITabViewModel<FilterFileItem>)this.TabItems[this.SelectedIndex];
            }

            bool silent = (sender is string && !String.IsNullOrEmpty(sender as string)) ? true : false;

            string logName = string.Empty;
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.DefaultExt = ".xml";
            dlg.Filter = "Xml Files (*.xml)|*.xml|Tat Files (*.tat)|*.tat|All Files (*.*)|*.*";

            dlg.InitialDirectory = Path.GetDirectoryName(tabItem.Tag) ?? Settings.FilterDirectory;
            dlg.FileName = Path.GetFileName(tabItem.Tag);

            Nullable<bool> result = false;

            // Show save file dialog box
            if (silent)
            {
                result = true;
                logName = (sender as string);
            }
            else
            {
                result = dlg.ShowDialog();
                logName = dlg.FileName;

                if (string.IsNullOrEmpty(logName))
                {
                    return;
                }
            }

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                SetStatus(string.Format("saving file:{0}", logName));

                RenameTabItem(logName);

                SaveFile(null);
            }

            return;
        }

        #endregion Public Methods

        #region Internal Methods

        internal bool VerifyAndOpenFile(string fileName)
        {
            SetStatus(string.Format("checking filter file:{0}", fileName));
            FilterFile filterFile = new FilterFile();
            if (String.IsNullOrEmpty((filterFile = (FilterFile)this.ViewManager.OpenFile(fileName)).Tag))
            {
                return false;
            }

            AddTabItem(filterFile);

            return true;
        }

        #endregion Internal Methods

        #region Private Methods

        private void tabItem_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            SetStatus("filterViewModel:tabItem_PropertyChanged");
            OnPropertyChanged(e.PropertyName);
        }

        private void TabItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SetStatus("filterViewModel:TabItems_CollectionChanged");
            OnPropertyChanged("TabItems");
        }

        private void VerifyIndex(FilterFileItem filterFileItem = null)
        {
            FilterFile filterFile = (FilterFile)CurrentFile();

            try
            {
                filterFile.EnablePatternNotifications(false);
                List<FilterFileItem> filterItems = filterFile.ContentItems.ToList();
                List<FilterFileItem> sortedFilterItems = new List<FilterFileItem>(filterItems.OrderBy(x => x.Index));

                bool dupes = false;
                bool needsSorting = false;
                List<int> indexList = new List<int>();

                for (int i = 0; i < sortedFilterItems.Count; i++)
                {
                    int index = sortedFilterItems[i].Index;
                    if (index != filterItems[i].Index)
                    {
                        needsSorting = true;
                    }

                    if (!indexList.Contains(index))
                    {
                        indexList.Add(index);
                    }
                    else
                    {
                        dupes = true;
                    }
                }

                // does it need to be resorted
                if (!needsSorting && !dupes)
                {
                    // do nothing
                    return;
                }
                else if (needsSorting && !dupes)
                {
                    this.TabItems[SelectedIndex].ContentList = filterFile.ContentItems = new ObservableCollection<FilterFileItem>(sortedFilterItems);
                }
                else if (dupes)
                {
                    int currentIndex = -1;

                    if (filterFileItem != null && sortedFilterItems.Count(x => x.Index == filterFileItem.Index) > 1)
                    {
                        // remove and insert selected item in list at lowest position in index of dupes
                        sortedFilterItems.RemoveAt(sortedFilterItems.IndexOf(filterFileItem));
                        sortedFilterItems.Insert((int)(sortedFilterItems.IndexOf(sortedFilterItems.First(x => x.Index == filterFileItem.Index))), filterFileItem);
                    }

                    for (int i = 0; i < sortedFilterItems.Count; i++)
                    {
                        int index = sortedFilterItems[i].Index;

                        if (index <= currentIndex)
                        {
                            filterItems[i] = sortedFilterItems[i];
                            filterItems[i].Index = ++currentIndex;
                        }
                        else
                        {
                            filterItems[i] = sortedFilterItems[i];
                            currentIndex = index;
                        }
                    }

                    this.TabItems[SelectedIndex].ContentList = filterFile.ContentItems = new ObservableCollection<FilterFileItem>(filterItems);
                }
            }
            catch (Exception ex)
            {
                SetStatus("VerifyIndex:exception:" + ex.ToString());
            }
            finally
            {
                filterFile.EnablePatternNotifications(true);
            }
        }

        private void ViewManager_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is FilterFileItem)
            {
                FilterFile filterFile = (FilterFile)CurrentFile();
                if (filterFile != null)
                {
                    ((FilterFileManager)this.ViewManager).ManageNewFilterFileItem(filterFile);
                    VerifyIndex((sender as FilterFileItem));
                }
            }

            OnPropertyChanged(sender, e);
        }

        #endregion Private Methods
    }
}