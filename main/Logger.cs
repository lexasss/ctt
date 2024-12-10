using System.IO;
using System.Windows;

namespace CTT;

internal class Logger
{
    public static Logger Instance => _instance ??= new();

    /// <summary>
    /// Clears the log data
    /// </summary>
    public void Reset()
    {
        lock (_records)
        {
            _records.Clear();
        }
    }

    /// <summary>
    /// Add a time-stamped record to the log data
    /// </summary>
    /// <param name="items">any data to save into a single record</param>
    public void Add(params object[] items)
    {
        var record = string.Join('\t', [DateTime.Now.Ticks, ..items]);

        lock (_records)
        {
            _records.Add(record);
        }
    }

    /// <summary>
    /// Add a record to the log data
    /// </summary>
    /// <param name="items">any data to save into a single record</param>
    public void AddInfo(params object[] items)
    {
        var record = string.Join('\t', items);

        lock (_records)
        {
            _records.Add(record);
        }
    }

    /// <summary>
    /// Saves records to a log file and clears the log data
    /// </summary>
    /// <returns>filename if saved successfully, otherwise null</returns>
    public string? Save()
    {
        if (_records.Count == 0)
            return null;

        if (string.IsNullOrEmpty(_settings.LogFolder))
        {
            var folderName = SelectLogFolder(_settings.LogFolder);
            if (folderName != null)
                _settings.LogFolder = folderName;
            else
                return null;
        }

        var filename = Path.Join(_settings.LogFolder, $"ctt-{DateTime.Now:u}.txt".ToPath());

        try
        {
            using var writer = new StreamWriter(filename);

            lock (_records)
            {
                foreach (var record in _records)
                {
                    writer.WriteLine(record);
                }

                _records.Clear();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex.Message);
            filename = null;
            MessageBox.Show($"Cannot save data into '{filename}':\n{ex.Message}", App.Name, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        return filename;
    }

    public static string? SelectLogFolder(string? folderName = null)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog()
        {
            Title = $"Select a folder to store {App.Name} log files",
            DefaultDirectory = !string.IsNullOrEmpty(folderName) ?
                folderName :
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        if (dialog.ShowDialog() == true)
        {
            return dialog.FolderName;
        }

        return null;
    }

    // Internal

    protected Logger() { }

    static Logger? _instance = null;

    readonly List<string> _records = [];
    readonly Settings _settings = Settings.Instance;
}
