using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FPCalzados.Infrastructure;

namespace FPCalzados.App;

internal sealed class CustomerHistoryDialog : Window
{
    private CustomerHistoryDialog(CustomerHistory history)
    {
        Title = $"Historial de cliente - {history.Customer}";
        Width = 960;
        Height = 720;
        MinWidth = 820;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var root = new DockPanel { Margin = new Thickness(12) };
        var close = new Button { Content = "Cerrar", HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 90 };
        close.Click += (_, _) => Close();
        DockPanel.SetDock(close, Dock.Bottom);
        root.Children.Add(close);

        var panel = new StackPanel();
        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = panel };
        root.Children.Add(scroll);

        panel.Children.Add(new TextBlock
        {
            Text = history.Customer,
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 4)
        });
        panel.Children.Add(new TextBlock { Text = $"Teléfono: {Blank(history.Phone)}", Margin = new Thickness(0, 0, 0, 8) });
        panel.Children.Add(BuildTotals(history));
        panel.Children.Add(BuildGrid("Ventas", history.Sales, [
            Column("Venta", "SaleId", "70"),
            Column("Fecha", "CreatedAt", "140", "{0:dd/MM/yyyy HH:mm}"),
            Column("Vence", "DueDate", "95"),
            Column("Neto", "NetTotal", "110", "{0:C0}"),
            Column("Cobrado", "PaidTotal", "110", "{0:C0}"),
            Column("Saldo", "BalanceDue", "110", "{0:C0}"),
            Column("Estado", "Status", "110")
        ]));
        panel.Children.Add(BuildGrid("Pagos", history.Payments, [
            Column("Venta", "SaleId", "70"),
            Column("Fecha", "PaidAt", "140", "{0:dd/MM/yyyy HH:mm}"),
            Column("Medio", "Method", "100"),
            Column("Monto", "Amount", "110", "{0:C0}"),
            Column("Cuotas", "Installments", "80")
        ]));
        panel.Children.Add(BuildGrid("Devoluciones", history.Returns, [
            Column("Venta", "SaleId", "70"),
            Column("Fecha", "CreatedAt", "140", "{0:dd/MM/yyyy HH:mm}"),
            Column("Cant.", "Quantity", "70"),
            Column("Monto", "Amount", "110", "{0:C0}"),
            Column("A devolver", "CashToReturn", "110", "{0:C0}"),
            Column("Motivo", "Reason", "*")
        ]));

        Content = root;
    }

    public static void Show(Window owner, CustomerHistory history)
    {
        new CustomerHistoryDialog(history) { Owner = owner }.ShowDialog();
    }

    private static Grid BuildTotals(CustomerHistory history)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        AddTotal(grid, 0, 0, "Comprado", history.TotalPurchased);
        AddTotal(grid, 0, 1, "Cobrado", history.TotalPaid);
        AddTotal(grid, 1, 0, "Devuelto", history.TotalReturned);
        AddTotal(grid, 1, 1, "Saldo", history.BalanceDue);
        return grid;
    }

    private static void AddTotal(Grid grid, int row, int column, string label, decimal value)
    {
        var text = new TextBlock
        {
            Text = $"{label}: {value:C0}",
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 2, 12, 2)
        };
        Grid.SetRow(text, row);
        Grid.SetColumn(text, column);
        grid.Children.Add(text);
    }

    private static FrameworkElement BuildGrid<T>(string title, IReadOnlyList<T> rows, IReadOnlyList<DataGridTextColumn> columns)
    {
        var group = new GroupBox { Header = title, Margin = new Thickness(0, 6, 0, 6) };
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            ItemsSource = rows,
            MinHeight = 90,
            MaxHeight = 190
        };
        foreach (var column in columns)
        {
            grid.Columns.Add(column);
        }

        group.Content = grid;
        return group;
    }

    private static DataGridTextColumn Column(string header, string path, string width, string? format = null)
    {
        var binding = new Binding(path);
        if (format is not null)
        {
            binding.StringFormat = format;
        }

        return new DataGridTextColumn
        {
            Header = header,
            Binding = binding,
            Width = width == "*"
                ? new DataGridLength(1, DataGridLengthUnitType.Star)
                : new DataGridLength(double.Parse(width, System.Globalization.CultureInfo.InvariantCulture), DataGridLengthUnitType.Pixel)
        };
    }

    private static string Blank(string value) => string.IsNullOrWhiteSpace(value) ? "-" : value;
}
