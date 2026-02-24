using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using LogMaverick.Models;
using LogMaverick.Services;
using System.Linq;

namespace LogMaverick.Views {
    public partial class ConfigWindow : Window {
        private ObservableCollection<ServerConfig> _servers;
        private ObservableCollection<string> _keywords;
        public ConfigWindow(ObservableCollection<ServerConfig> servers, ObservableCollection<string> keywords = null) {
            InitializeComponent();
            _servers = servers;
            _keywords = keywords ?? new ObservableCollection<string>();
            SrvList.ItemsSource = _servers;
            KeywordList.ItemsSource = _keywords;
        }
        private void SrvList_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (SrvList.SelectedItem is ServerConfig s) {
                TxtAlias.Text = s.Alias; TxtHost.Text = s.Host;
                TxtPort.Text = s.Port.ToString(); TxtUser.Text = s.Username;
                TxtPass.Password = s.Password; TxtPath.Text = s.RootPath;
                BtnDel.IsEnabled = true;
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
            }
            var settings = ConfigService.Load();
            settings.Servers = _servers.ToList();
            settings.AlertKeywords = _keywords.ToList();
            ConfigService.Save(settings); this.Close();
        }
        private void AddKeyword_Click(object sender, RoutedEventArgs e) {
            string kw = TxtKeyword.Text.Trim();
            if (!string.IsNullOrEmpty(kw) && !_keywords.Contains(kw)) {
                _keywords.Add(kw); TxtKeyword.Text = "";
            }
        }
        private void DelKeyword_Click(object sender, RoutedEventArgs e) {
            if (KeywordList.SelectedItem is string kw) _keywords.Remove(kw);
        }
    }
}
