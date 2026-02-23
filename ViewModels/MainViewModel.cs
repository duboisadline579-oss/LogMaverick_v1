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
        private string _filterText;

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
        public string FilterText { get => _filterText; set { _filterText = value; OnPropertyChanged(); } }
        public MainViewModel() {
            var saved = ConfigService.Load();
            if (saved != null) foreach(var s in saved) Servers.Add(s);
            
            _engine.OnLogReceived += (log) => {
                if (IsPaused || ExcludedTids.Contains(log.Tid)) return;
                
                Application.Current?.Dispatcher.Invoke(() => {
                    // 필터 텍스트가 있으면 필터링
                    if (!string.IsNullOrEmpty(FilterText) && !log.Message.Contains(FilterText, StringComparison.OrdinalIgnoreCase)) return;

                    var cat = log.Category?.ToUpper() ?? "OTHER";
                    var target = cat switch {
                        "MACHINE" => MachineLogs, "DRIVER" => DriverLogs, "PROCESS" => ProcessLogs, _ => OtherLogs
                    };
                    
                    target.Insert(0, log);
                    if (target.Count > 5000) target.RemoveAt(5000);
                    
                    if (log.Type == LogType.Error || log.Type == LogType.Critical) {
                        ErrorHistory.Insert(0, log);
                        OnPropertyChanged(nameof(ErrorVisibility));
                    }
                });
            };
            _engine.OnStatusChanged += (s) => StatusMessage = s;
        }
