using ClosedXML.Excel;
using FPCalzados.Domain;
using Microsoft.EntityFrameworkCore;

namespace FPCalzados.Infrastructure;

internal sealed class AuditApplicationService(AppDbContextFactory factory, IClock clock)
{
    public void Record(UserRole? role, string action, string entity, string? entityId, string message)
    {
        using var db = factory.CreateDbContext();
        db.AuditLogs.Add(new AuditLog
        {
            OccurredAt = clock.Now,
            Role = role,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            Message = message
        });
        db.SaveChanges();
    }

    public IReadOnlyList<AuditLogRow> GetAuditLogs(int take = 300)
    {
        using var db = factory.CreateDbContext();
        return db.AuditLogs.AsNoTracking()
            .OrderByDescending(item => item.Id)
            .Take(Math.Clamp(take, 1, 2000))
            .AsEnumerable()
            .Select(item => new AuditLogRow(
                item.Id,
                item.OccurredAt,
                item.Role,
                item.Action,
                item.Entity,
                item.EntityId,
                item.Message))
            .ToList();
    }

    public string ExportAuditLogReport(string directory)
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"auditoria-{DateTime.Now:yyyyMMdd-HHmmss}.xlsx");
        using var workbook = new XLWorkbook();
        var worksheet = workbook.AddWorksheet("Auditoria");
        var headers = new[] { "Fecha", "Rol", "Accion", "Entidad", "Id", "Mensaje" };
        for (var index = 0; index < headers.Length; index++)
        {
            worksheet.Cell(1, index + 1).Value = headers[index];
            worksheet.Cell(1, index + 1).Style.Font.Bold = true;
        }

        var row = 2;
        foreach (var item in GetAuditLogs(2000))
        {
            worksheet.Cell(row, 1).Value = item.OccurredAt.DateTime;
            worksheet.Cell(row, 2).Value = item.Role?.ToString() ?? "Sistema";
            worksheet.Cell(row, 3).Value = item.Action;
            worksheet.Cell(row, 4).Value = item.Entity;
            worksheet.Cell(row, 5).Value = item.EntityId;
            worksheet.Cell(row, 6).Value = item.Message;
            row++;
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(path);
        return path;
    }
}
