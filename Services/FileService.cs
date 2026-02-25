using System;
using System.Collections.Generic;
using Renci.SshNet;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public static class FileService {
        public static List<FileNode> GetRemoteTree(ServerConfig config) {
            var roots = new Dictionary<string, FileNode>();
            try {
                using var client = new SshClient(config.Host, config.Port, config.Username, config.Password);
                client.Connect();
                var cmd = client.RunCommand($"find {config.RootPath} -maxdepth 3 -name \"*.log\" | sort");
                foreach (var path in cmd.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
                    var trimmed = path.Trim();
                    int lastSlash = trimmed.LastIndexOf('/');
                    string dir = lastSlash > 0 ? trimmed.Substring(0, lastSlash) : config.RootPath;
                    string file = lastSlash > 0 ? trimmed.Substring(lastSlash + 1) : trimmed;
                    string relDir = dir.StartsWith(config.RootPath) ? dir.Substring(config.RootPath.Length).TrimStart('/') : dir;
                    string dirLabel = string.IsNullOrEmpty(relDir) ? "/" : "/" + relDir;
                    if (!roots.ContainsKey(dir))
                        roots[dir] = new FileNode { Name = dirLabel, FullPath = dir, IsDirectory = true };
                    roots[dir].Children.Add(new FileNode { Name = file, FullPath = trimmed, IsDirectory = false });
                }
            } catch (Exception ex) { Console.WriteLine("Tree Error: " + ex.Message); }
            return new List<FileNode>(roots.Values);
        }
    }
}
