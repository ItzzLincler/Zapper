using Microsoft.EntityFrameworkCore;
using NSwag;
using NSwag.Generation.Processors.Security;
using Zapper.Data;
using Zapper.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

//builder.Services.AddScoped<ScraperService>();
//builder.Services.AddSingleton<PeriodicScrapeService>();
//builder.Services.AddHostedService<PeriodicScrapeService>();
//builder.Services.AddDbContext<ScrapedProductsContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("ScrapedItemsContext")));
//builder.Services.AddScoped<IScraper, TmsScraper>();
builder.Services.AddSwaggerDocument();



var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    app.UseOpenApi();
    app.UseSwaggerUi3();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

//service
//app.MapGet("/background", (PeriodicHostedService service) => {
//    return new PeriodicHostedServiceState(service.IsEnabled);
//});

//app.MapMethods("/background", new[] { "PATCH" }, (PeriodicHostedServiceState state, PeriodicScrapeService service) => { 
//    service.IsEnabled = state.IsEnabled;
//});
app.Run();

