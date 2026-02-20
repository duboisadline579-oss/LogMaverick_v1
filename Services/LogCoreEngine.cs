using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Renci.SshNet;
using LogMaverick.Models;
namespace LogMaverick.Services {
    public class LogCoreEngine : IDisposable {
        private SshClient? _client;
        private ShellStream? _stream;
        public bool IsPaused { get; set; } = false;
        private Queue<LogEntry> _contextBuffer = new Queue<LogEntry>(500);
        public event Action<LogEntry>? OnLogReceived;
        public event Action<string>? OnAnomalyDetected;
        public void Start(ServerConfig config) {
            _client = new SshClient(config.Host, config.Port, config.Username, config.Password);
            _client.Connect();
            _stream = _client.CreateShellStream("MaverickCore", 0, 0, 0, 0, 1024);
            _stream.DataReceived += (s, e) => {
                if (IsPaused) return;
                var raw = Encoding.UTF8.GetString(e.Data);
                foreach (var line in raw.Split('\n')) {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var entry = Parse(line);
                    OnLogReceived?.Invoke(entry);
                }
            };
            _stream.WriteLine("tail -f " + config.LogPath);
        }
        private LogEntry Parse(string line) {
            var entry = new LogEntry { Message = line.Trim() };
            if (Regex.IsMatch(line, "ERROR|FAIL|Exception", RegexOptions.IgnoreCase)) {
                entry.Level = "ERROR"; entry.Color = "#FF5555";
                if (line.Contains("Critical")) OnAnomalyDetected?.Invoke("🔥 임계치 에러 감지!");
            } else if (line.Contains("WARN")) {
                entry.Level = "WARN"; entry.Color = "#FFB86C";
            }
            _contextBuffer.Enqueue(entry);
            if (_contextBuffer.Count > 500) _contextBuffer.Dequeue();
            return entry;
        }
        public List<LogEntry> GetContext() => new List<LogEntry>(_contextBuffer);
        public void Dispose() { _stream?.Dispose(); _client?.Disconnect(); _client?.Dispose(); }
    }
}
