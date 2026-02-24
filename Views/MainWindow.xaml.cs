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
            if (VM.IsConnected) { VM.Disconnect(); FileTree.ItemsSource = null; VM.StatusMessage = "🔌 연결 해제됨"; return; }
            if (VM.SelectedServer == null) { VM.StatusMessage = "⚠ 서버를 먼저 선택하세요"; return; }
            VM.IsLoading = true; VM.StatusMessage = "🔄 서버 연결 중...";
            try {
                var tree = await System.Threading.Tasks.Task.Run(() => FileService.GetRemoteTree(VM.SelectedServer));
                FileTree.ItemsSource = tree;
                VM.IsConnected = true;
                VM.IsLoading = false; VM.StatusMessage = "✅ 연결됨 — 파일을 더블클릭하여 로그 스트리밍 시작";
            } catch (Exception ex) { VM.IsLoading = false; VM.StatusMessage = "❌ 연결 실패: " + ex.Message; }
        }
        private void Refresh_Click(object sender, RoutedEventArgs e) {
            if (VM.SelectedServer == null) { VM.StatusMessage = "⚠ 서버를 먼저 선택하세요"; return; }
            FileTree.ItemsSource = FileService.GetRemoteTree(VM.SelectedServer);
            VM.StatusMessage = "🔄 새로고침 완료";
        }
        private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            if (FileTree.SelectedItem is FileNode node && !node.IsDirectory)
                VM.StatusMessage = $"📄 선택됨: {node.FullPath} — 더블클릭하여 연결";
        }
        private async void File_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (FileTree.SelectedItem is FileNode node && !node.IsDirectory) {
                if (VM.SelectedServer == null) { VM.StatusMessage = "⚠ 서버를 먼저 선택하세요"; return; }
                try { await VM.ConnectAsync(VM.SelectedServer, node.FullPath); }
                catch (Exception ex) { VM.IsLoading = false; VM.StatusMessage = "❌ 연결 실패: " + ex.Message; }
            }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { VM.SaveSettings(); VM.Disconnect(); }
        private void Pause_Click(object sender, RoutedEventArgs e) => VM.IsPaused = !VM.IsPaused;
        private void Clear_Click(object sender, RoutedEventArgs e) => VM.ClearAll();
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
        private void Copy_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                Clipboard.SetText(log.Message);
        }
        private void Exclude_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                VM.ExcludedTids.Add(log.Tid);
        }
        private void LogList_TargetUpdated(object sender, System.Windows.Data.DataTransferEventArgs e) {
            if (sender is ListView lv && lv.Items.Count > 0 && !VM.IsPaused)
                lv.ScrollIntoView(lv.Items[0]);
        }
        private async void Tab_RightClick(object sender, MouseButtonEventArgs e) {
            if (VM.SelectedServer == null) return;
            var tab = MainTabs.SelectedItem as TabItem;
            if (tab == null) return;
            string cat = tab.Tag?.ToString() ?? "MACHINE";
            var menu = new System.Windows.Controls.ContextMenu();
            var i1 = new System.Windows.Controls.MenuItem { Header = $"📂 {cat} 파일 지정" };
            i1.Click += async (s, ev) => {
                if (FileTree.SelectedItem is FileNode node && !node.IsDirectory)
                    await VM.ConnectSessionAsync(VM.SelectedServer, cat, node.FullPath);
                else VM.StatusMessage = "⚠ 파일을 먼저 선택하세요";
            };
            var i2 = new System.Windows.Controls.MenuItem { Header = $"⏹ {cat} 연결 해제" };
            i2.Click += (s, ev) => VM.StopSession(cat);
            menu.Items.Add(i1); menu.Items.Add(i2); menu.IsOpen = true;
        }
        private void TraceTid_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is System.Windows.Controls.ListView lv && lv.SelectedItem is LogMaverick.Models.LogEntry log)
                new TidTraceWindow(log.Tid) { Owner = this }.Show();
        }
        private void ErrorBox_Click(object sender, RoutedEventArgs e) {
            VM.ResetErrors();
            new ErrorWindow(VM.ErrorHistory) { Owner = this }.Show();
        }
    }
}
