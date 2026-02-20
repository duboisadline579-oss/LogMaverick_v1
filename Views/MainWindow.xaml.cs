using System.Windows;
using System.Windows.Input;
using LogMaverick.Models;
using LogMaverick.ViewModels;

namespace LogMaverick.Views {
    public partial class MainWindow : Window {
        private MainViewModel VM => (MainViewModel)DataContext;
        public MainWindow() { InitializeComponent(); DataContext = new MainViewModel(); }
        private void Add_Click(object sender, RoutedEventArgs e) { /* 구현 생략 */ }
        private void Del_Click(object sender, RoutedEventArgs e) { /* 구현 생략 */ }
        private void File_DoubleClick(object sender, MouseButtonEventArgs e) {
            if(SrvList.SelectedItem is ServerConfig s && FileTree.SelectedItem is FileNode f) VM.Connect(s, f.FullPath);
        }
        private void Log_DoubleClick(object sender, MouseButtonEventArgs e) {
            var log = (LogEntry)((FrameworkElement)e.OriginalSource).DataContext;
            if(log != null) MessageBox.Show($"TID: {log.Tid}\n\n{log.Message}", "Log Detail & TID Trace");
        }
        private void Clear_Click(object sender, RoutedEventArgs e) => VM.ClearAll();
    }
}
