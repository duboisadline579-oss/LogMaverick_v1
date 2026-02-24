using System.Collections.Generic;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public static class FileService {
        public static List<FileNode> GetRemoteTree(ServerConfig config) {
            var engine = new LogCoreEngine();
            return engine.GetFileTree(config);
        }
    }
}
