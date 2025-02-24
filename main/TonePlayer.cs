using NAudio.Wave;

namespace CTT;

public class TonePlayer
{
    public static bool IsEnabled { get; set; } = false;

    public static double MaxPitch { get; set; } = 1000;

    public TonePlayer()
    {
        _waveOut.DesiredLatency = 50;
    }

    public void Start()
    {
        if (IsEnabled)
        {
            _sineGen.Frequency = 0;

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

    // Internal

    WaveOut _waveOut = new();
    SineGenerator _sineGen = new();
}

public class SineGenerator : ISampleProvider
{
    /// <summary>
    /// Initializes a new instance for the Generator
    /// </summary>
    /// <param name="sampleRate">Desired sample rate</param>
    public SineGenerator(int sampleRate = 44100)
    {
        _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 2);
    }

    /// <summary>
    /// The waveformat of this WaveProvider (same as the source)
    /// </summary>
    public WaveFormat WaveFormat => _waveFormat;

    /// <summary>
    /// Frequency for the Generator. (-20000.0 - 20000.0 Hz)
    /// Negative value affect left channel, positive values affect right channel
    /// </summary>
    public double Frequency { get; set; } = 0.0;

    /// <summary>
    /// Gain for the Generator. (0.0 to 1.0)
    /// </summary>
    public double Gain { get; set; } = 1;

    /// <summary>
    /// Reads from this provider.
    /// </summary>
    public int Read(float[] buffer, int offset, int count)
    {
        int outIndex = offset;

        var freqLeft = Frequency < 0 ? -Frequency : 0;
        double stepLeft = TWO_PI * freqLeft / _waveFormat.SampleRate;

        var freqRight = Frequency > 0 ? Frequency : 0;
        double stepRight = TWO_PI * freqRight / _waveFormat.SampleRate;

        for (int si = 0; si < count / _waveFormat.Channels; si++)
        {
            for (int ci = 0; ci < _waveFormat.Channels; ci++)
            {
                double sampleValue = Gain * Math.Sin(ci == 0 ? _phaseLeft : _phaseRight);
                buffer[outIndex++] = (float)sampleValue;
            }

            _phaseLeft += stepLeft;
            _phaseRight += stepRight;
        }
        /*
        float[] b = new float[count / 2];
        for (int i = 0; i < count / 2; i++)
        {
            b[i] = buffer[2 * i];
        }
        System.Diagnostics.Debug.WriteLine(string.Join("\n", b));
        */

        return count;
    }

    // Internal

    const double TWO_PI = 2 * Math.PI;

    readonly WaveFormat _waveFormat;

    static double _phaseLeft = 0;
    static double _phaseRight = 0;
}