using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using LogMaverick.Models;
using LogMaverick.ViewModels;

namespace LogMaverick.Views {
    public partial class MainWindow : Window {
        private MainViewModel VM => (MainViewModel)DataContext;
        public MainWindow() { 
            InitializeComponent(); 
            this.DataContext = new MainViewModel(); 
        }

        private void Config_Click(object sender, RoutedEventArgs e) {
            var win = new ConfigWindow(VM.Servers) { Owner = this };
            if (win.ShowDialog() == true) VM.StatusMessage = "SERVER LIST UPDATED";
        }

        private async void Connect_Click(object sender, RoutedEventArgs e) {
            if (VM.SelectedServer != null && FileTree.SelectedItem is FileNode node) {
                if (node.IsDirectory) { MessageBox.Show("파일을 선택해주세요."); return; }
                await VM.ConnectAsync(VM.SelectedServer, node.FullPath);
            } else {
                MessageBox.Show("서버와 파일을 선택해야 합니다.");
            }
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) {
            if (VM.SelectedServer == null) return;
            // 워크스페이스 내 FileTree 로딩 로직 연동
            FileTree.ItemsSource = LogMaverick.Services.FileService.GetRemoteTree(VM.SelectedServer);
            VM.StatusMessage = "FILE TREE REFRESHED";
        }

        private void ErrorBox_Click(object sender, RoutedEventArgs e) {
            var win = new ErrorWindow(VM.ErrorHistory) { Owner = this };
            win.Show();
        }
        private void Pause_Click(object sender, RoutedEventArgs e) => VM.IsPaused = !VM.IsPaused;
        private void Clear_Click(object sender, RoutedEventArgs e) => VM.ClearAll();
        
        private void Export_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedItem is TabItem tab) VM.ExportLogs(tab.Header.ToString());
        }

        private void Log_DoubleClick(object sender, MouseButtonEventArgs e) {
            if ((sender as ListView)?.SelectedItem is LogEntry log) {
                var win = new TidTraceWindow(log.Tid) { Owner = this };
                win.Show();
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log) {
                Clipboard.SetText($"[{log.Time:HH:mm:ss}] {log.Message}");
                VM.StatusMessage = "COPIED TO CLIPBOARD";
            }
        }

        private void Exclude_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log) {
                VM.ExcludedTids.Add(log.Tid);
                VM.StatusMessage = $"EXCLUDED TID: {log.Tid}";
            }
        }

        private void LogList_TargetUpdated(object sender, DataTransferEventArgs e) {
            if (sender is ListView lv && lv.Items.Count > 0 && !VM.IsPaused)
                lv.ScrollIntoView(lv.Items[0]);
        }

        private void File_DoubleClick(object sender, MouseButtonEventArgs e) => Connect_Click(sender, e);
    }
}
