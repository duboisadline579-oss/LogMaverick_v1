using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace LogMaverick.Views {
    public partial class ColumnManagerWindow : Window {
        private List<(string Name, GridViewColumn Col)> _cols;
        private List<CheckBox> _checks = new();
        public ColumnManagerWindow(List<(string, GridViewColumn)> cols) {
            InitializeComponent();
            _cols = cols;
            foreach (var (name, col) in cols) {
                var cb = new CheckBox {
                    Content = name,
                    IsChecked = col.Width > 0,
                    Foreground = System.Windows.Media.Brushes.White,
                    Margin = new Thickness(0, 4, 0, 4)
                };
                _checks.Add(cb);
                ColPanel.Children.Add(cb);
            }
        }
        private void Apply_Click(object sender, RoutedEventArgs e) {
            for (int i = 0; i < _cols.Count; i++) {
                var (name, col) = _cols[i];
                if (_checks[i].IsChecked == true) {
                    if (col.Width == 0) col.Width = name == "Time" ? 90 : name == "TID" ? 70 : name == "Type" ? 80 : name == "Category" ? 80 : 400;
                } else { col.Width = 0; }
            }
            this.Close();
        }
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}
