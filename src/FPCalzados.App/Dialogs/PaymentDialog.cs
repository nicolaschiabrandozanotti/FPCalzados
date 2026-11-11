using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using FPCalzados.Domain;

namespace FPCalzados.App;

internal readonly record struct RegisterPaymentRequest(PaymentMethod Method, decimal Amount, int Installments);

internal sealed class PaymentDialog : Window
{
    private readonly ComboBox _methodBox = new() { Margin = new Thickness(4), DisplayMemberPath = "Label", SelectedValuePath = "Method" };
    private readonly TextBox _amountBox = new() { Margin = new Thickness(4) };
    private readonly TextBox _installmentsBox = new() { Margin = new Thickness(4), Text = "1" };
    private readonly decimal _maxAmount;
    private RegisterPaymentRequest? _request;

    private PaymentDialog(IReadOnlyList<PaymentMethodOption> methods, decimal maxAmount)
    {
        _maxAmount = maxAmount;
        Title = "Registrar pago";
        Width = 360;
        Height = 240;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        _methodBox.ItemsSource = methods;
        _methodBox.SelectedValue = PaymentMethod.Cash;
        _amountBox.Text = maxAmount.ToString("0.##", CultureInfo.CurrentCulture);

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = $"Saldo pendiente: {maxAmount:C0}" });
        panel.Children.Add(new TextBlock { Text = "Medio de pago" });
        panel.Children.Add(_methodBox);
        panel.Children.Add(new TextBlock { Text = "Monto" });
        panel.Children.Add(_amountBox);
        panel.Children.Add(new TextBlock { Text = "Cuotas" });
        panel.Children.Add(_installmentsBox);

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

    public static RegisterPaymentRequest? Show(Window owner, IReadOnlyList<PaymentMethodOption> methods, decimal maxAmount)
    {
        var dialog = new PaymentDialog(methods, maxAmount) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog._request : null;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (_methodBox.SelectedValue is not PaymentMethod method)
        {
            MessageBox.Show(this, "Selecciona un medio de pago.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!decimal.TryParse(_amountBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) || amount <= 0m || amount > _maxAmount)
        {
            MessageBox.Show(this, "Monto invalido.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(_installmentsBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var installments) || installments <= 0)
        {
            MessageBox.Show(this, "Cuotas invalidas.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _request = new RegisterPaymentRequest(method, amount, installments);
        DialogResult = true;
    }
}
