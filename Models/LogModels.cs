using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace LogMaverick.Models {
    public enum LogType { System, Error, Exception, Critical }
    
    public class ServerConfig : INotifyPropertyChanged {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Alias { get; set; } = "New Server";
        public string Host { get; set; } = "";
        public int Port { get; set; } = 22;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string RootPath { get; set; } = "/var/log";
        public event PropertyChangedEventHandler? PropertyChanged;
    }

    public class LogEntry {
        public DateTime Time { get; set; } = DateTime.Now;
        public string Tid { get; set; } = "0000";
        public string Message { get; set; } = "";
        public string Category { get; set; } = "OTHERS";
        public LogType Type { get; set; } = LogType.System;
        public bool IsHighlighted { get; set; } = false;
        public string Color => IsHighlighted ? "#FFD700" : (Type == LogType.Error ? "#FF4500" : (Type == LogType.Exception ? "#FF00FF" : "#DCDCDC"));
    }

    public class FileNode {
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public bool IsDirectory { get; set; }
    }
}
