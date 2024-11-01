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

        _controller.ReStart();
    }

    // Internal

    readonly Controller _controller;
    readonly Inputs.Input? _joystick;

    private void Joystick_Updated(object? sender, Point e)
    {
        _controller.Update(e);
    }

    private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (e.Key == System.Windows.Input.Key.Enter)
        {
            _controller.ReStart();
        }
        else if (e.Key == System.Windows.Input.Key.D0 || e.Key == System.Windows.Input.Key.NumPad0)
        {
            _controller.Stop();
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
    }
}