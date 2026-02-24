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
                    var dir = System.IO.Path.GetDirectoryName(trimmed) ?? "";
                    var file = System.IO.Path.GetFileName(trimmed);
                    if (!roots.ContainsKey(dir))
                        roots[dir] = new FileNode { Name = System.IO.Path.GetFileName(dir), FullPath = dir, IsDirectory = true };
                    roots[dir].Children.Add(new FileNode { Name = file, FullPath = trimmed, IsDirectory = false });
                }
            } catch (Exception ex) { Console.WriteLine("Tree Error: " + ex.Message); }
            return new List<FileNode>(roots.Values);
        }
    }
}
