using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace MHRiseModManager.Views.Converter
{
    public class DateTimeConverter : IValueConverter
    {
        // DateTimeを「yyyy/MM/dd HH:mm:ss.fff」形式の文字列に変換
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            DateTime date = (DateTime)value;
            return date.ToString("yyyy/MM/dd HH:mm:ss");
        }
        // 文字列をDateTimeに変換
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string strValue = value as string;
            DateTime resultDateTime;
            if (DateTime.TryParse(strValue, out resultDateTime))
            {
                return resultDateTime;
            }
            return DependencyProperty.UnsetValue;
        }
    }
}
