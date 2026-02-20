using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LogMaverick.Models {
    public class ServerConfig : INotifyPropertyChanged {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Alias { get; set; } = "운영 서버";
        public string Host { get; set; } = "";
        public string Username { get; set; } = "";
        public string Password { get; set; } = ""; 
        public string RootPath { get; set; } = "/var/log";
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class LogEntry {
        public DateTime Time { get; set; } = DateTime.Now;
        public string Level { get; set; } = "INFO";
        public string Source { get; set; } = "SYS";
        public string Tid { get; set; } = "0000";
        public string Message { get; set; } = "";
        public string Category { get; set; } = "MACHINE"; // 탭 구분용
        public string TextColor { get; set; } = "#DCDCDC";
    }

    public class FileNode {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsDirectory { get; set; }
        public List<FileNode> Children { get; set; } = new List<FileNode>();
    }
}
