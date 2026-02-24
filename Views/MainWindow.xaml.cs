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
            new ConfigWindow(VM.Servers) { Owner = this }.ShowDialog();

        private async void Connect_Click(object sender, RoutedEventArgs e) {
            if (VM.IsConnected) { VM.Disconnect(); return; }
            if (VM.SelectedServer == null) { VM.StatusMessage = "⚠ 서버를 먼저 선택하세요"; return; }
            if (FileTree.SelectedItem is not FileNode node) { VM.StatusMessage = "⚠ 파일을 선택하세요 (🔄 버튼으로 목록 로드 후 선택)"; return; }
            try { await VM.ConnectAsync(VM.SelectedServer, node.FullPath); }
            catch (Exception ex) {
                VM.StatusMessage = "❌ 연결 실패: " + ex.Message;
                MessageBox.Show($"연결 실패\n\n원인: {ex.Message}\n\n확인사항:\n- Host/Port 확인\n- Username/Password 확인\n- 서버 SSH 허용 여부 확인", "연결 실패", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void FileTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            if (FileTree.SelectedItem is FileNode node)
                VM.StatusMessage = $"📄 선택됨: {node.FullPath} — CONNECT 버튼을 누르세요";
        }
        private void Refresh_Click(object sender, RoutedEventArgs e) {
            if (VM.SelectedServer == null) { VM.StatusMessage = "⚠ 서버를 먼저 선택하세요"; return; }
            try {
                VM.StatusMessage = "🔄 파일 트리 로딩 중...";
                FileTree.ItemsSource = FileService.GetRemoteTree(VM.SelectedServer);
                VM.StatusMessage = "✅ 파일 목록 로드 완료 — 파일 선택 후 CONNECT 하세요";
            } catch (Exception ex) { VM.StatusMessage = "❌ 트리 로드 실패: " + ex.Message; }
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) { VM.SaveSettings(); VM.Disconnect(); }
        private void Pause_Click(object sender, RoutedEventArgs e) => VM.IsPaused = !VM.IsPaused;
        private void Clear_Click(object sender, RoutedEventArgs e) => VM.ClearAll();
        private void Export_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedItem is TabItem tab) VM.ExportLogs(tab.Header.ToString());
        }
        private void ExportAll_Click(object sender, RoutedEventArgs e) => VM.ExportAll();
        private void MainTabs_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (MainTabs.SelectedItem is TabItem tab && tab.Header != null) VM.ResetTab(tab.Header.ToString());
        }
        private void Log_DoubleClick(object sender, RoutedEventArgs e) {
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
        private void File_DoubleClick(object sender, MouseButtonEventArgs e) => Connect_Click(null, null);
        private async void Tab_RightClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            if (VM.SelectedServer == null) { VM.StatusMessage = "⚠ 서버를 먼저 선택하세요"; return; }
            var tab = MainTabs.SelectedItem as TabItem;
            if (tab == null) return;
            string cat = tab.Tag?.ToString() ?? "MACHINE";
            var menu = new ContextMenu();
            var i1 = new MenuItem { Header = $"📂 {cat} 파일 지정" };
            i1.Click += async (s, ev) => { if (FileTree.SelectedItem is FileNode node) await VM.ConnectSessionAsync(VM.SelectedServer, cat, node.FullPath); else VM.StatusMessage = "⚠ 파일을 먼저 선택하세요"; };
            var i2 = new MenuItem { Header = $"⏹ {cat} 연결 해제" };
            i2.Click += (s, ev) => VM.StopSession(cat);
            menu.Items.Add(i1); menu.Items.Add(i2); menu.IsOpen = true;
        }
        private void ErrorBox_Click(object sender, RoutedEventArgs e) {
            VM.ResetErrors();
            new ErrorWindow(VM.ErrorHistory) { Owner = this }.Show();
        }
    }
}
    // append
