using System;
using System.Text;
using Newtonsoft.Json.Linq;
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
                    var raw = Encoding.UTF8.GetString(e.Data);
                    foreach(var line in raw.Split('\n')) {
                        if(string.IsNullOrWhiteSpace(line) || line.Contains("tail")) continue;
                        
                        var entry = new LogEntry { Message = line.Trim() };
                        try {
                            // JSON 파싱 시도 (요구사항: 테이블 형식)
                            var json = JObject.Parse(line);
                            entry.Source = json["source"]?.ToString() ?? "SYS";
                            entry.Tid = json["tid"]?.ToString() ?? "0000";
                            entry.Message = json["msg"]?.ToString() ?? line;
                        } catch { /* 일반 텍스트 로그 처리 */ }

                        if(line.ToUpper().Contains("ERROR")) entry.TextColor = "#FF6347";
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
