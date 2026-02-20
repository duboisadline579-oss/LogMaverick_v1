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

        private void Config_Click(object sender, RoutedEventArgs e) {
            var configWin = new ConfigWindow(VM.Servers) { Owner = this };
            if (configWin.ShowDialog() == true) VM.StatusMessage = "SERVER CONFIG UPDATED";
        }

        private void ErrorBox_Click(object sender, RoutedEventArgs e) {
            new ErrorWindow(VM.ErrorHistory) { Owner = this }.Show();
        }

        private void Pause_Click(object sender, RoutedEventArgs e) => VM.IsPaused = !VM.IsPaused;
        private void Clear_Click(object sender, RoutedEventArgs e) => VM.ClearAll();
        private void Refresh_Click(object sender, RoutedEventArgs e) {
            if (VM.SelectedServer == null) { MessageBox.Show("서버를 선택하세요."); return; }
            FileTree.ItemsSource = new LogCoreEngine().GetFileTree(VM.SelectedServer);
            VM.StatusMessage = "TREE REFRESHED";
        }

        private async void Connect_Click(object sender, RoutedEventArgs e) {
            if (FileTree.SelectedItem is FileNode node && VM.SelectedServer != null) {
                if (node.IsDirectory) return;
                await VM.ConnectAsync(VM.SelectedServer, node.FullPath);
            } else { MessageBox.Show("서버와 파일을 선택해야 합니다."); }
        }

        private async void File_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (FileTree.SelectedItem is FileNode node && !node.IsDirectory && VM.SelectedServer != null) {
                await VM.ConnectAsync(VM.SelectedServer, node.FullPath);
            }
        }
        private void Log_DoubleClick(object sender, MouseButtonEventArgs e) {
            if ((sender as ListView)?.SelectedItem is LogEntry log) {
                new TidTraceWindow(log.Tid) { Owner = this }.Show();
            }
        }

        private void Copy_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log) {
                Clipboard.SetText(log.Message);
                VM.StatusMessage = "COPIED TO CLIPBOARD";
            }
        }

        private void Exclude_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log) {
                VM.ExcludedTids.Add(log.Tid);
                VM.StatusMessage = $"TID {log.Tid} EXCLUDED";
            }
        }

        private void LogList_TargetUpdated(object sender, DataTransferEventArgs e) {
            if (sender is ListView lv && lv.Items.Count > 0 && !VM.IsPaused) {
                lv.ScrollIntoView(lv.Items[0]);
            }
        }

        private void Export_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedItem is TabItem currentTab) VM.ExportLogs(currentTab.Header.ToString());
        }
    }
}
