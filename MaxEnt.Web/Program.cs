using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MaxEnt.Web;
using MaxEnt.Web.Data;
using MaxEnt.Web.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

// Initialize SQLite native bindings before any SQLite usage.
Batteries_V2.Init();

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Open ONE connection and keep it open for the lifetime of the app.
// SQLite :memory: databases are destroyed the moment their last connection closes,
// so we must hand EF Core a single persistent connection instead of a connection string.
var sqliteConnection = new SqliteConnection("Data Source=:memory:");
sqliteConnection.Open();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(sqliteConnection), ServiceLifetime.Singleton);
builder.Services.AddSingleton<IAppItemRepository, AppItemRepository>();
builder.Services.AddSingleton<IExperimentLogRepository, ExperimentLogRepository>();
builder.Services.AddSingleton<IFileDownloadService, BlazorFileDownloadService>();
builder.Services.AddSingleton<IMaxEntArtifactManager, MaxEntArtifactManager>();

var host = builder.Build();

// Create schema once on the shared connection.
var db = host.Services.GetRequiredService<AppDbContext>();
await db.Database.EnsureCreatedAsync();

await host.RunAsync();
