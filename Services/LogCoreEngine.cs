using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Renci.SshNet;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public class LogCoreEngine : IDisposable {
        private SshClient? _client;
        private ShellStream? _stream;
        private CancellationTokenSource? _cts;
        public event Action<LogEntry>? OnLogReceived;
        public event Action<string>? OnStatusChanged;
        private readonly Regex _tidRegex = new Regex(@"(?i)(?:TID[:\s-]*|\[|ID:)(\d+)", RegexOptions.Compiled);

        private string ExtractTid(string message) {
            var match = _tidRegex.Match(message);
            return match.Success ? match.Groups[1].Value : "0000";
        }
        public List<FileNode> GetFileTree(ServerConfig config) {
            var nodes = new List<FileNode>();
            try {
                using var client = new SshClient(config.Host, config.Port, config.Username, config.Password);
                client.Connect();
                var cmd = client.RunCommand($"find {config.RootPath} -maxdepth 2 -name \"*.log\"");
                foreach(var path in cmd.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
                    nodes.Add(new FileNode { Name = System.IO.Path.GetFileName(path), FullPath = path.Trim() });
                }
            } catch (Exception ex) { OnStatusChanged?.Invoke("Tree Error: " + ex.Message); }
            return nodes;
        }

        public async Task StartStreamingAsync(ServerConfig config, string filePath) {
            Dispose();
            _cts = new CancellationTokenSource();
            await Task.Run(() => {
                try {
                    _client = new SshClient(config.Host, config.Port, config.Username, config.Password);
                    _client.KeepAliveInterval = TimeSpan.FromSeconds(30);
                    _client.Connect();
                    _stream = _client.CreateShellStream("LogStream", 0, 0, 0, 0, 4096);
                    _stream.WriteLine($"tail -n 100 -F {filePath}");
                    while (_client.IsConnected && !_cts.Token.IsCancellationRequested) {
                        if (_stream.DataAvailable) ProcessRawData(_stream.Read(), filePath);
                        Thread.Sleep(50);
                    }
                } catch (Exception ex) { OnStatusChanged?.Invoke("Stream Error: " + ex.Message); }
            }, _cts.Token);
        }
        private void ProcessRawData(string data, string filePath) {
            var lines = data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines) {
                if (string.IsNullOrWhiteSpace(line) || line.Contains("tail -n")) continue;
                string up = line.ToUpper();
                LogType type = up.Contains("ERROR") || up.Contains("FAIL") ? LogType.Error : (up.Contains("EXCEPTION") ? LogType.Exception : LogType.System);
                string cat = filePath.ToLower().Contains("machine") ? "MACHINE" : (filePath.ToLower().Contains("process") ? "PROCESS" : (filePath.ToLower().Contains("driver") ? "DRIVER" : "OTHERS"));
                OnLogReceived?.Invoke(new LogEntry { Time = DateTime.Now, Message = line.Trim(), Category = cat, Type = type, Tid = ExtractTid(line) });
            }
        }
        public void Dispose() { _cts?.Cancel(); _stream?.Dispose(); _client?.Disconnect(); _client?.Dispose(); }
    }
}
