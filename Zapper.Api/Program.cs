using Microsoft.EntityFrameworkCore;
using Zapper.Api.Data;
using Zapper.Api.Models;
using Zapper.Api.Services;
using Zapper.Api.Services.Scrapers;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.

builder.Services.AddControllers();
builder.Services.Configure<ImageSettings>(builder.Configuration.GetSection(ImageSettings.BaseImagePathKey));
builder.Services.AddSingleton<PeriodicScrapeService>();

builder.Services.AddHostedService<PeriodicScrapeService>( provider => provider.GetService<PeriodicScrapeService>());
builder.Services.AddDbContext<ScrapedProductsContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("ScrapedItemsContext")));
//builder.Services.AddScoped<IScraper, TmsScraper>();
//builder.Services.AddScoped<IScraper, KspScraper>();

builder.Services.AddControllers().AddNewtonsoftJson();
//builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocument(c =>
{
    c.Title = "Zapper Api";
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseOpenApi();
    app.UseSwaggerUi3();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.UseRouting();

app.Run();
