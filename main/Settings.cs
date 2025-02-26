﻿using System.ComponentModel;
using System.Text.Json;
using System.Windows.Controls;
using System.Windows.Media;

namespace CTT;

class Settings : INotifyPropertyChanged
{
    public static Settings Instance => _instance ??= new();

    public SolidColorBrush LineColor { get; set; }
    public SolidColorBrush FarLineColor { get; set; }
    public SolidColorBrush BackgroundColor { get; set; }
    public double LineWidth { get; set; }
    public double FarLineWidth { get; set; }
    public double FarThreshold { get; set; }
    public double FieldSize { get; set; }
    public double[] Lambdas { get; set; }
    public Inputs.InputType Input { get; set; }
    public Orientation Orientation { get; set; }
    public double KeyboardGain { get; set; }
    public double OffsetGain { get; set; }
    public double InputGain { get; set; }
    public double NoiseGain { get; set; }

    public bool IsProperTrackingTimerVisible { get; set; } = false;
    public bool IsTrackingTimerVisible { get; set; } = false;

    public bool IsOldCTTBugEnabled { get; set; } = false;

    public bool TonePlayer_IsEnabled { get; set; } = false;
    public double TonePlayer_MaxFrequency { get; set; } = 1000;
    public int TonePlayer_SoundsDeviceIndex { get; set; } = -1;
    public ToneType TonePlayer_ToneType { get; set; } = ToneType.Sine;
    public int TonePlayer_TonePulseDuration { get; set; } = 0;

    public string LogFolder
    {
        get => _logFolder;
        set
        {
            _logFolder = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LogFolder)));
        }
    }

    public event EventHandler? Updated;
    public event PropertyChangedEventHandler? PropertyChanged;

    public void ShowDialog()
    {
        var modifiedSettings = new Settings();

        modifiedSettings.IsProperTrackingTimerVisible = IsProperTrackingTimerVisible;
        modifiedSettings.IsTrackingTimerVisible = IsTrackingTimerVisible;

        var dialog = new SettingsDialog(modifiedSettings);
        if (dialog.ShowDialog() ?? false)
        {
            modifiedSettings.Save();
            
            IsProperTrackingTimerVisible = modifiedSettings.IsProperTrackingTimerVisible;
            IsTrackingTimerVisible = modifiedSettings.IsTrackingTimerVisible;

            Load();
            Updated?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Save()
    {
        var settings = Properties.Settings.Default;

        settings.LineColor = LineColor.Serialize();
        settings.FarLineColor = FarLineColor.Serialize();
        settings.BackgroundColor = BackgroundColor.Serialize();

        settings.LineWidth = LineWidth;
        settings.FarLineWidth = FarLineWidth;
        settings.FarThreshold = FarThreshold;
        settings.FieldSize = FieldSize;

        settings.Lambdas = JsonSerializer.Serialize(Lambdas);
        settings.Input = (int)Input;
        settings.Orientation = (int)Orientation;

        settings.KeyboardGain = KeyboardGain;
        settings.OffsetGain = OffsetGain;
        settings.InputGain = InputGain;
        settings.NoiseGain = NoiseGain;

        settings.IsOldCTTBugEnabled = IsOldCTTBugEnabled;

        settings.TonePlayer_IsEnabled = TonePlayer_IsEnabled;
        settings.TonePlayer_MaxFrequency = TonePlayer_MaxFrequency;
        settings.TonePlayer_SoundsDeviceIndex = TonePlayer_SoundsDeviceIndex;
        settings.TonePlayer_ToneType = (int)TonePlayer_ToneType;
        settings.TonePlayer_TonePulseDuration = TonePlayer_TonePulseDuration;

        settings.LogFolder = LogFolder;

        settings.Save();
    }

    public void InverseOrientation()
    {
        Orientation = Orientation == Orientation.Horizontal ? Orientation.Vertical : Orientation.Horizontal;
        Updated?.Invoke(this, EventArgs.Empty);
    }
        

    // Internal

    static Settings? _instance = null;

    string _logFolder = "";

#pragma warning disable CS8618
    private Settings()
    {
        Load();

        if (string.IsNullOrEmpty(_logFolder))
        {
            _logFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }
    }
    #pragma warning restore CS8618

    private void Load()
    {
        var settings = Properties.Settings.Default;

        LineColor = SolidColorBrushExt.Deserialize(settings.LineColor);
        FarLineColor = SolidColorBrushExt.Deserialize(settings.FarLineColor);
        BackgroundColor = SolidColorBrushExt.Deserialize(settings.BackgroundColor);

        LineWidth = settings.LineWidth;
        FarLineWidth = settings.FarLineWidth;
        FarThreshold = settings.FarThreshold;
        FieldSize = settings.FieldSize;

        try
        {
            Lambdas = JsonSerializer.Deserialize<double[]>(settings.Lambdas) ?? [];
        }
        catch
        {
            Lambdas = [0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5];
        }

        Input = (Inputs.InputType)settings.Input;
        Orientation = (Orientation)settings.Orientation;

        KeyboardGain = settings.KeyboardGain;
        OffsetGain = settings.OffsetGain;
        InputGain = settings.InputGain;
        NoiseGain = settings.NoiseGain;

        IsOldCTTBugEnabled = settings.IsOldCTTBugEnabled;

        TonePlayer_IsEnabled = settings.TonePlayer_IsEnabled;
        TonePlayer_MaxFrequency = settings.TonePlayer_MaxFrequency;
        TonePlayer_SoundsDeviceIndex = settings.TonePlayer_SoundsDeviceIndex;
        TonePlayer_ToneType = (ToneType)settings.TonePlayer_ToneType;
        TonePlayer_TonePulseDuration = settings.TonePlayer_TonePulseDuration;

        _logFolder = settings.LogFolder;
    }
}
