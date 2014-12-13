﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RegexViewer
{
    //public class RegexViewModel : MainViewModel, INotifyPropertyChanged, RegexViewer.IViewModel
    public class LogViewModel : BaseViewModel<LogFileItem>
    {
        #region Public Constructors
        FilterViewModel _filterViewModel;
        LogFileManager _logFileManager;
        int _previousIndex = -1;
        public LogViewModel(FilterViewModel filterViewModel)
        {
            _filterViewModel = filterViewModel;
            //_filterViewModel.TabItems.CollectionChanged += TabItems_CollectionChanged;
            this.TabItems = new ObservableCollection<ITabViewModel<LogFileItem>>();
            this.ViewManager = new LogFileManager();
            _logFileManager = (LogFileManager)this.ViewManager;
             
            _filterViewModel.PropertyChanged += _filterViewModel_PropertyChanged;
            this.PropertyChanged += LogViewModel_PropertyChanged;
 
            // load tabs from last session
            foreach (LogFile logFile in this.ViewManager.OpenFiles(this.Settings.CurrentLogFiles.ToArray()))
            {
                AddTabItem(logFile);
            }

          
        }

        void LogViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // dont handle count updates

            SetStatus("LogViewModel.PropertyChanged" + e.PropertyName);
            FilterActiveTabItem();
        }

        void _filterViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // dont handle count updates

            SetStatus("_filterViewModel.PropertyChanged" + e.PropertyName);
            FilterActiveTabItem();
        }

        void TabItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // dont handle  count updates

            SetStatus("_filterViewModel.CollectionChanged" + sender.ToString());
            FilterActiveTabItem();
        }

        #endregion Public Constructors

        #region Public Methods
        public override void NewFile(object sender)
        {
            
            SetStatus("new file not implemented");
            throw new NotImplementedException();
        }
        public override void AddTabItem(IFile<LogFileItem> logFile)
        {
          // _filterViewModel.PropertyChanged -= _filterViewModel_PropertyChanged;
          
            if (!this.TabItems.Any(x => String.Compare((string)x.Tag, logFile.Tag, true) == 0))
            {
                SetStatus("adding tab:" + logFile.Tag);
                LogTabViewModel tabItem = new LogTabViewModel();
                tabItem.Name = this.TabItems.Count.ToString();

                tabItem.ContentList = FilterTabItem(logFile);
  
                //tabItem.ContentList = ((LogFile)logFile).ContentItems;
                tabItem.Tag = logFile.Tag;
                tabItem.Header = logFile.FileName;
                TabItems.Add(tabItem);
            }

          //  _filterViewModel.PropertyChanged += _filterViewModel_PropertyChanged;
          
        }

        private void FilterActiveTabItem()
        {
          //  Debug.Assert(TabItems != null & SelectedIndex != -1);

            if (this.TabItems.Count > 0 
                && this.TabItems.Count >= SelectedIndex 
                && _logFileManager.FileManager.Exists(x => x.Tag == this.TabItems[SelectedIndex].Tag))
            {
                FilterFile filterFile = (FilterFile)_filterViewModel.ViewManager.FileManager.First(x => x.Tag == _filterViewModel.TabItems[_filterViewModel.SelectedIndex].Tag);
                List<FilterFileItem> filterFileItems = _logFileManager.CleanFilterList(filterFile);

                if (_previousIndex == SelectedIndex & _logFileManager.CompareFilterList(filterFileItems))
                {
                    return;
                }

                _previousIndex = SelectedIndex;
                LogFile activeLogFile = (LogFile)this.ViewManager.FileManager.First(x => x.Tag == this.TabItems[SelectedIndex].Tag);
                //LogFile activeLogFile = (LogFile)this.TabItems[SelectedIndex];

                this.TabItems[SelectedIndex].ContentList = FilterTabItem(activeLogFile);
            }
        }

        private ObservableCollection<LogFileItem> FilterTabItem(IFile<LogFileItem> logFile)
        {

           // FilterFile filterFile = (FilterFile)_filterViewModel.ViewManager.FileManager.First(x => x.Tag == _filterViewModel.TabItems[SelectedIndex].Tag);
            FilterFile filterFile = (FilterFile)_filterViewModel.ViewManager.FileManager.First(x => x.Tag == _filterViewModel.TabItems[_filterViewModel.SelectedIndex].Tag);
            // todo: move to filter class


            List<FilterFileItem> filterFileItems = _logFileManager.CleanFilterList(filterFile);

            //if (_previousIndex == SelectedIndex & _logFileManager.CompareFilterList(filterFileItems))
            //{
            //    return logFile.ContentItems;
            //}

            //_previousIndex = SelectedIndex;
            return (_logFileManager.ApplyFilter(logFile.ContentItems, filterFileItems));
        }

        /// <summary>
        /// Opens OpenFileDialog
        /// to test supply valid string file in argument sender
        /// </summary>
        /// <param name="sender"></param>
        public override void OpenFile(object sender)
        {
            SetStatus("opening file");
            bool test = false;
            if(sender is string && !String.IsNullOrEmpty(sender as string))
            {
                test = true;
            }

            string logName = string.Empty;
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".csv"; // Default file extension
            //dlg.Filter = "Text Files (*.txt)|*.txt|Csv Files (*.csv)|*.csv|All Files (*.*)|*.*"; // Filter files by extension
            dlg.Filter = "All Files (*.*)|*.*|Csv Files (*.csv)|*.csv|Text Files (*.txt)|*.txt"; // Filter files by extension

            Nullable<bool> result = false;
            // Show open file dialog box
            if (test)
            {
                result = true;
                logName = (sender as string);
            }
            else
            {
                result = dlg.ShowDialog();
                logName = dlg.FileName;
            }

            // Process open file dialog box results
            if (result == true && File.Exists(logName))
            {
                // Open document
                
                SetStatus(string.Format("opening file:{0}", logName));
                LogFile logFileItems = new LogFile();
                if (String.IsNullOrEmpty((logFileItems = (LogFile)this.ViewManager.OpenFile(logName)).Tag))
                {
                    return;
                }

                // make new tab
                AddTabItem(logFileItems);
            }
            else
            {

            }
        }

        #endregion Public Methods

    }
}