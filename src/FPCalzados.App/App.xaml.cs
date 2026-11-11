using System.Windows;
using FPCalzados.Infrastructure;

namespace FPCalzados.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, args) =>
        {
            var path = CrashLog.WriteException(PortablePaths.FromExecutable(), args.Exception);
            MessageBox.Show(
                $"La aplicacion tuvo un error al iniciar o ejecutar.\n\nDetalle guardado en:\n{path}",
                "FPCalzados",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
            Shutdown(1);
        };

        base.OnStartup(e);
    }
}
