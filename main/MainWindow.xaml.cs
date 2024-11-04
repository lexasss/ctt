using System.Windows;

namespace CTT;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        _controller = new Controller(lineHorizontal);
        DataContext = _controller;

        CreateInput();

        _settings.Updated += Settings_Updated;
    }

    // Internal

    readonly Settings _settings = Settings.Instance;
    readonly Controller _controller;

    Inputs.Input? _input;

    private void CreateInput()
    {
        if (_settings.Input == Inputs.InputType.Mouse)
        {
            var mouseDevices = Inputs.Mouse.ListDevices();

            if (mouseDevices.Length > 0)
            {
                _input = new Inputs.Mouse(0);
            }
            else
            {
                MessageBox.Show("No mice found. Please connect and restart the application, or choose another input.",
                    App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else if (_settings.Input == Inputs.InputType.Joystick)
        {
            var joystictDevices = Inputs.Joystick.ListDevices();

            if (joystictDevices.Length > 0)
            {
                _input = new Inputs.Joystick(0);
            }
            else
            {
                MessageBox.Show("No joysticks found. Please connect and restart the application, or choose another input.", 
                    App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            throw new NotSupportedException("Input type is not supported.");
        }

        if (_input != null)
        {
            _input.Updated += Joystick_Updated;
        }
    }

    private void Joystick_Updated(object? sender, Point e)
    {
        _controller.Update(e);
    }

    private void Settings_Updated(object? sender, EventArgs e)
    {
        Background = _settings.BackgroundColor;

        MinWidth = _settings.FieldSize + 16;        // SystemParameters does not allow to match window size and the task field size
        MinHeight = _settings.FieldSize + 40;

        brdContainer.Width = _settings.FieldSize;
        brdContainer.Height = _settings.FieldSize;

        Container_SizeChanged(this, null);

        if (_input?.Type != _settings.Input)
        {
            _input?.Dispose();
            CreateInput();
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