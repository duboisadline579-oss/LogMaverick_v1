using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LogMaverick.Models;
using LogMaverick.Services;
using LogMaverick.ViewModels;

namespace LogMaverick.Views {
    public partial class MainWindow : Window {
        private MainViewModel VM => (MainViewModel)DataContext;
        public MainWindow() { InitializeComponent(); this.DataContext = new MainViewModel(); }
        private void Config_Click(object sender, RoutedEventArgs e) =>
            new ConfigWindow(VM.Servers, VM.AlertKeywords) { Owner = this }.ShowDialog();
        private async void Connect_Click(object sender, RoutedEventArgs e) {
            if (VM.IsConnected) {
                VM.Disconnect(); FileTree.ItemsSource = null;
                TxtFileGuide.Text = "📄 파일을 선택하면 경로가 표시됩니다"; return;
            }
            if (VM.SelectedServer == null) { VM.StatusMessage = "⚠ 서버를 먼저 선택하세요"; return; }
            VM.IsLoading = true;
            try {
                var server = VM.SelectedServer;
                var tree = await System.Threading.Tasks.Task.Run(() => FileService.GetRemoteTree(server));
                FileTree.ItemsSource = tree;
                VM.IsConnected = true; VM.IsLoading = false;
                VM.StatusMessage = "✅ 연결됨 — 📁 파일을 더블클릭하세요";
            } catch (Exception ex) {
                VM.IsLoading = false; VM.IsConnected = false;
                VM.StatusMessage = $"❌ 연결 실패: {ex.Message}";
                MessageBox.Show($"연결 실패\n\n원인: {ex.Message}\n\n확인:\n• Host/IP\n• Port (기본 22)\n• Username/Password\n• SSH 서비스 실행 여부\n• 방화벽 정책", "연결 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e) {
            if (!VM.IsConnected) { VM.StatusMessage = "⚠ 먼저 CONNECT 버튼으로 연결하세요"; return; }
            try {
                FileTree.ItemsSource = FileService.GetRemoteTree(VM.SelectedServer);
                VM.StatusMessage = "🔄 파일 목록 새로고침 완료";
            } catch (Exception ex) { VM.StatusMessage = $"❌ 새로고침 실패: {ex.Message}"; }
        }
        private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            if (FileTree.SelectedItem is FileNode node && !node.IsDirectory)
                TxtFileGuide.Text = $"📄 {node.FullPath} — 더블클릭하여 스트리밍 시작";
            else if (FileTree.SelectedItem is FileNode dir && dir.IsDirectory)
                TxtFileGuide.Text = $"📁 {dir.FullPath}";
        }
        private async void File_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (FileTree.SelectedItem is FileNode node && !node.IsDirectory) {
                if (VM.SelectedServer == null) { VM.StatusMessage = "⚠ 서버가 선택되지 않았습니다"; return; }
                try {
                    VM.StatusMessage = $"🔄 스트리밍 시작: {node.Name}...";
                    await VM.ConnectAsync(VM.SelectedServer, node.FullPath);
                } catch (Exception ex) {
                    VM.StatusMessage = $"❌ 스트리밍 실패: {ex.Message}";
                    MessageBox.Show($"파일 스트리밍 실패\n\n파일: {node.FullPath}\n원인: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { VM.SaveSettings(); VM.Disconnect(); }
        private void Pause_Click(object sender, RoutedEventArgs e) => VM.IsPaused = !VM.IsPaused;
        private void AutoScroll_Click(object sender, RoutedEventArgs e) => VM.AutoScroll = !VM.AutoScroll;
        private void Clear_Click(object sender, RoutedEventArgs e) => VM.ClearAll();
        private void ClearErrors_Click(object sender, RoutedEventArgs e) { VM.ErrorHistory.Clear(); VM.ResetErrors(); }
        private void ClearFilter_Click(object sender, RoutedEventArgs e) => VM.FilterText = "";
        private void Filter_KeyDown(object sender, KeyEventArgs e) {
            if (e.Key == Key.Enter && !string.IsNullOrEmpty(VM.FilterText))
                VM.AddFilterHistory(VM.FilterText);
        }
        private void Level_Click(object sender, RoutedEventArgs e) {
            if (sender is Button btn) VM.LevelFilter = btn.Tag?.ToString() ?? "ALL";
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
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log) {
                log.IsBookmarked = !log.IsBookmarked;
                VM.StatusMessage = log.IsBookmarked ? $"🔖 북마크: {log.Message.Substring(0, Math.Min(40, log.Message.Length))}" : "북마크 해제됨";
            }
        }
        private void Exclude_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log) {
                if (!VM.ExcludedTids.Contains(log.Tid)) {
                    VM.ExcludedTids.Add(log.Tid);
                    VM.StatusMessage = $"🚫 TID {log.Tid} 제외됨";
                }
            }
        }
        private void LogList_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e) {
            if (VM.AutoScroll && sender is ListView lv && lv.Items.Count > 0 && !VM.IsPaused)
                lv.ScrollIntoView(lv.Items[0]);
        }
        private async void Tab_RightClick(object sender, MouseButtonEventArgs e) {
            if (!VM.IsConnected) { VM.StatusMessage = "⚠ 먼저 CONNECT 버튼으로 연결하세요"; return; }
            var tab = MainTabs.SelectedItem as TabItem;
            if (tab == null) return;
            string cat = tab.Tag?.ToString() ?? "MACHINE";
            var menu = new System.Windows.Controls.ContextMenu();
            var i1 = new System.Windows.Controls.MenuItem { Header = $"📂 {cat} 파일 지정" };
            i1.Click += async (s, ev) => {
                if (FileTree.SelectedItem is FileNode node && !node.IsDirectory)
                    await VM.ConnectSessionAsync(VM.SelectedServer, cat, node.FullPath);
                else VM.StatusMessage = "⚠ 파일트리에서 .log 파일을 먼저 선택하세요";
            };
            var i2 = new System.Windows.Controls.MenuItem { Header = $"⏹ {cat} 스트리밍 중지" };
            i2.Click += (s, ev) => { VM.StopSession(cat); VM.StatusMessage = $"⏹ {cat} 중지됨"; };
            menu.Items.Add(i1); menu.Items.Add(i2); menu.IsOpen = true;
        }
        private void ErrorBox_Click(object sender, RoutedEventArgs e) {
            VM.ResetErrors();
            new ErrorWindow(VM.ErrorHistory) { Owner = this }.Show();
        }
        private void ConfigException_Click(object sender, RoutedEventArgs e) =>
            new ConfigWindow(VM.Servers, VM.AlertKeywords) { Owner = this }.ShowDialog();
    }
}
