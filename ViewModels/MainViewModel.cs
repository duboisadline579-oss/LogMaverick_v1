using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using LogMaverick.Models;
using LogMaverick.Services;
namespace LogMaverick.ViewModels {
    public class MainViewModel {
        private readonly LogCoreEngine _engine = new();
        public ObservableCollection<LogEntry> Logs { get; } = new();
        private readonly object _lock = new();
        public MainViewModel() {
            BindingOperations.EnableCollectionSynchronization(Logs, _lock);
            _engine.OnLogReceived += (entry) => {
                Application.Current.Dispatcher.Invoke(() => {
                    lock(_lock) {
                        Logs.Add(entry);
                        if (Logs.Count > 10000) Logs.RemoveAt(0);
                    }
                });
            };
            _engine.OnAnomalyDetected += (msg) => MessageBox.Show(msg);
        }
        public void Connect(ServerConfig config) => _engine.Start(config);
        public void TogglePause() => _engine.IsPaused = !_engine.IsPaused;
    }
}
