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
    }

    public static DeviceInstance[] ListDevices()
    {
        _devices = ListDevices(InputType.Joystick);
        return _devices;
    }


    // Internal

    static DeviceInstance[]? _devices;

    readonly SharpDX.DirectInput.Joystick _joystick;

    protected override void Step()
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
    }
}
