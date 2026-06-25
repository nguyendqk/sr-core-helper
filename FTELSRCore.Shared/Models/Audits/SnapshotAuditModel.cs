using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace FTELSRCore.Models.Audits
{
    public record SnapshotAuditModel
    {
        public CreatorInfo Creator { get; init; }
        public string TableName { get; init; }
        public string Ip { get; init; }
        public string Address { get; init; }
        public string Device { get; init; }
        public string Method { get; init; }
        public Dictionary<string, object> KeyValues { get; } = [];
        public Dictionary<string, object> OldValues { get; } = [];
        public Dictionary<string, object> NewValues { get; } = [];
        public List<string> ChangedColumns { get; } = [];
        public List<PropertyEntry> TemporaryProperties { get; } = [];

        public bool HasTemporaryProperties => TemporaryProperties.Count is not 0;
    }
}