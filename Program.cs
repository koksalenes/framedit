using Mediaration.Constants;
using Mediaration.Services.ImageOptimizer;
using Mediaration.Services.MetadataCleaner;
using Mediaration.Services.SoundParser;
using Mediaration.Services.VideoFrameParser;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IFrameParserService, FrameParserService>();
builder.Services.AddScoped<IMetadataCleanerService, MetadataCleanerService>();
builder.Services.AddScoped<ISoundParserService, SoundParserService>();
builder.Services.AddScoped<IImageOptimizerService, ImageOptimizerService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = AppConstants.Upload.HttpRequestBytes;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseRouting();
app.UseStaticFiles();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
