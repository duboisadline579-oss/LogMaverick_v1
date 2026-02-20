using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace LogMaverick.Helpers {
    public class CountToVisibilityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (value is int count && count > 0) ? Visibility.Visible : Visibility.Collapsed;
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class PauseColorConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            bool isPaused = (value is bool b) && b;
            // 주황색(#CC4400) 또는 어두운 회색(#222222) 반환
            string colorCode = isPaused ? "#CC4400" : "#222222";
            return (SolidColorBrush)new ComponentModel.BrushConverter().ConvertFrom(colorCode);
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
