using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Text.Json;
using ERP_HRM_CMS.Models.Entities;

namespace ERP_HRM_CMS.Helpers
{
    public class AuditEntry
    {
        public AuditEntry(EntityEntry entry)
        {
            Entry = entry;
        }

        public EntityEntry Entry { get; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? IpAddress { get; set; }
        public string EntityName { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? RecordId { get; set; }
        public Dictionary<string, object?> OldValues { get; } = new();
        public Dictionary<string, object?> NewValues { get; } = new();
        public List<string> ChangedColumns { get; } = new();

        public AuditLog ToAuditLog()
        {
            return new AuditLog
            {
                UserId = UserId,
                Username = Username,
                Action = Action,
                EntityName = EntityName,
                RecordId = RecordId,
                OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
                NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues),
                AffectedColumns = ChangedColumns.Count == 0 ? null : JsonSerializer.Serialize(ChangedColumns),
                IpAddress = IpAddress,
                CreatedAt = VietnamTime.Now
            };
        }
    }
}
