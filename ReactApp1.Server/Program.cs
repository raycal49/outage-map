using Azure.Identity;
using Geo.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;
using  NetTopologySuite;
using NetTopologySuite.IO.Converters;
using ReactApp1.Server.Infrastructure.Http;
using ReactApp1.Server.Models;
using StackExchange.Redis;
using System.Collections.Immutable;
using System.Runtime.Intrinsics;
using System.Text.Json.Serialization;
using static NetTopologySuite.Geometries.Utilities.GeometryMapper;

// Cause of your runtime error:
// Login failed because the target database ReactApp1Db does not exist yet (or user lacks access).
// Fix: ensure database is created / migrations applied before first usage and enable retry strategy.

var opts = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory,
    WebRootPath = "wwwroot"
};
var builder = WebApplication.CreateBuilder(opts);

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName!
        .Replace("+", ".")
        .Replace("`", "_"));
});

var connString = builder.Configuration.GetConnectionString("DefaultConnection")
                 ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not configured.");

Console.WriteLine($"[DEBUG] Using connection string: {connString}");

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connString, o =>
    {
        o.UseNetTopologySuite();
        // Adds transient failure resiliency suggested by the exception message.
        o.EnableRetryOnFailure();
    });

    options.EnableDetailedErrors();
    options.EnableSensitiveDataLogging();
    options.LogTo(Console.WriteLine);

});

// this is where we grab the token from secrets.json!
builder.Services.Configure<MapBoxOptions>(
    builder.Configuration.GetSection("Mapbox")); // binds MapBox:Token
builder.Services.AddTransient<MapBoxDirections>();

// DirectionsService is added here
builder.Services.AddHttpClient<DirectionsService>(client =>
{
    client.BaseAddress = new Uri("https://api.mapbox.com/directions/v5/mapbox/driving-traffic/");
});

// with the two above blocks of code defined, we can inject our DbContext into our web app
// while still adhering to MVC.

// GetConnectionString("Redis") is shorthand for GetSection("ConnectionStrings")["Redis"], if I recall correctly.
// it navigates to where connection strings are stored, in the key ConnectionStrings and specifically looks for the connection
// string for "Redis", where "Redis" is probably a key and the associated value is likely
// the connectionString for "Redis", if I had to guess.
string connectionString = builder.Configuration.GetConnectionString("Redis");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = connectionString;
});

builder.Services.AddHttpClient("OutageDataSource", HttpClient =>
{
    HttpClient.BaseAddress = new Uri("https://centerpoint.datacapable.com/datacapable/v2/p/centerpoint/r/texas/map/events");
});

var mapboxSecret = builder.Configuration["Mapbox"];

MapboxOptions token = new();

builder.Services.AddOptions<MapboxOptions>()
    .Bind(builder.Configuration)
    .Validate(options => !string.IsNullOrEmpty(options.MapboxToken), "MapboxToken is missing from configuration!")
    .ValidateOnStart();

builder.Services.AddHttpClient("MapBox", httpClient =>
{
    httpClient.BaseAddress = new Uri("https://api.mapbox.com");
    httpClient.DefaultRequestHeaders.Accept.ParseAdd("application/json");
})
    .AddHttpMessageHandler<MapBoxDirections>();

builder.Services.AddMapBoxGeocoding()
    .AddKey("Mapbox");

builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()); // if you use NTS GeoJSON
    o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
    o.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

//builder.Services.AddControllers().AddJsonOptions(o =>
//{
//    o.JsonSerializerOptions.Converters.Add(new NetTopologySuite.IO.Converters.GeoJsonConverterFactory()); // if you use NTS GeoJSON
//    o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals;
//});

var app = builder.Build();

// Ensure database exists / migrations applied before handling requests (prevents login failure).
//using (var scope = app.Services.CreateScope())
//{
//    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//    // Applies pending migrations (creates DB if it does not exist).
//    db.Database.Migrate();
//}

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DisplayRequestDuration();
    });

    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.MapFallbackToFile("/index.html");

app.Run();
