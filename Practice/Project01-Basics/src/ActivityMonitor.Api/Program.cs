using ActivityMonitor.Api.Data;
using ActivityMonitor.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => builder.Configuration.Bind("Jwt", options));
builder.Services.AddAuthorization();

builder.Services.AddDbContext<ActivityDbContext>(options =>
    options.UseSqlServer(
        "Server=sql-prod-01.trg.local;Database=ActivityMonitor;User Id=am_app;Password=Tr4ck!ng2026;TrustServerCertificate=True"));

builder.Services.AddSingleton<IActivityService, ActivityService>();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
