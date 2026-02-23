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
            new ConfigWindow(VM.Servers) { Owner = this }.ShowDialog();
        }

        private async void Connect_Click(object sender, RoutedEventArgs e) {
            if (VM.SelectedServer != null && FileTree.SelectedItem is FileNode node)
                await VM.ConnectAsync(VM.SelectedServer, node.FullPath);
        }

        private void Refresh_Click(object sender, RoutedEventArgs e) {
            if (VM.SelectedServer == null) return;
            FileTree.ItemsSource = LogMaverick.Services.FileService.GetRemoteTree(VM.SelectedServer);
            VM.StatusMessage = "TREE REFRESHED";
        }
        private void Pause_Click(object sender, RoutedEventArgs e) => VM.IsPaused = !VM.IsPaused;
        private void Clear_Click(object sender, RoutedEventArgs e) => VM.ClearAll();
        private void Export_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedItem is TabItem tab) VM.ExportLogs(tab.Header.ToString());
        }

        private void Log_DoubleClick(object sender, MouseButtonEventArgs e) {
            if ((sender as ListView)?.SelectedItem is LogEntry log)
                new TidTraceWindow(log.Tid) { Owner = this }.Show();
        }

        private void Copy_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                Clipboard.SetText(log.Message);
        }

        private void Exclude_Click(object sender, RoutedEventArgs e) {
            if (MainTabs.SelectedContent is ListView lv && lv.SelectedItem is LogEntry log)
                VM.ExcludedTids.Add(log.Tid);
        }

        private void LogList_TargetUpdated(object sender, DataTransferEventArgs e) {
            if (sender is ListView lv && lv.Items.Count > 0 && !VM.IsPaused)
                lv.ScrollIntoView(lv.Items[0]);
        }

        private void File_DoubleClick(object sender, MouseButtonEventArgs e) => Connect_Click(null, null);
        private void ErrorBox_Click(object sender, RoutedEventArgs e) => new ErrorWindow(VM.ErrorHistory) { Owner = this }.Show();
    }
}
