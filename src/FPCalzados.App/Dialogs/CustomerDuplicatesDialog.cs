using System.Windows;
using System.Windows.Controls;
using FPCalzados.Infrastructure;

namespace FPCalzados.App.Dialogs;

public sealed class CustomerDuplicatesDialog : Window
{
    private readonly ApplicationStore store;
    private readonly ComboBox target = new() { DisplayMemberPath = nameof(CustomerDuplicateRow.Name), SelectedValuePath = nameof(CustomerDuplicateRow.CustomerId) };
    private readonly DataGrid sources = new() { AutoGenerateColumns = true, SelectionMode = DataGridSelectionMode.Extended, IsReadOnly = true, Height = 280 };
    private readonly TextBlock status = new() { TextWrapping = TextWrapping.Wrap };

    public CustomerDuplicatesDialog(Window owner, ApplicationStore store)
    {
        this.store = store;
        Owner = owner;
        Title = "Revisar clientes duplicados";
        Width = 760;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var panel = new StackPanel { Margin = new Thickness(18) };
        panel.Children.Add(new TextBlock
        {
            Text = "Selecciona el cliente que se conservará y, en la grilla, uno o más registros que se fusionarán. Todas sus ventas pasarán al cliente conservado.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 12)
        });
        panel.Children.Add(new TextBlock { Text = "Cliente a conservar" });
        target.Margin = new Thickness(0, 4, 0, 12);
        panel.Children.Add(target);
        panel.Children.Add(sources);
        status.Margin = new Thickness(0, 10, 0, 0);
        panel.Children.Add(status);

        var merge = new Button
        {
            Content = "Fusionar seleccionados",
            HorizontalAlignment = HorizontalAlignment.Right,
            Padding = new Thickness(16, 7, 16, 7),
            Margin = new Thickness(0, 12, 0, 0)
        };
        merge.Click += (_, _) => Merge();
        panel.Children.Add(merge);
        Content = panel;
        LoadRows();
    }

    private void LoadRows()
    {
        var rows = store.GetPotentialCustomerDuplicates();
        target.ItemsSource = rows;
        sources.ItemsSource = rows;
        target.SelectedIndex = rows.Count > 0 ? 0 : -1;
        status.Text = rows.Count == 0
            ? "No se detectaron duplicados automáticos."
            : $"Se encontraron {rows.Count} registros candidatos.";
    }

    private void Merge()
    {
        if (target.SelectedValue is not int targetId)
        {
            status.Text = "Selecciona el cliente que se conservará.";
            return;
        }

        var sourceIds = sources.SelectedItems.Cast<CustomerDuplicateRow>()
            .Select(row => row.CustomerId)
            .Where(id => id != targetId)
            .ToArray();
        if (sourceIds.Length == 0)
        {
            status.Text = "Selecciona al menos un registro distinto del cliente a conservar.";
            return;
        }

        var confirmation = MessageBox.Show(this,
            "La fusión reasignará las ventas y eliminará los registros duplicados. ¿Continuar?",
            "Confirmar fusión", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirmation != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            store.MergeCustomers(targetId, sourceIds);
            LoadRows();
            status.Text = "Clientes fusionados correctamente.";
        }
        catch (Exception exception)
        {
            status.Text = exception.Message;
        }
    }
}
