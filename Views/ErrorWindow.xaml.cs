using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using LogMaverick.Models;

namespace LogMaverick.Views {
    public partial class ErrorWindow : Window {
        private ObservableCollection<LogEntry> _errors;
        public ObservableCollection<KeywordRule> Keywords { get; }
        public ErrorWindow(ObservableCollection<LogEntry> errors, ObservableCollection<KeywordRule> keywords) {
            this.Closed += (s, e) => Owner?.Activate();
            InitializeComponent();
            _errors = errors;
            Keywords = keywords;
            ErrorList.ItemsSource = errors;
            KeywordList.ItemsSource = keywords;
        }
        private void Error_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (ErrorList.SelectedItem is LogEntry log)
                new LogDetailWindow(log) { Owner = this }.Show();
        }
        private void Keyword_DoubleClick(object sender, MouseButtonEventArgs e) { }
        private void AddKeyword_Click(object sender, RoutedEventArgs e) {
            string kw = TxtKeyword.Text.Trim();
            string color = TxtColor.Text.Trim();
            if (string.IsNullOrEmpty(kw)) return;
            if (!color.StartsWith("#")) color = "#FF4500";
            Keywords.Add(new KeywordRule { Keyword = kw, Color = color, Notify = ChkNotify.IsChecked == true });
            TxtKeyword.Text = "";
        }
        private void DelKeyword_Click(object sender, RoutedEventArgs e) {
            if (KeywordList.SelectedItem is KeywordRule kw) Keywords.Remove(kw);
        }
        private void Clear_Click(object sender, RoutedEventArgs e) => _errors.Clear();
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
