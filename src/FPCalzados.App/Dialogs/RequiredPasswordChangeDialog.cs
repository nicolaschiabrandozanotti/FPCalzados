using System.Windows;
using System.Windows.Controls;

namespace FPCalzados.App.Dialogs;

public sealed class RequiredPasswordChangeDialog : Window
{
    private readonly PasswordBox password = new();
    private readonly PasswordBox confirmation = new();
    private readonly TextBlock error = new() { Foreground = System.Windows.Media.Brushes.Firebrick, TextWrapping = TextWrapping.Wrap };

    public string NewPassword => password.Password;

    public RequiredPasswordChangeDialog(Window owner, bool required = true)
    {
        Owner = owner;
        Title = required ? "Cambio obligatorio de contraseña" : "Cambiar mi contraseña";
        Width = 460;
        MinHeight = 330;
        MaxHeight = 600;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResizeWithGrip;

        var panel = new StackPanel { Margin = new Thickness(24) };
        panel.Children.Add(new TextBlock
        {
            Text = required
                ? "La contraseña temporal debe cambiarse antes de usar la aplicación."
                : "Ingresa y confirma tu nueva contraseña.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 16)
        });
        panel.Children.Add(new TextBlock { Text = "Nueva contraseña (mínimo 10 caracteres, con letras y números)" });
        password.Margin = new Thickness(0, 4, 0, 12);
        panel.Children.Add(password);
        panel.Children.Add(new TextBlock { Text = "Confirmar contraseña" });
        confirmation.Margin = new Thickness(0, 4, 0, 12);
        panel.Children.Add(confirmation);
        panel.Children.Add(error);

        var accept = new Button
        {
            Content = "Guardar contraseña",
            IsDefault = true,
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(16, 7, 16, 7),
            Margin = new Thickness(0, 16, 0, 0)
        };
        accept.Click += (_, _) => Accept();
        var cancel = new Button
        {
            Content = required ? "Cancelar ingreso" : "Cancelar",
            IsCancel = true,
            Padding = new Thickness(16, 7, 16, 7),
            Margin = new Thickness(8, 16, 0, 0)
        };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(cancel);
        buttons.Children.Add(accept);
        panel.Children.Add(buttons);
        Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
    }

    private void Accept()
    {
        if (password.Password.Length < 10 || !password.Password.Any(char.IsLetter) || !password.Password.Any(char.IsDigit))
        {
            error.Text = "La contraseña debe tener al menos 10 caracteres e incluir letras y números.";
            return;
        }

        if (!string.Equals(password.Password, confirmation.Password, StringComparison.Ordinal))
        {
            error.Text = "Las contraseñas no coinciden.";
            return;
        }

        DialogResult = true;
    }
}
