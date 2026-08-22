using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nagger.Host;
using Nagger.Host.Api;
using Nagger.Host.Api.ExceptionHandling;
using Nagger.Host.Composition.Mediator;
using Nagger.Host.Composition.Persistence;
using Nagger.Host.Infrastructure;
using Nagger.Host.Mcp;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:5000");
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddNaggerPersistence(builder.Configuration);
builder.Services.AddNaggerMediator();
builder
    .Services.AddMcpServer()
    .WithHttpTransport()
    .WithTools<McpTaskTools>(new JsonSerializerOptions(JsonSerializerDefaults.Web));
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler(options => options.AllowStatusCode404Response = true);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Database.MigrateAsync();

app.UseExceptionHandler();

app.MapTaskEndpoints();
app.MapRecurringTaskEndpoints();
app.MapReportEndpoints();
app.MapMcp("/mcp");

app.Run();

public partial class Program;
