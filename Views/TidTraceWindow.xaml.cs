using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LogMaverick.Models;
using LogMaverick.ViewModels;

namespace LogMaverick.Views {
    public partial class TidTraceWindow : Window {
        private ObservableCollection<LogEntry> _logs = new();
        private string _tid;
        private MainViewModel _vm;

        public TidTraceWindow(string tid) {
            this.Closed += (s, e) => Owner?.Activate();
            InitializeComponent();
            _tid = tid;
            this.Title = $"TID Explorer - {tid}";
            TidTitle.Text = $"TID: {tid}";
            if (Application.Current.MainWindow?.DataContext is MainViewModel vm) {
                _vm = vm;
                LoadLogs();
                vm.MachineLogs.CollectionChanged += OnLogsChanged;
                vm.ProcessLogs.CollectionChanged += OnLogsChanged;
                vm.DriverLogs.CollectionChanged += OnLogsChanged;
                vm.OtherLogs.CollectionChanged += OnLogsChanged;
            }
            LogList.ItemsSource = _logs;
            UpdateCount();
        }
        private void LoadLogs() {
            _logs.Clear();
            var all = _vm.MachineLogs.Concat(_vm.ProcessLogs).Concat(_vm.DriverLogs).Concat(_vm.OtherLogs)
                .Where(l => l.Tid == _tid).OrderByDescending(l => l.Time);
            foreach (var log in all) _logs.Add(log);
        }
        private void OnLogsChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) {
            if (e.NewItems == null) return;
            Dispatcher.InvokeAsync(() => {
                foreach (LogEntry log in e.NewItems)
                    if (log.Tid == _tid) { _logs.Insert(0, log); UpdateCount(); }
            });
        }
        private void UpdateCount() => TxtCount.Text = $"{_logs.Count}건";
        private void Search_Changed(object sender, TextChangedEventArgs e) {
            string q = TxtSearch.Text.Trim();
            SearchHint.Visibility = string.IsNullOrEmpty(q) ? Visibility.Visible : Visibility.Collapsed;
            if (string.IsNullOrEmpty(q)) { LogList.ItemsSource = _logs; TxtCount.Text = $"{_logs.Count}건"; return; }
            var matched = _logs.Where(l => l.Message.Contains(q, StringComparison.OrdinalIgnoreCase)).ToList();
            LogList.ItemsSource = matched;
            TxtCount.Text = $"{matched.Count}건 매칭";
            if (matched.Any()) LogList.ScrollIntoView(matched.First());
        }
        private void Copy_Click(object sender, RoutedEventArgs e) {
            var text = string.Join("\n", _logs.Select(l => $"[{l.Time:HH:mm:ss}] [{l.Category}] {l.Message}"));
            if (!string.IsNullOrEmpty(text)) Clipboard.SetText(text);
        }
        private void Log_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (LogList.SelectedItem is LogEntry log)
                new LogDetailWindow(log) { Owner = this }.Show();
        }
        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
        protected override void OnClosed(EventArgs e) {
            if (_vm != null) {
                _vm.MachineLogs.CollectionChanged -= OnLogsChanged;
                _vm.ProcessLogs.CollectionChanged -= OnLogsChanged;
                _vm.DriverLogs.CollectionChanged -= OnLogsChanged;
                _vm.OtherLogs.CollectionChanged -= OnLogsChanged;
            }
            base.OnClosed(e);
        }
    }
}
