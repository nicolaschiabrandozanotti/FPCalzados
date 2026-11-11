namespace FPCalzados.Domain;

public sealed class BusinessSettings
{
    public int Id { get; set; } = 1;
    public string Name { get; set; } = "FPCalzados";
    public string Address { get; set; } = "";
    public string Phone { get; set; } = "";
    public string TaxId { get; set; } = "";
    public string TicketFooter { get; set; } = "Gracias por su compra";
}
