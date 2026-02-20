using System.Windows;
using System.Linq;
using LogMaverick.Models;
using LogMaverick.ViewModels;
using System.Collections.Generic;

namespace LogMaverick.Views {
    public partial class TidTraceWindow : Window {
        public TidTraceWindow(string tid) {
            InitializeComponent();
            TidTitle.Text = "TRACE RESULTS FOR TID: " + tid;
            
            // 안전한 ViewModel 참조 방식
            var mainWin = Application.Current.Windows.OfType<MainWindow>().FirstOrDefault();
            if (mainWin?.DataContext is MainViewModel vm) {
                var allLogs = new List<LogEntry>();
                allLogs.AddRange(vm.MachineLogs);
                allLogs.AddRange(vm.ProcessLogs);
                allLogs.AddRange(vm.DriverLogs);
                allLogs.AddRange(vm.OtherLogs);

                TraceList.ItemsSource = allLogs.Where(x => x.Tid == tid).OrderBy(x => x.Time).ToList();
            }
        }
    }
}
