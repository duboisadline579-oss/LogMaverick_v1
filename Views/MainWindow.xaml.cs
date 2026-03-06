using System;
using System.Threading.Tasks;
using System.Windows.Data;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using LogMaverick.Models;
using LogMaverick.Services;
using LogMaverick.ViewModels;

namespace LogMaverick.Views {
    public partial class MainWindow : Window {
        private MainViewModel VM => (MainViewModel)DataContext;
        private bool _leftPanelVisible = true;
        public MainWindow() { InitializeComponent(); this.DataContext = new MainViewModel(); }
        private void Config_Click(object sender, RoutedEventArgs e) =>
            new ConfigWindow(VM.Servers) { Owner = this }.ShowDialog();
        private async void Connect_Click(object sender, RoutedEventArgs e) {
            if (VM.IsConnected) {
                VM.Disconnect(); FileTree.ItemsSource = null;
                TxtTreeSearch.Text = ""; VM.SearchTree("");
                TxtFileGuide.Text = "📄 파일을 선택하면 경로가 표시됩니다"; return;
            }
            if (VM.SelectedServer == null) { VM.StatusMessage = "⚠ 서버를 먼저 선택하세요"; return; }
            VM.IsLoading = true;
            try {
                var server = VM.SelectedServer;
                var tree = await System.Threading.Tasks.Task.Run(() => FileService.GetRemoteTree(server));
                VM.SetTree(tree); FileTree.ItemsSource = VM.FilteredTree;
                VM.IsConnected = true; VM.IsLoading = false;
                VM.StatusMessage = "✅ 연결됨 — 📁 파일을 더블클릭하세요";
            } catch (Exception ex) {
                VM.IsLoading = false; VM.IsConnected = false;
                VM.StatusMessage = $"❌ 연결 실패: {ex.Message}";
                MessageBox.Show($"연결 실패\n원인: {ex.Message}", "연결 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Hide_Click(object sender, RoutedEventArgs e) {
            _leftPanelVisible = !_leftPanelVisible;
            LeftCol.Width = _leftPanelVisible ? new GridLength(300) : new GridLength(0);
            BtnHide.Content = _leftPanelVisible ? "◀" : "▶";
            BtnShowPanel.Visibility = _leftPanelVisible ? Visibility.Collapsed : Visibility.Visible;
        }
        private void TreeSearch_Changed(object sender, TextChangedEventArgs e) {
            string q = TxtTreeSearch.Text.Trim();
            TreeSearchHint.Visibility = string.IsNullOrEmpty(q) ? Visibility.Visible : Visibility.Collapsed;
            VM.SearchTree(q);
        }
        private void ClearTreeSearch_Click(object sender, RoutedEventArgs e) {
            TxtTreeSearch.Text = ""; TreeSearchHint.Visibility = Visibility.Visible; VM.SearchTree("");
        }
        private void FileTree_RightClick(object sender, MouseButtonEventArgs e) {
            var menu = new ContextMenu();
            var expand = new MenuItem { Header = "▶ 모두 펴기" };
            expand.Click += (s, ev) => SetAllExpanded(FileTree.Items, true);
            var collapse = new MenuItem { Header = "◀ 모두 접기" };
            collapse.Click += (s, ev) => SetAllExpanded(FileTree.Items, false);
            menu.Items.Add(expand); menu.Items.Add(collapse);
            menu.IsOpen = true; e.Handled = true;
        }
        private void SetAllExpanded(System.Windows.Controls.ItemCollection items, bool expanded) {
            foreach (var item in items) {
                var container = FileTree.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (container != null) { container.IsExpanded = expanded; SetAllExpandedItem(container, expanded); }
            }
        }
        private void SetAllExpandedItem(TreeViewItem parent, bool expanded) {
            foreach (var item in parent.Items) {
                var container = parent.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (container != null) { container.IsExpanded = expanded; SetAllExpandedItem(container, expanded); }
            }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e) {
            if (!VM.IsConnected) { VM.StatusMessage = "⚠ 먼저 CONNECT로 연결하세요"; return; }
            try {
                var t = FileService.GetRemoteTree(VM.SelectedServer);
                VM.SetTree(t); FileTree.ItemsSource = VM.FilteredTree;
                VM.StatusMessage = "🔄 파일 목록 새로고침 완료";
            } catch (Exception ex) { VM.StatusMessage = $"❌ 새로고침 실패: {ex.Message}"; }
        }
        private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            if (FileTree.SelectedItem is FileNode node && !node.IsDirectory)
                TxtFileGuide.Text = $"📄 {node.FullPath} — 더블클릭하여 스트리밍 시작";
            else if (FileTree.SelectedItem is FileNode dir && dir.IsDirectory)
                TxtFileGuide.Text = $"📁 {dir.FullPath} — 더블클릭: 최신 파일 자동 선택";
        }
        private async void File_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (FileTree.SelectedItem is not FileNode node) return;
            if (VM.SelectedServer == null) { VM.StatusMessage = "⚠ 서버가 선택되지 않았습니다"; return; }
            FileNode target = node;
            if (node.IsDirectory) {
                var latest = node.Children.Where(c => !c.IsDirectory).OrderByDescending(c => c.Name).FirstOrDefault();
                if (latest == null) { VM.StatusMessage = "⚠ 폴더에 파일이 없습니다"; return; }
                target = latest;
            }
            string fn = System.IO.Path.GetFileName(target.FullPath).ToLower();
            string dp = target.FullPath.ToLower();
            string cat = (fn.Contains("machine")||dp.Contains("machine")) ? "MACHINE"
                       : (fn.Contains("process")||dp.Contains("process")) ? "PROCESS"
                       : (fn.Contains("driver")||dp.Contains("driver"))  ? "DRIVER" : "OTHERS";
            if (VM.SessionFiles.TryGetValue(cat, out var cur) && cur == target.FullPath) {
                VM.StopSession(cat); VM.StatusMessage = $"⏹ {cat} 스트리밍 중지됨"; return;
            }
            try {
                VM.StatusMessage = $"🔄 {cat} 스트리밍 시작: {target.Name}...";
                await VM.ConnectSessionAsync(VM.SelectedServer, cat, target.FullPath);
            } catch (Exception ex) { VM.StatusMessage = $"❌ 실패: {ex.Message}"; }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { VM.SaveSettings(); VM.Disconnect(); }
        private void Pause_Click(object sender, RoutedEventArgs e) => VM.IsPaused = !VM.IsPaused;
        private void AutoScroll_Click(object sender, RoutedEventArgs e) => VM.AutoScroll = !VM.AutoScroll;
        private void Clear_Click(object sender, RoutedEventArgs e) => VM.ClearAll();
        private void ClearErrors_Click(object sender, RoutedEventArgs e) { VM.ErrorHistory.Clear(); VM.ResetErrors(); }
        private void ClearFilter_Click(object sender, RoutedEventArgs e) => VM.FilterText = "";
        private void Filter_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(VM.FilterText)) VM.AddFilterHistory(VM.FilterText);
        }
        private void Level_Click(object sender, RoutedEventArgs e) {
            string tag = (sender as FrameworkElement)?.Tag?.ToString() ?? "ALL";
            VM.LevelFilter = tag; VM.StatusMessage = $"🔍 레벨 필터: {tag}";
        }
        private void Export_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedItem is TabItem tab) VM.ExportLogs(tab.Header?.ToString() ?? "");
        }
        private void ExportAll_Click(object sender, RoutedEventArgs e) => VM.ExportAll();
        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (MainTabs?.SelectedItem is TabItem tab && tab?.Header != null)
                try { VM.ResetTab(tab.Header.ToString()); } catch { }
        }
        private void Log_DoubleClick(object sender, MouseButtonEventArgs e) {
            if ((sender as ListView)?.SelectedItem is LogEntry log)
                new LogDetailWindow(log) { Owner = this }.Show(); this.Activate();
        }
        private void TraceTid_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                new TidTraceWindow(log.Tid) { Owner = this }.Show(); this.Activate();
        }
        private void Copy_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                Clipboard.SetText(log.Message);
        }
        private void Bookmark_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                VM.ToggleBookmark(log);
        }
        private void ShowBookmarks_Click(object sender, RoutedEventArgs e) =>
            new BookmarkWindow(VM.BookmarkedLogs) { Owner = this }.Show();
        private void Exclude_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log) {
                if (!VM.ExcludedTids.Contains(log.Tid)) {
                    VM.ExcludedTids.Add(log.Tid);
                    VM.RemoveLogsByTid(log.Tid);
                    VM.StatusMessage = $"🚫 TID {log.Tid} 제외됨 (기존 로그 제거)";
                }
            }
        }
        private void LogList_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e) {
            if (VM.AutoScroll && !VM.IsPaused && sender is ListView lv && lv.Items.Count > 0)
                lv.ScrollIntoView(lv.Items[0]);
        }
        private async void Tab_RightClick(object sender, MouseButtonEventArgs e) {
            if (!VM.IsConnected) { VM.StatusMessage = "⚠ 먼저 CONNECT로 연결하세요"; return; }
            var tab = MainTabs.SelectedItem as TabItem; if (tab == null) return;
            string cat = tab.Tag?.ToString() ?? "MACHINE";
            var menu = new ContextMenu();
            var i1 = new MenuItem { Header = $"📂 {cat} 파일 지정 (파일트리에서 선택 후)" };
            i1.Click += async (s, ev) => {
                if (FileTree.SelectedItem is FileNode node && !node.IsDirectory)
                    await VM.ConnectSessionAsync(VM.SelectedServer, cat, node.FullPath);
                else VM.StatusMessage = "⚠ 파일트리에서 .log 파일을 먼저 선택하세요";
            };
            var i2 = new MenuItem { Header = $"⏹ {cat} 스트리밍 중지" };
            i2.Click += (s, ev) => { VM.StopSession(cat); VM.StatusMessage = $"⏹ {cat} 중지됨"; };
            var sep = new Separator();
            var i3 = new MenuItem { Header = "🔖 북마크 목록 보기" };
            i3.Click += (s, ev) => new BookmarkWindow(VM.BookmarkedLogs) { Owner = this }.Show();
            menu.Items.Add(i1); menu.Items.Add(i2); menu.Items.Add(sep); menu.Items.Add(i3);
            menu.IsOpen = true;
        }
        private void Header_RightClick(object sender, MouseButtonEventArgs e) {
            if (MainTabs.SelectedContent is not ListView lv) return;
            if (lv.View is not GridView gv) return;
            var cols = new List<(string, GridViewColumn)>();
            string[] names = { "Time", "TID", "Type", "Category", "Message" };
            for (int i = 0; i < gv.Columns.Count && i < names.Length; i++)
                cols.Add((names[i], gv.Columns[i]));
            new ColumnManagerWindow(cols) { Owner = this }.ShowDialog();
        }
        private void Backup_Click(object sender, RoutedEventArgs e) {
            try {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, $"backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");
                ConfigService.Backup(path); VM.StatusMessage = $"✅ 백업 완료: {path}";
            } catch (Exception ex) { MessageBox.Show("백업 실패: " + ex.Message); }
        }
        private void Restore_Click(object sender, RoutedEventArgs e) {
            var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "JSON|*.json", Title = "설정 파일 선택" };
            if (dlg.ShowDialog() == true) {
                try { ConfigService.Restore(dlg.FileName); VM.StatusMessage = "✅ 복원 완료 — 재시작 필요"; }
                catch (Exception ex) { MessageBox.Show("복원 실패: " + ex.Message); }
            }
        }
        private void ManageColumns_Click(object sender, RoutedEventArgs e) => Header_RightClick(sender, null);
        private void ErrorBox_Click(object sender, RoutedEventArgs e) {
            VM.ResetErrors();
            new ErrorWindow(VM.ErrorHistory, VM.AlertKeywords) { Owner = this }.Show();
        }
        private void ConfigException_Click(object sender, RoutedEventArgs e) =>
            new ConfigWindow(VM.Servers) { Owner = this }.ShowDialog();
    }
}
