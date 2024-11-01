using System.Windows;

namespace CTT.Inputs;

abstract class Input
{
    public event EventHandler<Point>? Updated;

    public Input()
    {
        _timer.Interval = 20;
        _timer.AutoReset = true;
        _timer.Elapsed += (s, e) => Updated?.Invoke(this, new Point(_x, _y));
        _timer.Start();
    }

    // Internal

    readonly System.Timers.Timer _timer = new();

    protected double _x = 0;
    protected double _y = 0;
}
