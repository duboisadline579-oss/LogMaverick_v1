using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using LogMaverick.Models;
using LogMaverick.Services;

namespace LogMaverick.ViewModels {
    public class MainViewModel : INotifyPropertyChanged {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private LogCoreEngine _engine = new LogCoreEngine();
        private string _statusMessage = "⚙ 서버를 선택하고 🔄 버튼으로 파일 목록을 불러오세요";
        private bool _isPaused, _isConnected;
        private string _connectedFile = "", _filterText;
        private ServerConfig _selectedServer;
        private int _newMachine, _newProcess, _newDriver, _newOther, _newErrors;
        public Dictionary<string, string> SessionFiles { get; } = new();

        public ObservableCollection<ServerConfig> Servers { get; } = new();
        public ObservableCollection<LogEntry> MachineLogs { get; } = new();
        public ObservableCollection<LogEntry> ProcessLogs { get; } = new();
        public ObservableCollection<LogEntry> DriverLogs { get; } = new();
        public ObservableCollection<LogEntry> OtherLogs { get; } = new();
        public ObservableCollection<LogEntry> ErrorHistory { get; } = new();
        public ObservableCollection<string> ExcludedTids { get; } = new();
        public ObservableCollection<string> AlertKeywords { get; } = new();
        public ServerConfig SelectedServer { get => _selectedServer; set { _selectedServer = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool IsPaused { get => _isPaused; set { _isPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(PauseStatusText)); } }
        public bool IsConnected { get => _isConnected; set { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConnectionStatusText)); OnPropertyChanged(nameof(ConnectionStatusColor)); OnPropertyChanged(nameof(ConnectButtonText)); OnPropertyChanged(nameof(ConnectButtonColor)); } }
        public string ConnectedFile { get => _connectedFile; set { _connectedFile = value; OnPropertyChanged(); } }
        public string FilterText { get => _filterText; set { _filterText = value; OnPropertyChanged(); ApplyFilter(); } }
        public string PauseStatusText => IsPaused ? "▶ RESUME" : "⏸ PAUSE";
        public string ConnectButtonText => IsConnected ? "⏹  DISCONNECT" : "▶  CONNECT";
        public string ConnectButtonColor => IsConnected ? "#FF4500" : "#007AFF";
        public string ConnectionStatusText => IsConnected ? "● CONNECTED" : "○ DISCONNECTED";
        public string ConnectionStatusColor => IsConnected ? "#00C853" : "#666666";
        public Visibility ErrorVisibility => ErrorHistory.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        public int NewMachine { get => _newMachine; set { _newMachine = value; OnPropertyChanged(); OnPropertyChanged(nameof(MachineTab)); OnPropertyChanged(nameof(MachineFlash)); } }
        public int NewProcess { get => _newProcess; set { _newProcess = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProcessTab)); OnPropertyChanged(nameof(ProcessFlash)); } }
        public int NewDriver { get => _newDriver; set { _newDriver = value; OnPropertyChanged(); OnPropertyChanged(nameof(DriverTab)); OnPropertyChanged(nameof(DriverFlash)); } }
        public int NewOther { get => _newOther; set { _newOther = value; OnPropertyChanged(); OnPropertyChanged(nameof(OtherTab)); OnPropertyChanged(nameof(OtherFlash)); } }
        public int NewErrors { get => _newErrors; set { _newErrors = value; OnPropertyChanged(); OnPropertyChanged(nameof(ErrorFlash)); } }
        public string MachineTab => _newMachine > 0 ? $"⚙ MACHINE ({_newMachine})" : "⚙ MACHINE";
        public string ProcessTab => _newProcess > 0 ? $"⚡ PROCESS ({_newProcess})" : "⚡ PROCESS";
        public string DriverTab => _newDriver > 0 ? $"🔧 DRIVER ({_newDriver})" : "🔧 DRIVER";
        public string OtherTab => _newOther > 0 ? $"📋 OTHERS ({_newOther})" : "📋 OTHERS";
        public string ErrorFlash => _newErrors > 0 ? "#FF2222" : "#440000";
        public string MachineFlash => _newMachine > 0 ? "#007AFF" : "#161616";
        public string ProcessFlash => _newProcess > 0 ? "#007AFF" : "#161616";
        public string DriverFlash => _newDriver > 0 ? "#007AFF" : "#161616";
        public string OtherFlash => _newOther > 0 ? "#007AFF" : "#161616";
        public MainViewModel() {
            var settings = ConfigService.Load();
            foreach(var s in settings.Servers) Servers.Add(s);
            foreach(var t in settings.ExcludedTids) ExcludedTids.Add(t);
            foreach(var k in settings.AlertKeywords) AlertKeywords.Add(k);
            _engine.OnLogReceived += (log) => {
                if (IsPaused || ExcludedTids.Contains(log.Tid)) return;
                Application.Current?.Dispatcher.InvokeAsync(() => {
                    if (!string.IsNullOrEmpty(FilterText) && !log.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase)) return;
                    var target = log.Category?.ToUpper() switch {
                        "MACHINE" => MachineLogs, "DRIVER" => DriverLogs, "PROCESS" => ProcessLogs, _ => OtherLogs
                    };
                    target.Insert(0, log);
                    if (target.Count > 5000) target.RemoveAt(target.Count - 1);
                    switch (log.Category?.ToUpper()) {
                        case "MACHINE": NewMachine++; break;
                        case "PROCESS": NewProcess++; break;
                        case "DRIVER": NewDriver++; break;
                        default: NewOther++; break;
                    }
                    if (log.Type == LogType.Error || log.Type == LogType.Exception || log.Type == LogType.Critical) {
                        ErrorHistory.Insert(0, log);
                        if (ErrorHistory.Count > 1000) ErrorHistory.RemoveAt(ErrorHistory.Count - 1);
                        NewErrors++; OnPropertyChanged(nameof(ErrorVisibility));
                    }
                    if (AlertKeywords.Any(k => log.Message.Contains(k, StringComparison.OrdinalIgnoreCase)))
                        ShowAlert(log);
                });
            };
            _engine.OnStatusChanged += (s) => Application.Current?.Dispatcher.InvokeAsync(() => {
                StatusMessage = s; IsConnected = _engine.HasSession("MACHINE") || _engine.HasSession("PROCESS") || _engine.HasSession("DRIVER") || _engine.HasSession("OTHERS");
            });
        }
        private void ShowAlert(LogEntry log) {
            try {
                var icon = new System.Windows.Forms.NotifyIcon();
                icon.Icon = System.Drawing.SystemIcons.Warning;
                icon.Visible = true;
                icon.ShowBalloonTip(3000, "⚠ LogMaverick 알림", $"[{log.Category}] {log.Message.Substring(0, Math.Min(80, log.Message.Length))}", System.Windows.Forms.ToolTipIcon.Warning);
            } catch { }
        }
        private void ApplyFilter() {
            foreach (var log in MachineLogs.Concat(ProcessLogs).Concat(DriverLogs).Concat(OtherLogs))
                log.IsHighlighted = !string.IsNullOrEmpty(FilterText) && log.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
        }
        public async Task ConnectSessionAsync(ServerConfig config, string category, string filePath) {
            SessionFiles[category] = filePath;
            await _engine.StartSessionAsync(config, category, filePath);
        }
        public void StopSession(string category) { _engine.StopSession(category); SessionFiles.Remove(category); }
        public async Task ConnectAsync(ServerConfig s, string p) { ClearAll(); ConnectedFile = p; await _engine.StartSessionAsync(s, "MACHINE", p); }
        public void Disconnect() { _engine.Dispose(); IsConnected = false; ConnectedFile = ""; SessionFiles.Clear(); StatusMessage = "🔌 연결 해제됨"; }
        public void ExportLogs(string tab) {
            try {
                var list = tab.Contains("MACHINE") ? MachineLogs : tab.Contains("PROCESS") ? ProcessLogs : tab.Contains("DRIVER") ? DriverLogs : OtherLogs;
                if (!list.Any()) { StatusMessage = "⚠ 내보낼 로그가 없습니다"; return; }
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Log_{tab}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllLines(path, list.Select(l => $"[{l.Time:HH:mm:ss}] [{l.Tid}] {l.Message}"));
                StatusMessage = $"✅ 저장: {path}";
            } catch (Exception ex) { MessageBox.Show("Export 실패: " + ex.Message); }
        }
        public void ExportAll() {
            try {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"Log_ALL_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                var all = MachineLogs.Concat(ProcessLogs).Concat(DriverLogs).Concat(OtherLogs).OrderByDescending(l => l.Time);
                File.WriteAllLines(path, all.Select(l => $"[{l.Time:HH:mm:ss}] [{l.Category}] [{l.Tid}] {l.Message}"));
                StatusMessage = $"✅ 전체 저장: {path}";
            } catch (Exception ex) { MessageBox.Show("Export 실패: " + ex.Message); }
        }
        public void ResetTab(string tab) {
            if (tab.Contains("MACHINE")) NewMachine = 0;
            else if (tab.Contains("PROCESS")) NewProcess = 0;
            else if (tab.Contains("DRIVER")) NewDriver = 0;
            else NewOther = 0;
        }
        public void ResetErrors() => NewErrors = 0;
        public void SaveSettings() {
            var settings = new AppSettings {
                Servers = Servers.ToList(),
                ExcludedTids = ExcludedTids.ToList(),
                AlertKeywords = AlertKeywords.ToList()
            };
            ConfigService.Save(settings);
        }
        public void ClearAll() {
            MachineLogs.Clear(); ProcessLogs.Clear(); DriverLogs.Clear(); OtherLogs.Clear(); ErrorHistory.Clear();
            NewMachine = 0; NewProcess = 0; NewDriver = 0; NewOther = 0; NewErrors = 0;
            OnPropertyChanged(nameof(ErrorVisibility));
        }
    }
}
