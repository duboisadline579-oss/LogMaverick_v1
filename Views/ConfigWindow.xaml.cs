using System.Windows;
using System.Windows.Controls;
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
        private void SrvList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (SrvList.SelectedItem is ServerConfig s) {
                TxtAlias.Text = s.Alias; TxtHost.Text = s.Host;
                TxtPort.Text = s.Port.ToString(); TxtUser.Text = s.Username;
                TxtPath.Text = s.RootPath; BtnDel.IsEnabled = true;
            }
        }
        private void New_Click(object sender, RoutedEventArgs e) {
            var s = new ServerConfig { Alias = "New Server" };
            _servers.Add(s); SrvList.SelectedItem = s;
        }
        private void Del_Click(object sender, RoutedEventArgs e) {
            if (SrvList.SelectedItem is ServerConfig s) { _servers.Remove(s); BtnDel.IsEnabled = false; }
        }
        private void Save_Click(object sender, RoutedEventArgs e) {
            if (SrvList.SelectedItem is ServerConfig s) {
                s.Alias = TxtAlias.Text; s.Host = TxtHost.Text;
                s.Port = int.TryParse(TxtPort.Text, out int p) ? p : 22;
                s.Username = TxtUser.Text; s.Password = TxtPass.Password;
                s.RootPath = TxtPath.Text;
            } else {
                var s2 = new ServerConfig { Alias = TxtAlias.Text, Host = TxtHost.Text, Port = int.TryParse(TxtPort.Text, out int p) ? p : 22, Username = TxtUser.Text, Password = TxtPass.Password, RootPath = TxtPath.Text };
                _servers.Add(s2);
            }
            var settings = ConfigService.Load();
            settings.Servers = _servers.ToList();
            ConfigService.Save(settings); this.Close();
        }
    }
}
