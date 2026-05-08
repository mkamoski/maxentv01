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

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=app.db"));
builder.Services.AddScoped<IAppItemRepository, AppItemRepository>();
builder.Services.AddScoped<IFileDownloadService, BlazorFileDownloadService>();
builder.Services.AddScoped<IMaxEntArtifactManager, MaxEntArtifactManager>();

var host = builder.Build();

await using var scope = host.Services.CreateAsyncScope();
await using var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
await db.Database.EnsureCreatedAsync();

await host.RunAsync();
