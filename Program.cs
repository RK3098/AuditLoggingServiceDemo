using System;
using System.Net.Http.Headers;
using System.Text;
using Audit.IngestApi.Hubs;
using Audit.IngestApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = WebApplication.CreateBuilder(args);

// Configuration for CouchDB - set in appsettings.json or override via env
var couchBase = builder.Configuration["Couch:BaseUrl"] ?? "http://localhost:5984";
var couchUser = builder.Configuration["Couch:User"] ?? "admin";
var couchPass = builder.Configuration["Couch:Password"] ?? "password";

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Typed HttpClient + basic auth for CouchDB
builder.Services.AddHttpClient<CouchDbService>(client =>
{
    client.BaseAddress = new Uri(couchBase);
    var auth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{couchUser}:{couchPass}"));
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", auth);
});

// App services
builder.Services.AddSingleton<RuleEngine>();
builder.Services.AddHostedService<DummyEventService>();

var app = builder.Build();

app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseRouting();
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();
app.MapHub<AuditHub>("/auditHub");

app.MapFallbackToFile("index.html");

app.Run();
