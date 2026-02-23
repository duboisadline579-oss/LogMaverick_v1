using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using LogMaverick.Models;
using LogMaverick.ViewModels;

namespace LogMaverick.Views {
    public partial class TidTraceWindow : Window {
        public ObservableCollection<LogEntry> TracedLogs { get; } = new ObservableCollection<LogEntry>();
        public string TargetTid { get; }

        public TidTraceWindow(string tid) {
            InitializeComponent();
            this.TargetTid = tid;
            this.Title = $"TID Explorer - {tid}";
            this.DataContext = this;
            // 부모 창(MainWindow)의 ViewModel에서 해당 TID 로그만 복사
            if (Application.Current.MainWindow.DataContext is MainViewModel vm) {
                var filtered = vm.MachineLogs.Concat(vm.ProcessLogs)
                                 .Where(l => l.Tid == tid)
                                 .OrderByDescending(l => l.Time);
                foreach (var log in filtered) TracedLogs.Add(log);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
        
        private void Copy_Click(object sender, RoutedEventArgs e) {
            var selected = (TracedLogs.Count > 0) ? string.Join("\n", TracedLogs.Select(l => l.Message)) : "";
            if (!string.IsNullOrEmpty(selected)) Clipboard.SetText(selected);
        }
    }
}
