using SharpDX.DirectInput;
using System.Windows;

namespace CTT.Inputs;

class Mouse : Input
{
    public override InputType Type => InputType.Mouse;

    public Mouse(int deviceIndex) : base()
    {
        if (_devices == null)
        {
            _devices = ListDevices();
        }

        var selectedDevice = _devices[deviceIndex];

        _mouse = new SharpDX.DirectInput.Mouse(_directInput);
        _mouse.Properties.BufferSize = 128;
        _mouse.Acquire();

        Task.Run(Run, _cts.Token);

        Application.Current.Exit += (s, e) => _cts.Cancel();

        Settings.Instance.Updated += Instance_Updated;

        Instance_Updated(this, EventArgs.Empty);
    }

    public static DeviceInstance[] ListDevices()
    {
        List<DeviceInstance> mouseDevices = [];

        foreach (var deviceInstance in _directInput.GetDevices(DeviceType.Mouse, DeviceEnumerationFlags.AllDevices))
            mouseDevices.Add(deviceInstance);

        _devices = mouseDevices.ToArray();
        return _devices;
    }


    // Internal

    static readonly DirectInput _directInput = new();
    static DeviceInstance[]? _devices;

    double _movementScale = 1f / 250;     // -1..1 inside 500 px

    readonly SharpDX.DirectInput.Mouse _mouse;
    readonly CancellationTokenSource _cts = new();

    bool _isLeftButtonPressed = false;

    int _relX = 0;
    int _relY = 0;

    private void Instance_Updated(object? sender, EventArgs e)
    {
        _movementScale = 1f / (Settings.Instance.FieldSize / 2);
    }

    private void Run()
    {
        while (!_cts.IsCancellationRequested)
        {
            _mouse.Poll();
            var datas = _mouse.GetBufferedData();
            if (datas.Length > 0)
            {
                foreach (var data in datas)
                {
                    if (data.Offset == MouseOffset.Buttons0)
                    {
                        _isLeftButtonPressed = data.Value != 0;
                        if (!_isLeftButtonPressed)
                        {
                            _relX = 0;
                            _relY = 0;
                            _x = 0;
                            _y = 0;
                        }
                    }
                    else if (data.Offset == MouseOffset.X)
                    {
                        if (_isLeftButtonPressed)
                        {
                            _relX += data.Value;
                            _x = _movementScale * _relX;
                        }
                    }
                    else if (data.Offset == MouseOffset.Y)
                    {
                        if (_isLeftButtonPressed)
                        {
                            _relY += data.Value;
                            _y = _movementScale * _relY;
                        }
                    }
                }
            }

            Thread.Sleep(10);
        }
    }
}
