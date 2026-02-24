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
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private LogCoreEngine _engine = new LogCoreEngine();
        private string _statusMessage = "🟡 서버를 선택하고 REFRESH 후 파일을 선택하세요";
        private bool _isPaused = false;
        private bool _isConnected = false;
        private ServerConfig _selectedServer;
        private string _filterText;

        public ObservableCollection<ServerConfig> Servers { get; } = new();
        public ObservableCollection<LogEntry> MachineLogs { get; } = new();
        public ObservableCollection<LogEntry> ProcessLogs { get; } = new();
        public ObservableCollection<LogEntry> DriverLogs { get; } = new();
        public ObservableCollection<LogEntry> OtherLogs { get; } = new();
        public ObservableCollection<LogEntry> ErrorHistory { get; } = new();
        public ObservableCollection<string> ExcludedTids { get; } = new();

        public ServerConfig SelectedServer { get => _selectedServer; set { _selectedServer = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool IsPaused { get => _isPaused; set { _isPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(PauseStatusText)); } }
        public bool IsConnected { get => _isConnected; set { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConnectionStatusText)); OnPropertyChanged(nameof(ConnectionStatusColor)); } }
        public string PauseStatusText => IsPaused ? "RESUME" : "PAUSE";
        public string ConnectionStatusText => IsConnected ? "● CONNECTED" : "○ DISCONNECTED";
        public string ConnectionStatusColor => IsConnected ? "#00C853" : "#666";
        public Visibility ErrorVisibility => ErrorHistory.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public string FilterText { get => _filterText; set { _filterText = value; OnPropertyChanged(); ApplyFilter(); } }
        public MainViewModel() {
            var saved = ConfigService.Load();
            if (saved != null) foreach(var s in saved) Servers.Add(s);
            _engine.OnLogReceived += (log) => {
                if (IsPaused || ExcludedTids.Contains(log.Tid)) return;
                Application.Current?.Dispatcher.Invoke(() => {
                    if (!string.IsNullOrEmpty(FilterText) && !log.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase)) return;
                    var target = log.Category?.ToUpper() switch {
                        "MACHINE" => MachineLogs, "DRIVER" => DriverLogs, "PROCESS" => ProcessLogs, _ => OtherLogs
                    };
                    target.Insert(0, log);
                    if (target.Count > 5000) target.RemoveAt(target.Count - 1);
                    if (log.Type == LogType.Error || log.Type == LogType.Exception || log.Type == LogType.Critical) {
                        ErrorHistory.Insert(0, log);
                        if (ErrorHistory.Count > 1000) ErrorHistory.RemoveAt(ErrorHistory.Count - 1);
                        OnPropertyChanged(nameof(ErrorVisibility));
                    }
                });
            };
            _engine.OnStatusChanged += (s) => {
                StatusMessage = s;
                IsConnected = s.StartsWith("✅");
            };
        }
        private void ApplyFilter() {
            var all = MachineLogs.Concat(ProcessLogs).Concat(DriverLogs).Concat(OtherLogs).ToList();
            foreach (var log in all) {
                log.IsHighlighted = !string.IsNullOrEmpty(FilterText) &&
                    log.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
            }
        }
        public void ExportLogs(string tabName) {
            try {
                var list = tabName.ToUpper() switch {
                    "MACHINE" => MachineLogs, "DRIVER" => DriverLogs, "PROCESS" => ProcessLogs, _ => OtherLogs
                };
                if (!list.Any()) { StatusMessage = "⚠ 내보낼 로그가 없습니다"; return; }
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Log_{tabName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllLines(path, list.Select(l => $"[{l.Time:HH:mm:ss}] [{l.Tid}] {l.Message}"));
                StatusMessage = $"✅ 저장완료: {path}";
            } catch (Exception ex) { MessageBox.Show("Export 실패: " + ex.Message); }
        }
        public void ExportAll() {
            try {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Log_ALL_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var all = MachineLogs.Concat(ProcessLogs).Concat(DriverLogs).Concat(OtherLogs).OrderByDescending(l => l.Time);
                File.WriteAllLines(path, all.Select(l => $"[{l.Time:HH:mm:ss}] [{l.Category}] [{l.Tid}] {l.Message}"));
                StatusMessage = $"✅ 전체 저장완료: {path}";
            } catch (Exception ex) { MessageBox.Show("Export 실패: " + ex.Message); }
        }
        public async Task ConnectAsync(ServerConfig s, string p) { ClearAll(); await _engine.StartStreamingAsync(s, p); }
        public void Disconnect() { _engine.Dispose(); IsConnected = false; StatusMessage = "🔌 연결 해제됨"; }
        public void ClearAll() {
            MachineLogs.Clear(); ProcessLogs.Clear(); DriverLogs.Clear(); OtherLogs.Clear(); ErrorHistory.Clear();
            OnPropertyChanged(nameof(ErrorVisibility));
        }
    }
}
