using System;
using System.Collections.Generic;
using System.Text;
using Renci.SshNet;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public class LogCoreEngine : IDisposable {
        private SshClient? _client;
        private ShellStream? _stream;
        public event Action<LogEntry>? OnLogReceived;

        public List<FileNode> GetFileTree(ServerConfig config) {
            var nodes = new List<FileNode>();
            try {
                using var client = new SshClient(config.Host, config.Username, config.Password);
                client.Connect();
                var cmd = client.RunCommand("find " + config.RootPath + " -maxdepth 2");
                foreach(var path in cmd.Result.Split('\n')) {
                    if(string.IsNullOrWhiteSpace(path)) continue;
                    nodes.Add(new FileNode { Name = System.IO.Path.GetFileName(path), FullPath = path });
                }
            } catch { }
            return nodes;
        }

        public void StartStreaming(ServerConfig config, string filePath) {
            _client?.Dispose();
            _client = new SshClient(config.Host, config.Username, config.Password);
            _client.Connect();
            _stream = _client.CreateShellStream("LogStream", 0,0,0,0,1024);
            _stream.DataReceived += (s, e) => {
                var raw = Encoding.UTF8.GetString(e.Data);
                string category = "MACHINE";
                if(filePath.ToLower().Contains("process")) category = "PROCESS";
                else if(filePath.ToLower().Contains("driver")) category = "DRIVER";
                
                OnLogReceived?.Invoke(new LogEntry { Message = raw.Trim(), Category = category, Tid = "TID-" + new Random().Next(1000, 9999) });
            };
            _stream.WriteLine("tail -F " + filePath);
        }
        public void Dispose() { _stream?.Dispose(); _client?.Dispose(); }
    }
}
