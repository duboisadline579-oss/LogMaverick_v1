using System;
using System.Text;
using Renci.SshNet;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public class LogCoreEngine : IDisposable {
        private SshClient? _client;
        private ShellStream? _stream;
        public event Action<LogEntry>? OnLogReceived;
        public event Action<string, string>? OnStatusChanged;

        public void Connect(ServerConfig config) {
            try {
                OnStatusChanged?.Invoke("CONNECTING", "#FFB86C");
                _client = new SshClient(config.Host, config.Port, config.Username, config.GetPassword());
                _client.Connect();
                _stream = _client.CreateShellStream("Mav", 0, 0, 0, 0, 1024);
                _stream.DataReceived += (s, e) => {
                    var lines = Encoding.UTF8.GetString(e.Data).Split('\n');
                    foreach(var l in lines) {
                        if(string.IsNullOrWhiteSpace(l)) continue;
                        var entry = new LogEntry { Message = l.Trim() };
                        if(l.Contains("ERROR")) { entry.Level="ERR"; entry.TextColor="#FF6347"; }
                        OnLogReceived?.Invoke(entry);
                    }
                };
                _stream.WriteLine("tail -F " + config.LogPath);
                OnStatusChanged?.Invoke("CONNECTED", "#007AFF");
            } catch { OnStatusChanged?.Invoke("FAILED", "#FF0000"); }
        }
        public void Dispose() { _stream?.Dispose(); _client?.Dispose(); }
    }
}
