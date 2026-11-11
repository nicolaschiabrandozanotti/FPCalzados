using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using FPCalzados.Domain;
using FPCalzados.Infrastructure;

namespace FPCalzados.App;

internal readonly record struct StockAdjustmentRequest(int QuantityChange, StockMovementType Type, string Reason);

internal sealed class StockAdjustmentDialog : Window
{
    private readonly TextBox _quantityBox = new() { Margin = new Thickness(4), Text = "1" };
    private readonly ComboBox _typeBox = new() { Margin = new Thickness(4), DisplayMemberPath = "Label", SelectedValuePath = "Type" };
    private readonly TextBox _reasonBox = new() { Margin = new Thickness(4), Text = "Ajuste manual" };
    private readonly int _currentStock;
    private StockAdjustmentRequest? _request;

    private StockAdjustmentDialog(InventoryRow row)
    {
        _currentStock = row.StockQuantity;
        Title = "Movimiento manual de stock";
        Width = 420;
        Height = 300;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        _typeBox.ItemsSource = new[]
        {
            new MovementTypeOption(StockMovementType.Purchase, "Ingreso / compra"),
            new MovementTypeOption(StockMovementType.Adjustment, "Ajuste manual")
        };
        _typeBox.SelectedValue = StockMovementType.Adjustment;

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = $"Producto: {row.DisplayName}", TextWrapping = TextWrapping.Wrap, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = $"Stock actual: {row.StockQuantity}" });
        panel.Children.Add(new TextBlock { Text = "Cantidad a mover (+ ingresa, - descuenta)" });
        panel.Children.Add(_quantityBox);
        panel.Children.Add(new TextBlock { Text = "Tipo" });
        panel.Children.Add(_typeBox);
        panel.Children.Add(new TextBlock { Text = "Motivo" });
        panel.Children.Add(_reasonBox);

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

    public static StockAdjustmentRequest? Show(Window owner, InventoryRow row)
    {
        var dialog = new StockAdjustmentDialog(row) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog._request : null;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(_quantityBox.Text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var quantity) || quantity == 0)
        {
            MessageBox.Show(this, "Cantidad invalida.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_currentStock + quantity < 0)
        {
            MessageBox.Show(this, "El movimiento deja el stock en negativo.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_typeBox.SelectedValue is not StockMovementType type)
        {
            MessageBox.Show(this, "Selecciona un tipo de movimiento.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(_reasonBox.Text))
        {
            MessageBox.Show(this, "Carga un motivo.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _request = new StockAdjustmentRequest(quantity, type, _reasonBox.Text.Trim());
        DialogResult = true;
    }

    private sealed record MovementTypeOption(StockMovementType Type, string Label);
}
