using System.IO;
using System.Collections.Generic;
using System.Text;
using System;
using Newtonsoft.Json;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public class AppSettings {
        public List<ServerConfig> Servers { get; set; } = new();
        public List<string> ExcludedTids { get; set; } = new();
        public List<string> AlertKeywords { get; set; } = new();
        public List<string> FilterHistory { get; set; } = new();
        public Dictionary<string, string> LastFiles { get; set; } = new();
    }
    public static class ConfigService {
        private const string FileName = "configs.json";
        private static string Encrypt(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        private static string Decrypt(string s) { try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); } catch { return s; } }
        public static void Save(AppSettings settings) {
            foreach (var c in settings.Servers) c.Password = Encrypt(c.Password);
            File.WriteAllText(FileName, JsonConvert.SerializeObject(settings, Formatting.Indented));
            foreach (var c in settings.Servers) c.Password = Decrypt(c.Password);
        }
        public static AppSettings Load() {
            if (!File.Exists(FileName)) return new AppSettings();
            try {
                var s = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(FileName)) ?? new AppSettings();
                foreach (var c in s.Servers) c.Password = Decrypt(c.Password);
                return s;
            } catch { return new AppSettings(); }
        }
    }
}
