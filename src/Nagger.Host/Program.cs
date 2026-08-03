using System.Diagnostics;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Nagger.Core.Tasks;
using Nagger.Host.Api;
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

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<NaggerDbContext>().Database.MigrateAsync();

app.UseExceptionHandler(errorApp => errorApp.Run(async context =>
{
    var error = context.Features.Get<IExceptionHandlerFeature>()?.Error;
    if (error is ValidationException validation)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new ValidationError(validation.Errors));
        AppLog.ValidationRejected(app.Logger, context.Request.Path);
        return;
    }

    AppLog.UnexpectedFailure(app.Logger, context.Request.Path, error?.GetType().Name ?? "Unknown");
    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    await context.Response.WriteAsJsonAsync(new { error = "An unexpected error occurred." });
}));

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
