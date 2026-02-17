using Microsoft.EntityFrameworkCore;
using YMS.Server;
using YMS.Server.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IOpenLabMailer, Mailer>();

var envProvider = Environment.GetEnvironmentVariable("DB_PROVIDER");
var configProvider = builder.Configuration["Database:Provider"];
var dbProvider = string.IsNullOrWhiteSpace(envProvider) ? configProvider : envProvider;
dbProvider ??= "Sqlite";

builder.Services.AddDbContext<AppDbContext>(options =>
{
    if (dbProvider.Equals("Oracle", StringComparison.OrdinalIgnoreCase))
    {
        var oracleConnection = builder.Configuration.GetConnectionString("OracleConnection")
            ?? builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "User Id=YMS;Password=YMS;Data Source=localhost:1521/XEPDB1";

        options.UseOracle(oracleConnection);
        return;
    }

    var sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection")
        ?? "Data Source=yms.db";

    options.UseSqlite(sqliteConnection);
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient", policy =>
    {
        policy
            .SetIsOriginAllowed(origin =>
            {
                if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                {
                    return false;
                }

                return uri.Host is "localhost" or "127.0.0.1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowClient");
app.UseAuthorization();
app.MapControllers();

var disableSeedFromConfig = builder.Configuration.GetValue<bool>("Database:DisableSeed");
var disableSeedFromEnv = string.Equals(
    Environment.GetEnvironmentVariable("DISABLE_SEED"),
    "true",
    StringComparison.OrdinalIgnoreCase);

if (!disableSeedFromConfig && !disableSeedFromEnv)
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.InitializeAsync(dbContext);
}

app.Run();
