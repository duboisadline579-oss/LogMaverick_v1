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
        private string _filterText = "";

        public ObservableCollection<ServerConfig> Servers { get; } = new ObservableCollection<ServerConfig>();
        public ObservableCollection<LogEntry> MachineLogs { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> ProcessLogs { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> ErrorHistory { get; } = new ObservableCollection<LogEntry>();
        public HashSet<string> ExcludedTids { get; } = new HashSet<string>();

        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public string FilterText { get => _filterText; set { _filterText = value; OnPropertyChanged(); } }
        public bool IsPaused { get => _isPaused; set { _isPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(PauseStatusText)); } }
        public string PauseStatusText => IsPaused ? "RESUME" : "PAUSE";
        public Visibility ErrorVisibility => ErrorHistory.Count > 0 ? Visibility.Visible : Visibility.Collapsed;

        public MainViewModel() {
            var saved = ConfigService.Load();
            if (saved != null) foreach(var s in saved) Servers.Add(s);
            _engine.OnStatusChanged += (status) => StatusMessage = status;
            _engine.OnLogReceived += (log) => {
                if (IsPaused || ExcludedTids.Contains(log.Tid)) return;
                Application.Current.Dispatcher.Invoke(() => {
                    if (!string.IsNullOrEmpty(FilterText) && !log.Message.Contains(FilterText)) return;

                    if (log.Type != LogType.System) {
                        ErrorHistory.Insert(0, log);
                        if (ErrorHistory.Count > 500) ErrorHistory.RemoveAt(500);
                        OnPropertyChanged(nameof(ErrorVisibility));
                    }

                    if (log.Category == "MACHINE") MachineLogs.Insert(0, log);
                    else if (log.Category == "PROCESS") ProcessLogs.Insert(0, log);
                    
                    if (MachineLogs.Count > 5000) MachineLogs.RemoveAt(5000);
                    if (ProcessLogs.Count > 5000) ProcessLogs.RemoveAt(5000);
                });
            };
        }
        public async Task ConnectAsync(ServerConfig s, string path) {
            ClearAll();
            StatusMessage = "CONNECTING TO " + s.Host + "...";
            await _engine.StartStreamingAsync(s, path);
        }

        public void ClearAll() {
            MachineLogs.Clear(); ProcessLogs.Clear(); ErrorHistory.Clear();
            OnPropertyChanged(nameof(ErrorVisibility));
            StatusMessage = "LOGS CLEARED";
        }

        public void ExportLogs(string tabName) {
            try {
                var targetList = tabName == "MACHINE" ? MachineLogs : ProcessLogs;
                if (!targetList.Any()) { MessageBox.Show("내보낼 로그가 없습니다."); return; }
                string fileName = $"Export_{tabName}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                using (StreamWriter sw = new StreamWriter(fileName)) {
                    foreach (var log in targetList) sw.WriteLine($"[{log.Time:HH:mm:ss}] [{log.Tid}] {log.Message}");
                }
                StatusMessage = "EXPORTED: " + fileName;
                MessageBox.Show("저장 완료: " + fileName);
            } catch (Exception ex) { MessageBox.Show("내보내기 실패: " + ex.Message); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
