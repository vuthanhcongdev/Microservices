using Hangfire;
using Hangfire.API.Extensions;
using Infrastructure.Middlewares;
using Infrastructure.ScheduledJobs;
using Serilog;

Log.Information("Starting Basket API up");
Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateBootstrapLogger();

var builder = WebApplication.CreateBuilder(args);
Log.Information($"Start {builder.Environment.ApplicationName} up");

try
{
    builder.Host.AddAppConfigurations();
    // Add services to the container.
    builder.Services.AddConfigurationSettings(builder.Configuration);
    builder.Services.AddControllers();
    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();
    builder.Services.AddHangfireService();
    builder.Services.ConfigureServices();

    var app = builder.Build();

    app.MapGet("/", () => $"Welcome to {builder.Environment.ApplicationName}!");

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json",
                $"{builder.Environment.ApplicationName} v1"));
        });
    }

    app.UseRouting();

    app.UseHttpsRedirection();

    app.UseAuthorization();

    app.UseHangfireDashboard(builder.Configuration);

    app.UseEndpoints(endpoints =>
    {
        endpoints.MapDefaultControllerRoute();
    });

    app.Run();
}
catch (Exception ex)
{
    string type = ex.GetType().Name;
    if (type.Equals("StopTheHostException", StringComparison.Ordinal)) throw;

    Log.Fatal(ex, $"Unhandled exception: {ex.Message}");
}
finally
{
    Log.Information("Shut down Basket API complete");
    Log.CloseAndFlush();
}