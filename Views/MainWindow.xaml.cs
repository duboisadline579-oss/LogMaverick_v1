using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using LogMaverick.Models;
using LogMaverick.ViewModels;
using LogMaverick.Services;

namespace LogMaverick.Views {
    public partial class MainWindow : Window {
        private MainViewModel VM => (MainViewModel)DataContext;

        public MainWindow() { 
            InitializeComponent(); 
            this.DataContext = new MainViewModel(); 
        }

        private void Config_Click(object sender, RoutedEventArgs e) => new ConfigWindow(VM.Servers) { Owner = this }.ShowDialog();
        private void ErrorBox_Click(object sender, RoutedEventArgs e) => new ErrorWindow(VM.ErrorHistory) { Owner = this }.Show();
        private void Clear_Click(object sender, RoutedEventArgs e) => VM.ClearAll();
        private void Pause_Click(object sender, RoutedEventArgs e) => VM.IsPaused = !VM.IsPaused;
        
        private void Export_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedItem is TabItem t) VM.ExportLogs(t.Header.ToString());
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) {
            var selected = SrvList.SelectedItem as ServerConfig;
            if (selected == null) { MessageBox.Show("서버를 선택하세요."); return; }
            FileTree.ItemsSource = new LogCoreEngine().GetFileTree(selected);
            VM.StatusMessage = "TREE REFRESHED";
        }

        private async void Connect_Click(object sender, RoutedEventArgs e) {
            var node = FileTree.SelectedItem as FileNode;
            var server = SrvList.SelectedItem as ServerConfig;
            if (node != null && server != null) {
                await VM.ConnectAsync(server, node.FullPath);
            } else { MessageBox.Show("서버와 로그 파일을 선택하세요."); }
        }

        private async void File_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (FileTree.SelectedItem is FileNode n && !n.IsDirectory && SrvList.SelectedItem is ServerConfig s) {
                await VM.ConnectAsync(s, n.FullPath);
            }
        }

        private void Log_DoubleClick(object sender, MouseButtonEventArgs e) {
            if ((sender as ListView)?.SelectedItem is LogEntry log) {
                new TidTraceWindow(log.Tid) { Owner = this }.Show();
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e) {
            if ((MainTabs.SelectedContent as ListView)?.SelectedItem is LogEntry log) {
                Clipboard.SetText(log.Message);
            }
        }

        private void Exclude_Click(object sender, RoutedEventArgs e) {
            if ((MainTabs.SelectedContent as ListView)?.SelectedItem is LogEntry log) {
                VM.ExcludedTids.Add(log.Tid);
                VM.StatusMessage = $"TID {log.Tid} EXCLUDED";
            }
        }

        private void LogList_TargetUpdated(object sender, DataTransferEventArgs e) {
            if (sender is ListView lv && lv.Items.Count > 0 && !VM.IsPaused) {
                lv.ScrollIntoView(lv.Items[lv.Items.Count - 1]);
            }
        }
    }
}
