using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using LogMaverick.Models;
using System.Linq;

namespace LogMaverick.Views {
    public partial class ErrorWindow : Window {
        private ObservableCollection<LogEntry> _history;
        public ErrorWindow(ObservableCollection<LogEntry> history) {
            InitializeComponent();
            _history = history;
            this.DataContext = this;
            ErrorList.ItemsSource = _history;
        }

        private void Error_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (ErrorList.SelectedItem is LogEntry selected) {
                // TID 추적 윈도우 호출 (메인 뷰모델의 모든 로그에서 해당 TID 필터링)
                var tidWin = new TidTraceWindow(selected.Tid);
                tidWin.Show();
            }
        }
    }
}
