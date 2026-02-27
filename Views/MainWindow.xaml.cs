using System;
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
            new ConfigWindow(VM.Servers, VM.AlertKeywords, VM.ExcludedTids) { Owner = this }.ShowDialog();
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
                MessageBox.Show($"연결 실패\n원인: {ex.Message}\n\n확인:\n• Host/IP\n• Port\n• Username/Password\n• SSH\n• 방화벽", "연결 실패", MessageBoxButton.OK, MessageBoxImage.Error);
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
        private void Log_DoubleClick(object sender, MouseButtonEventArgs e) {
            if ((sender as ListView)?.SelectedItem is LogEntry log)
                new LogDetailWindow(log) { Owner = this }.Show();
        }
        private void TraceTid_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                new TidTraceWindow(log.Tid) { Owner = this }.Show();
        }
        private void Copy_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                Clipboard.SetText(log.Message);
        }
        private void Bookmark_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                VM.ToggleBookmark(log);
        }
        private void Exclude_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                if (!VM.ExcludedTids.Contains(log.Tid)) { VM.ExcludedTids.Add(log.Tid); VM.StatusMessage = $"🚫 TID {log.Tid} 제외됨"; }
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
            var i1 = new MenuItem { Header = $"📂 {cat} 파일 지정" };
            i1.Click += async (s, ev) => {
                if (FileTree.SelectedItem is FileNode node && !node.IsDirectory)
                    await VM.ConnectSessionAsync(VM.SelectedServer, cat, node.FullPath);
                else VM.StatusMessage = "⚠ 파일트리에서 .log 파일을 먼저 선택하세요";
            };
            var i2 = new MenuItem { Header = $"⏹ {cat} 스트리밍 중지" };
            i2.Click += (s, ev) => { VM.StopSession(cat); VM.StatusMessage = $"⏹ {cat} 중지됨"; };
            menu.Items.Add(i1); menu.Items.Add(i2); menu.IsOpen = true;
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
            new ConfigWindow(VM.Servers, VM.AlertKeywords, VM.ExcludedTids) { Owner = this }.ShowDialog();
    }
}
