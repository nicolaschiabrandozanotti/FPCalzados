using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace FPCalzados.App;

public sealed class ProductVariantEditRow(
    int? variantId,
    string color,
    string size,
    int stockQuantity,
    string? sku) : INotifyPropertyChanged
{
    private string _color = color;
    private string _size = size;
    private int _stockQuantity = stockQuantity;
    private string? _sku = sku;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int? VariantId { get; } = variantId;

    public string Color
    {
        get => _color;
        set => SetField(ref _color, value);
    }

    public string Size
    {
        get => _size;
        set => SetField(ref _size, value);
    }

    public int StockQuantity
    {
        get => _stockQuantity;
        set => SetField(ref _stockQuantity, value);
    }

    public string? Sku
    {
        get => _sku;
        set => SetField(ref _sku, value);
    }

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
