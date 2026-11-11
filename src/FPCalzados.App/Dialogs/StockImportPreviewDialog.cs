using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using FPCalzados.Domain;

namespace FPCalzados.App;

internal sealed class StockImportPreviewDialog : Window
{
    private bool _accepted;

    private StockImportPreviewDialog(ImportResult<StockImportRow> result)
    {
        Title = "Vista previa de importación";
        Width = 980;
        Height = 720;
        MinWidth = 820;
        MinHeight = 560;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var root = new DockPanel { Margin = new Thickness(12) };
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = "Cancelar", MinWidth = 90 };
        cancel.Click += (_, _) => DialogResult = false;
        var accept = new Button { Content = "Importar", MinWidth = 90, IsEnabled = result.IsValid && result.Rows.Count > 0 };
        accept.Click += (_, _) => { _accepted = true; DialogResult = true; };
        buttons.Children.Add(cancel);
        buttons.Children.Add(accept);
        DockPanel.SetDock(buttons, Dock.Bottom);
        root.Children.Add(buttons);

        var panel = new StackPanel();
        root.Children.Add(panel);
        panel.Children.Add(new TextBlock
        {
            Text = result.IsValid
                ? $"Se importarán {result.Rows.Count} filas. Revisá la vista previa antes de confirmar."
                : $"Importación rechazada: hay {result.Errors.Count} errores para corregir.",
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, 0, 0, 8)
        });

        if (result.Errors.Count > 0)
        {
            panel.Children.Add(BuildGrid("Errores", result.Errors, 160, [
                Column("Fila", "RowNumber", "70"),
                Column("Mensaje", "Message", "*")
            ]));
        }

        panel.Children.Add(BuildGrid("Filas detectadas", result.Rows, 420, [
            Column("Fila", "SourceRowNumber", "60"),
            Column("Marca", "Brand", "120"),
            Column("Modelo", "Model", "140"),
            Column("Color", "Color", "100"),
            Column("Talle", "Size", "70"),
            Column("Cantidad", "Quantity", "80"),
            Column("Costo", "SupplierCost", "100", "{0:C0}"),
            Column("Precio", "CashPrice", "100", "{0:C0}"),
            Column("Codigo", "Sku", "120")
        ]));

        Content = root;
    }

    public static bool Show(Window owner, ImportResult<StockImportRow> result)
    {
        var dialog = new StockImportPreviewDialog(result) { Owner = owner };
        return dialog.ShowDialog() == true && dialog._accepted;
    }

    private static FrameworkElement BuildGrid<T>(string title, IReadOnlyList<T> rows, double maxHeight, IReadOnlyList<DataGridTextColumn> columns)
    {
        var group = new GroupBox { Header = title, Margin = new Thickness(0, 6, 0, 6) };
        var grid = new DataGrid
        {
            AutoGenerateColumns = false,
            IsReadOnly = true,
            CanUserAddRows = false,
            ItemsSource = rows,
            MinHeight = 90,
            MaxHeight = maxHeight
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
}
