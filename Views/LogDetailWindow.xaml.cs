using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using LogMaverick.Models;

namespace LogMaverick.Views {
    public partial class LogDetailWindow : Window {
        private LogEntry _log;
        public LogDetailWindow(LogEntry log) {
            InitializeComponent();
            _log = log;
            TxtHeader.Text = $"[{log.Time:yyyy-MM-dd HH:mm:ss}]  TID: {log.Tid}  CAT: {log.Category}";
            TxtType.Text = $"TYPE: {log.Type}";
            TxtType.Foreground = log.Type switch {
                LogType.Error => System.Windows.Media.Brushes.OrangeRed,
                LogType.Exception => System.Windows.Media.Brushes.Violet,
                LogType.Critical => System.Windows.Media.Brushes.Red,
                _ => System.Windows.Media.Brushes.LightGreen
            };
            TxtRaw.Text = log.Message;
            ParseAndDisplay(log.Message);
        }
        private void ParseAndDisplay(string message) {
            // JSON 파싱 시도
            try {
                var token = JToken.Parse(message);
                TxtPretty.Text = token.ToString(Newtonsoft.Json.Formatting.Indented);
                if (token is JObject obj) {
                    var items = new List<KvItem>();
                    foreach (var p in obj.Properties()) {
                        string val = p.Value.ToString();
                        if (p.Value.Type == JTokenType.String && (val.TrimStart().StartsWith("<") || val.TrimStart().StartsWith("{")))
                            val = TryPrettyXml(val) ?? TryPrettyJson(val) ?? val;
                        items.Add(new KvItem { Key = p.Name, Value = val });
                    }
                    KvList.ItemsSource = items;
                }
                return;
            } catch { }
            // XML 파싱 시도
            try {
                TxtPretty.Text = TryPrettyXml(message) ?? message;
                return;
            } catch { }
            TxtPretty.Text = message;
        }
        private string TryPrettyXml(string xml) {
            try {
                var doc = new XmlDocument();
                doc.LoadXml(xml);
                var sb = new StringBuilder();
                var settings = new XmlWriterSettings { Indent = true, IndentChars = "  " };
                using var writer = XmlWriter.Create(sb, settings);
                doc.Save(writer);
                return sb.ToString();
            } catch { return null; }
        }
        private string TryPrettyJson(string json) {
            try { return JToken.Parse(json).ToString(Newtonsoft.Json.Formatting.Indented); }
            catch { return null; }
        }
        private void Copy_Click(object sender, RoutedEventArgs e) => Clipboard.SetText(_log.Message);
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
    public class KvItem { public string Key { get; set; } public string Value { get; set; } }
}
