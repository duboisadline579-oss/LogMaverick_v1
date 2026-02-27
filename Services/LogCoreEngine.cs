using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public class StreamSession : IDisposable {
        public string Category { get; }
        public string FilePath { get; }
        private SshClient? _client;
        private ShellStream? _stream;
        private CancellationTokenSource? _cts;
        private bool _autoReconnect = true;
        private int _reconnectAttempts = 0;
        private long _lastOffset = 0;
        private const int MaxRetry = 5;
        public event Action<LogEntry>? OnLogReceived;
        public event Action<string>? OnStatusChanged;
        private readonly Regex _tidRegex = new Regex(@"(?i)(?:TID[:\s-]*|\[|ID:)(\d+)", RegexOptions.Compiled);
        private static readonly Regex _timeRegex = new Regex(@"(\d{4}[\/-]\d{2}[\/-]\d{2}[T ]\d{2}:\d{2}:\d{2})", RegexOptions.Compiled);
        private static readonly Regex _jsonTidRegex = new Regex(@"""[Tt]id""\s*:\s*""?([\w\d]+)""?", RegexOptions.Compiled);
        public StreamSession(string category, string filePath) { Category = category; FilePath = filePath; }
        private string ExtractTid(string msg) { var m = _tidRegex.Match(msg); return m.Success ? m.Groups[1].Value : "0000"; }
        public async Task StartAsync(ServerConfig config) { _autoReconnect = true; _reconnectAttempts = 0; await ConnectAndStream(config); }
        private async Task ConnectAndStream(ServerConfig config) {
            _cts = new CancellationTokenSource();
            await Task.Run(async () => {
                try {
                    _client = new SshClient(config.Host, config.Port, config.Username, config.Password);
                    _client.KeepAliveInterval = TimeSpan.FromSeconds(15);
                    _client.Connect();
                    if (!_client.IsConnected) throw new Exception("SSH 연결 실패");
                    _reconnectAttempts = 0;
                    OnStatusChanged?.Invoke($"✅ [{Category}] 연결됨: {FilePath}");
                    while (!_cts.Token.IsCancellationRequested) {
                        if (_client?.IsConnected != true) break;
                        try {
                            var sizeCmd = _client.RunCommand($"wc -c < {FilePath}");
                            long fileSize = long.TryParse(sizeCmd.Result.Trim(), out var sz) ? sz : 0;
                            if (fileSize < _lastOffset) { _lastOffset = 0; OnStatusChanged?.Invoke($"🔄 [{Category}] 파일 교체 감지, 재시작"); }
                            if (fileSize > _lastOffset) {
                                var readCmd = _client.RunCommand($"tail -c +{_lastOffset + 1} {FilePath}");
                                if (!string.IsNullOrEmpty(readCmd.Result)) { ProcessData(readCmd.Result); _lastOffset = fileSize; }
                            }
                        } catch { }
                        await Task.Delay(500, _cts.Token);
                    }
                } catch (Exception ex) {
                    if (_autoReconnect && _reconnectAttempts < MaxRetry) {
                        _reconnectAttempts++;
                        OnStatusChanged?.Invoke($"❌ [{Category}] {ex.Message} 재연결 {_reconnectAttempts}/{MaxRetry}");
                        await Task.Delay(3000);
                        await ConnectAndStream(config);
                    } else { OnStatusChanged?.Invoke($"❌ [{Category}] 연결 실패: {ex.Message}"); }
                }
            }, _cts.Token);
        }
        private void ProcessData(string data) {
            foreach (var line in data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) {
                if (string.IsNullOrWhiteSpace(line) || line.Contains("tail -")) continue;
                string up = line.ToUpper();
                LogType type = up.Contains("EXCEPTION") ? LogType.Exception :
                    up.Contains("CRITICAL") || up.Contains("FATAL") ? LogType.Critical :
                    up.Contains("ERROR") || up.Contains("FAIL") ? LogType.Error :
                    up.Contains("WARN") || up.Contains("WARNING") ? LogType.Warn :
                    up.Contains("INFO") ? LogType.Info : LogType.System;
                var t = _timeRegex.Match(line);
                DateTime logTime = t.Success && DateTime.TryParse(t.Groups[1].Value, out var dt) ? dt : DateTime.Now;
                string tid = _jsonTidRegex.Match(line) is var jm && jm.Success ? jm.Groups[1].Value : ExtractTid(line);
                OnLogReceived?.Invoke(new LogEntry { Time = logTime, Message = line.Trim(), Category = Category, Type = type, Tid = tid });
            }
        }
        public void ResumeFromOffset() { }
        public void Dispose() { _autoReconnect = false; _cts?.Cancel(); _client?.Disconnect(); _client?.Dispose(); }
    }
    public class LogCoreEngine : IDisposable {
        private readonly Dictionary<string, StreamSession> _sessions = new();
        public event Action<LogEntry>? OnLogReceived;
        public event Action<string>? OnStatusChanged;
        public async Task StartSessionAsync(ServerConfig config, string category, string filePath) {
            if (_sessions.TryGetValue(category, out var existing)) { existing.Dispose(); _sessions.Remove(category); }
            var session = new StreamSession(category, filePath);
            session.OnLogReceived += (log) => OnLogReceived?.Invoke(log);
            session.OnStatusChanged += (s) => OnStatusChanged?.Invoke(s);
            _sessions[category] = session;
            await session.StartAsync(config);
        }
        public void StopSession(string category) { if (_sessions.TryGetValue(category, out var s)) { s.Dispose(); _sessions.Remove(category); } }
        public bool HasSession(string category) => _sessions.ContainsKey(category);
        public void Dispose() { foreach (var s in _sessions.Values) s.Dispose(); _sessions.Clear(); }
    }
}
