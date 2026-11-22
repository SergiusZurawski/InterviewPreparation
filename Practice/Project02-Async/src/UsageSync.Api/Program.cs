using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using UsageSync.Api.Data;
using UsageSync.Api.Infrastructure;
using UsageSync.Api.Options;
using UsageSync.Api.Services;
using UsageSync.Api.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => builder.Configuration.Bind("Jwt", options));
builder.Services.AddAuthorization();

builder.Services.AddDbContext<UsageDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Usage")));

builder.Services.AddHttpClient(NotificationClient.HttpClientName, client =>
    client.BaseAddress = new Uri(builder.Configuration["Notifications:WebhookBaseUrl"]!));
builder.Services.AddSingleton<INotificationClient, NotificationClient>();

builder.Services.Configure<SyncOptions>(builder.Configuration.GetSection(SyncOptions.SectionName));

builder.Services.AddScoped<IVendorUsageClient, VendorUsageClient>();
builder.Services.AddScoped<IUsageSyncService, UsageSyncService>();
builder.Services.AddSingleton<ISyncStatusStore, SyncStatusStore>();
builder.Services.AddHostedService<UsageSyncWorker>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
