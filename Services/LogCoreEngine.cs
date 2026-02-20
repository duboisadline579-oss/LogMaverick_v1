using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Renci.SshNet;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public class LogCoreEngine : IDisposable {
        private SshClient? _client;
        private ShellStream? _stream;
        public event Action<LogEntry>? OnLogReceived;

        // TID 패턴 추출 (예: [1234] 또는 TID:1234 또는 T-1234 등 검색)
        private string ExtractTid(string message) {
            var match = Regex.Match(message, @"(?i)(TID[:\s-]*|\[)(\d+)\]?");
            return match.Success ? match.Groups[2].Value : "0000";
        }
        public List<FileNode> GetFileTree(ServerConfig config) {
            var nodes = new List<FileNode>();
            try {
                using var client = new SshClient(config.Host, config.Username, config.Password);
                client.Connect();
                // .log 파일만 2단계 깊이까지 검색
                var cmd = client.RunCommand($"find {config.RootPath} -maxdepth 2 -name \"*.log\"");
                foreach(var path in cmd.Result.Split('\n')) {
                    if(string.IsNullOrWhiteSpace(path)) continue;
                    nodes.Add(new FileNode { 
                        Name = System.IO.Path.GetFileName(path), 
                        FullPath = path, 
                        IsDirectory = false 
                    });
                }
            } catch (Exception ex) {
                Console.WriteLine($"Tree Error: {ex.Message}");
            }
            return nodes;
        }
        public void StartStreaming(ServerConfig config, string filePath) {
            _client?.Dispose();
            _client = new SshClient(config.Host, config.Username, config.Password);
            _client.Connect();
            
            _stream = _client.CreateShellStream("LogMaverickStream", 0, 0, 0, 0, 1024);
            _stream.DataReceived += (s, e) => {
                var raw = Encoding.UTF8.GetString(e.Data);
                foreach (var line in raw.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)) {
                    string upperLine = line.ToUpper();
                    
                    // 1. 에러 타입 판별
                    LogType type = LogType.System;
                    if (upperLine.Contains("ERROR") || upperLine.Contains("FAIL")) type = LogType.Error;
                    else if (upperLine.Contains("EXCEPTION") || upperLine.Contains("CRITICAL")) type = LogType.Exception;

                    // 2. 카테고리 판별 (경로 기반)
                    string category = "OTHERS";
                    string lowerPath = filePath.ToLower();
                    if (lowerPath.Contains("machine")) category = "MACHINE";
                    else if (lowerPath.Contains("process")) category = "PROCESS";
                    else if (lowerPath.Contains("driver")) category = "DRIVER";

                    OnLogReceived?.Invoke(new LogEntry {
                        Time = DateTime.Now,
                        Message = line.Trim(),
                        Category = category,
                        Type = type,
                        Tid = ExtractTid(line)
                    });
                }
            };
            _stream.WriteLine($"tail -F {filePath}");
        }
        public void Dispose() { _stream?.Dispose(); _client?.Dispose(); }
    }
}
