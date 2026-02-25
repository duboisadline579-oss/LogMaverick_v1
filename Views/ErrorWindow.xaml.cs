using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LogMaverick.Models;

namespace LogMaverick.Views {
    public partial class ErrorWindow : Window {
        private ObservableCollection<LogEntry> _errors;
        public ErrorWindow(ObservableCollection<LogEntry> errors) {
            InitializeComponent();
            _errors = errors;
            ErrorList.ItemsSource = errors;
        }
        private void Error_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (ErrorList.SelectedItem is LogEntry log)
                new LogDetailWindow(log) { Owner = this }.Show();
        }
        private void Clear_Click(object sender, RoutedEventArgs e) => _errors.Clear();
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
