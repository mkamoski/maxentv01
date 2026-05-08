using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MaxEnt.Web;
using MaxEnt.Web.Data;
using MaxEnt.Web.Services;
using Microsoft.EntityFrameworkCore;
using SQLitePCL;

// Initialize SQLite - required before any EF Core/SQLite usage
Batteries_V2.Init();

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");

// Use Singleton so the single in-memory SQLite connection is shared across the app lifetime.
// Scoped in Blazor WASM would create a new DbContext (and a fresh, empty in-memory DB) per
// component, causing EnsureCreatedAsync to create tables in a different instance than the one
// components later receive from DI.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=:memory:"), ServiceLifetime.Singleton);
builder.Services.AddSingleton<IAppItemRepository, AppItemRepository>();
builder.Services.AddSingleton<IExperimentLogRepository, ExperimentLogRepository>();
builder.Services.AddSingleton<IFileDownloadService, BlazorFileDownloadService>();
builder.Services.AddSingleton<IMaxEntArtifactManager, MaxEntArtifactManager>();

var host = builder.Build();

// Resolve the singleton directly so EnsureCreatedAsync runs on the same instance
// that all repositories and components will use.
var db = host.Services.GetRequiredService<AppDbContext>();
await db.Database.EnsureCreatedAsync();

await host.RunAsync();
