using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using LogMaverick.Models;

namespace LogMaverick.Views {
    public partial class ErrorWindow : Window {
        private ObservableCollection<LogEntry> _errors;
        private ObservableCollection<string> _keywords;
        public ErrorWindow(ObservableCollection<LogEntry> errors, ObservableCollection<string> keywords) {
            InitializeComponent();
            _errors = errors; _keywords = keywords;
            ErrorList.ItemsSource = errors;
            KeywordList.ItemsSource = keywords;
        }
        private void Error_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (ErrorList.SelectedItem is LogEntry log)
                new LogDetailWindow(log) { Owner = this }.Show();
        }
        private void AddKeyword_Click(object sender, RoutedEventArgs e) {
            string kw = TxtKeyword.Text.Trim();
            if (!string.IsNullOrEmpty(kw) && !_keywords.Contains(kw)) { _keywords.Add(kw); TxtKeyword.Text = ""; }
        }
        private void DelKeyword_Click(object sender, RoutedEventArgs e) {
            if (KeywordList.SelectedItem is string kw) _keywords.Remove(kw);
        }
        private void Clear_Click(object sender, RoutedEventArgs e) => _errors.Clear();
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
