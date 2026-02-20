using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using LogMaverick.Models;
using LogMaverick.Services;

namespace LogMaverick.ViewModels {
    public class MainViewModel : INotifyPropertyChanged {
        private readonly LogCoreEngine _engine = new();
        private const int MAX_LOGS = 10000;
        private string _filterText = "";
        public string FilterText { get => _filterText; set { _filterText = value; OnProp("FilterText"); } }
        
        private bool _isPaused = false;
        public bool IsPaused { get => _isPaused; set { _isPaused = value; OnProp("IsPaused"); OnProp("PauseStatusText"); } }
        public string PauseStatusText => IsPaused ? "▶ RESUME" : "⏸ PAUSE";
        public HashSet<string> ExcludedTids { get; } = new HashSet<string>();

        public ObservableCollection<LogEntry> MachineLogs { get; } = new();
        public ObservableCollection<LogEntry> ProcessLogs { get; } = new();
        public ObservableCollection<LogEntry> DriverLogs { get; } = new();
        public ObservableCollection<LogEntry> OtherLogs { get; } = new();
        public ObservableCollection<LogEntry> ErrorHistory { get; } = new();
        public ObservableCollection<ServerConfig> Servers { get; } = new();
        public Visibility ErrorVisibility => ErrorHistory.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public MainViewModel() {
            var saved = ConfigService.Load();
            if (saved != null) foreach(var s in saved) Servers.Add(s);
            _engine.OnLogReceived += (e) => Application.Current.Dispatcher.Invoke(() => {
                if (IsPaused || ExcludedTids.Contains(e.Tid)) return;
                if (!string.IsNullOrEmpty(FilterText) && e.Message.Contains(FilterText)) e.IsHighlighted = true;
                if (e.Type != LogType.System) { ErrorHistory.Insert(0, e); OnProp("ErrorVisibility"); }
                var target = e.Category switch { "MACHINE"=>MachineLogs, "PROCESS"=>ProcessLogs, "DRIVER"=>DriverLogs, _=>OtherLogs };
                target.Insert(0, e);
                if (target.Count > MAX_LOGS) target.RemoveAt(MAX_LOGS);
            });
        }

        public async Task ConnectAsync(ServerConfig s, string path) => await _engine.StartStreamingAsync(s, path);
        public void ExportLogs(string cat) {
            var target = cat switch { "MACHINE"=>MachineLogs, "PROCESS"=>ProcessLogs, "DRIVER"=>DriverLogs, _=>OtherLogs };
            File.WriteAllText($"Export_{cat}_{DateTime.Now:HHmmss}.txt", string.Join("\n", target.Select(l => l.Message)));
            MessageBox.Show("Export Complete.");
        }
        public void ClearAll() { MachineLogs.Clear(); ProcessLogs.Clear(); DriverLogs.Clear(); OtherLogs.Clear(); ErrorHistory.Clear(); OnProp("ErrorVisibility"); }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnProp(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
