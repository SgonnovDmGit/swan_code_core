using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SwanCode.Core.Services.Theme;

namespace SwanCode.Core.Helpers
{
    /// <summary>
    /// Конвертер AppTheme → локализованная строка (str_ThemeLight / str_ThemeDark / str_Theme1C).
    /// Локализационные ключи живут в продуктовой сборке, поэтому используется Application.Current.FindResource.
    /// </summary>
    public class AppThemeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppTheme theme)
            {
                var key = theme switch
                {
                    AppTheme.Light => "str_ThemeLight",
                    AppTheme.Dark => "str_ThemeDark",
                    AppTheme.OneC => "str_Theme1C",
                    _ => "str_ThemeLight"
                };
                return Application.Current.FindResource(key) as string ?? theme.ToString();
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Имя тула → человекочитаемое локализованное имя (T-000102).
    /// Ищет ключ str_Tool_&lt;имя&gt; в ресурсах приложения (словарь живёт в продуктовой
    /// сборке); нет ключа — показывает сырое имя. ConverterParameter не используется.
    /// </summary>
    public class ToolDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string name || string.IsNullOrEmpty(name))
                return value ?? string.Empty;

            return Application.Current?.TryFindResource("str_Tool_" + name) as string ?? name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility.Visible;
        }
    }

    public class InvertBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility.Collapsed;
        }
    }

    public class InvertBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? false : true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? false : true;
        }
    }

    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>Обратный StringToVisibilityConverter — пустое → Visible (для placeholder-текста в TextBox).
    /// ConverterParameter="invert" переворачивает: непустое → Visible (для контентных блоков, которые
    /// прячутся когда строка пустая).</summary>
    public class EmptyStringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var isEmpty = string.IsNullOrEmpty(value as string);
            var invert = string.Equals(parameter as string, "invert", StringComparison.OrdinalIgnoreCase);
            if (invert)
                return isEmpty ? Visibility.Collapsed : Visibility.Visible;
            return isEmpty ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>Nullable collection → Visibility: null или пустая → Collapsed, иначе Visible.</summary>
    public class CollectionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is System.Collections.ICollection c && c.Count > 0) return Visibility.Visible;
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class WidthRatioConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not double available || double.IsNaN(available) || available <= 0)
                return double.PositiveInfinity;

            var ratio = 0.7;
            if (parameter is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var p))
                ratio = p;
            else if (parameter is double pd)
                ratio = pd;

            var result = available * ratio;
            if (result < 400) result = 400;
            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }

    public class RowIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is DataGridRow row)
                return row.GetIndex() + 1;
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
