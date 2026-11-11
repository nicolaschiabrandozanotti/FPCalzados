using System.Globalization;
using System.Windows.Data;
using FPCalzados.Domain;

namespace FPCalzados.App;

public sealed class PaymentMethodDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            PaymentMethod.Cash => "Efectivo",
            PaymentMethod.Debit => "Debito",
            PaymentMethod.Credit => "Credito",
            _ => ""
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed class SaleStatusDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            SaleStatus.Open => "Abierta",
            SaleStatus.PartiallyPaid => "Pago parcial",
            SaleStatus.Paid => "Pagada",
            SaleStatus.Returned => "Devuelta",
            SaleStatus.Cancelled => "Cancelada",
            _ => ""
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed class AlertTypeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            AlertType.LowStock => "Bajo stock",
            AlertType.OverduePayment => "Pago vencido",
            _ => ""
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}

public sealed class StockMovementTypeDisplayConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            StockMovementType.Purchase => "Compra",
            StockMovementType.Sale => "Venta",
            StockMovementType.Return => "Devolución",
            StockMovementType.Adjustment => "Ajuste",
            StockMovementType.Import => "Importación",
            _ => ""
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
