using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Linq;
using System.Threading.Tasks;
using Audit.IngestApi.Hubs;
using Audit.IngestApi.Models;
using Audit.IngestApi.Services;

namespace Audit.IngestApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuditController : ControllerBase
    {
        private readonly CouchDbService _couch;
        private readonly RuleEngine _rules;
        private readonly IHubContext<AuditHub> _hub;

        public AuditController(CouchDbService couch, RuleEngine rules, IHubContext<AuditHub> hub)
        {
            _couch = couch;
            _rules = rules;
            _hub = hub;
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AuditEvent ev)
        {
            ev.EventId = string.IsNullOrWhiteSpace(ev.EventId) ? Guid.NewGuid().ToString() : ev.EventId;
            ev.Timestamp = ev.Timestamp == default ? DateTime.UtcNow : ev.Timestamp;
            ev.ReceivedAt = DateTime.UtcNow;

            await _couch.EnsureDatabaseExistsAsync("audit_logs");
            await _couch.SaveDocumentAsync("audit_logs", ev.EventId, ev);

            var alerts = _rules.Evaluate(ev);
            if (alerts.Any())
            {
                await _couch.EnsureDatabaseExistsAsync("alerts");
                foreach (var a in alerts)
                {
                    await _couch.SaveDocumentAsync("alerts", a.AlertId, a);
                    await _hub.Clients.All.SendAsync("Alert", a);
                }
            }

            await _hub.Clients.All.SendAsync("NewEvent", ev);

            return CreatedAtAction(nameof(Post), new { id = ev.EventId }, ev);
        }

        [HttpGet("events")]
        public async Task<IActionResult> GetEvents()
        {
            var raw = await _couch.GetAllDocsRawAsync("audit_logs", 200);
            return Content(raw, "application/json");
        }

        [HttpGet("alerts")]
        public async Task<IActionResult> GetAlerts()
        {
            var raw = await _couch.GetAllDocsRawAsync("alerts", 200);
            return Content(raw, "application/json");
        }
    }
}
