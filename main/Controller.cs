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

    public double LinePositionX { get; private set; } = 0;
    public double LinePositionY { get; private set; } = 0;
    public Brush LineColor { get; private set; }
    public double LineWidth { get; private set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Controller()
    {
        _orientation = _settings.Orientation;
        _settings.Updated += Settings_Updated;

        _noisePhase = _random.NextDouble();

        _lambda = _settings.Lambdas[_lambdaIndex];

        LineColor = _settings.LineColor;
        LineWidth = _settings.LineWidth;

        Reset();
    }

    public void Start()
    {
        _isRunning = true;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));

        Reset();
    }

    public void Stop()
    {
        _isRunning = false;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRunning)));

        Reset();
    }

    /// <summary>
    /// Updates line position based on input
    /// </summary>
    /// <param name="input">Input X and Y, normalized between -1 and 1</param>
    public void Update(Point input)
    {
        if (!_isRunning)
            return;

        _noisePhase += K_NOISE_STEP;
        _noise = (Math.Cos(_noisePhase) * 2 - 1) * K_NOISE_GAIN;

        var inputValue = _orientation == Orientation.Horizontal ? input.X : input.Y;
        var speed = (_offset * K_OFFSET_GAIN + inputValue * K_INPUT_GAIN + _noise) * _lambda * K_SPEED;
        _offset += speed;
        AssureIsVisible(ref _offset);

        if (_orientation == Orientation.Horizontal)
        {
            LinePositionX = _ref + _offset;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LinePositionX)));
        }
        else
        {
            LinePositionY = _ref + _offset;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(LinePositionY)));
            //System.Diagnostics.Debug.WriteLine($"Y={input.Y:F3} >> {_offset:F3} >> {LinePositionY:F3}");
        }

        var isFar = Math.Abs(_offset) > _settings.FarThreshold;
        if ((isFar && !_isFar) || (!isFar && _isFar))
            UpdateDistanceCategory(isFar);

        _logger.Add(_lambda, (_offset / _ref).ToString("F4"), inputValue.ToString("F4"));
    }

    // Internal

    const double K_NOISE_GAIN = 0.6;
    const double K_NOISE_STEP = 0.008;
    const double K_OFFSET_GAIN = 0.1;
    const double K_INPUT_GAIN = 50;
    const double K_SPEED = 0.2;

    readonly Random _random = new();
    readonly Settings _settings = Settings.Instance;
    readonly Logger _logger = Logger.Instance;

    Orientation _orientation;
    int _lambdaIndex = 0;
    double _lambda;

    bool _isRunning = false;

    double _noisePhase;
    double _noise = 0;

    double _ref = 0;

    double _offset = 0;

    bool _isFar = false;

    private void Reset()
    {
        _noisePhase = _random.NextDouble();
        _noise = 0;

        _ref = _settings.FieldSize / 2;
        _offset = 0;

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

    private void AssureIsVisible(ref double offset)
    {
        if (offset < -_ref)
            offset = -_ref;
        else if (offset > _ref)
            offset = _ref;
    }

    private void Settings_Updated(object? sender, EventArgs e)
    {
        if (_orientation != _settings.Orientation)
        {
            _orientation = _settings.Orientation;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Orientation)));
        }

        Reset();
    }
}
