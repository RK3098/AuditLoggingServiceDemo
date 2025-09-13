using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Audit.IngestApi.Models;

namespace Audit.IngestApi.Services
{
    public class RuleEngine
    {
        private readonly ConcurrentDictionary<string, List<DateTime>> _failedAttempts = new();

        public List<Alert> Evaluate(AuditEvent ev)
        {
            var alerts = new List<Alert>();

            if (string.Equals(ev.Action, "access_denied", StringComparison.OrdinalIgnoreCase))
            {
                var list = _failedAttempts.GetOrAdd(ev.UserId ?? "unknown", _ => new List<DateTime>());
                lock (list)
                {
                    list.Add(DateTime.UtcNow);
                    list.RemoveAll(t => (DateTime.UtcNow - t) > TimeSpan.FromMinutes(1));
                    if (list.Count >= 3)
                    {
                        alerts.Add(new Alert {
                            AlertId = Guid.NewGuid().ToString(),
                            EventId = ev.EventId,
                            Severity = "HIGH",
                            Reason = $"Multiple failed access attempts ({list.Count}) for {ev.UserId}",
                            CreatedAt = DateTime.UtcNow
                        });
                        list.Clear();
                    }
                }
            }

            if (string.Equals(ev.Action, "config_change", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(ev.Role, "admin", StringComparison.OrdinalIgnoreCase))
            {
                alerts.Add(new Alert {
                    AlertId = Guid.NewGuid().ToString(),
                    EventId = ev.EventId,
                    Severity = "MEDIUM",
                    Reason = $"Config change by non-admin: {ev.UserId}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (!string.IsNullOrEmpty(ev.Shift))
            {
                var hr = ev.Timestamp.ToUniversalTime().Hour;
                bool outOfShift = ev.Shift.ToUpper() switch {
                    "A" => hr < 6 || hr >= 14,
                    "B" => hr < 14 || hr >= 22,
                    "C" => hr >= 6 && hr < 22,
                    _ => false
                };
                if (outOfShift)
                {
                    alerts.Add(new Alert {
                        AlertId = Guid.NewGuid().ToString(),
                        EventId = ev.EventId,
                        Severity = "LOW",
                        Reason = $"Access outside assigned shift ({ev.Shift})",
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            return alerts;
        }
    }
}
