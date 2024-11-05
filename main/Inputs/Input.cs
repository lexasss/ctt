using SharpDX.DirectInput;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace CTT.Inputs;

enum InputType
{
    Mouse,
    Joystick,
    Keyboard
}

abstract class Input : IDisposable
{
    public abstract InputType Type { get; }

    public event EventHandler<Point>? Updated;

    public Input()
    {
        Application.Current.Exit += App_Exit;

        _timer.Interval = 20;
        _timer.AutoReset = true;
        _timer.Elapsed += (s, e) => Updated?.Invoke(this, new Point(_x, _y));
        _timer.Start();

        Task.Run(RunCycle, _cts.Token);
    }

    public void Dispose()
    {
        _timer.Dispose();

        Application.Current.Exit -= App_Exit;
    }

    public static DeviceInstance[] ListDevices(InputType type)
    {
        List<DeviceInstance> devices = [];

        DeviceType inputType = type switch
        {
            InputType.Mouse => DeviceType.Mouse,
            InputType.Joystick => DeviceType.Joystick,
            InputType.Keyboard => DeviceType.Keyboard,
            _ => throw new NotSupportedException($"Type '{type}' is not supported")
        };

        foreach (var deviceInstance in _directInput.GetDevices(inputType, DeviceEnumerationFlags.AllDevices))
            devices.Add(deviceInstance);

        return devices.ToArray();
    }

    // Internal

    readonly System.Timers.Timer _timer = new();

    readonly CancellationTokenSource _cts = new();

    protected static readonly DirectInput _directInput = new();

    protected double _x = 0;
    protected double _y = 0;

    private void App_Exit(object s, ExitEventArgs e) => _cts.Cancel();

    protected abstract void Step(); // this should update _x and _y

    private async void RunCycle()
    {
        await Task.Delay(100);    // just in case, as this loop may start earlier then a device is initialized

        while (!_cts.IsCancellationRequested)
        {
            Step();
            Thread.Sleep(10);
        }
    }
}