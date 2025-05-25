using System;
using System.Globalization;
using System.Windows.Data;

namespace rawinator
{
    public class PlusSignValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                int decimals = 0;
                if (parameter is string p && int.TryParse(p, out int dec))
                {
                    decimals = dec;
                }

                string format = decimals > 0 ? $"F{decimals}" : "F0";
                string str = d.ToString(format, culture);
                if (d > 0)
                {
                    return "+" + str;
                }
                return str;
            }
            return value?.ToString() ?? "";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}