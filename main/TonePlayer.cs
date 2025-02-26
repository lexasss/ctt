using NAudio.Wave;

namespace CTT;

public enum ToneType
{
    Sine,
    Triangle,
    Harmonics
}

public class SoundDevice(int index, string name)
{
    public int Index => index;
    public string Name => name;
    public override string ToString() => name;
}

public class TonePlayer : IDisposable
{
    public string Name { get; }
    public bool IsEnabled { get; set; } = false;
    public double MaxFrequency { get; set; } = 1000;
    public int SoundsDeviceIndex { get; set; } = -1;
    public ToneType ToneType
    { 
        get => _toneGenerator.ToneType;
        set => _toneGenerator.ToneType = value;
    }
    public int TonePulseDuration
    {
        get => _toneGenerator.TonePulseDuration;
        set => _toneGenerator.TonePulseDuration = value;
    }


    public TonePlayer(string name)
    {
        Name = name;

        _waveOut.DesiredLatency = 50;
    }

    public TonePlayer() : this("") { }

    public void Start()
    {
        if (IsEnabled)
        {
            _toneGenerator.Frequency = 0;
            _toneGenerator.Reset();

            _waveOut.DeviceNumber = SoundsDeviceIndex;
            _waveOut.Init(_toneGenerator);
            _waveOut.Play();
        }
    }

    public void Stop()
    {
        if (_waveOut.PlaybackState == PlaybackState.Playing)
        {
            _waveOut.Stop();
        }
    }

    public void Dispose()
    {
        _waveOut.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Sets the sine frequence from 0 Hz to <see cref="MaxFrequency"/> Hz, or
    /// affects the pulse interval if <see cref="TonePulseDuration"/> is >0.
    /// Negative parameter values affect the left channel, and positive values affect the right channel
    /// </summary>
    /// <param name="factor">-1..1</param>
    public void SetPitchFactor(double factor)
    {
        _toneGenerator.Frequency = factor * MaxFrequency;
        //_toneGenerator.Frequency = Math.Sign(factor) * Math.Exp(Math.Abs(factor) * 3.5 - 2.5) * MaxFrequency / Math.E;
    }

    public static TonePlayer Load(string name)
    {
        var settings = Properties.Settings.Default;
        try
        {
            return new TonePlayer(name)
            {
                IsEnabled = (bool)settings[$"{name}_{nameof(IsEnabled)}"],
                MaxFrequency = (double)settings[$"{name}_{nameof(MaxFrequency)}"],
                SoundsDeviceIndex = (int)settings[$"{name}_{nameof(SoundsDeviceIndex)}"],
                ToneType = (ToneType)(int)settings[$"{name}_{nameof(ToneType)}"],
                TonePulseDuration = (int)settings[$"{name}_{nameof(TonePulseDuration)}"],
            };
        }
        catch
        {
            return new TonePlayer(name);
        }
    }

    public static SoundDevice[] GetSoundDevices()
    {
        var result = new List<SoundDevice>() { new(-1, "Default") };
        for (int i = 0; i < WaveOut.DeviceCount; i++)
        {
            var caps = WaveOut.GetCapabilities(i);
            result.Add(new(i, caps.ProductName));
        }
        return result.ToArray();
    }

    // Internal

    readonly WaveOut _waveOut = new();
    readonly ToneGenerator _toneGenerator = new();
}

internal record class Harmonic(int Index, double Gain, double Phase);

internal class ToneGenerator : ISampleProvider
{
    public WaveFormat WaveFormat => _waveFormat;

    /// <summary>
    /// (-20000.0 - 20000.0 Hz)
    /// Negative values affect left channel, positive values affect right channel
    /// </summary>
    public double Frequency { get; set; } = 0.0;

    /// <summary>
    /// 0..1
    /// </summary>
    public double Gain { get; set; } = 1;

    public ToneType ToneType { get; set; } = ToneType.Sine;

    /// <summary>
    /// In miliseconds. Zero means continuos tone. Avoid long pulse durations (>200ms)
    /// </summary>
    public int TonePulseDuration { get; set; } = 0;

    /// <summary>
    /// Initializes a new instance for the Generator
    /// </summary>
    /// <param name="sampleRate">Desired sample rate</param>
    public ToneGenerator(int sampleRate = 44100)
    {
        _stepDuration = 1000d / sampleRate;
        _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
        _totalGain = Harmonics.Sum(h => h.Gain);
    }

    public void Reset()
    {
        _accDuration = 0;
        _mode = TonePulseDuration == 0 ? Mode.Continuos : Mode.Off;
    }

    public Harmonic[] Harmonics { get; set; } = [
        new Harmonic(1, 1, 0),
        new Harmonic(2, 0.61, Math.PI / 3),
        new Harmonic(3, 0.39, Math.PI / 4),
        new Harmonic(4, 0.21, Math.PI / 7),
        new Harmonic(5, 0.11, Math.PI / 2),
    ];

    const double PULSE_FREQUENCY = 200;
    const double MAX_PULSE_INTERVAL = 3000;

    /// <summary>
    /// Reads from this provider.
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int outIndex = offset;
        int sampleCount = count / _waveFormat.Channels;

        var freq = _mode == Mode.Continuos ? Math.Abs(Frequency) : PULSE_FREQUENCY;

        //_tonePulseInterval = Math.Min(MAX_PULSE_INTERVAL, 50 * MAX_PULSE_INTERVAL / Math.Abs(Frequency));
        _tonePulseInterval = Math.Min(MAX_PULSE_INTERVAL, MAX_PULSE_INTERVAL / Math.Exp(0.005 * Math.Abs(Frequency)));

        var freqLeft = Frequency < 0 ? freq : 0;
        double stepLeft = TWO_PI * freqLeft / _waveFormat.SampleRate;

        var freqRight = Frequency > 0 ? freq : 0;
        double stepRight = TWO_PI * freqRight / _waveFormat.SampleRate;

        for (int si = 0; si < sampleCount; si++)
        {
            _accDuration += _stepDuration;
            if (_mode == Mode.Off && _accDuration > _tonePulseInterval)
            {
                _mode = Mode.On;
                _accDuration = 0;
            }
            else if (_mode == Mode.On && _accDuration > TonePulseDuration)
            {
                _mode = Mode.Off;
                _accDuration = 0;
            }

            for (int ci = 0; ci < _waveFormat.Channels; ci++)
            {
                var phase = (ci == 0 ? _phaseLeft : _phaseRight) % TWO_PI;
                double sampleValue = (ToneType switch
                {
                    ToneType.Sine => Math.Sin(phase),
                    ToneType.Triangle => (phase > Math.PI ? TWO_PI - phase : phase) / PI_2 - 1,
                    ToneType.Harmonics => Harmonics.Sum(h => h.Gain * Math.Sin(h.Phase + h.Index * phase)) / _totalGain,
                    _ => throw new NotImplementedException("This tone type is not supported")
                }) * Gain;

                buffer[outIndex++] = (float)sampleValue;
            }

            if (_mode != Mode.Off)
            {
                _phaseLeft += stepLeft;
                _phaseRight += stepRight;
            }
        }

        return count;
    }

    // Internal

    enum Mode { Continuos, Off, On }

    const double TWO_PI = 2 * Math.PI;
    const double PI_2 = Math.PI / 2;

    readonly double _stepDuration;  // ms
    readonly WaveFormat _waveFormat;

    static double _phaseLeft = 0;
    static double _phaseRight = 0;

    static double _accDuration = 0;

    double _totalGain;
    double _tonePulseInterval;
    Mode _mode = Mode.Continuos;
}