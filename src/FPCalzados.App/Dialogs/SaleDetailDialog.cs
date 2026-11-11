using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FPCalzados.Infrastructure;

namespace FPCalzados.App;

internal sealed class SaleDetailDialog : Window
{
    private SaleDetailDialog(SaleDetail detail)
    {
        Title = $"Detalle de venta #{detail.SaleId}";
        Width = 920;
        Height = 720;
        MinWidth = 780;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var root = new DockPanel { Margin = new Thickness(12) };
        var close = new Button { Content = "Cerrar", HorizontalAlignment = HorizontalAlignment.Right, MinWidth = 90 };
        close.Click += (_, _) => Close();
        DockPanel.SetDock(close, Dock.Bottom);
        root.Children.Add(close);

        var scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
        var panel = new StackPanel();
        scroll.Content = panel;
        root.Children.Add(scroll);

        panel.Children.Add(new TextBlock
        {
            Text = $"Venta #{detail.SaleId} - {detail.CreatedAt:dd/MM/yyyy HH:mm}",
            FontSize = 20,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        panel.Children.Add(new TextBlock { Text = $"Cliente: {Blank(detail.Customer)}    Tel: {Blank(detail.Phone)}" });
        panel.Children.Add(new TextBlock { Text = $"Vencimiento: {detail.DueDate?.ToString("dd/MM/yyyy") ?? "-"}    Estado: {detail.Status}" });
        panel.Children.Add(new TextBlock { Text = " " });

        panel.Children.Add(BuildTotals(detail));
        panel.Children.Add(BuildGrid("Productos", detail.Items, [
            Column("Producto", "Product", "*"),
            Column("Color", "Color", "90"),
            Column("Talle", "Size", "70"),
            Column("Cant.", "Quantity", "70"),
            Column("Dev.", "ReturnedQuantity", "70"),
            Column("Precio", "FinalUnitPrice", "100", "{0:C0}"),
            Column("Total", "LineTotal", "110", "{0:C0}")
        ]));
        panel.Children.Add(BuildGrid("Pagos", detail.Payments, [
            Column("Fecha", "PaidAt", "140", "{0:dd/MM/yyyy HH:mm}"),
            Column("Medio", "Method", "100"),
            Column("Monto", "Amount", "110", "{0:C0}"),
            Column("Cuotas", "Installments", "80")
        ]));
        panel.Children.Add(BuildGrid("Devoluciones", detail.Returns, [
            Column("Fecha", "CreatedAt", "140", "{0:dd/MM/yyyy HH:mm}"),
            Column("Cant.", "Quantity", "70"),
            Column("Monto", "Amount", "110", "{0:C0}"),
            Column("A devolver", "CashToReturn", "110", "{0:C0}"),
            Column("Motivo", "Reason", "*")
        ]));

        Content = root;
    }

    public static void Show(Window owner, SaleDetail detail)
    {
        new SaleDetailDialog(detail) { Owner = owner }.ShowDialog();
    }

    private static Grid BuildTotals(SaleDetail detail)
    {
        var grid = new Grid { Margin = new Thickness(0, 0, 0, 12) };
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition());

        AddTotal(grid, 0, 0, "Bruto", detail.GrossTotal);
        AddTotal(grid, 0, 1, "Devuelto", detail.ReturnedTotal);
        AddTotal(grid, 1, 0, "Neto", detail.NetTotal);
        AddTotal(grid, 1, 1, "Cobrado", detail.PaidTotal);
        AddTotal(grid, 2, 0, "Saldo", detail.BalanceDue);
        AddTotal(grid, 2, 1, "Dinero a devolver", detail.CashToReturn);
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
            MaxHeight = 180
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
