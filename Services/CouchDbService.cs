using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Audit.IngestApi.Services
{
    public class CouchDbService
    {
        private readonly HttpClient _client;
        private readonly ILogger<CouchDbService> _log;
        private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

        public CouchDbService(HttpClient client, ILogger<CouchDbService> log)
        {
            _client = client;
            _log = log;
        }

        public async Task EnsureDatabaseExistsAsync(string dbName)
        {
            var resp = await _client.PutAsync($"/{dbName}", null);
            if (resp.IsSuccessStatusCode || resp.StatusCode == HttpStatusCode.PreconditionFailed) return;
            _log.LogWarning("Create DB {db} returned {code}", dbName, resp.StatusCode);
        }

        public async Task SaveDocumentAsync(string dbName, string id, object doc)
        {
            var json = JsonSerializer.Serialize(doc, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var escapedId = Uri.EscapeDataString(id);
            var resp = await _client.PutAsync($"/{dbName}/{escapedId}", content);

            if (!resp.IsSuccessStatusCode)
            {
                var fallback = await _client.PostAsync($"/{dbName}", content);
                if (!fallback.IsSuccessStatusCode)
                    _log.LogError("Failed to save doc to {db}: {code}", dbName, fallback.StatusCode);
            }
        }

        public async Task<string> GetAllDocsRawAsync(string dbName, int limit = 100)
        {
            var resp = await _client.GetAsync($"/{dbName}/_all_docs?include_docs=true&descending=true&limit={limit}");
            resp.EnsureSuccessStatusCode();
            return await resp.Content.ReadAsStringAsync();
        }
    }
}
