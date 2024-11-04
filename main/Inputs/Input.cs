using System.Windows;

namespace CTT.Inputs;

enum InputType
{
    Mouse,
    Joystick
}

abstract class Input : IDisposable
{
    public abstract InputType Type { get; }

    public event EventHandler<Point>? Updated;

    public Input()
    {
        _timer.Interval = 20;
        _timer.AutoReset = true;
        _timer.Elapsed += (s, e) => Updated?.Invoke(this, new Point(_x, _y));
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }

    // Internal

    readonly System.Timers.Timer _timer = new();

    protected double _x = 0;
    protected double _y = 0;
}
