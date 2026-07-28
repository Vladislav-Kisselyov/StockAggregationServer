using Microsoft.EntityFrameworkCore;
using SAS.Application.Configuration;
using SAS.Application.Contracts;
using SAS.Application.Services;
using SAS.Infrastructure.Adapters;
using SAS.Infrastructure.Contracts;
using SAS.Infrastructure.Configuration;
using SAS.Infrastructure.Jobs;
using SAS.Infrastructure.Persistence;
using SAS.Infrastructure.WebSockets;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<IQuoteDeduplicator, QuoteDeduplicator>();
builder.Services.AddSingleton<IQuoteProcessor, QuoteProcessor>();

builder.Services.AddSingleton<QuoteStorageBackgroundService>();
builder.Services.AddSingleton<IQuoteStorage>(
    sp => sp.GetRequiredService<QuoteStorageBackgroundService>());
builder.Services.AddHostedService(
    sp => sp.GetRequiredService<QuoteStorageBackgroundService>());

builder.Services.Configure<AggregatorSettings>(
    builder.Configuration.GetSection(nameof(AggregatorSettings)));
builder.Services.Configure<StorageSettings>(
    builder.Configuration.GetSection(nameof(StorageSettings)));

builder.Services.AddSingleton<IExchangeAdapter, ExchangeAdapterTypeA>();
builder.Services.AddSingleton<IExchangeAdapter, ExchangeAdapterTypeB>();
builder.Services.AddSingleton<IExchangeAdapter, ExchangeAdapterTypeC>();
builder.Services.AddSingleton<IWebSocketExchangeClient, WebSocketExchangeClient>();

builder.Services.AddHostedService<DeduplicationCleanupBackgroundService>();
builder.Services.AddHostedService<ExchangeSupervisorBackgroundService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
