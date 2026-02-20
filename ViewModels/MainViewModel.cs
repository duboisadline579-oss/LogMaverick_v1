using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.ComponentModel;
using LogMaverick.Models;
using LogMaverick.Services;

namespace LogMaverick.ViewModels {
    public class MainViewModel : INotifyPropertyChanged {
        private readonly LogCoreEngine _engine = new();
        public ObservableCollection<LogEntry> LogTableData { get; } = new();
        public ObservableCollection<ServerConfig> Servers { get; } = new();
        private string _status = "OFFLINE";
        private string _color = "#444";

        public string StatusText { get => _status; set { _status = value; OnProp("StatusText"); } }
        public string StatusColor { get => _color; set { _color = value; OnProp("StatusColor"); } }

        public MainViewModel() {
            BindingOperations.EnableCollectionSynchronization(LogTableData, new object());
            foreach(var s in ConfigService.Load()) Servers.Add(s);
            _engine.OnLogReceived += (e) => Application.Current.Dispatcher.Invoke(() => {
                LogTableData.Insert(0, e);
                if(LogTableData.Count > 5000) LogTableData.RemoveAt(5000);
            });
            _engine.OnStatusChanged += (s, c) => { StatusText = s; StatusColor = c; };
        }
        public void Connect(ServerConfig s) => _engine.Connect(s);
        public event PropertyChangedEventHandler? PropertyChanged;
        void OnProp(string n) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
