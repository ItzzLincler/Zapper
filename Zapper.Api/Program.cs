using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Zapper.Api.Data;
using Zapper.Api.Models;
using Zapper.Api.Services;
using Zapper.Api.Services.Scrapers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<ImageSettings>(builder.Configuration.GetSection(ImageSettings.BaseImagePathKey));
builder.Services.AddSingleton<PeriodicScrapeService>();

builder.Services.AddHostedService<PeriodicScrapeService>(provider => provider.GetService<PeriodicScrapeService>());
builder.Services.AddDbContext<ProductsContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("ZapperDB_Context")));
//builder.Services.AddDbContext<OldContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("ZapperDB_OldContext")));
builder.Services.AddScoped<ScraperBase, CompMasterScraper>();
builder.Services.AddScoped<ScraperBase, TmsScraper>();
builder.Services.AddScoped<ScraperBase, KspScraper>();

builder.Services.AddControllers().AddNewtonsoftJson();
//builder.Services.AddEndpointsApiExplorer();.......................0
builder.Services.AddSwaggerDocument(c =>
{
    c.Title = "Zapper Api";
});
//var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

//builder.Services.AddCors(options =>
//{
//    options.AddPolicy(name: MyAllowSpecificOrigins,
//                      policy =>
//                      {
//                          policy
//                          .AllowAnyOrigin()
//                          .AllowAnyHeader()
//                          .AllowAnyMethod();
//                      });
//});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi3();
}

app.UseHttpsRedirection();
app.UseCors(b => b.SetIsOriginAllowed(origin => true).AllowAnyHeader().AllowAnyMethod().AllowCredentials());

app.UseAuthorization();
app.UseStaticFiles(new StaticFileOptions()
{
    DefaultContentType ="image/jpeg",
    RequestPath = new PathString("/Images"),
    FileProvider = new PhysicalFileProvider("V:\\Data\\Services\\Zapper\\Thumbnails"),
});

app.MapControllers();
app.UseRouting();

app.Run();
