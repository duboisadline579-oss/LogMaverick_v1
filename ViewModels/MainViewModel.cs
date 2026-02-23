using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Linq;
using LogMaverick.Models;
using LogMaverick.Services;

namespace LogMaverick.ViewModels {
    public class MainViewModel : INotifyPropertyChanged {
        private LogCoreEngine _engine = new LogCoreEngine();
        private string _statusMessage = "READY";
        private bool _isPaused = false;
        private ServerConfig _selectedServer;

        public ObservableCollection<ServerConfig> Servers { get; } = new ObservableCollection<ServerConfig>();
        public ObservableCollection<LogEntry> MachineLogs { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> ProcessLogs { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> DriverLogs { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> OtherLogs { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> ErrorHistory { get; } = new ObservableCollection<LogEntry>();
        public HashSet<string> ExcludedTids { get; } = new HashSet<string>();

        public ServerConfig SelectedServer { get => _selectedServer; set { _selectedServer = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool IsPaused { get => _isPaused; set { _isPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(PauseStatusText)); } }
        public string PauseStatusText => IsPaused ? "RESUME" : "PAUSE";
        public Visibility ErrorVisibility => ErrorHistory.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public string FilterText { get; set; }
        public MainViewModel() {
            var saved = ConfigService.Load();
            if (saved != null) foreach(var s in saved) Servers.Add(s);
            _engine.OnLogReceived += (log) => {
                if (IsPaused || ExcludedTids.Contains(log.Tid)) return;
                Application.Current?.Dispatcher.Invoke(() => {
                    if (!string.IsNullOrEmpty(FilterText) && !log.Message.Contains(FilterText)) return;
                    var target = log.Category?.ToUpper() switch {
                        "MACHINE" => MachineLogs, "DRIVER" => DriverLogs, "PROCESS" => ProcessLogs, _ => OtherLogs
                    };
                    target.Insert(0, log);
                    if (target.Count > 5000) target.RemoveAt(5000);
                    if (log.Type != LogType.System) ErrorHistory.Insert(0, log);
                });
            };
            _engine.OnStatusChanged += (s) => StatusMessage = s;
        }
        public async Task ConnectAsync(ServerConfig s, string path) {
            ClearAll();
            await _engine.StartStreamingAsync(s, path);
        }

        public void ClearAll() {
            MachineLogs.Clear(); ProcessLogs.Clear(); DriverLogs.Clear(); OtherLogs.Clear(); ErrorHistory.Clear();
            OnPropertyChanged(nameof(ErrorVisibility));
        }

        public void ExportLogs(string tabName) {
            try {
                var list = tabName == "MACHINE" ? MachineLogs : ProcessLogs;
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"Log_{tabName}_{DateTime.Now:HHmmss}.txt");
                File.WriteAllLines(path, list.Select(l => $"[{l.Time:HH:mm:ss}] {l.Message}"));
                MessageBox.Show("저장 완료: " + path);
            } catch (Exception ex) { MessageBox.Show("실패: " + ex.Message); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string n = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
