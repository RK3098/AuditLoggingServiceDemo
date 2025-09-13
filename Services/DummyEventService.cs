using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Audit.IngestApi.Hubs;
using Audit.IngestApi.Models;

namespace Audit.IngestApi.Services
{
    public class DummyEventService : BackgroundService
    {
        private readonly ILogger<DummyEventService> _log;
        private readonly CouchDbService _couch;
        private readonly IHubContext<AuditHub> _hub;
        private readonly RuleEngine _rules;
        private readonly Random _rand = new();

        public DummyEventService(ILogger<DummyEventService> log, CouchDbService couch, IHubContext<AuditHub> hub, RuleEngine rules)
        {
            _log = log;
            _couch = couch;
            _hub = hub;
            _rules = rules;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _log.LogInformation("DummyEventService starting.");
            await _couch.EnsureDatabaseExistsAsync("audit_logs");
            await _couch.EnsureDatabaseExistsAsync("alerts");

            string[] users = { "operator_1", "operator_2", "supervisor_1" };

            while (!stoppingToken.IsCancellationRequested)
            {
                var ev = new AuditEvent {
                    EventId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.UtcNow,
                    UserId = users[_rand.Next(users.Length)],
                    Role = "operator",
                    DeviceId = $"gate-{_rand.Next(1,6)}",
                    Action = WeightedAction(),
                    Resource = $"machine-{_rand.Next(1,20)}",
                    Success = true,
                    Shift = new[] { "A", "B", "C" }[_rand.Next(3)],
                    Location = "factory-1"
                };

                if (_rand.NextDouble() < 0.05)
                {
                    ev.Action = "access_denied";
                    ev.Success = false;
                }

                await _couch.SaveDocumentAsync("audit_logs", ev.EventId, ev);

                var alerts = _rules.Evaluate(ev);
                foreach (var a in alerts)
                {
                    await _couch.SaveDocumentAsync("alerts", a.AlertId, a);
                    await _hub.Clients.All.SendAsync("Alert", a);
                }

                await _hub.Clients.All.SendAsync("NewEvent", ev);

                await Task.Delay(400, stoppingToken);
            }
        }

        private string WeightedAction()
        {
            var r = _rand.Next(100);
            if (r < 80) return "access_granted";
            if (r < 90) return "access_denied";
            if (r < 95) return "login";
            return "config_change";
        }
    }
}
