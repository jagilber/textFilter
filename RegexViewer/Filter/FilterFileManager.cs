﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

//using System.Windows.Media;
using System.Xml;

namespace RegexViewer
{
    public class FilterFileManager : BaseFileManager<FilterFileItem>
    {
        #region Public Constructors

        public FilterFileManager()
        {
            this.FileManager = new List<IFile<FilterFileItem>>();
        }

        #endregion Public Constructors

        #region Public Methods

        public void ManageNewFilterFileItem(FilterFile filterFile)
        {
            // add blank new item so defaults / modifications can be set some type of bug
            IEnumerable<FilterFileItem> results = null;
            int indexMax = -1;

            SetStatus("ManageNewFilterFileItem:" + filterFile.FileName);

            results = filterFile.ContentItems.Where(x => x.Enabled == false
                    && x.Exclude == false
                    && x.Regex == false
                    && string.IsNullOrEmpty(x.Filterpattern)
                    && string.IsNullOrEmpty(x.Notes));

            if (filterFile.ContentItems.Count > 0)
            {
                indexMax = filterFile.ContentItems.Max(x => x.Index);
            }

            if (results == null | results != null && results.Count() == 0)
            {
                FilterFileItem fileItem = new FilterFileItem();

                filterFile.EnablePatternNotifications(false);
                fileItem.Index = indexMax + 1;

                filterFile.ContentItems.Add(fileItem);
                filterFile.EnablePatternNotifications(true);
            }
            else if (results.Count() == 1)
            {
                if (results.ToList()[0].Index != indexMax)
                {
                    filterFile.EnablePatternNotifications(false);
                    results.ToList()[0].Index = indexMax + 1;
                    filterFile.EnablePatternNotifications(true);
                }

                return;
            }
            else
            {
                for (int i = 0; i < results.Count() - 1; i++)
                {
                    filterFile.ContentItems.Remove(results.ToList()[i]);
                }
            }
        }

        public override IFile<FilterFileItem> NewFile(string LogName, ObservableCollection<FilterFileItem> fileItems = null)
        {
            FilterFile filterFile = new FilterFile();
            ManageNewFilterFileItem(filterFile);

            FileManager.Add(ManageFileProperties(LogName, filterFile));

            this.Settings.AddFilterFile(LogName);
            OnPropertyChanged("FilterFileManager");
            return filterFile;
        }

        public override IFile<FilterFileItem> OpenFile(string logName)
        {
            FilterFile filterFile = new FilterFile();

            try
            {
                if (FileManager.Exists(x => String.Compare(x.Tag, logName, true) == 0))
                {
                    SetStatus("file already open:" + logName);
                    return filterFile;
                }

                filterFile.ContentItems = new ObservableCollection<FilterFileItem>(ReadFile(logName));
                ManageNewFilterFileItem(filterFile);

                ManageFileProperties(logName, filterFile);
                FileManager.Add(filterFile);
                this.Settings.AddFilterFile(logName);
                OnPropertyChanged("FilterFileManager");

                return filterFile;
            }
            catch (Exception e)
            {
                SetStatus(string.Format("error opening filter file:{0}:{1}", logName, e.ToString()));
                return filterFile;
            }
        }

        public override List<IFile<FilterFileItem>> OpenFiles(string[] files)
        {
            List<IFile<FilterFileItem>> filterFileItems = new List<IFile<FilterFileItem>>();

            foreach (string file in files)
            {
                FilterFile filterFile = new FilterFile();
                if (String.IsNullOrEmpty((filterFile = (FilterFile)OpenFile(file)).Tag))
                {
                    continue;
                }
                filterFile.PropertyChanged += filterFile_PropertyChanged;
                filterFileItems.Add(filterFile);
            }

            return filterFileItems;
        }

        public override List<FilterFileItem> ReadFile(string logName)
        {
            if (Path.GetExtension(logName).ToLower().Contains("tat"))
            {
                return ReadTatFile(logName);
            }

            List<FilterFileItem> filterFileItems = new List<FilterFileItem>();
            XmlDocument doc = new XmlDocument();
            doc.Load(logName);

            XmlNode root = doc.DocumentElement;

            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                FilterFileItem fileItem = new FilterFileItem();
                fileItem.Count = 0;
                fileItem.BackgroundColor = ReadStringNodeItem(root, "backgroundcolor", i);
                fileItem.CaseSensitive = ReadBoolNodeItem(root, "casesensitive", i);
                fileItem.Enabled = ReadBoolNodeItem(root, "enabled", i);
                fileItem.Exclude = ReadBoolNodeItem(root, "exclude", i);
                fileItem.Regex = ReadBoolNodeItem(root, "regex", i);
                fileItem.Filterpattern = ReadStringNodeItem(root, "filterpattern", i);
                fileItem.ForegroundColor = ReadStringNodeItem(root, "foregroundcolor", i);
                fileItem.Index = ReadIntNodeItem(root, "index", i);
                fileItem.Notes = ReadStringNodeItem(root, "notes", i);

                filterFileItems.Add(fileItem);
            }
            return filterFileItems;
        }

        public override bool SaveFile(string FileName, ObservableCollection<FilterFileItem> fileItems)
        {
            try
            {
                if (File.Exists(FileName))
                {
                    File.Delete(FileName);
                }

                SetStatus("saving file:" + FileName);

                if (Path.GetExtension(FileName).ToLower().Contains("tat") && SaveTatFile(FileName, fileItems))
                {
                    return true;
                }

                XmlTextWriter xmlw = new XmlTextWriter(FileName, System.Text.Encoding.UTF8);
                xmlw.Formatting = Formatting.Indented;
                xmlw.WriteStartDocument();
                xmlw.WriteStartElement("filters");

                foreach (FilterFileItem item in fileItems)
                {
                    xmlw.WriteStartElement("filter");

                    xmlw.WriteStartElement("filterpattern");
                    xmlw.WriteString(item.Filterpattern);
                    xmlw.WriteEndElement();

                    xmlw.WriteStartElement("backgroundcolor");
                    xmlw.WriteString(item.BackgroundColor);
                    xmlw.WriteEndElement();

                    xmlw.WriteStartElement("foregroundcolor");
                    xmlw.WriteString(item.ForegroundColor);
                    xmlw.WriteEndElement();

                    xmlw.WriteStartElement("casesensitive");
                    xmlw.WriteString(item.CaseSensitive.ToString());
                    xmlw.WriteEndElement();

                    xmlw.WriteStartElement("index");
                    xmlw.WriteString(item.Index.ToString());
                    xmlw.WriteEndElement();

                    xmlw.WriteStartElement("enabled");
                    xmlw.WriteString(item.Enabled.ToString());
                    xmlw.WriteEndElement();

                    xmlw.WriteStartElement("exclude");
                    xmlw.WriteString(item.Exclude.ToString());
                    xmlw.WriteEndElement();

                    xmlw.WriteStartElement("regex");
                    xmlw.WriteString(item.Regex.ToString());
                    xmlw.WriteEndElement();

                    xmlw.WriteStartElement("notes");
                    xmlw.WriteString(item.Notes.ToString());
                    xmlw.WriteEndElement();

                    xmlw.WriteEndElement();
                }

                xmlw.WriteEndElement();
                xmlw.WriteEndDocument();

                xmlw.Close();

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

        private void filterFile_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == FilterFileItemEvents.Count |
                e.PropertyName == FilterFileItemEvents.Notes)
            {
                // dont forward count updates
                return;
            }

            OnPropertyChanged(sender, e);
        }

        /// <summary>
        /// Finds Color Name from string of RGB #(FFFFFF) returns KnownColor color name
        /// </summary>
        /// <param name="rgbColor"></param>
        /// <returns></returns>
        private string FindColorName(string rgbColor)
        {
            SetStatus("FindColorName:" + rgbColor);

            if (!Regex.IsMatch(rgbColor, "[0-9A-Fa-f]{6}"))
            {
                SetStatus("FindColorName: invalid. returning black");
                return "black";
            }

            System.Array colorsArray = Enum.GetValues(typeof(KnownColor));
            KnownColor[] allColors = new KnownColor[colorsArray.Length];
            Array.Copy(colorsArray, allColors, colorsArray.Length);

            Color newColor = System.Drawing.Color.FromArgb(0xff,
                                Convert.ToByte(rgbColor.Substring(0, 2), 16),
                                Convert.ToByte(rgbColor.Substring(2, 2), 16),
                                Convert.ToByte(rgbColor.Substring(4, 2), 16));

            foreach (KnownColor color in allColors)
            {
                Color tempColor = Color.FromKnownColor(color);
                if (!tempColor.IsSystemColor
                    && tempColor.R == newColor.R
                    && tempColor.G == newColor.G
                    && tempColor.B == newColor.B)
                {
                    SetStatus("FindColorName return:" + color.ToString());
                    return color.ToString();
                }
            }

            return "white";
        }

        private FilterFile ManageFileProperties(string LogName, FilterFile filterFile)
        {
            filterFile.FileName = Path.GetFileName(LogName);
            filterFile.Tag = LogName;

            // todo rework this:
            filterFile.EnablePatternNotifications(false);
            filterFile.EnablePatternNotifications(true);
            filterFile.PropertyChanged += filterFile_PropertyChanged;
            return filterFile;
        }

        private bool ReadAttributeBool(XmlNode node, string attName, int item)
        {
            try
            {
                // for tat files
                string val = (node.ChildNodes.Item(item).Attributes[attName].Value.ToString());
                if (val.ToLower() == "y" | val.ToLower() == "true")
                {
                    return true;
                }
                else if (val.ToLower() == "n" | val.ToLower() == "false")
                {
                    return false;
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        private string ReadAttributeString(XmlNode node, string attName, int item)
        {
            try
            {
                return (node.ChildNodes.Item(item).Attributes[attName].Value.ToString());
            }
            catch
            {
                return string.Empty;
            }
        }

        private bool ReadBoolNodeItem(XmlNode node, string nodeName, int item)
        {
            try
            {
                return Convert.ToBoolean((node.ChildNodes.Item(item).SelectSingleNode(nodeName)).InnerXml);
            }
            catch
            {
                return false;
            }
        }

        private int ReadIntNodeItem(XmlNode node, string nodeName, int item)
        {
            try
            {
                return Convert.ToInt32((node.ChildNodes.Item(item).SelectSingleNode(nodeName)).InnerXml);
            }
            catch
            {
                return 0;
            }
        }

        private string ReadStringNodeItem(XmlNode node, string nodeName, int item)
        {
            try
            {
                return (node.ChildNodes.Item(item).SelectSingleNode(nodeName)).InnerText;
            }
            catch
            {
                return string.Empty;
            }
        }

        private List<FilterFileItem> ReadTatFile(string logName)
        {
            List<FilterFileItem> filterFileItems = new List<FilterFileItem>();
            XmlDocument doc = new XmlDocument();
            doc.Load(logName);

            XmlNode root = doc.DocumentElement.ChildNodes[0];

            for (int i = 0; i < root.ChildNodes.Count; i++)
            {
                FilterFileItem fileItem = new FilterFileItem();
                fileItem.Count = 0;

                fileItem.BackgroundColor = FindColorName(ReadAttributeString(root, "backColor", i));
                fileItem.ForegroundColor = FindColorName(ReadAttributeString(root, "foreColor", i));

                fileItem.CaseSensitive = ReadAttributeBool(root, "case_sensitive", i);
                fileItem.Enabled = ReadAttributeBool(root, "enabled", i);
                fileItem.Exclude = ReadAttributeBool(root, "excluding", i);
                fileItem.Regex = ReadAttributeBool(root, "regex", i);
                fileItem.Filterpattern = ReadAttributeString(root, "text", i);
                fileItem.TatType = ReadAttributeString(root, "type", i);

                fileItem.Index = i; // ReadIntNodeItem(root, "index", i);
                //fileItem.Notes = ReadStringNodeItem(root, "notes", i);

                filterFileItems.Add(fileItem);
            }
            return filterFileItems;
        }

        private bool SaveTatFile(string FileName, ObservableCollection<FilterFileItem> fileItems)
        {
            try
            {
                XmlTextWriter xmlw = new XmlTextWriter(FileName, System.Text.Encoding.UTF8);
                xmlw.Formatting = Formatting.Indented;
                xmlw.WriteStartDocument();

                xmlw.WriteStartElement("TextAnalysisTool.NET");
                xmlw.WriteAttributeString("version", "2015-01-28");
                xmlw.WriteAttributeString("showOnlyFilteredLines", "False");

                xmlw.WriteStartElement("filters");

                foreach (FilterFileItem item in fileItems.OrderBy(x => x.Index))
                {
                    xmlw.WriteStartElement("filter");

                    xmlw.WriteAttributeString("enabled", item.Enabled ? "y" : "n");
                    xmlw.WriteAttributeString("excluding", item.Exclude ? "y" : "n");

                    string fColor = item.Foreground.ToString();
                    fColor = fColor.Substring(fColor.Length - 6);

                    string bColor = item.Background.ToString();
                    bColor = bColor.Substring(bColor.Length - 6);

                    xmlw.WriteAttributeString("foreColor", fColor);
                    xmlw.WriteAttributeString("backColor", bColor);
                    // tat may not support this setting xmlw.WriteAttributeString("notes", item.Notes.ToString());

                    // dont currently support marker and is not saved in regexviewer filter file
                    xmlw.WriteAttributeString("type", item.TatType ?? "matches_text");

                    // dont currently support 'y' in gui
                    xmlw.WriteAttributeString("case_sensitive", item.CaseSensitive ? "y" : "n");
                    xmlw.WriteAttributeString("regex", item.Regex ? "y" : "n");
                    xmlw.WriteAttributeString("text", item.Filterpattern);

                    xmlw.WriteEndElement();
                }

                xmlw.WriteEndElement();
                xmlw.WriteEndDocument();

                xmlw.Close();
                return true;
            }
            catch (Exception e)
            {
                SetStatus("SaveAsTat exception:" + e.ToString());
                return false;
            }
        }

        #endregion Private Methods
    }
}