using System;
using System.Collections.Generic;
using System.Linq;
using Renci.SshNet;
using LogMaverick.Models;

namespace LogMaverick.Services {
    public static class FileService {
        public static List<FileNode> GetRemoteTree(ServerConfig config) {
            var root = new FileNode { Name = config.RootPath, FullPath = config.RootPath, IsDirectory = true };
            try {
                using var client = new SshClient(config.Host, config.Port, config.Username, config.Password);
                client.Connect();
                var cmd = client.RunCommand($"find {config.RootPath} -maxdepth 4 -name \"*.log\" | sort");
                foreach (var path in cmd.Result.Split('\n', StringSplitOptions.RemoveEmptyEntries)) {
                    var trimmed = path.Trim();
                    var parts = trimmed.Substring(config.RootPath.Length).TrimStart('/').Split('/');
                    InsertNode(root, config.RootPath, parts, 0, trimmed);
                }
            } catch (Exception ex) { Console.WriteLine("Tree Error: " + ex.Message); }
            return new List<FileNode> { root };
        }
        private static void InsertNode(FileNode parent, string currentPath, string[] parts, int idx, string fullPath) {
            if (idx >= parts.Length) return;
            string part = parts[idx];
            bool isLast = idx == parts.Length - 1;
            string nodePath = currentPath.TrimEnd('/') + "/" + part;
            var existing = parent.Children.FirstOrDefault(c => c.Name == part);
            if (existing == null) {
                existing = new FileNode { Name = part, FullPath = nodePath, IsDirectory = !isLast };
                parent.Children.Add(existing);
            }
            if (!isLast) InsertNode(existing, nodePath, parts, idx + 1, fullPath);
            else existing.FullPath = fullPath;
        }
    }
}
