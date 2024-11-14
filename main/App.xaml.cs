﻿using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace CTT;

public partial class App : Application
{
    public static string Name => "CTT";

    public App() : base()
    {
        var settings = CTT.Properties.Settings.Default;
        if (settings.CallUpgrade)
        {
            settings.Upgrade();
            settings.CallUpgrade = false;
            settings.Save();
        }

        // Set the US-culture across the application to avoid decimal point parsing/logging issues
        var culture = CultureInfo.GetCultureInfo("en-US");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;

        // Force all TextBox's to select its content upon focused
        EventManager.RegisterClassHandler(typeof(TextBox),
            UIElement.GotFocusEvent,
            new RoutedEventHandler(TextBox_GotFocus));
    }

    private void TextBox_GotFocus(object sender, RoutedEventArgs e)
    {
        (sender as TextBox)?.SelectAll();
    }
}
