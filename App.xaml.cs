using System.Windows;
using LogMaverick.Helpers;

namespace LogMaverick {
    public partial class App : Application {
        protected override void OnStartup(StartupEventArgs e) {
            base.OnStartup(e);
            Resources["CountToVis"] = new CountToVisibilityConverter();
            Resources["PauseColorConverter"] = new PauseColorConverter();
            Resources["LogColorConverter"] = new LogTypeColorConverter();
            Resources["FlashConverter"] = new NewLogFlashConverter();
        }
    }
}
