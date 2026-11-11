namespace FPCalzados.Infrastructure;

public sealed class PortablePaths
{
    private PortablePaths(string root)
    {
        Root = root;
        DataDirectory = Path.Combine(root, "data");
        BackupDirectory = Path.Combine(root, "backups");
        ExportDirectory = Path.Combine(root, "exports");
        LogDirectory = Path.Combine(root, "logs");
        DatabasePath = Path.Combine(DataDirectory, "FPCalzados.sqlite");
    }

    public string Root { get; }
    public string DataDirectory { get; }
    public string BackupDirectory { get; }
    public string ExportDirectory { get; }
    public string LogDirectory { get; }
    public string DatabasePath { get; }

    public static PortablePaths FromRoot(string root)
    {
        return new PortablePaths(Path.GetFullPath(root));
    }

    public static PortablePaths FromExecutable()
    {
        var root = AppContext.BaseDirectory;
        return FromRoot(root);
    }

    public void EnsureDirectories()
    {
        Directory.CreateDirectory(DataDirectory);
        Directory.CreateDirectory(BackupDirectory);
        Directory.CreateDirectory(ExportDirectory);
        Directory.CreateDirectory(LogDirectory);
    }
}
