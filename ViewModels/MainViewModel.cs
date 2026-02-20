using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using LogMaverick.Models;
using LogMaverick.Services;

namespace LogMaverick.ViewModels {
    public class MainViewModel : INotifyPropertyChanged {
        private readonly LogCoreEngine _engine = new LogCoreEngine();
        public ObservableCollection<ServerConfig> Servers { get; } = new ObservableCollection<ServerConfig>();
        public ObservableCollection<LogEntry> MachineLogs { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> ProcessLogs { get; } = new ObservableCollection<LogEntry>();
        public ObservableCollection<LogEntry> DriverLogs { get; } = new ObservableCollection<LogEntry>();

        private int _errorCount = 0;
        public int ErrorCount { get => _errorCount; set { _errorCount = value; OnProp("ErrorCount"); OnProp("ErrorVisibility"); } }
        public Visibility ErrorVisibility => ErrorCount > 0 ? Visibility.Visible : Visibility.Collapsed;

        public MainViewModel() {
            foreach(var s in ConfigService.Load()) Servers.Add(s);
            _engine.OnLogReceived += (e) => Application.Current.Dispatcher.Invoke(() => {
                if(e.Message.ToUpper().Contains("ERROR") || e.Message.ToUpper().Contains("EXCEPTION")) ErrorCount++;
                if(e.Category == "PROCESS") ProcessLogs.Insert(0, e);
                else if(e.Category == "DRIVER") DriverLogs.Insert(0, e);
                else MachineLogs.Insert(0, e);
            });
        }

        public void ClearAll() { MachineLogs.Clear(); ProcessLogs.Clear(); DriverLogs.Clear(); ErrorCount = 0; }
        public void Connect(ServerConfig s, string path) => _engine.StartStreaming(s, path);
        public List<FileNode> LoadFiles(ServerConfig s) => _engine.GetFileTree(s);

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnProp(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
