using System.Collections.ObjectModel;
using System.Windows;
using System.ComponentModel;
using LogMaverick.Models;
using LogMaverick.Services;

namespace LogMaverick.ViewModels {
    public class MainViewModel : INotifyPropertyChanged {
        private readonly LogCoreEngine _engine = new();
        public ObservableCollection<LogEntry> LogTableData { get; } = new();
        public ObservableCollection<ServerConfig> Servers { get; } = new();
        
        public string StatusText { get; set; } = "IDLE";
        public string StatusColor { get; set; } = "#444";

        public MainViewModel() {
            var saved = ConfigService.Load();
            foreach(var s in saved) Servers.Add(s);
            
            _engine.OnLogReceived += (e) => Application.Current.Dispatcher.Invoke(() => {
                LogTableData.Insert(0, e);
                if(LogTableData.Count > 5000) LogTableData.RemoveAt(5000);
            });
            _engine.OnStatusChanged += (s, c) => { StatusText = s; StatusColor = c; OnPropertyChanged("StatusText"); OnPropertyChanged("StatusColor"); };
        }

        public void AddServer(ServerConfig s) {
            Servers.Add(s);
            ConfigService.Save(new System.Collections.Generic.List<ServerConfig>(Servers));
        }

        public void RemoveServer(ServerConfig s) {
            Servers.Remove(s);
            ConfigService.Save(new System.Collections.Generic.List<ServerConfig>(Servers));
        }

        public void Connect(ServerConfig s) => _engine.Connect(s);
        public event PropertyChangedEventHandler? PropertyChanged;
        void OnPropertyChanged(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
