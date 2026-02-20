using System.Windows;
using LogMaverick.ViewModels;
using LogMaverick.Models;
namespace LogMaverick.Views {
    public partial class MainWindow : Window {
        private readonly MainViewModel _vm = new();
        public MainWindow() { InitializeComponent(); DataContext = _vm; }
        private void Connect_Click(object sender, RoutedEventArgs e) {
            _vm.Connect(new ServerConfig { Host="IP입력", Username="root", Password="비번" });
        }
        private void Pause_Click(object sender, RoutedEventArgs e) => _vm.TogglePause();
    }
}
