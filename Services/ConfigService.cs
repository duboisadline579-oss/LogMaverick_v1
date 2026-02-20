using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public static class ConfigService {
        private static string Path = "server_configs.json";
        public static void Save(List<ServerConfig> c) => File.WriteAllText(Path, JsonConvert.SerializeObject(c));
        public static List<ServerConfig> Load() => File.Exists(Path) ? JsonConvert.DeserializeObject<List<ServerConfig>>(File.ReadAllText(Path)) : new();
    }
}
