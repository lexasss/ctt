using CTT.Inputs;
using Microsoft.Windows.Shell;
using SharpDX.DirectInput;
using System.Windows;

namespace CTT;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        _controller = new Controller();
        DataContext = _controller;

        _settings.Updated += Settings_Updated;
    }

    // Internal

    readonly Settings _settings = Settings.Instance;
    readonly Controller _controller;

    Inputs.Input? _input;

    private bool CreateInput()
    {
        DeviceInstance[] inputDevices;

        inputDevices = Inputs.Input.ListDevices(_settings.Input);
        if (inputDevices.Length > 0)
        {
            if (_settings.Input == Inputs.InputType.Mouse)
                _input = new Inputs.Mouse();
            else if (_settings.Input == Inputs.InputType.Joystick)
                _input = new Inputs.Joystick(0);
            else if (_settings.Input == Inputs.InputType.Keyboard)
                _input = new Inputs.Keyboard();
        }
        else
        {
            MessageBox.Show($"Found no device of type '{_settings.Input}'. Please connect, or choose another input.",
                    App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        if (_input != null)
        {
            _input.Updated += Input_Updated;
        }

        return _input != null;
    }

    private void Input_Updated(object? sender, Point e)
    {
        _controller.Update(e);
    }

    private void Settings_Updated(object? sender, EventArgs e)
    {
        Background = _settings.BackgroundColor;

        MinWidth = _settings.FieldSize + 17;        // SystemParameters does not allow to match window size and the task field size
        MinHeight = _settings.FieldSize + SystemParameters.CaptionHeight + 18;

        Title = $"{App.Name} - {_settings.Input}";

        brdContainer.Width = _settings.FieldSize;
        brdContainer.Height = _settings.FieldSize;

        Container_SizeChanged(this, null);

        if (_input?.Type != _settings.Input)
        {
            _input?.Dispose();
            _input = null;

            if (!CreateInput())
            {
                _settings.Input = Input.GetFirstExistingType();
                _settings.Save();

                Task.Run(() => Dispatcher.Invoke(_settings.ShowDialog));
            }
        }
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
        else if (e.Key == System.Windows.Input.Key.D0 || e.Key == System.Windows.Input.Key.NumPad0)
        {
            _controller.LambdaIndex = 9;
        }
        else if (e.Key == System.Windows.Input.Key.F2)
        {
            if (!_controller.IsRunning)
                _settings.ShowDialog();
        }
        else if (e.Key == System.Windows.Input.Key.T)
        {
            if (!_controller.IsRunning)
                _settings.InverseOrientation();
        }
    }

    private void Container_SizeChanged(object sender, SizeChangedEventArgs? e)
    {
        var canvasCenterY = brdContainer.ActualHeight / 2;
        lineHorzTopThreshold.Y1 = canvasCenterY - _settings.FarThreshold;
        lineHorzTopThreshold.Y2 = canvasCenterY - _settings.FarThreshold;
        lineHorzBottomThreshold.Y1 = canvasCenterY + _settings.FarThreshold;
        lineHorzBottomThreshold.Y2 = canvasCenterY + _settings.FarThreshold;

        var canvasCenterX = brdContainer.ActualWidth / 2;
        lineVertLeftThreshold.X1 = canvasCenterX - _settings.FarThreshold;
        lineVertLeftThreshold.X2 = canvasCenterX - _settings.FarThreshold;
        lineVertRightThreshold.X1 = canvasCenterX + _settings.FarThreshold;
        lineVertRightThreshold.X2 = canvasCenterX + _settings.FarThreshold;
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