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
        public MainWindow() { InitializeComponent(); DataContext = new MainViewModel(); }

        private void Config_Click(object sender, RoutedEventArgs e) => new ConfigWindow(VM.Servers) { Owner = this }.ShowDialog();
        private void ErrorBox_Click(object sender, RoutedEventArgs e) => new ErrorWindow(VM.ErrorHistory) { Owner = this }.Show();
        private void Clear_Click(object sender, RoutedEventArgs e) => VM.ClearAll();
        private void Pause_Click(object sender, RoutedEventArgs e) => VM.IsPaused = !VM.IsPaused;
        private void Export_Click(object sender, RoutedEventArgs e) => VM.ExportLogs((MainTabs.SelectedItem as TabItem)?.Header?.ToString() ?? "OTHERS");

        private void Refresh_Click(object sender, RoutedEventArgs e) {
            if (SrvList.SelectedItem is ServerConfig s) FileTree.ItemsSource = new LogCoreEngine().GetFileTree(s);
        }

        private async void Connect_Click(object sender, RoutedEventArgs e) {
            if (FileTree.SelectedItem is FileNode n && SrvList.SelectedItem is ServerConfig s) await VM.ConnectAsync(s, n.FullPath);
        }

        private async void File_DoubleClick(object sender, MouseButtonEventArgs e) {
            if (FileTree.SelectedItem is FileNode n && !n.IsDirectory && SrvList.SelectedItem is ServerConfig s) await VM.ConnectAsync(s, n.FullPath);
        }

        private void Log_DoubleClick(object sender, MouseButtonEventArgs e) {
            if ((sender as ListView)?.SelectedItem is LogEntry log) new TidTraceWindow(log.Tid) { Owner = this }.Show();
        }

        private void Copy_Click(object sender, RoutedEventArgs e) {
            if ((MainTabs.SelectedContent as ListView)?.SelectedItem is LogEntry log) Clipboard.SetText(log.Message);
        }

        private void Exclude_Click(object sender, RoutedEventArgs e) {
            if ((MainTabs.SelectedContent as ListView)?.SelectedItem is LogEntry log) {
                VM.ExcludedTids.Add(log.Tid);
                MessageBox.Show($"TID {log.Tid} Excluded.");
            }
        }

        private void LogList_TargetUpdated(object sender, DataTransferEventArgs e) {
            if (sender is ListView lv && lv.Items.Count > 0) lv.ScrollIntoView(lv.Items[0]);
        }
    }
}
