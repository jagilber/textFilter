﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RegexViewer
{
    public class LogFileManager : BaseFileManager<LogFileItem>
    {
        #region Public Constructors

        public LogFileManager()
        {
            this.FileManager = new List<IFile<LogFileItem>>();
        }

        #endregion Public Constructors

        #region Public Methods

        public ObservableCollection<LogFileItem> ApplyColor(ObservableCollection<LogFileItem> logFileItems, List<FilterFileItem> filterFileItems, bool showAll = false)
        {
            DateTime timer = DateTime.Now;
            SetStatus(string.Format("ApplyColor:start time: {0}", timer.ToString("hh:mm:ss.fffffff")));

            List<FilterFileItem> filterItems = VerifyFilterPatterns(filterFileItems);

            try
            {
                foreach (LogFileItem item in logFileItems)

                //Parallel.ForEach(logFileItems,
                //        () => 0,
                //        ()
                //        item =>
                {
                    if (item.FilterIndex < 0 | item.FilterIndex >= filterItems.Count)
                    {
                        item.Background = Settings.BackgroundColor;
                        item.Foreground = Settings.ForegroundColor;
                    }
                    else
                    {
                        item.Foreground = filterItems[item.FilterIndex].Foreground;
                        item.Background = filterItems[item.FilterIndex].Background;
                    }

                    item.FontSize = Settings.FontSize;
                }//);

                SetStatus(string.Format("ApplyColor:total time in seconds: {0}", DateTime.Now.Subtract(timer).TotalSeconds));

                if (showAll)
                {
                    return logFileItems;
                }
                else
                {
                    return new ObservableCollection<LogFileItem>(logFileItems.Where(x => x.FilterIndex > -1));
                }
            }
            catch (Exception e)
            {
                SetStatus("ApplyColor:exception" + e.ToString());

                return new ObservableCollection<LogFileItem>();
            }
        }

        public ObservableCollection<LogFileItem> ApplyFilter(LogTabViewModel logTab, LogFile logFile, List<FilterFileItem> filterFileItems, FilterCommand filterCommand)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            DateTime timer = DateTime.Now;
            SetStatus(string.Format("ApplyFilter:start time: {0} log file: {1} ", timer.ToString("hh:mm:ss.fffffff"), logFile.Tag));

            List<FilterFileItem> filterItems = VerifyFilterPatterns(filterFileItems, logTab);
            Debug.Print(string.Format("ApplyFilter: filterItems.Count={0}:{1}", Thread.CurrentThread.ManagedThreadId, filterItems.Count));

            try
            {
                Parallel.ForEach(logFile.ContentItems, logItem =>
                {
                    if (string.IsNullOrEmpty(logItem.Content))
                    {
                        Debug.Print(string.Format("ApplyFilter: logItem.Content empty={0}:{1}", Thread.CurrentThread.ManagedThreadId, logItem.Content));
                        // used for goto line as it needs all line items
                        logItem.FilterIndex = int.MinValue;
                        return;
                    }

                    int filterIndex = int.MaxValue; // int.MinValue;

                    if (Settings.CountMaskedMatches)
                    {
                        logItem.Masked = new int[filterItems.Count, 1];
                    }

                    // clear out groups
                    logItem.Group1 = string.Empty;
                    logItem.Group2 = string.Empty;
                    logItem.Group3 = string.Empty;
                    logItem.Group4 = string.Empty;

                    // make sure all matches have all includes
                    foreach (FilterFileItem item in filterItems.Where(x => x.Include == true))
                    {
                        if (item.Regex)
                        {
                            if (!Regex.IsMatch(logItem.Content, item.Filterpattern, item.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase))
                            {
                                logItem.FilterIndex = int.MinValue;
                                return;
                            }
                        }
                        else
                        {
                            if (!logItem.Content.ToLower().Contains(item.Filterpattern.ToLower()))
                            {
                                logItem.FilterIndex = int.MinValue;
                                return;
                            }
                        }
                    }

                    bool matchSet = false;

                    for (int filterItemCount = 0; filterItemCount < filterItems.Count; filterItemCount++)
                    {
                        bool match = false;
                        FilterFileItem filterItem = filterItems[filterItemCount];
                        Debug.Print(string.Format("ApplyFilter: loop:{0} filterItem.Pattern={1}:{2} logItem.Content:{3}", filterItemCount,
                            Thread.CurrentThread.ManagedThreadId, filterItem.Filterpattern, logItem.Content));

                        // unnamed and named groups
                        if (logTab.GroupCount > 0 && filterItem.Regex)
                        {
                            MatchCollection mc = Regex.Matches(logItem.Content, filterItem.Filterpattern, filterItem.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                            if (mc.Count > 0)
                            {
                                match = true;

                                foreach (Match m in mc)
                                {
                                    if (!string.IsNullOrEmpty(m.Groups[1].Value.ToString()))
                                    {
                                        logItem.Group1 += (string.IsNullOrEmpty(logItem.Group1) ? "" : ";\n") + m.Groups[1].Value.ToString();
                                    }

                                    if (!string.IsNullOrEmpty(m.Groups[2].Value.ToString()))
                                    {
                                        logItem.Group2 += (string.IsNullOrEmpty(logItem.Group2) ? "" : ";\n") + m.Groups[2].Value.ToString();
                                    }

                                    if (!string.IsNullOrEmpty(m.Groups[3].Value.ToString()))
                                    {
                                        logItem.Group3 += (string.IsNullOrEmpty(logItem.Group3) ? "" : ";\n") + m.Groups[3].Value.ToString();
                                    }

                                    if (!string.IsNullOrEmpty(m.Groups[4].Value.ToString()))
                                    {
                                        logItem.Group4 += (string.IsNullOrEmpty(logItem.Group4) ? "" : ";\n") + m.Groups[4].Value.ToString();
                                    }
                                }
                            }
                        }
                        else if (filterItem.Regex && Regex.IsMatch(logItem.Content, filterItem.Filterpattern, filterItem.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase))
                        {
                            match = true;
                        }
                        else if (!filterItem.Regex)
                        {
                            if (filterItem.CaseSensitive && logItem.Content.Contains(filterItem.Filterpattern))
                            {
                                match = true;
                            }
                            else if (logItem.Content.ToLower().Contains(filterItem.Filterpattern.ToLower()))
                            {
                                match = true;
                            }
                        }

                        Debug.Print(string.Format("ApplyFilter:** loop:{0} filterItem Match={1}:{2} **", filterItemCount, Thread.CurrentThread.ManagedThreadId, match));

                        if (!matchSet)
                        {
                            if (match && filterItem.Exclude)
                            {
                                filterIndex = (filterItemCount * -1) - 1;
                                Debug.Print(string.Format("ApplyFilter: loop:{0} filterItem.Exclusion and match filterIndex={1}:{2}", filterItemCount,
                                    Thread.CurrentThread.ManagedThreadId, filterIndex));

                                matchSet = true;
                                // break;
                            }
                            else if (!match && !filterItem.Exclude)
                            {
                                filterIndex = int.MinValue;
                                Debug.Print(string.Format("ApplyFilter: loop:{0} not filterItem.Exclusion and not match filterIndex={1}:{2}",
                                    filterItemCount, Thread.CurrentThread.ManagedThreadId, filterIndex));
                            }
                            else if (match)
                            {
                                filterIndex = filterItemCount;
                                Debug.Print(string.Format("ApplyFilter: loop:{0} setting filterIndex={1}:{2}", filterItemCount,
                                    Thread.CurrentThread.ManagedThreadId, filterIndex));
                                matchSet = true;
                                // break;
                            }
                        }
                        else if (matchSet && match && Settings.CountMaskedMatches)
                        {
                            logItem.Masked[filterItemCount, 0] = 1;
                            Debug.Print(string.Format("ApplyFilter: loop:{0} masked match filterIndex={1}:{2}", filterItemCount,
                                Thread.CurrentThread.ManagedThreadId, filterItemCount));
                        }

                        if (matchSet && !Settings.CountMaskedMatches)
                        {
                            Debug.Print(string.Format("ApplyFilter: loop:{0} not filterItem.Exclude CountMaskedMatches={1}:{2}", filterItemCount,
                                Thread.CurrentThread.ManagedThreadId, Settings.CountMaskedMatches));
                            break;
                        }
                    }

                    Debug.Print(string.Format("ApplyFilter: loop finished set filterIndex={0}:{1}", Thread.CurrentThread.ManagedThreadId, filterIndex));
                    logItem.FilterIndex = filterIndex;
                });

                // write totals negative indexes arent displayed and are only used for counting
                int filterCount = 0;
                for (int i = 0; i < filterFileItems.Count; i++)
                {
                    filterFileItems[i].Count = logFile.ContentItems.Count(x => x.FilterIndex == i | x.FilterIndex == (i * -1) - 1);

                    if (Settings.CountMaskedMatches)
                    {
                        filterFileItems[i].MaskedCount = logFile.ContentItems.Count(x => (x.FilterIndex != int.MaxValue) & (x.FilterIndex != int.MinValue) && x.Masked[i, 0] == 1);
                        SetStatus(string.Format("ApplyFilter:filterItem masked counttotal: {0}", filterFileItems[i].MaskedCount));
                    }

                    SetStatus(string.Format("ApplyFilter:filterItem counttotal: {0}", filterFileItems[i].Count));

                    filterCount += filterFileItems[i].Count;
                }

                SetStatus(string.Format("ApplyFilter:total time in seconds: {0} logfile total count: {1} logfile filter count: {2} log file: {3}", DateTime.Now.Subtract(timer).TotalSeconds, logFile.ContentItems.Count, filterCount, logFile.Tag));
                Mouse.OverrideCursor = null;
                return new ObservableCollection<LogFileItem>(logFile.ContentItems.Where(x => x.FilterIndex > -1));
            }
            catch (Exception e)
            {
                SetStatus("ApplyFilter:exception" + e.ToString());
                Mouse.OverrideCursor = null;
                return new ObservableCollection<LogFileItem>();
            }
        }

        public override IFile<LogFileItem> NewFile(string LogName, ObservableCollection<LogFileItem> logFileItems = null)
        {
            LogFile logFile = new LogFile();
            if (logFileItems != null)
            {
                logFile.ContentItems = logFileItems;
            }

            FileManager.Add(ManageFileProperties(LogName, logFile));
            logFile.Modified = true;
            this.Settings.AddLogFile(LogName);
            OnPropertyChanged("LogFileManager");
            return logFile;
        }

        public override IFile<LogFileItem> OpenFile(string LogName)
        {
            IFile<LogFileItem> logFile = new LogFile();
            if (FileManager.Exists(x => String.Compare(x.Tag, LogName, true) == 0))
            {
                SetStatus("file already open:" + LogName);
                return logFile;
            }

            if (File.Exists(LogName))
            {
                logFile.FileName = Path.GetFileName(LogName);
                logFile.Tag = LogName;
                logFile.ContentItems = ((LogFile)ReadFile(LogName)).ContentItems;
                FileManager.Add(logFile);
                this.Settings.AddLogFile(LogName);
            }
            else
            {
                SetStatus("log file does not exist:" + LogName);
                this.Settings.RemoveLogFile(LogName);
            }

            return logFile;
        }

        public override List<IFile<LogFileItem>> OpenFiles(string[] files)
        {
            List<IFile<LogFileItem>> textBlockItems = new List<IFile<LogFileItem>>();

            foreach (string file in files)
            {
                LogFile logFile = new LogFile();
                if (String.IsNullOrEmpty((logFile = (LogFile)OpenFile(file)).Tag))
                {
                    continue;
                }

                textBlockItems.Add(logFile);
            }

            return textBlockItems;
        }

        public override IFile<LogFileItem> ReadFile(string fileName)
        {
            // BOM UTF - 8 0xEF,0xBB,0xBF BOM UTF - 16 FE FF NO BOM assume ansi but utf-8 doesnt
            // have to have one either
            Encoding encoding = Encoding.Default;
            LogFile logFile = new LogFile();
            logFile.FileName = Path.GetFileName(fileName);
            // find bom
            using (System.IO.StreamReader sr = new System.IO.StreamReader(fileName, true))
            {
                encoding = sr.CurrentEncoding;

                SetStatus("current encoding:" + encoding.EncodingName);

                while (!sr.EndOfStream)
                {
                    // if bom not supplied, try to determine utf-16 (unicode)
                    string line = sr.ReadLine();
                    byte[] bytes = Encoding.UTF8.GetBytes(line);
                    string newLine = Encoding.UTF8.GetString(bytes).Replace("\0", "");
                    SetStatus(string.Format("check encoding: bytes:{0} string: {1}", bytes.Length, newLine.Length));

                    if (bytes.Length > 0 && newLine.Length > 0
                        && ((bytes.Length - newLine.Length) * 2 - 1 == bytes.Length
                            | (bytes.Length - newLine.Length) * 2 == bytes.Length))
                    {
                        SetStatus(string.Format("new encoding:Unicode bytes:{0} string: {1}", bytes.Length, newLine.Length));

                        encoding = Encoding.Unicode;
                        break;
                    }
                    else if (bytes.Length > 0 && newLine.Length > 0)
                    {
                        break;
                    }
                }
            }

            // todo: use mapped file only for large files?
            List<LogFileItem> logFileItems = new List<LogFileItem>();

            using (System.IO.StreamReader sr = new System.IO.StreamReader(fileName, encoding))
            {
                string line;
                int count = 0;
                // http: //cc.davelozinski.com/c-sharp/the-fastest-way-to-read-and-process-text-files
                while ((line = sr.ReadLine()) != null)
                {
                    LogFileItem logFileItem = new LogFileItem();
                    //logFileItem.Content = line;
                    logFileItem.Content = line;
                    logFileItem.Background = Settings.BackgroundColor;
                    logFileItem.Foreground = Settings.ForegroundColor;
                    logFileItem.FontSize = Settings.FontSize;
                    logFileItem.FontFamily = new System.Windows.Media.FontFamily(Settings.FontName);
                    logFileItem.Index = count++;
                    logFileItems.Add(logFileItem);
                }

                sr.Close();
            }

            logFile.ContentItems = new ObservableCollection<LogFileItem>(logFileItems);

            return logFile;
        }

        public override bool SaveFile(string FileName, IFile<LogFileItem> file)
        {
            try
            {
                LogFile logFile = (LogFile)file;

                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }

                using (StreamWriter writer = File.CreateText(FileName))
                {
                    foreach (LogFileItem item in logFile.ContentItems)
                    {
                        writer.WriteLine(item.Content);
                    }

                    writer.Close();
                }

                SetStatus("saving file:" + FileName);
                return true;
            }
            catch (Exception e)
            {
                SetStatus("SaveFile:exception: " + e.ToString());
                return false;
            }
        }

        #endregion Public Methods

        #region Private Methods

        private LogFile ManageFileProperties(string LogName, LogFile filterFile)
        {
            filterFile.FileName = Path.GetFileName(LogName);
            filterFile.Tag = LogName;

            return filterFile;
        }

        private List<FilterFileItem> VerifyFilterPatterns(List<FilterFileItem> filterFileItems, LogTabViewModel logTab = null)
        {
            bool getGroups = false;
            int groupCount = 0;
            List<string> groupNames = new List<string>();

            if (logTab != null)
            {
                getGroups = true;
            }

            List<FilterFileItem> filterItems = new List<FilterFileItem>();

            foreach (FilterFileItem filterItem in filterFileItems)
            {
                if (string.IsNullOrEmpty(filterItem.Filterpattern))
                {
                    continue;
                }

                FilterFileItem newFilter = new FilterFileItem()
                {
                    Background = filterItem.Background,
                    Enabled = filterItem.Enabled,
                    Exclude = filterItem.Exclude,
                    Filterpattern = filterItem.Filterpattern,
                    Foreground = filterItem.Foreground,
                    Include = filterItem.Include,
                    Regex = filterItem.Regex
                };

                if (newFilter.Regex)
                {
                    try
                    {
                        Regex test = new Regex(filterItem.Filterpattern);
                        if (getGroups)
                        {
                            // unnamed groups
                            newFilter.GroupCount = test.GetGroupNumbers().Length - 1;
                            groupCount = Math.Max(groupCount, newFilter.GroupCount);

                            // named groups group1 group2 group3 group4 and compare to groupCount which is unnamed
                            //foreach (string name in test.GetGroupNames().Where(x => x.ToLower().StartsWith("group")))
                            //{
                            //    if (!groupNames.Contains(name))
                            //    {
                            //        groupNames.Add(name.ToLower());
                            //    }
                            //}

                            
                        }
                    }
                    catch
                    {
                        SetStatus("not a regex:" + filterItem.Filterpattern);
                        newFilter.Regex = false;
                        newFilter.Filterpattern = Regex.Escape(filterItem.Filterpattern);
                    }
                }

                filterItems.Add(newFilter);
            }

            if (getGroups)
            {
                logTab.SetGroupCount(Math.Max(groupCount,groupNames.Count));
            }

            return filterItems;
        }

        #endregion Private Methods
    }
}