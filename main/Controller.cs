﻿using System.ComponentModel;
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
    public double ProperTrackingDuration { get; private set; } = 0;
    public bool IsLongProperTracking { get; private set; } = false;

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

        _noisePhase += K_NOISE_PHASE_STEP;
        var noise = (Math.Cos(_noisePhase) * 2 - 1) +
                 (Math.Cos(_noisePhase * 2) * 2 - 1) / 2;

        var inputValue = _orientation == Orientation.Horizontal ? input.X : input.Y;
        var speed = (_offset * K_OFFSET_GAIN + inputValue * K_INPUT_GAIN + noise * K_NOISE_GAIN) * _lambda / _ref;
        _offset = (_offset + speed).ToRange(-1, 1);

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

        var isFar = Math.Abs(offsetPixels) > _settings.FarThreshold;
        if ((isFar && !_isFar) || (!isFar && _isFar))
        {
            UpdateDistanceCategory(isFar);
        }

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
    const double K_NOISE_GAIN = 0.02;
    const double K_OFFSET_GAIN = 8;
    const double K_INPUT_GAIN = 10;

    const double PROPER_TRACKING_DURATION_THRESHOLD = 60; // seconds

    readonly Random _random = new();
    readonly Settings _settings = Settings.Instance;
    readonly Logger _logger = Logger.Instance;

    Orientation _orientation;
    int _lambdaIndex = 0;
    double _lambda;

    bool _isRunning = false;

    double _noisePhase;

    double _ref = 0;

    double _offset = 0;
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
