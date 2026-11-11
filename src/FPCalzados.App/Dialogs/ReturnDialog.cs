using System.Windows;
using System.Windows.Controls;
using FPCalzados.Infrastructure;

namespace FPCalzados.App;

internal readonly record struct ReturnRequest(int SaleItemId, int Quantity, string Reason);

internal sealed class ReturnDialog : Window
{
    private readonly ComboBox _itemBox = new() { Margin = new Thickness(4), DisplayMemberPath = nameof(ReturnableSaleItemRow.Product) };
    private readonly TextBox _quantityBox = new() { Margin = new Thickness(4), Text = "1" };
    private readonly TextBox _reasonBox = new() { Margin = new Thickness(4), Text = "Arrepentimiento" };
    private ReturnRequest? _request;

    private ReturnDialog(IReadOnlyList<ReturnableSaleItemRow> items)
    {
        Title = "Devolver item";
        Width = 420;
        Height = 260;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        _itemBox.ItemsSource = items;
        _itemBox.SelectedIndex = 0;

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "Item" });
        panel.Children.Add(_itemBox);
        panel.Children.Add(new TextBlock { Text = "Cantidad" });
        panel.Children.Add(_quantityBox);
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

    public static ReturnRequest? Show(Window owner, IReadOnlyList<ReturnableSaleItemRow> items)
    {
        var dialog = new ReturnDialog(items) { Owner = owner };
        return dialog.ShowDialog() == true ? dialog._request : null;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (_itemBox.SelectedItem is not ReturnableSaleItemRow row)
        {
            return;
        }

        if (!int.TryParse(_quantityBox.Text, out var quantity) ||
            quantity <= 0 ||
            quantity > row.PendingReturnQuantity)
        {
            MessageBox.Show(this, "Cantidad invalida.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _request = new ReturnRequest(row.SaleItemId, quantity, _reasonBox.Text.Trim());
        DialogResult = true;
    }
}
