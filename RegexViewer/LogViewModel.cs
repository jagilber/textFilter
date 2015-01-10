﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace RegexViewer
{
    
    public class LogViewModel : BaseViewModel<LogFileItem>
    {
        #region Private Fields

        private FilterViewModel _filterViewModel;
        private Command _keyDownCommand;
        private Command _pageDownCommand;
        private Command _pageUpCommand;
        private Command _ctrlEndCommand;
        private Command _ctrlHomeCommand;
        private Command _mouseWheelCommand;
        private Command _hideCommand;
        private bool _hiding = false;
        private LogFileManager _logFileManager;
        private Command _quickFindChangedCommand;
        private string _quickFindText = string.Empty;
        private int _previousIndex;
        #endregion Private Fields

        #region Public Constructors

        public LogViewModel(FilterViewModel filterViewModel)
        {
            _filterViewModel = filterViewModel;
            this.TabItems = new ObservableCollection<ITabViewModel<LogFileItem>>();
            this.ViewManager = new LogFileManager();
            _logFileManager = (LogFileManager)this.ViewManager;

            // load tabs from last session
            foreach (LogFile logFile in this.ViewManager.OpenFiles(this.Settings.CurrentLogFiles.ToArray()))
            {
                AddTabItem(logFile);
                FilterLogTabItems(null, logFile);
            }

            // FilterActiveTabItem();

            _filterViewModel.PropertyChanged += _filterViewModel_PropertyChanged;
            this.PropertyChanged += LogViewModel_PropertyChanged;
        }

        #endregion Public Constructors

        #region Public Properties

        public Command HideCommand
        {
            get
            {
                if (_hideCommand == null)
                {
                    _hideCommand = new Command(HideExecuted);
                }
                _hideCommand.CanExecute = true;

                return _hideCommand;
            }
            set { _hideCommand = value; }
        }

     
        public Command PageDownCommand
        {
            get
            {
                if (_pageDownCommand == null)
                {
                    _pageDownCommand = new Command(PageDownExecuted);
                }
                _pageDownCommand.CanExecute = true;

                return _pageDownCommand;
            }
            set { _pageDownCommand = value; }
        }

        public Command KeyDownCommand
        {
            get
            {
                if (_keyDownCommand == null)
                {
                    _keyDownCommand = new Command(KeyDownExecuted);
                }
                _keyDownCommand.CanExecute = true;

                return _keyDownCommand;
            }
            set { _keyDownCommand = value; }
        }


        public Command CtrlHomeCommand
        {
            get
            {
                if (_ctrlHomeCommand == null)
                {
                    _ctrlHomeCommand = new Command(CtrlHomeExecuted);
                }
                _ctrlHomeCommand.CanExecute = true;

                return _ctrlHomeCommand;
            }
            set { _ctrlHomeCommand = value; }
        }

        public Command CtrlEndCommand
        {
            get
            {
                if (_ctrlEndCommand == null)
                {
                    _ctrlEndCommand = new Command(CtrlEndExecuted);
                }
                _ctrlEndCommand.CanExecute = true;

                return _ctrlEndCommand;
            }
            set { _ctrlEndCommand = value; }
        }



        public Command PageUpCommand
        {
            get
            {
                if (_pageUpCommand == null)
                {
                    _pageUpCommand = new Command(PageUpExecuted);
                }
                _pageUpCommand.CanExecute = true;

                return _pageUpCommand;
            }
            set { _pageUpCommand = value; }
        }
        public Command QuickFindChangedCommand
        {
            get
            {
                if (_quickFindChangedCommand == null)
                {
                    _quickFindChangedCommand = new Command(QuickFindChangedExecuted);
                }
                _quickFindChangedCommand.CanExecute = true;

                return _quickFindChangedCommand;
            }
            set { _quickFindChangedCommand = value; }
        }

        public string QuickFindText
        {
            get
            {
                return _quickFindText;
            }
            set
            {
                if (_quickFindText != value)
                {
                    _quickFindText = value;
                  //  OnPropertyChanged("QuickFindText");
                }
            }
        }

        #endregion Public Properties

        #region Public Methods

        public override void AddTabItem(IFile<LogFileItem> logFile)
        {
            if (!this.TabItems.Any(x => String.Compare((string)x.Tag, logFile.Tag, true) == 0))
            {
                SetStatus("adding tab:" + logFile.Tag);
                LogTabViewModel tabItem = new LogTabViewModel()
                {
                    Name = logFile.FileName,
                    Tag = logFile.Tag,
                    Header = logFile.FileName
                };

                TabItems.Add(tabItem);
            }
        }

        public void FilterLogTabItems(FilterFileItem filter = null, LogFile logFile = null, FilterCommand filterIntent = FilterCommand.Filter)
        {
            if (logFile == null)
            {
                // find logFile if it was not supplied
                logFile = (LogFile)_logFileManager.FileManager.First(x => x.Tag == this.TabItems[SelectedIndex].Tag);
            }

            List<FilterFileItem> filterFileItems = _filterViewModel.FilterList(filter);

            switch (_filterViewModel.DetermineFilterAction(filterIntent))
            {
                case FilterCommand.Current:

                    // refilter if log tab changed
                    if(_previousIndex != SelectedIndex)
                    {
                        _previousIndex = SelectedIndex;
                        goto case FilterCommand.Filter;
                    }
                    return;

                case FilterCommand.DynamicFilter:
                    //this.TabItems[this.SelectedIndex].ContentList = _logFileManager.ResetColors(logFile.ContentItems);
                    //goto case FilterCommand.Filter;

                case FilterCommand.Filter:
                    this.TabItems[this.SelectedIndex].ContentList = _logFileManager.ApplyFilter(logFile, filterFileItems, filterIntent == FilterCommand.Highlight);
                    return;

                case FilterCommand.Reset:
                    this.TabItems[this.SelectedIndex].ContentList = _logFileManager.ResetColors(logFile.ContentItems);
                    return;

                default:
                    return;
            }
        }

        public void MouseWheelExecuted(object sender, KeyEventArgs e)
        {
            SetStatus("MouseWheelExecuted");
            throw new NotImplementedException();
        }
        public void PageDownExecuted(object sender)
        {
            SetStatus("PageDownExecuted");
            throw new NotImplementedException();
        }

        public void KeyDownExecuted(object sender)
        {
            SetStatus("KeyDownExecuted");
            throw new NotImplementedException();
        }
        public void CtrlEndExecuted(object sender)
        {
            SetStatus("CtrlEndExecuted");
            throw new NotImplementedException();
        }
        public void CtrlHomeExecuted(object sender)
        {
            SetStatus("CtrlHomeExecuted");
            throw new NotImplementedException();
        }
        public void PageUpExecuted(object sender)
        {
            SetStatus("PageUpExecuted");
            throw new NotImplementedException();
        }
        public void HideExecuted(object sender)
        {
            if (!_hiding)
            {
                this.FilterLogTabItems(null, null, FilterCommand.Highlight);
            }
            else
            {
                // send empty function to reset to current filter in filterview
                
                if(!string.IsNullOrEmpty(QuickFindText))
                {
                       QuickFindChangedExecuted(null);
                    //this.FilterLogTabItems(null, null, FilterCommand.Reset);
                }
                else
                {
                    //this.FilterLogTabItems(null, null, FilterCommand.DynamicFilter);
                    this.FilterLogTabItems(null, null, FilterCommand.Reset);
                }
            }

            _hiding = !_hiding;
        }

        public override void NewFile(object sender)
        {
            SetStatus("new file not implemented");
            throw new NotImplementedException();
        }

        /// <summary>
        /// Opens OpenFileDialog to test supply valid string file in argument sender
        /// </summary>
        /// <param name="sender"></param>
        public override void OpenFile(object sender)
        {
            SetStatus("opening file");
            bool test = false;
            if (sender is string && !String.IsNullOrEmpty(sender as string))
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

        public void QuickFindChangedExecuted(object sender)
        {

            FilterFileItem fileItem = new FilterFileItem();

            if (sender is string)
            {
                string filter = (sender as string);
                if (string.IsNullOrEmpty(filter))
                {
                    // send empty function to reset to current filter in filterview
                    this.FilterLogTabItems(null, null, FilterCommand.Reset);
                    QuickFindText = string.Empty;
                    return;
                }

               
                fileItem.Filterpattern = QuickFindText = (sender as string);
            }
            else
            {
                fileItem.Filterpattern = QuickFindText;
            }

            
            
            
            try
            {
                Regex test = new Regex(fileItem.Filterpattern);
                fileItem.Regex = true;
            }
            catch
            {
                SetStatus("quick find not a regex:" + fileItem.Filterpattern);
                fileItem.Regex = false;
            }

            fileItem.Enabled = true;
            this.FilterLogTabItems(fileItem, null, FilterCommand.DynamicFilter);
               
        }

        public override void SaveFile(object sender)
        {
            SetStatus("save file not implemented");
            throw new NotImplementedException();
        }

        #endregion Public Methods

        #region Private Methods

        private void _filterViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // dont handle count updates

            SetStatus("_filterViewModel.PropertyChanged" + e.PropertyName);
            FilterLogTabItems();
        }

        private void LogViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SetStatus("LogViewModel.PropertyChanged" + e.PropertyName);
            FilterLogTabItems();
        }

        private void TabItems_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            SetStatus("_filterViewModel.CollectionChanged" + sender.ToString());
            FilterLogTabItems();
        }

        #endregion Private Methods
    }
}