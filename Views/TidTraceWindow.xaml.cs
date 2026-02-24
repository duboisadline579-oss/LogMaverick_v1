using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using LogMaverick.Models;
using LogMaverick.ViewModels;

namespace LogMaverick.Views {
    public partial class TidTraceWindow : Window {
        public ObservableCollection<LogEntry> TracedLogs { get; } = new();
        public string TargetTid { get; }

        public TidTraceWindow(string tid) {
            InitializeComponent();
            TargetTid = tid;
            this.Title = $"TID Explorer - {tid}";
            this.DataContext = this;
            if (Application.Current.MainWindow.DataContext is MainViewModel vm) {
                var filtered = vm.MachineLogs
                    .Concat(vm.ProcessLogs)
                    .Concat(vm.DriverLogs)
                    .Concat(vm.OtherLogs)
                    .Where(l => l.Tid == tid)
                    .OrderByDescending(l => l.Time);
                foreach (var log in filtered) TracedLogs.Add(log);
            }
            TidTitle.Text = $"TID: {tid} — {TracedLogs.Count}건";
        }
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
        private void Copy_Click(object sender, RoutedEventArgs e) {
            var text = string.Join("\n", TracedLogs.Select(l => $"[{l.Time:HH:mm:ss}] [{l.Category}] {l.Message}"));
            if (!string.IsNullOrEmpty(text)) Clipboard.SetText(text);
        }
    }
}
