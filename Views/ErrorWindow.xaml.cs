using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using LogMaverick.Models;

namespace LogMaverick.Views {
    public partial class ErrorWindow : Window {
        private ObservableCollection<LogEntry> _history;
        public ErrorWindow(ObservableCollection<LogEntry> history) {
            InitializeComponent();
            _history = history;
            ErrorList.ItemsSource = _history;
        }
        private void Error_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (ErrorList.SelectedItem is LogEntry log)
                new TidTraceWindow(log.Tid) { Owner = this }.Show();
        }
        private void Clear_Click(object sender, RoutedEventArgs e) => _history.Clear();
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
