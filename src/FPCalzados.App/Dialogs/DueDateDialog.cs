using System.Windows;
using System.Windows.Controls;

namespace FPCalzados.App;

internal sealed class DueDateDialog : Window
{
    private readonly DatePicker _datePicker = new() { Margin = new Thickness(4) };
    private DateOnly? _dueDate;

    private DueDateDialog(DateOnly? currentDueDate)
    {
        Title = "Cambiar vencimiento";
        Width = 330;
        Height = 170;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.NoResize;
        _datePicker.SelectedDate = currentDueDate?.ToDateTime(TimeOnly.MinValue);

        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "Nuevo vencimiento" });
        panel.Children.Add(_datePicker);

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var clear = new Button { Content = "Sin vencimiento" };
        clear.Click += (_, _) =>
        {
            _dueDate = null;
            DialogResult = true;
        };
        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => DialogResult = false;
        var accept = new Button { Content = "Aceptar" };
        accept.Click += Accept_Click;
        buttons.Children.Add(clear);
        buttons.Children.Add(cancel);
        buttons.Children.Add(accept);
        panel.Children.Add(buttons);
        Content = panel;
    }

    public static DateOnly? Show(Window owner, DateOnly? currentDueDate, out bool accepted)
    {
        var dialog = new DueDateDialog(currentDueDate) { Owner = owner };
        accepted = dialog.ShowDialog() == true;
        return accepted ? dialog._dueDate : currentDueDate;
    }

    private void Accept_Click(object sender, RoutedEventArgs e)
    {
        if (_datePicker.SelectedDate is null)
        {
            MessageBox.Show(this, "Selecciona una fecha o usa 'Sin vencimiento'.", "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _dueDate = DateOnly.FromDateTime(_datePicker.SelectedDate.Value);
        DialogResult = true;
    }
}
