using CTT.Inputs;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace CTT;

[ValueConversion(typeof(Orientation), typeof(Visibility))]
class OrientationToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        Orientation orientation = (Orientation)value;
        if ((string)parameter == "vertical")
        {
            return orientation == Orientation.Vertical ? Visibility.Visible : Visibility.Collapsed;
        }
        else
        {
            return orientation == Orientation.Horizontal ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

[ValueConversion(typeof(InputType), typeof(bool))]
class InputTypeToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value?.ToString() == (string)parameter;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

[ValueConversion(typeof(bool), typeof(Visibility))]
class BoolToInversedVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (bool)value ? Visibility.Hidden : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        (Visibility)value != Visibility.Visible;
}

[ValueConversion(typeof(double), typeof(double))]
class ValueMultipliedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        (double)value * double.Parse((string)parameter);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}

[ValueConversion(typeof(double), typeof(string))]
class NumberToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int digitsAfterDecimal = parameter == null ? 0 : (int)parameter;
        return ((double)value).ToString($"F{digitsAfterDecimal}");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

/*
[ValueConversion(typeof(SolidColorBrush), typeof(Brush))]
public class ColorBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => (Brush)value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => (SolidColorBrush)value;
}*/

[ValueConversion(typeof(SolidColorBrush), typeof(Color))]
public class BrushToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => ((SolidColorBrush)value).Color;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => new SolidColorBrush((Color)value);
}

[ValueConversion(typeof(string), typeof(string))]
public class PathUIConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrEmpty((string)value) ? "[not selected yet]" : value;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value;
}

[ValueConversion(typeof(double[]), typeof(string))]
public class ListOfDoublesToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.Join(" ", (double[])value);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        ((string)value)
            .Split(" ")
            .Where(item => !string.IsNullOrEmpty(item))
            .Select(item => double.TryParse(item, out double number) ? number : 0)
            .Where(number => number > 0)
            .ToArray();
}
