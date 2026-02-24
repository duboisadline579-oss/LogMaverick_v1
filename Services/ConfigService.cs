using System.IO;
using System.Collections.Generic;
using System.Text;
using System;
using Newtonsoft.Json;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public static class ConfigService {
        private const string FileName = "configs.json";
        private static string Encrypt(string s) => Convert.ToBase64String(Encoding.UTF8.GetBytes(s));
        private static string Decrypt(string s) { try { return Encoding.UTF8.GetString(Convert.FromBase64String(s)); } catch { return s; } }
        public static void Save(List<ServerConfig> configs) {
            foreach (var c in configs) c.Password = Encrypt(c.Password);
            File.WriteAllText(FileName, JsonConvert.SerializeObject(configs, Formatting.Indented));
            foreach (var c in configs) c.Password = Decrypt(c.Password);
        }
        public static List<ServerConfig> Load() {
            if (!File.Exists(FileName)) return new List<ServerConfig>();
            var list = JsonConvert.DeserializeObject<List<ServerConfig>>(File.ReadAllText(FileName)) ?? new List<ServerConfig>();
            foreach (var c in list) c.Password = Decrypt(c.Password);
            return list;
        }
    }
}
