using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public static class ConfigService {
        private const string FileName = "configs.json";
        public static void Save(List<ServerConfig> configs) {
            File.WriteAllText(FileName, JsonConvert.SerializeObject(configs, Formatting.Indented));
        }
        public static List<ServerConfig> Load() {
            if (!File.Exists(FileName)) return new List<ServerConfig>();
            return JsonConvert.DeserializeObject<List<ServerConfig>>(File.ReadAllText(FileName)) ?? new List<ServerConfig>();
        }
    }
}
