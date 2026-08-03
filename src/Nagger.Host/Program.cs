using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Nagger.Host.Api;
using Nagger.Host.Api.ExceptionHandling;
using Nagger.Host.Composition.Mediator;
using Nagger.Host.Composition.Persistence;
using Nagger.Host;
using Nagger.Host.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls(builder.Configuration["Urls"] ?? "http://127.0.0.1:5000");
builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole();
builder.Services.AddNaggerPersistence(builder.Configuration);
builder.Services.AddNaggerMediator();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler(options => options.AllowStatusCode404Response = true);
builder.Services.AddExceptionHandler<ApiExceptionHandler>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Database.MigrateAsync();

app.UseExceptionHandler();

app.Use(async (context, next) =>
{
    var timer = Stopwatch.StartNew();
    await next(context);
    AppLog.RequestCompleted(app.Logger, context.Request.Path, context.Response.StatusCode, timer.ElapsedMilliseconds);
});

app.MapTaskEndpoints();
app.MapReportEndpoints();

app.Run();

public partial class Program;
