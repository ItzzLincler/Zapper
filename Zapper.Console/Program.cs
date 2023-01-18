using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure.Internal;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using Zapper.Api;
using Zapper.Api.Data;
using Zapper.Api.Models;

var connectionString = "Host=192.168.1.246; Port=5432; Username=Admin; Password=12345678; Database=ZapperDB";




var optionsBuilder = new DbContextOptionsBuilder<ProductsContext>();
optionsBuilder.UseNpgsql(connectionString);
var context = new ProductsContext(optionsBuilder.Options);


var withTimer = Stopwatch.StartNew();
var with = await context.ScrapedProducts.Include(p => p.Changes).ToListAsync();
withTimer.Stop();

var withoutTimer = Stopwatch.StartNew();
var without = await context.ScrapedProducts.Include(p => p.Changes).ToListAsync();
withoutTimer.Stop();

Console.WriteLine($"With: {withTimer.Elapsed}, Without: {withoutTimer.Elapsed}");

var products = context.ScrapedProducts.Include(p => p.Changes);
var most = products.OrderByDescending(p => p.Changes.Count).Skip(200).Take(10);
foreach (var item in most)
{
    Console.WriteLine($"{item.Name}");
    Console.WriteLine($"from: {item.ProductSource}" + ":\n");
    foreach (var change in item.Changes)
    {
        Console.WriteLine(change);
    }
    Console.WriteLine(item.ProductLink);
    Console.WriteLine();
}
Console.Read();