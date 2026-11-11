using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FPCalzados.Domain;

namespace FPCalzados.App;

public partial class MainWindow
{
    private void RequireAdmin()
    {
        if (_currentRole != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Esta accion requiere rol Admin.");
        }
    }

    private static DateOnly GetDate(DatePicker picker)
    {
        return DateOnly.FromDateTime(picker.SelectedDate ?? DateTime.Today);
    }

    private static int ParseInt(string text, string field)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out var value) &&
            !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            throw new FormatException($"El campo {field} debe ser entero.");
        }

        return value;
    }

    private int ParseSaleQuantity()
    {
        var quantity = ParseInt(SaleQuantityBox.Text, "cantidad");
        if (quantity <= 0 || quantity > 1000)
        {
            throw new InvalidOperationException("La cantidad debe estar entre 1 y 1000.");
        }

        return quantity;
    }

    private static bool TryParseInt(string text, out int value)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.CurrentCulture, out value) ||
               int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static int? ParseNullableInt(string text)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static decimal ParseDecimal(string text, string field)
    {
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out var value) &&
            !decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
        {
            throw new FormatException($"El campo {field} debe ser numerico.");
        }

        return value;
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        return decimal.TryParse(text, NumberStyles.Number, CultureInfo.CurrentCulture, out value) ||
               decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
    }

    private static decimal? ParseOptionalDecimal(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        return ParseDecimal(text, "valor opcional");
    }

    private static string Money(decimal value)
    {
        return value.ToString("C0", CultureInfo.CurrentCulture);
    }

    private static string FormatPaymentMethod(PaymentMethod method)
    {
        return method switch
        {
            PaymentMethod.Cash => "Efectivo",
            PaymentMethod.Debit => "Debito",
            PaymentMethod.Credit => "Credito",
            _ => method.ToString()
        };
    }

    private static string FormatSaleStatus(SaleStatus status)
    {
        return status switch
        {
            SaleStatus.Open => "Abierta",
            SaleStatus.PartiallyPaid => "Pago parcial",
            SaleStatus.Paid => "Pagada",
            SaleStatus.Returned => "Devuelta",
            SaleStatus.Cancelled => "Cancelada",
            _ => status.ToString()
        };
    }

    private static string FormatUserRole(UserRole role)
    {
        return role switch
        {
            UserRole.Admin => "Admin",
            UserRole.Seller => "Vendedor",
            _ => role.ToString()
        };
    }

    private void Status(string message)
    {
        StatusText.Text = message;
    }

    private void Log(string message)
    {
        ReportLogBox.AppendText($"{DateTime.Now:HH:mm:ss} {message}{Environment.NewLine}");
        ReportLogBox.ScrollToEnd();
        Status(message);
    }

    private async Task RunBusyAsync(Func<Task> action, string busyMessage = "Procesando...")
    {
        try
        {
            IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;
            Status(busyMessage);
            await action();
        }
        catch (Exception ex)
        {
            ShowError(ex);
        }
        finally
        {
            Mouse.OverrideCursor = null;
            IsEnabled = true;
        }
    }

    private void ShowError(Exception ex)
    {
        Status(ex.Message);
        MessageBox.Show(this, ex.Message, "FPCalzados", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
