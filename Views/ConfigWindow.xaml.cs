using System.Windows;
using System.Collections.ObjectModel;
using LogMaverick.Models;
using LogMaverick.Services;
using System.Linq;

namespace LogMaverick.Views {
    public partial class ConfigWindow : Window {
        private ObservableCollection<ServerConfig> _servers;
        public ConfigWindow(ObservableCollection<ServerConfig> servers) {
            InitializeComponent();
            _servers = servers;
            SrvList.ItemsSource = _servers;
        }
        private void Add_Click(object sender, RoutedEventArgs e) => _servers.Add(new ServerConfig { Alias = "New Server" });
        private void Del_Click(object sender, RoutedEventArgs e) { if(SrvList.SelectedItem is ServerConfig s) _servers.Remove(s); }
        private void Save_Click(object sender, RoutedEventArgs e) {
            ConfigService.Save(_servers.ToList());
            this.Close();
        }
    }
}
