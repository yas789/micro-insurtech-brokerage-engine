using MicroInsurTech.CoreEngine.Data;
using MicroInsurTech.CoreEngine.Schemes;
using MicroInsurTech.CoreEngine.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddScoped<IUnderwriterService, AvivaScheme>();
builder.Services.AddScoped<IUnderwriterService, AXAScheme>();
builder.Services.AddScoped<IUnderwriterService, NichePropertyCover>();
builder.Services.AddScoped<IQuoteOrchestrationService, QuoteOrchestrationService>();
builder.Services.AddScoped<IQuotePersistenceService, QuotePersistenceService>();
builder.Services.AddDbContext<BrokerageDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("BrokerDatabase")
        ?? throw new InvalidOperationException("Connection string 'BrokerDatabase' is required.");

    options.UseSqlServer(connectionString);
});
builder.Services.AddHttpClient<IPostcodeLookupService, PostcodesIoLookupService>(client =>
{
    var baseUrl = builder.Configuration["POSTCODES_API_BASE_URL"] ?? "https://postcodes.io";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(5);
});

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new { status = "Core engine running" }));
app.MapControllers();

app.Run();
