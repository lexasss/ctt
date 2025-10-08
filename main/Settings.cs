using System.ComponentModel;
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
    public int ProperTrackingDurationThreshold { get; set; } = 60;
    public bool IsTrackingTimerVisible { get; set; } = false;

    public bool IsOldCTTBugEnabled { get; set; } = false;

    public bool TonePlayer1_IsEnabled { get; set; } = false;
    public double TonePlayer1_MaxFrequency { get; set; } = 1000;
    public int TonePlayer1_SoundsDeviceIndex { get; set; } = -1;
    public ToneType TonePlayer1_ToneType { get; set; } = ToneType.Sine;
    public int TonePlayer1_PulseDuration { get; set; } = 0;

    public bool TonePlayer2_IsEnabled { get; set; } = false;
    public double TonePlayer2_MaxFrequency { get; set; } = 1000;
    public int TonePlayer2_SoundsDeviceIndex { get; set; } = -1;
    public ToneType TonePlayer2_ToneType { get; set; } = ToneType.Sine;
    public int TonePlayer2_PulseDuration { get; set; } = 0;

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

        settings.ProperTrackingDurationThreshold = ProperTrackingDurationThreshold;

        settings.IsOldCTTBugEnabled = IsOldCTTBugEnabled;

        settings.TonePlayer1_IsEnabled = TonePlayer1_IsEnabled;
        settings.TonePlayer1_MaxFrequency = TonePlayer1_MaxFrequency;
        settings.TonePlayer1_SoundsDeviceIndex = TonePlayer1_SoundsDeviceIndex;
        settings.TonePlayer1_ToneType = (int)TonePlayer1_ToneType;
        settings.TonePlayer1_PulseDuration = TonePlayer1_PulseDuration;

        settings.TonePlayer2_IsEnabled = TonePlayer2_IsEnabled;
        settings.TonePlayer2_MaxFrequency = TonePlayer2_MaxFrequency;
        settings.TonePlayer2_SoundsDeviceIndex = TonePlayer2_SoundsDeviceIndex;
        settings.TonePlayer2_ToneType = (int)TonePlayer2_ToneType;
        settings.TonePlayer2_PulseDuration = TonePlayer2_PulseDuration;

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

        ProperTrackingDurationThreshold = settings.ProperTrackingDurationThreshold;

        IsOldCTTBugEnabled = settings.IsOldCTTBugEnabled;

        TonePlayer1_IsEnabled = settings.TonePlayer1_IsEnabled;
        TonePlayer1_MaxFrequency = settings.TonePlayer1_MaxFrequency;
        TonePlayer1_SoundsDeviceIndex = settings.TonePlayer1_SoundsDeviceIndex;
        TonePlayer1_ToneType = (ToneType)settings.TonePlayer1_ToneType;
        TonePlayer1_PulseDuration = settings.TonePlayer1_PulseDuration;

        TonePlayer2_IsEnabled = settings.TonePlayer2_IsEnabled;
        TonePlayer2_MaxFrequency = settings.TonePlayer2_MaxFrequency;
        TonePlayer2_SoundsDeviceIndex = settings.TonePlayer2_SoundsDeviceIndex;
        TonePlayer2_ToneType = (ToneType)settings.TonePlayer2_ToneType;
        TonePlayer2_PulseDuration = settings.TonePlayer2_PulseDuration;

        _logFolder = settings.LogFolder;
    }
}
