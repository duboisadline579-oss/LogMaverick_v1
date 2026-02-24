using System.Windows;
using LogMaverick.Models;

namespace LogMaverick.Views {
    public partial class LogDetailWindow : Window {
        public LogDetailWindow(LogEntry log) {
            InitializeComponent();
            TxtDetail.Text = $"[{log.Time:HH:mm:ss}] [{log.Category}] [{log.Tid}]\n\n{log.Message}";
        }
    }
}
