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
    public double MaxPitch { get; set; } = 1000;
    public int SoundsDeviceIndex { get; set; } = -1;
    public ToneType ToneType { get; set; } = ToneType.Sine;

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
            _sineGen.Frequency = 0;
            _sineGen.ToneType = ToneType;

            _waveOut.DeviceNumber = SoundsDeviceIndex;
            _waveOut.Init(_sineGen);
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
    /// Sets the sine frequence from 0 Hz to <see cref="MaxPitch"/> Hz.
    /// Negative values affect the left channel, and positive values affect the right channel
    /// </summary>
    /// <param name="factor">-1..1</param>
    public void SetPitchFactor(double factor)
    {
        //_sineGen.Frequency = factor * MaxPitch;
        _sineGen.Frequency = Math.Sign(factor) * Math.Exp(Math.Abs(factor) * 3.5 - 2.5) * MaxPitch / Math.E;
    }

    /*public void Save()
    {
        var settings = Properties.Settings.Default;
        settings[$"{Name}_IsEnabled"] = IsEnabled;
        settings[$"{Name}_MaxFrequency"] = MaxPitch;
        settings[$"{Name}_SoundsDeviceIndex"] = SoundsDeviceIndex;
        settings[$"{name}_ToneType"] = (int)ToneType;
        settings.Save();
    }*/

    public static TonePlayer Load(string name)
    {
        var settings = Properties.Settings.Default;
        return new TonePlayer(name)
        {
            IsEnabled = (bool)settings[$"{name}_IsEnabled"],
            MaxPitch = (double)settings[$"{name}_MaxFrequency"],
            SoundsDeviceIndex = (int)settings[$"{name}_SoundsDeviceIndex"],
            ToneType = (ToneType)(int)settings[$"{name}_ToneType"]
        };
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
    readonly SineGenerator _sineGen = new();
}

internal record class Harmonic(int Index, double Gain, double Phase);

internal class SineGenerator : ISampleProvider
{
    public WaveFormat WaveFormat => _waveFormat;

    /// <summary>
    /// Frequency for the Generator. (-20000.0 - 20000.0 Hz)
    /// Negative value affect left channel, positive values affect right channel
    /// </summary>
    public double Frequency { get; set; } = 0.0;

    /// <summary>
    /// 0.0 .. 1.0
    /// </summary>
    public double Gain { get; set; } = 1;

    public ToneType ToneType { get; set; } = ToneType.Sine;

    /// <summary>
    /// Initializes a new instance for the Generator
    /// </summary>
    /// <param name="sampleRate">Desired sample rate</param>
    public SineGenerator(int sampleRate = 44100)
    {
        _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
        _totalGain = Harmonics.Sum(h => h.Gain);
    }

    public Harmonic[] Harmonics { get; set; } = [
        new Harmonic(1, 1, 0),
        new Harmonic(2, 0.61, Math.PI / 3),
        new Harmonic(3, 0.39, Math.PI / 4),
        new Harmonic(4, 0.21, Math.PI / 7),
        new Harmonic(5, 0.11, Math.PI / 2),
    ];

    /// <summary>
    /// Reads from this provider.
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int outIndex = offset;
        int sampleCount = count / _waveFormat.Channels;

        var freqLeft = Frequency < 0 ? -Frequency : 0;
        double stepLeft = TWO_PI * freqLeft / _waveFormat.SampleRate;

        var freqRight = Frequency > 0 ? Frequency : 0;
        double stepRight = TWO_PI * freqRight / _waveFormat.SampleRate;

        for (int si = 0; si < sampleCount; si++)
        {
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

            _phaseLeft += stepLeft;
            _phaseRight += stepRight;
        }

        return count;
    }

    // Internal

    const double TWO_PI = 2 * Math.PI;
    const double PI_2 = Math.PI / 2;

    readonly WaveFormat _waveFormat;

    static double _phaseLeft = 0;
    static double _phaseRight = 0;

    double _totalGain;
}