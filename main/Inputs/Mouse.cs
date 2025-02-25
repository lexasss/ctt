using SharpDX.DirectInput;

namespace CTT.Inputs;

class Mouse : Input
{
    public override InputType Type => InputType.Mouse;

    public Mouse() : base()
    {
        if (_devices == null)
        {
            _devices = ListDevices();
        }

        var mouse = new SharpDX.DirectInput.Mouse(_directInput);
        mouse.Properties.BufferSize = 128;
        mouse.Acquire();

        _mouse = mouse;
    }

    public static DeviceInstance[] ListDevices()
    {
        _devices = ListDevices(InputType.Mouse);
        return _devices;
    }


    // Internal

    const double SCALE = 1f / 250;     // -1..1 inside 500 px

    static DeviceInstance[]? _devices;

    readonly SharpDX.DirectInput.Mouse _mouse;

    bool _isLeftButtonPressed = false;

    int _relX = 0;
    int _relY = 0;

    protected override void Step()
    {
        if (_mouse == null)
            return;

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
                        _x = (SCALE * _relX).ToRange(-1, 1);
                    }
                }
                else if (data.Offset == MouseOffset.Y)
                {
                    if (_isLeftButtonPressed)
                    {
                        _relY += data.Value;
                        _y = (SCALE * _relY).ToRange(-1, 1);
                    }
                }
            }
        }
    }
}
