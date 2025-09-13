using System;
using System.Collections.Generic;

namespace Audit.IngestApi.Models
{
    public class Alert
    {
        public string AlertId { get; set; }
        public string EventId { get; set; }
        public string Severity { get; set; }
        public string Reason { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<string> RelatedEventIds { get; set; } = new();
    }
}
