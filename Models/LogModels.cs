using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;

namespace LogMaverick.Models {
    public class ServerConfig {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Alias { get; set; } = "운영 서버";
        public string Host { get; set; } = "";
        public int Port { get; set; } = 22;
        public string Username { get; set; } = "";
        public string EncryptedPassword { get; set; } = "";
        public string LogPath { get; set; } = "/var/log/syslog";

        private static readonly byte[] Key = Encoding.UTF8.GetBytes("MaverickSecureKey32BytesLong!!!!");
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("MaverickIV16Byte");

        public void SetPassword(string pw) {
            using var aes = Aes.Create();
            var enc = aes.CreateEncryptor(Key, IV);
            var data = Encoding.UTF8.GetBytes(pw);
            EncryptedPassword = Convert.ToBase64String(enc.TransformFinalBlock(data, 0, data.Length));
        }
        public string GetPassword() {
            if (string.IsNullOrEmpty(EncryptedPassword)) return "";
            using var aes = Aes.Create();
            var dec = aes.CreateDecryptor(Key, IV);
            var data = Convert.FromBase64String(EncryptedPassword);
            return Encoding.UTF8.GetString(dec.TransformFinalBlock(data, 0, data.Length));
        }
    }
    public class LogEntry {
        public DateTime Time { get; set; } = DateTime.Now;
        public string Level { get; set; } = "INFO";
        public string Source { get; set; } = "SYS";
        public string Message { get; set; } = "";
        public string TextColor { get; set; } = "#DCDCDC";
    }
}
