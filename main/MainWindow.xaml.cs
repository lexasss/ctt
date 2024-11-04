using System.Windows;

namespace CTT;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        _controller = new Controller(lineHorizontal);
        DataContext = _controller;

        var joystictDevices = Inputs.Joystick.ListDevices();

        if (joystictDevices.Length > 0)
        {
            _joystick = new Inputs.Joystick(0);
            _joystick.Updated += Joystick_Updated;
        }

        _settings.Updated += Settings_Updated;
    }

    // Internal

    readonly Settings _settings = Settings.Instance;
    readonly Controller _controller;
    readonly Inputs.Input? _joystick;

    private void Joystick_Updated(object? sender, Point e)
    {
        _controller.Update(e);
    }

    private void Settings_Updated(object? sender, EventArgs e)
    {
        Background = _settings.BackgroundColor;

        MinWidth = _settings.FieldSize + 16;        // SystemInformation does not contain proper numbers
        MinHeight = _settings.FieldSize + 40;

        brdContainer.Width = _settings.FieldSize;
        brdContainer.Height = _settings.FieldSize;

        Container_SizeChanged(this, null);
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            if (_controller.IsRunning)
                _controller.Stop();
            else
                _controller.Start();
        }
        else if (e.Key == System.Windows.Input.Key.Escape)
        {
            Close();
        }
        else if (e.Key >= System.Windows.Input.Key.D1 && e.Key <= System.Windows.Input.Key.D9)
        {
            _controller.LambdaIndex = e.Key - System.Windows.Input.Key.D1;
        }
        else if (e.Key >= System.Windows.Input.Key.NumPad1 && e.Key <= System.Windows.Input.Key.NumPad9)
        {
            _controller.LambdaIndex = e.Key - System.Windows.Input.Key.NumPad1;
        }
        else if (e.Key == System.Windows.Input.Key.F2)
        {
            if (!_controller.IsRunning)
                _settings.ShowDialog();
        }
    }

    private void Container_SizeChanged(object sender, SizeChangedEventArgs? e)
    {
        var canvasCenterY = canvas.ActualHeight / 2;
        lineTopThreshold.Y1 = canvasCenterY - _settings.FarThreshold;
        lineTopThreshold.Y2 = canvasCenterY - _settings.FarThreshold;
        lineBottomThreshold.Y1 = canvasCenterY + _settings.FarThreshold;
        lineBottomThreshold.Y2 = canvasCenterY + _settings.FarThreshold;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        Settings_Updated(this, EventArgs.Empty);
    }

    private void Window_Closed(object sender, EventArgs e)
    {
        _controller.Stop();
        _settings.Save();

        Logger.Instance.Save();
    }
}