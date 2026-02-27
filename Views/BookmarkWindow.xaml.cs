using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using LogMaverick.Models;

namespace LogMaverick.Views {
    public partial class BookmarkWindow : Window {
        private ObservableCollection<LogEntry> _bookmarks;
        public BookmarkWindow(ObservableCollection<LogEntry> bookmarks) {
            this.Closed += (s, e) => Owner?.Activate();
            InitializeComponent();
            _bookmarks = bookmarks;
            BmList.ItemsSource = bookmarks;
        }
        private void Bm_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (BmList.SelectedItem is LogEntry log)
                new LogDetailWindow(log) { Owner = this }.Show();
        }
        private void Del_Click(object sender, RoutedEventArgs e) {
            if (BmList.SelectedItem is LogEntry log) {
                log.IsBookmarked = false;
                _bookmarks.Remove(log);
            }
        }
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
