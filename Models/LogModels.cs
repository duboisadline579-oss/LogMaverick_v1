using System;
namespace LogMaverick.Models {
    public class ServerConfig {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Alias { get; set; } = "운영 서버";
        public string Host { get; set; } = "";
        public int Port { get; set; } = 22;
        public string Username { get; set; } = "";
        public string Password { get; set; } = ""; 
        public string LogPath { get; set; } = "/var/log/syslog";
    }
    public class LogEntry {
        public DateTime Time { get; set; } = DateTime.Now;
        public string Message { get; set; } = "";
        public string Level { get; set; } = "INFO";
        public string Color { get; set; } = "#F8F8F2";
        public string TransactionId { get; set; } = "";
    }
}
