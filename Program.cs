using Framedit.Services.ImageOptimizer;
using Framedit.Services.MetadataCleaner;
using Framedit.Services.SoundParser;
using Framedit.Services.VideoFrameParser;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddScoped<IFrameParserService, FrameParserService>();
builder.Services.AddScoped<IMetadataCleanerService, MetadataCleanerService>();
builder.Services.AddScoped<ISoundParserService, SoundParserService>();
builder.Services.AddScoped<IImageOptimizerService, ImageOptimizerService>();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 1_000_000_000;
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
