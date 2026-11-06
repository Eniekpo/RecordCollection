using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RecordCollection;
using RecordCollection.Services;
using RecordCollection.State;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddScoped<CollectionState>();
builder.Services.AddScoped<IJsonStorageService, JsonStorageService>();
builder.Services.AddScoped<IRecordService, RecordService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IBackupService, BackupService>();
builder.Services.AddScoped<IStatisticsService, StatisticsService>();
builder.Services.AddScoped<IImportExportService, ImportExportService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();

await builder.Build().RunAsync();
