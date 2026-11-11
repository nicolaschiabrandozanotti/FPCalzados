using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace FPCalzados.App;

internal readonly record struct CashRegisterDialogRequest(decimal Amount, string? Notes);

internal sealed class CashRegisterDialog : Window
{
    private readonly TextBox _amountBox = new() { Margin = new Thickness(4) };
    private readonly TextBox _notesBox = new() { Margin = new Thickness(4), AcceptsReturn = true, Height = 70 };
    private CashRegisterDialogRequest? _request;

    private CashRegisterDialog(string title, string amountLabel, decimal suggestedAmount)
    {
        Title = title;
        Width = 380;
        Height = 260;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        _amountBox.Text = suggestedAmount.ToString("0.##", CultureInfo.CurrentCulture);

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = amountLabel });
        panel.Children.Add(_amountBox);
        panel.Children.Add(new TextBlock { Text = "Notas (opcional)" });
        panel.Children.Add(_notesBox);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => DialogResult = false;
        var accept = new Button { Content = "Aceptar" };
        accept.Click += Accept_Click;
        buttons.Children.Add(cancel);
        buttons.Children.Add(accept);
        panel.Children.Add(buttons);
        Content = panel;
    }

    public static CashRegisterDialogRequest? Show(Window owner, string title, string amountLabel, decimal suggestedAmount = 0m)
    {
        var dialog = new CashRegisterDialog(title, amountLabel, suggestedAmount) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog._request : null;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(_amountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) || amount < 0m)
        {
            MessageBox.Show(this, "Monto invalido.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _request = new CashRegisterDialogRequest(amount, string.IsNullOrWhiteSpace(_notesBox.Text) ? null : _notesBox.Text.Trim());
        DialogResult = true;
    }
}
