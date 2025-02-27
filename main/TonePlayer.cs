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
    public int PulseDuration
    {
        get => _toneGenerator.PulseDuration;
        set => _toneGenerator.PulseDuration = value;
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
    /// Sets the sine frequence from 0 Hz to <see cref="MaxFrequency"/> Hz,
    /// or affects the pulse interval if <see cref="TonePulseDuration"/> is >0.
    /// </summary>
    /// <param name="factor">-1..1: negative parameter values affect the left channel,
    /// and positive values affect the right channel</param>
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
                PulseDuration = (int)settings[$"{name}_{nameof(PulseDuration)}"],
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
