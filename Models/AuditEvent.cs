using System;
using System.Collections.Generic;

namespace Audit.IngestApi.Models
{
    public class AuditEvent
    {
        public string EventId { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserId { get; set; }
        public string Role { get; set; }
        public string DeviceId { get; set; }
        public string Action { get; set; }
        public string Resource { get; set; }
        public bool Success { get; set; }
        public string Shift { get; set; }
        public string Location { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
        public DateTime ReceivedAt { get; set; }
    }
}
