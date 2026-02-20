using System.Windows;
using LogMaverick.Models;
using LogMaverick.ViewModels;
namespace LogMaverick.Views {
    public partial class MainWindow : Window {
        public MainWindow() { InitializeComponent(); DataContext = new MainViewModel(); }
        private void BtnConnect_Click(object sender, RoutedEventArgs e) {
            var s = (ServerConfig)ServerListBox.SelectedItem;
            if(s != null) ((MainViewModel)DataContext).Connect(s);
        }
        private void Log_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e) {
            var log = (LogEntry)((FrameworkElement)e.OriginalSource).DataContext;
            if(log != null) {
                var win = new LogDetailWindow();
                win.TxtDetail.Text = log.Message;
                win.Show();
            }
        }
    }
}
