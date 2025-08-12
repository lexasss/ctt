using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CTT;

class Controller : INotifyPropertyChanged
{
    public Orientation Orientation => _orientation;

    public int LambdaIndex
    {
        get => _lambdaIndex;
        set
        {
            if (_lambdaIndex >= 0 && _lambdaIndex < _settings.Lambdas.Length)
            {
                _lambdaIndex = value;
                _lambda = _settings.Lambdas[_lambdaIndex];
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Lambda)));
            }
        }
    }

    public double Lambda => _lambda;

    public bool IsRunning => _isRunning;
    public bool IsServerReady => _server.IsListening;

    public double LinePositionX { get; private set; } = 0;
    public double LinePositionY { get; private set; } = 0;
    public Brush LineColor { get; private set; }
    public double LineWidth { get; private set; }
    public string TrackingDuration { get; private set; } = "";
    public double ProperTrackingDuration { get; private set; } = 0;
    public bool IsLongProperTracking { get; private set; } = false;
    public bool IsTrackingTimerVisible => _isRunning && _settings.IsTrackingTimerVisible;
    public bool IsProperTrackingTimerVisible => _isRunning && _settings.IsProperTrackingTimerVisible;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event EventHandler<bool>? IsRunningChanged;
    public event EventHandler<bool>? ConnectionStatusChanged;


    public Controller()
    {
        _orientation = _settings.Orientation;
        _settings.Updated += Settings_Updated;

        _noisePhase = _random.NextDouble();

        _lambda = _settings.Lambdas[_lambdaIndex];

        _server.ClientConnected += (s, e) => ConnectionStatusChanged?.Invoke(this, true);
        _server.ClientDisconnected += (s, e) => ConnectionStatusChanged?.Invoke(this, false);
        _server.Data += Server_Data;

        _server.Start();

        LineColor = _settings.LineColor;
        LineWidth = _settings.LineWidth;

        Reset();
    }

    public void Start()
    {
        _isRunning = true;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTrackingTimerVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsProperTrackingTimerVisible)));

        _logger.AddInfo("time", "lambda", "position", "input");

        Reset();

        _tonePlayer1.Start();
        _tonePlayer2.Start();

        IsRunningChanged?.Invoke(this, true);
    }

    public void Stop()
    {
        _isRunning = false;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsTrackingTimerVisible)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsProperTrackingTimerVisible)));

        _tonePlayer1.Stop();
        _tonePlayer2.Stop();

        Reset();

        IsRunningChanged?.Invoke(this, false);
    }

    /// <summary>
    /// Updates line position based on input
    /// </summary>
    /// <param name="input">Input X and Y, normalized between -1 and 1</param>
    public void Update(Point input)
    {
        if (!_isRunning)
            return;

        _noisePhase += K_NOISE_PHASE_STEP;
        var noise = (Math.Cos(_noisePhase) * 2 - 1) +
                 (Math.Cos(_noisePhase * 2) * 2 - 1) / 2;

        var inputValue = _orientation == Orientation.Horizontal ? input.X : input.Y;
        var speed = (_offset * _settings.OffsetGain + inputValue * _settings.InputGain + noise * _settings.NoiseGain) * _lambda / _ref;
        _offset = (_offset + speed).ToRange(-1, 1);

        _tonePlayer1.SetPitchFactor(_offset);
        _tonePlayer2.SetPitchFactor(_offset);

        var offsetPixels = _offset * _ref;
        if (_orientation == Orientation.Horizontal)
        {
            LinePositionX = _ref + offsetPixels;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LinePositionX)));
        }
        else
        {
            LinePositionY = _ref + offsetPixels;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LinePositionY)));
            //System.Diagnostics.Debug.WriteLine($"Y={input.Y:F3} >> {_offset:F3} >> {LinePositionY:F3}");
        }

        var threshold = _settings.FarThreshold * (_settings.IsOldCTTBugEnabled ? 0.23 : 1);  // "* 0.23" is a re-implementation of a bug from old CTT
        var isFar = Math.Abs(offsetPixels) > threshold;
        if ((isFar && !_isFar) || (!isFar && _isFar))
        {
            UpdateDistanceCategory(isFar);
        }

        TrackingDuration = TimeSpan.FromSeconds((DateTime.Now.Ticks - _trackingStartTime) / 10_000_000).ToString();
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingDuration)));

        if (Math.Abs(_offset) >= 0.99)
        {
            _properTrackingStartTime = DateTime.Now.Ticks;
            _lastProperTrackingDuration = 0;
        }
        else
        {
            _lastProperTrackingDuration = (DateTime.Now.Ticks - _properTrackingStartTime) / 10_000_000;
        }

        if (_lastProperTrackingDuration != ProperTrackingDuration)
        {
            ProperTrackingDuration = _lastProperTrackingDuration;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProperTrackingDuration)));

            bool isLongProperTracking = ProperTrackingDuration >= PROPER_TRACKING_DURATION_THRESHOLD;
            if ((isLongProperTracking && !IsLongProperTracking) || (!isLongProperTracking && IsLongProperTracking))
            {
                IsLongProperTracking = isLongProperTracking;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLongProperTracking)));
            }
        }

        _logger.Add(_lambda, _offset.ToString("F4"), inputValue.ToString("F4"));
    }

    // Internal

    const double K_NOISE_PHASE_STEP = 0.08;

    const double PROPER_TRACKING_DURATION_THRESHOLD = 60; // seconds

    readonly string NET_COMMAND_START = "start";
    readonly string NET_COMMAND_STOP = "stop";
    readonly string NET_COMMAND_SET_LAMBDA = "lambda"; // followed by the index without a space/gap
    readonly string NET_COMMAND_EXIT = "exit";

    readonly Random _random = new();
    readonly Settings _settings = Settings.Instance;
    readonly Logger _logger = Logger.Instance;
    readonly TcpServer _server = new();
    readonly StringComparison _stringComparison = StringComparison.OrdinalIgnoreCase;

    TonePlayer _tonePlayer1 = TonePlayer.Load("TonePlayer1");
    TonePlayer _tonePlayer2 = TonePlayer.Load("TonePlayer2");

    Orientation _orientation;
    int _lambdaIndex = 0;
    double _lambda;

    bool _isRunning = false;

    double _noisePhase;

    double _ref = 0;

    double _offset = 0;
    long _trackingStartTime = 0;
    long _properTrackingStartTime = 0;
    double _lastProperTrackingDuration = 0;

    bool _isFar = false;

    private void Reset()
    {
        _noisePhase = _random.NextDouble();

        _ref = _settings.FieldSize / 2;
        _offset = 0;

        _properTrackingStartTime = DateTime.Now.Ticks;
        _lastProperTrackingDuration = 0;
        ProperTrackingDuration = 0;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ProperTrackingDuration)));

        _trackingStartTime = DateTime.Now.Ticks;
        TrackingDuration = "0:00";
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(TrackingDuration)));

        LinePositionX = _ref;
        LinePositionY = _ref;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LinePositionX)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LinePositionY)));

        UpdateDistanceCategory(false);
    }

    private void UpdateDistanceCategory(bool isFar)
    {
        _isFar = isFar;

        LineColor = _isFar ? _settings.FarLineColor : _settings.LineColor;
        LineWidth = _isFar ? _settings.FarLineWidth : _settings.LineWidth;

        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LineColor)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LineWidth)));
    }

    // Event handlers

    private void Server_Data(object? sender, string e)
    {
        if (e.Equals(NET_COMMAND_START, _stringComparison))
        {
            if (!IsRunning)
                Start();
        }
        else if (e.Equals(NET_COMMAND_STOP, _stringComparison))
        {
            if (IsRunning)
                Stop();
        }
        else if (e.StartsWith(NET_COMMAND_SET_LAMBDA, _stringComparison))
        {
            if (!IsRunning && int.TryParse(e.Substring(6).Trim(), out int index) &&
                index >= 0 && index < _settings.Lambdas.Length)
            {
                LambdaIndex = index;
            }
        }
        else if (e.Equals(NET_COMMAND_EXIT, _stringComparison))
        {
            if (IsRunning)
                Stop();
            Application.Current.Shutdown();
        }
    }

    private void Settings_Updated(object? sender, EventArgs e)
    {
        _tonePlayer1.Dispose();
        _tonePlayer1 = TonePlayer.Load(_tonePlayer1.Name);

        _tonePlayer2.Dispose();
        _tonePlayer2 = TonePlayer.Load(_tonePlayer2.Name);

        if (_orientation != _settings.Orientation)
        {
            _orientation = _settings.Orientation;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Orientation)));
        }

        Reset();
    }
}
