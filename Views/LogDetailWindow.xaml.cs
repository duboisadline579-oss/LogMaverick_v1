using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
                LogType.Warn => Brushes.Yellow,
                LogType.Info => Brushes.SkyBlue,
                _ => Brushes.LightGreen
            };
            TxtRaw.Text = log.Message;
            ParseAndDisplay(log.Message);
        }
        private void ParseAndDisplay(string message) {
            _allItems.Clear();
            string trimmed = message.Trim();
            int jsonStart = FindJsonStart(trimmed);
            int xmlStart = FindXmlStart(trimmed);
            try {
                if (jsonStart >= 0 && (xmlStart < 0 || jsonStart <= xmlStart)) {
                    if (jsonStart > 0) _allItems.Add(new KvItem { Key = "prefix", Value = trimmed.Substring(0, jsonStart).Trim(), KeyColor = "#888888" });
                    var token = JToken.Parse(trimmed.Substring(jsonStart));
                    if (token is JObject obj) ParseJsonObject(obj, "");
                    else if (token is JArray arr) _allItems.Add(new KvItem { Key = "array", Value = arr.ToString(Newtonsoft.Json.Formatting.Indented) });
                } else if (xmlStart >= 0) {
                    if (xmlStart > 0) _allItems.Add(new KvItem { Key = "prefix", Value = trimmed.Substring(0, xmlStart).Trim(), KeyColor = "#888888" });
                    string xmlPart = trimmed.Substring(xmlStart);
                    string pretty = PrettyXml(xmlPart);
                    _allItems.Add(new KvItem { Key = "📄 XML", Value = pretty ?? xmlPart, KeyColor = "#FFD700", ValueColor = "#AADDFF" });
                    if (pretty != null) { var doc = new XmlDocument(); doc.LoadXml(xmlPart); ParseXmlFlat(doc.DocumentElement, ""); }
                } else { ParseRaw(trimmed); }
            } catch { ParseRaw(trimmed); }
            KvList.ItemsSource = _allItems;
            TxtMatchCount.Text = $"{_allItems.Count}개";
        }
        private int FindJsonStart(string s) { for (int i = 0; i < s.Length; i++) if (s[i] == '{' || s[i] == '[') return i; return -1; }
        private int FindXmlStart(string s) { int idx = s.IndexOf('<'); if (idx < 0) return -1; if (idx+1 < s.Length && (s[idx+1]=='?' || s[idx+1]=='!' || char.IsLetter(s[idx+1]))) return idx; return -1; }
        private string PrettyXml(string xml) {
            try {
                var doc = new XmlDocument(); doc.LoadXml(xml);
                var sb = new StringBuilder();
                var ws = new XmlWriterSettings { Indent = true, IndentChars = "  ", NewLineChars = "\n", OmitXmlDeclaration = true, NewLineOnAttributes = false };
                using (var w = XmlWriter.Create(sb, ws)) doc.Save(w);
                return sb.ToString();
            } catch { return null; }
        }
        private void ParseJsonObject(JObject obj, string prefix) {
            foreach (var p in obj.Properties()) {
                string key = string.IsNullOrEmpty(prefix) ? p.Name : $"{prefix}.{p.Name}";
                string kc = p.Name.ToLower()=="tid" ? "#FFD700" : p.Name.ToLower().Contains("error") ? "#FF6B6B" : "#00BFFF";
                if (p.Value is JObject nested) ParseJsonObject(nested, key);
                else if (p.Value is JArray arr) {
                    for (int i = 0; i < arr.Count; i++) {
                        if (arr[i] is JObject ao) ParseJsonObject(ao, $"{key}[{i}]");
                        else _allItems.Add(new KvItem { Key = $"{key}[{i}]", Value = arr[i].ToString(), KeyColor = kc });
                    }
                } else {
                    string val = p.Value.ToString();
                    string vc = val.ToUpper().Contains("ERROR")||val.ToUpper().Contains("FAIL") ? "#FF6B6B" : val.ToUpper().Contains("OK")||val.ToUpper().Contains("SUCCESS") ? "#00C853" : "#DDDDDD";
                    _allItems.Add(new KvItem { Key = key, Value = val, KeyColor = kc, ValueColor = vc });
                }
            }
        }
        private void ParseXmlFlat(XmlNode node, string prefix) {
            if (node == null) return;
            string key = string.IsNullOrEmpty(prefix) ? node.Name : $"{prefix}.{node.Name}";
            if (node.Attributes != null)
                foreach (XmlAttribute a in node.Attributes)
                    _allItems.Add(new KvItem { Key = $"{key}@{a.Name}", Value = a.Value, KeyColor = "#FFD700" });
            if (node.HasChildNodes && node.ChildNodes.Count == 1 && node.FirstChild is XmlText)
                _allItems.Add(new KvItem { Key = key, Value = node.InnerText.Trim(), KeyColor = "#00BFFF" });
            else
                foreach (XmlNode c in node.ChildNodes)
                    if (c.NodeType == XmlNodeType.Element) ParseXmlFlat(c, key);
        }
        private void ParseRaw(string text) {
            var lines = text.Split(new[] { '\n', '|' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines) {
                var kv = line.Split(new[] { '=' }, 2);
                if (kv.Length == 2) _allItems.Add(new KvItem { Key = kv[0].Trim(), Value = kv[1].Trim() });
                else _allItems.Add(new KvItem { Key = "raw", Value = line.Trim() });
            }
        }
        private void Search_Changed(object sender, TextChangedEventArgs e) {
            string q = TxtSearch.Text.Trim();
            SearchHint.Visibility = string.IsNullOrEmpty(q) ? Visibility.Visible : Visibility.Collapsed;
            if (string.IsNullOrEmpty(q)) {
                foreach (var i in _allItems) i.ValueColor = "#DDDDDD";
                KvList.ItemsSource = null; KvList.ItemsSource = _allItems;
                TxtMatchCount.Text = $"{_allItems.Count}개"; return;
            }
            var matched = _allItems.Where(i => i.Key.Contains(q, StringComparison.OrdinalIgnoreCase) || i.Value.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            foreach (var i in matched) i.ValueColor = "#FFD700";
            KvList.ItemsSource = null; KvList.ItemsSource = matched;
            TxtMatchCount.Text = $"{matched.Count}개 매칭";
            if (matched.Any()) KvList.ScrollIntoView(matched.First());
        }
        private void Window_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.C && Keyboard.Modifiers == ModifierKeys.Control) {
                if (KvList.SelectedItems.Count > 0) {
                    var sb = new StringBuilder();
                    foreach (KvItem item in KvList.SelectedItems) sb.AppendLine($"{item.Key}\t{item.Value}");
                    Clipboard.SetText(sb.ToString());
                } else { Clipboard.SetText(_log.Message); }
            }
        }
        private void Copy_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(_log.Message);
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
