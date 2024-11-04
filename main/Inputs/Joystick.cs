using System.Windows;
using SharpDX.DirectInput;

namespace CTT.Inputs;

class Joystick : Input
{
    public string[] Effects => _joystick.GetEffects().Select(effect => effect.Name).ToArray();

    public override InputType Type => InputType.Joystick;

    public Joystick(int deviceIndex) : base()
    {
        if (_devices == null)
        {
            _devices = ListDevices();
        }

        var selectedDevice = _devices[deviceIndex];

        _joystick = new SharpDX.DirectInput.Joystick(_directInput, selectedDevice.InstanceGuid);
        _joystick.Properties.BufferSize = 128;
        _joystick.Acquire();

        Task.Run(Run, _cts.Token);

        Application.Current.Exit += (s, e) => _cts.Cancel();
    }

    public static DeviceInstance[] ListDevices()
    {
        List<DeviceInstance> joystickDevices = [];

        foreach (var deviceInstance in _directInput.GetDevices(DeviceType.Joystick, DeviceEnumerationFlags.AllDevices))
            joystickDevices.Add(deviceInstance);

        _devices = joystickDevices.ToArray();
        return _devices;
    }


    // Internal

    static readonly DirectInput _directInput = new();
    static DeviceInstance[]? _devices;

    readonly SharpDX.DirectInput.Joystick _joystick;
    readonly CancellationTokenSource _cts = new();

    private void Run()
    {
        while (!_cts.IsCancellationRequested)
        {
            _joystick.Poll();
            var datas = _joystick.GetBufferedData();
            if (datas.Length > 0)
            {
                foreach (var data in datas)
                {
                    if (data.Offset == JoystickOffset.X)
                        _x = (double)(data.Value - 0x8000) / 0x8000;
                    else if (data.Offset == JoystickOffset.Y)
                        _y = (double)(data.Value - 0x8000) / 0x8000;
                }
            }

            Thread.Sleep(10);
        }
    }
}
