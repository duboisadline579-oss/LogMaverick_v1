using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using WinApp = System.Windows.Application;
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
        private string _statusMessage = "▶ CONNECT 버튼을 눌러 서버에 연결하세요";
        private bool _isPaused, _isConnected, _isLoading, _autoScroll = true;
        private string _connectedFile = "", _filterText, _levelFilter = "ALL";
        private ServerConfig _selectedServer;
        private int _newMachine, _newProcess, _newDriver, _newOther, _newErrors;
        public Dictionary<string, string> SessionFiles { get; } = new();
        public ObservableCollection<ServerConfig> Servers { get; } = new();
        public ObservableCollection<LogEntry> MachineLogs { get; } = new();
        public ObservableCollection<LogEntry> ProcessLogs { get; } = new();
        public ObservableCollection<LogEntry> DriverLogs { get; } = new();
        public ObservableCollection<LogEntry> OtherLogs { get; } = new();
        public ObservableCollection<LogEntry> ErrorHistory { get; } = new();
        public ObservableCollection<LogEntry> BookmarkedLogs { get; } = new();
        public ObservableCollection<string> ExcludedTids { get; } = new();
        public ObservableCollection<KeywordRule> AlertKeywords { get; } = new();
        public ObservableCollection<string> FilterHistory { get; } = new();
        public Dictionary<string, string> LastFiles { get; } = new();
        private List<FileNode> _fullTree = new();
        public ObservableCollection<FileNode> FilteredTree { get; } = new();
        public Dictionary<string, double> ColumnWidths { get; } = new() {
            ["Time"] = 90, ["TID"] = 70, ["Type"] = 80, ["Message"] = 730
        };
        public ServerConfig SelectedServer { get => _selectedServer; set { _selectedServer = value; OnPropertyChanged(); } }
        public string StatusMessage { get => _statusMessage; set { _statusMessage = value; OnPropertyChanged(); } }
        public bool IsPaused { get => _isPaused; set { _isPaused = value; OnPropertyChanged(); OnPropertyChanged(nameof(PauseStatusText)); OnPropertyChanged(nameof(PauseButtonColor)); } }
        public bool AutoScroll { get => _autoScroll; set { _autoScroll = value; OnPropertyChanged(); OnPropertyChanged(nameof(AutoScrollText)); } }
        public string AutoScrollText => _autoScroll ? "⬇ AUTO" : "⬇ OFF";
        public bool IsConnected { get => _isConnected; set { _isConnected = value; OnPropertyChanged(); OnPropertyChanged(nameof(ConnectionStatusText)); OnPropertyChanged(nameof(ConnectionStatusColor)); OnPropertyChanged(nameof(ConnectButtonText)); OnPropertyChanged(nameof(ConnectButtonColor)); } }
        public bool IsLoading { get => _isLoading; set { _isLoading = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsNotLoading)); OnPropertyChanged(nameof(ConnectButtonText)); OnPropertyChanged(nameof(ConnectButtonColor)); OnPropertyChanged(nameof(ConnectionStatusText)); OnPropertyChanged(nameof(ConnectionStatusColor)); } }
        public bool IsNotLoading => !_isLoading;
        public string ConnectedFile { get => _connectedFile; set { _connectedFile = value; OnPropertyChanged(); } }
        public string FilterText { get => _filterText; set { _filterText = value; OnPropertyChanged(); ApplyFilter(); } }
        public string LevelFilter { get => _levelFilter; set { _levelFilter = value; OnPropertyChanged(); OnPropertyChanged(nameof(LvAllColor)); OnPropertyChanged(nameof(LvErrColor)); OnPropertyChanged(nameof(LvExcColor)); OnPropertyChanged(nameof(LvSysColor)); OnPropertyChanged(nameof(LevelFilterText)); ApplyFilter(); } }
        public string LvAllColor => _levelFilter == "ALL" ? "#007AFF" : "#2A2A2A";
        public string LvErrColor => _levelFilter == "ERROR" ? "#FF4500" : "#2A2A2A";
        public string LvExcColor => _levelFilter == "EXCEPTION" ? "#9933FF" : "#2A2A2A";
        public string LvSysColor => _levelFilter == "SYSTEM" ? "#0088CC" : "#2A2A2A";
        public string LevelFilterText => $"필터:{_levelFilter}";
        public string PauseStatusText => IsPaused ? "▶ RESUME" : "⏸ PAUSE";
        public string PauseButtonColor => IsPaused ? "#CC4400" : "#007AFF";
        public string ConnectButtonText => IsLoading ? "⏳ 로딩 중..." : IsConnected ? "⏹  DISCONNECT" : "▶  CONNECT";
        public string ConnectButtonColor => IsLoading ? "#888888" : IsConnected ? "#FF4500" : "#007AFF";
        public string ConnectionStatusText => IsConnected ? "● CONNECTED" : IsLoading ? "⏳ 연결 중..." : "○ DISCONNECTED";
        public string LogCountText => $"M:{MachineLogs.Count} P:{ProcessLogs.Count} D:{DriverLogs.Count} O:{OtherLogs.Count} | ERR:{ErrorHistory.Count}";
        public string ConnectionStatusColor => IsConnected ? "#00C853" : IsLoading ? "#FFD700" : "#666666";
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
        public string MachineFlash => _newMachine > 0 ? "#1A3A6A" : "Transparent";
        public string ProcessFlash => _newProcess > 0 ? "#1A3A6A" : "Transparent";
        public string DriverFlash => _newDriver > 0 ? "#1A3A6A" : "Transparent";
        public string OtherFlash => _newOther > 0 ? "#1A3A6A" : "Transparent";
        public MainViewModel() {
            var settings = ConfigService.Load();
            foreach(var s in settings.Servers) Servers.Add(s);
            foreach(var t in settings.ExcludedTids) ExcludedTids.Add(t);
            foreach(var k in settings.AlertKeywords) AlertKeywords.Add(new KeywordRule { Keyword = k, Color = "#FF4500", Notify = true });
            foreach(var f in settings.FilterHistory) FilterHistory.Add(f);
            foreach(var lf in settings.LastFiles) LastFiles[lf.Key] = lf.Value;
            if (settings.ColumnWidths != null)
                foreach(var cw in settings.ColumnWidths) ColumnWidths[cw.Key] = cw.Value;
            _engine.OnLogReceived += (log) => {
                if (IsPaused || ExcludedTids.Contains(log.Tid)) return;
                WinApp.Current?.Dispatcher.InvokeAsync(() => {
                    if (!MatchesFilter(log)) return;
                    var target = log.Category?.ToUpper() switch {
                        "MACHINE" => MachineLogs, "DRIVER" => DriverLogs, "PROCESS" => ProcessLogs, _ => OtherLogs
                    };
                    target.Insert(0, log);
                    if (target.Count > 5000) target.RemoveAt(target.Count - 1); OnPropertyChanged(nameof(LogCountText));
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
                    if (AlertKeywords.Any(k => log.Message.Contains(k.Keyword, StringComparison.OrdinalIgnoreCase)))
                        StatusMessage = $"🔔 [{log.Category}] {log.Message.Substring(0, Math.Min(60, log.Message.Length))}";
                });
            };
            _engine.OnStatusChanged += (s) => WinApp.Current?.Dispatcher.InvokeAsync(() => {
                StatusMessage = s;
                if (s.StartsWith("✅")) { IsConnected = true; IsLoading = false; }
                else if (s.StartsWith("❌") || s.StartsWith("🔌")) { IsConnected = false; IsLoading = false; }
            });
        }
        private bool MatchesFilter(LogEntry log) {
            if (!string.IsNullOrEmpty(FilterText) && !log.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase)) return false;
            if (LevelFilter == "ERROR" && log.Type != LogType.Error && log.Type != LogType.Critical) return false;
            if (LevelFilter == "EXCEPTION" && log.Type != LogType.Exception) return false;
            if (LevelFilter == "SYSTEM" && log.Type != LogType.System) return false;
            return true;
        }
        private void ApplyFilter() {
            foreach (var log in MachineLogs.Concat(ProcessLogs).Concat(DriverLogs).Concat(OtherLogs)) {
                bool hit = !string.IsNullOrEmpty(FilterText) && log.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase);
                log.IsHighlighted = hit;
                log.IsVisible = string.IsNullOrEmpty(FilterText) || hit;
            }
            OnPropertyChanged(nameof(MachineLogs));
            OnPropertyChanged(nameof(ProcessLogs));
            OnPropertyChanged(nameof(DriverLogs));
            OnPropertyChanged(nameof(OtherLogs));
        }
        public void AddFilterHistory(string text) {
            if (string.IsNullOrEmpty(text) || FilterHistory.Contains(text)) return;
            FilterHistory.Insert(0, text);
            if (FilterHistory.Count > 10) FilterHistory.RemoveAt(FilterHistory.Count - 1);
        }
        public void ToggleBookmark(LogEntry log) {
            log.IsBookmarked = !log.IsBookmarked;
            if (log.IsBookmarked) BookmarkedLogs.Insert(0, log);
            else { var f = BookmarkedLogs.FirstOrDefault(l => l == log); if (f != null) BookmarkedLogs.Remove(f); }
            StatusMessage = log.IsBookmarked ? $"🔖 북마크: {log.Message.Substring(0, Math.Min(40, log.Message.Length))}" : "북마크 해제됨";
        }
        public async Task ConnectSessionAsync(ServerConfig config, string category, string filePath) {
            if (SessionFiles.TryGetValue(category, out var old)) SetStreamingFile(old, false);
            SessionFiles[category] = filePath; LastFiles[category] = filePath; IsConnected = true;
            SetStreamingFile(filePath, true);
            await _engine.StartSessionAsync(config, category, filePath);
        }
        
        public void StopSession(string category) {
            if (SessionFiles.TryGetValue(category, out var path)) SetStreamingFile(path, false);
            _engine.StopSession(category); SessionFiles.Remove(category);
            IsConnected = _engine.HasSession("MACHINE") || _engine.HasSession("PROCESS") || _engine.HasSession("DRIVER") || _engine.HasSession("OTHERS");
        }
        public async Task ConnectAsync(ServerConfig s, string p) {
            if (!string.IsNullOrEmpty(ConnectedFile)) SetStreamingFile(ConnectedFile, false);
            ClearAll(); ConnectedFile = p;
            string fileName = System.IO.Path.GetFileName(p).ToLower();
            string dirName = p.ToLower();
            string cat = (fileName.Contains("machine") || dirName.Contains("machine")) ? "MACHINE" : (fileName.Contains("process") || dirName.Contains("process")) ? "PROCESS" : (fileName.Contains("driver") || dirName.Contains("driver")) ? "DRIVER" : "OTHERS";
            LastFiles[cat] = p; IsConnected = true;
            SetStreamingFile(p, true);
            await _engine.StartSessionAsync(s, cat, p);
        }
        public void SetStreamingFile(string path, bool streaming) {
            var node = FindNode(_fullTree, path);
            if (node != null) node.IsStreaming = streaming;
        }
        private FileNode? FindNode(System.Collections.Generic.List<FileNode> nodes, string path) {
            foreach (var n in nodes) {
                if (n.FullPath == path) return n;
                var found = FindNode(n.Children.ToList(), path);
                if (found != null) return found;
            }
            return null;
        }
        
        public void Disconnect() { SetStreamingFile(ConnectedFile, false); _engine.Dispose(); IsConnected = false; IsLoading = false; ConnectedFile = ""; SessionFiles.Clear(); ClearAll(); StatusMessage = "🔌 연결 해제됨"; }
        public void ExportLogs(string tab) {
            try {
                var list = tab.Contains("MACHINE") ? MachineLogs : tab.Contains("PROCESS") ? ProcessLogs : tab.Contains("DRIVER") ? DriverLogs : OtherLogs;
                if (!list.Any()) { StatusMessage = "⚠ 내보낼 로그가 없습니다"; return; }
                string exportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports"); Directory.CreateDirectory(exportDir);
                string path = Path.Combine(exportDir, $"Log_{tab}_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                File.WriteAllLines(path, list.Select(l => $"[{l.Time:HH:mm:ss}] [{l.Tid}] {l.Message}"));
                StatusMessage = $"✅ 저장: {path}";
            } catch (Exception ex) { MessageBox.Show("Export 실패: " + ex.Message); }
        }
        public void ExportAll() {
            try {
                string exportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports"); Directory.CreateDirectory(exportDir);
                string path = Path.Combine(exportDir, $"Log_ALL_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
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
            ConfigService.Save(new AppSettings {
                Servers = Servers.ToList(), ExcludedTids = ExcludedTids.ToList(),
                AlertKeywords = AlertKeywords.Select(k => k.Keyword).ToList(), FilterHistory = FilterHistory.ToList(),
                LastFiles = LastFiles, ColumnWidths = ColumnWidths
            });
        }
        public void SetTree(List<FileNode> tree) {
            _fullTree = tree; FilteredTree.Clear();
            foreach (var n in tree) FilteredTree.Add(n);
            foreach (var path in SessionFiles.Values) SetStreamingFile(path, true);
            if (!string.IsNullOrEmpty(ConnectedFile)) SetStreamingFile(ConnectedFile, true);
        }
        public void SearchTree(string q) {
            FilteredTree.Clear();
            if (string.IsNullOrEmpty(q)) { foreach (var n in _fullTree) FilteredTree.Add(n); return; }
            foreach (var n in _fullTree) {
                var result = FilterNode(n, q);
                if (result != null) FilteredTree.Add(result);
            }
        }
        private FileNode? FilterNode(FileNode node, string q) {
            bool match = node.Name.Contains(q, StringComparison.OrdinalIgnoreCase);
            var matchedChildren = new System.Collections.ObjectModel.ObservableCollection<FileNode>();
            foreach (var c in node.Children) {
                var r = FilterNode(c, q);
                if (r != null) matchedChildren.Add(r);
            }
            if (match) {
                var copy = new FileNode { Name = node.Name, FullPath = node.FullPath, IsDirectory = node.IsDirectory, IsStreaming = node.IsStreaming };
                foreach (var c in node.Children) copy.Children.Add(c);
                return copy;
            }
            if (matchedChildren.Any()) {
                var copy = new FileNode { Name = node.Name, FullPath = node.FullPath, IsDirectory = node.IsDirectory };
                foreach (var c in matchedChildren) copy.Children.Add(c);
                return copy;
            }
            return null;
        }
        public void ClearAll() {
            MachineLogs.Clear(); ProcessLogs.Clear(); DriverLogs.Clear(); OtherLogs.Clear();
            ErrorHistory.Clear(); BookmarkedLogs.Clear();
            NewMachine = 0; NewProcess = 0; NewDriver = 0; NewOther = 0; NewErrors = 0; OnPropertyChanged(nameof(LogCountText));
            OnPropertyChanged(nameof(ErrorVisibility));
        }
    }
}
