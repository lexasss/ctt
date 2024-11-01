using System.Windows.Media;

namespace CTT;

class Settings
{
    public static Settings Instance => _instance ??= new();
    public Brush LineColor { get; set; } = Brushes.Black;
    public Brush FarLineColor { get; set; } = Brushes.Red;
    public double LineWidth { get; set; } = 3;
    public double FarLineWidth { get; set; } = 15;
    public double FarThreshold { get; set; } = 100;
    public List<double> Lambdas { get; set; } = [0.5, 1, 1.5, 2, 2.5, 3, 3.5, 4, 4.5, 5];

    // Internal

    static Settings? _instance;

    private Settings() { }
}
