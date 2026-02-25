using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using Newtonsoft.Json.Linq;
using LogMaverick.Models;

namespace LogMaverick.Views {
    public class KvItem {
        public string Key { get; set; }
        public string Value { get; set; }
        public string KeyColor { get; set; } = "#00BFFF";
        public string ValueColor { get; set; } = "#DDDDDD";
    }
    public partial class LogDetailWindow : Window {
        private LogEntry _log;
        private List<KvItem> _allItems = new();
        public LogDetailWindow(LogEntry log) {
            InitializeComponent();
            _log = log;
            TxtHeader.Text = $"[{log.Time:yyyy-MM-dd HH:mm:ss}] TID:{log.Tid} CAT:{log.Category}";
            TxtType.Text = $"TYPE: {log.Type}";
            TxtType.Foreground = log.Type switch {
                LogType.Error => Brushes.OrangeRed,
                LogType.Exception => Brushes.Violet,
                LogType.Critical => Brushes.Red,
                _ => Brushes.LightGreen
            };
            ParseAndDisplay(log.Message);
        }
        private void ParseAndDisplay(string message) {
            _allItems.Clear();
            string trimmed = message.Trim();
            try {
                if (trimmed.StartsWith("{") || trimmed.StartsWith("[")) {
                    var token = JToken.Parse(trimmed);
                    if (token is JObject obj)
                        foreach (var p in obj.Properties()) {
                            string kColor = p.Name.ToLower() == "tid" ? "#FFD700" :
                                p.Name.ToLower().Contains("error") ? "#FF6B6B" : "#00BFFF";
                            string val = p.Value.Type == JTokenType.Object || p.Value.Type == JTokenType.Array
                                ? p.Value.ToString(Newtonsoft.Json.Formatting.Indented) : p.Value.ToString();
                            string vColor = val.ToUpper().Contains("ERROR") ? "#FF6B6B" :
                                val.ToUpper().Contains("OK") || val.ToUpper().Contains("SUCCESS") ? "#00C853" : "#DDDDDD";
                            _allItems.Add(new KvItem { Key = p.Name, Value = val, KeyColor = kColor, ValueColor = vColor });
                        }
                    else _allItems.Add(new KvItem { Key = "value", Value = token.ToString() });
                } else if (trimmed.StartsWith("<")) {
                    var doc = new XmlDocument();
                    doc.LoadXml(trimmed);
                    FlattenXml(doc.DocumentElement, "");
                } else {
                    foreach (var part in trimmed.Split(new[] { ' ', '|', ',' }, StringSplitOptions.RemoveEmptyEntries)) {
                        var kv = part.Split('=');
                        if (kv.Length == 2) _allItems.Add(new KvItem { Key = kv[0].Trim(), Value = kv[1].Trim() });
                        else _allItems.Add(new KvItem { Key = "raw", Value = part.Trim() });
                    }
                }
            } catch {
                _allItems.Add(new KvItem { Key = "raw", Value = trimmed, KeyColor = "#888888" });
            }
            KvList.ItemsSource = _allItems;
            TxtMatchCount.Text = $"{_allItems.Count}개";
        }
        private void FlattenXml(XmlNode node, string prefix) {
            if (node == null) return;
            string key = string.IsNullOrEmpty(prefix) ? node.Name : $"{prefix}.{node.Name}";
            if (node.HasChildNodes && node.FirstChild is XmlText)
                _allItems.Add(new KvItem { Key = key, Value = node.InnerText.Trim(), KeyColor = "#00BFFF" });
            else if (!node.HasChildNodes)
                _allItems.Add(new KvItem { Key = key, Value = "", KeyColor = "#888888" });
            if (node.Attributes != null)
                foreach (XmlAttribute attr in node.Attributes)
                    _allItems.Add(new KvItem { Key = $"{key}@{attr.Name}", Value = attr.Value, KeyColor = "#FFD700" });
            foreach (XmlNode child in node.ChildNodes)
                if (child.NodeType == XmlNodeType.Element) FlattenXml(child, key);
        }
        private void Search_Changed(object sender, TextChangedEventArgs e) {
            string q = TxtSearch.Text.Trim();
            SearchHint.Visibility = string.IsNullOrEmpty(q) ? Visibility.Visible : Visibility.Collapsed;
            if (string.IsNullOrEmpty(q)) {
                KvList.ItemsSource = null; KvList.ItemsSource = _allItems;
                TxtMatchCount.Text = $"{_allItems.Count}개"; return;
            }
            var matched = _allItems.Where(i =>
                i.Key.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                i.Value.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var i in matched) i.ValueColor = "#FFD700";
            KvList.ItemsSource = null; KvList.ItemsSource = matched;
            TxtMatchCount.Text = $"{matched.Count}개 매칭";
            if (matched.Any()) KvList.ScrollIntoView(matched.First());
        }
        private void Copy_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(_log.Message);
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
