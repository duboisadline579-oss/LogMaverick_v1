using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using LogMaverick.Models;

namespace LogMaverick.Helpers {
    public class CountToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            (value is int count && count > 0) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class PauseColorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool isPaused = value is bool b && b;
            return isPaused ? new SolidColorBrush(Color.FromRgb(204, 68, 0)) : new SolidColorBrush(Color.FromRgb(0, 122, 255));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class LogTypeColorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if (value is LogEntry log) {
                if (log.IsHighlighted) return new SolidColorBrush(Color.FromRgb(255, 215, 0));
                return log.Type switch {
                    LogType.Error => new SolidColorBrush(Color.FromRgb(255, 80, 80)),
                    LogType.Exception => new SolidColorBrush(Color.FromRgb(255, 100, 255)),
                    LogType.Critical => new SolidColorBrush(Color.FromRgb(255, 50, 50)),
                    _ => new SolidColorBrush(Color.FromRgb(221, 221, 221))
                };
            }
            return new SolidColorBrush(Color.FromRgb(221, 221, 221));
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
    public class NewLogFlashConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            (value is int count && count > 0) ? new SolidColorBrush(Color.FromRgb(0, 122, 255)) : new SolidColorBrush(Color.FromRgb(22, 22, 22));
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
