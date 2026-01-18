using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using ReactApp1.Server.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Route = ReactApp1.Server.Dtos.Route;
using ReactApp1.Server.Dtos;
using System.Net.NetworkInformation;
using NetTopologySuite;
using ReactApp1.Server.Infrastructure.Http;

namespace ReactApp1.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MetroRouteController : ControllerBase
    {
        private readonly DirectionsService _directionsService;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IDistributedCache _cache;
        private readonly AppDbContext _dbService;
        private readonly IConfiguration _config;

        //private AppDbContext _context;

        // this is how we constructor injection so that we don't need the [FromService] attribute
        // we take the constructor of the controller and inject an unallocated factory in it.
        // looks weird, tbh, is it just... okay to flat out put an unallocated factory some place???
        // i was taught unallocated variables are a big no-no.
        // but, yeah, this totally works this way. awesome.
        public MetroRouteController(IHttpClientFactory clientFactory, IDistributedCache cache, AppDbContext dbService,
            DirectionsService directionsService)
        {
            _clientFactory = clientFactory;
            _cache = cache;
            _dbService = dbService;
            _directionsService = directionsService;
        }

        //[HttpGet("OutageData")]
        //public async Task<ActionResult> GetOutageData()
        //{
        //    var client = _clientFactory.CreateClient("OutageDataSource");
            
        //    var response = await client.GetStringAsync(client.BaseAddress);

        //    //var json = System.IO.File.ReadAllText("Fixtures/events.json");

        //    var dataDto = JsonSerializer.Deserialize<List<OutageDataDto>>(response);

        //    var outageGeoJson = GeoJsonConversions.ConvertToFeatureCollection(dataDto);

        //    return Ok(outageGeoJson);
        //}

        [HttpGet("OutageData")]
        public ActionResult GetOutageData()
        {
            //var client = _clientFactory.CreateClient("OutageDataSource");

            //var response = await client.GetStringAsync(client.BaseAddress);

            var json = System.IO.File.ReadAllText("Fixtures/events.json");

            var dataDto = JsonSerializer.Deserialize<List<OutageDataDto>>(json);

            var outageGeoJson = GeoJsonConversions.ConvertToFeatureCollection(dataDto);

            return Ok(outageGeoJson);
        }

        //[HttpGet("Directions")]
        //public async Task<ActionResult<DirectionsResponseDto>> GetDirections(string coordinates)
        //{
        //    //var cached = await _cache.GetStringAsync(coordinates);

        //    //if (cached is not null)
        //    //{
        //    //    Console.WriteLine("Found direction data in cache.");
        //    //    var deserializedCache = JsonSerializer.Deserialize<DirectionsResponseDto>(cached);
        //    //    return Ok(deserializedCache);
        //    //}

        //    var mapClient = _clientFactory.CreateClient("MapBox");

        //    // Build the request URL with the provided parameters
        //    var url = $"/directions/v5/mapbox/driving-traffic/{coordinates}";

        //    var response = await mapClient.GetAsync(url);
        //    if (!response.IsSuccessStatusCode)
        //    {
        //        return StatusCode((int)response.StatusCode, "Failed to retrieve directions from MapBox.");
        //    }

        //    var responseContent = await response.Content.ReadAsStringAsync();

        //    var directionsData = JsonSerializer.Deserialize<DirectionsResponseDto>(responseContent);

        //    if (directionsData == null)
        //    {
        //        return NotFound("DirectionsResponseDto could not be deserialized.");
        //    }

        //    //await _cache.SetStringAsync(coordinates, responseContent);

        //    Console.WriteLine("Did not find directions data in cache.");

        //    return Ok(directionsData);
        //}

        [HttpGet("Directions")]
        public async Task<ActionResult<DirectionsResponseDto>> GetDirections(string coordinates)
        {
            var directions = await _directionsService.GetDirections(coordinates);

            return Ok(directions);
        }

        [HttpPost("SaveRoute")]
        public async Task<ActionResult<DisplayRouteEntity>> SaveRoute([FromBody] DisplayRouteDto dto)
        {
            Console.WriteLine("Entered the SaveRoute endpoint!");

            DisplayRouteEntity entity = new DisplayRouteEntity
            {
                Name = dto.Name,
                Distance = dto.Distance,
                Duration = dto.Duration
            };

            if (_dbService.Find<DisplayRouteEntity>(entity.Id) is null)
            {
                await _dbService.AddAsync(entity);
                await _dbService.SaveChangesAsync();
            }

            return Ok();
        }

        //[HttpGet("Geocoding")]
        //public async Task<Geo.MapBox.Models.Responses.Response<MapBoxGeocoding> >

        //[HttpGet("geocode")]
        //public async Task<ActionResult<Response<MapBoxGeocoding>>> GetGeocodeAsync(string query)
        //{
        //    if (string.IsNullOrWhiteSpace(query))
        //        return BadRequest("query is required.");

        //    GeocodingParameters param = new GeocodingParameters();
        //    param.Query = query;
        //    param.Proximity.Latitude = 29.7601;
        //    param.Proximity.Longitude = 95.3701;

        //    //var cacheKey = $"mapbox:geocode:{query}|limit=5|lang=en|prox=ip";
        //    var cached = await _cache.GetStringAsync(cacheKey);
        //    if (cached is not null)
        //    {
        //        var cachedResponse = JsonSerializer.Deserialize<Response<MapBoxGeocoding>>(cached);
        //        return Ok(cachedResponse);
        //    }

        //    var coder = _geocoder;
        //    //var client = _httpClientFactory.CreateClient("MapBox");

        //    var client = await _geocoder.GeocodingAsync(param);

        //    var encodedQuery = Uri.EscapeDataString(query);
        //    var url = $"/geocoding/v5/mapbox.places/{encodedQuery}.json?limit={limit}";
        //    if (!string.IsNullOrWhiteSpace(language)) url += $"&language={Uri.EscapeDataString(language)}";
        //    if (!string.IsNullOrWhiteSpace(proximity)) url += $"&proximity={Uri.EscapeDataString(proximity)}";

        //    using var response = await client.GetAsync(url);
        //    if (!response.IsSuccessStatusCode)
        //        return StatusCode((int)response.StatusCode, $"MapBox geocoding failed: {(int)response.StatusCode} {response.ReasonPhrase}");

        //    var json = await response.Content.ReadAsStringAsync();
        //    await _cache.SetStringAsync(cacheKey, json);

        //    var result = JsonSerializer.Deserialize<Response<MapBoxGeocoding>>(json, JsonOptions);
        //    if (result is null) return StatusCode((int)HttpStatusCode.InternalServerError, "Failed to deserialize geocoding response.");
        //    return Ok(result);
        //}

        [HttpGet("Forward")]
        public async Task<IActionResult> Forward(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query is required.");

            var token = _config["Mapbox:SecretToken"];

            if (string.IsNullOrWhiteSpace(token))
                return StatusCode(500, "Mapbox access token is not configured.");

            var cacheKey = query;

            //var cached = await _cache.GetStringAsync(cacheKey);
            //if (!string.IsNullOrEmpty(cached))
            //    return Content(cached, "application/json");

            var client = _clientFactory.CreateClient("MapBox");

            var url =
                "/search/geocode/v6/forward" +
                $"?q={Uri.EscapeDataString(query)}" +
                $"?access_token={Uri.EscapeDataString(token)}" +
                "&permanent=false" +
                "&autocomplete=true" +
                "&annotations=distance,duration,speed,congestion,congestion_numeric" +
                "&limit=5&country=us&language=en&proximity=-95.3698,29.7604";

            var resp = await client.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            if (!resp.IsSuccessStatusCode)
                return StatusCode((int)resp.StatusCode, body);

            // Cache the successful response (tune TTL as needed)
            //var options = new DistributedCacheEntryOptions
            //{
            //    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            //};
            //await _cache.SetStringAsync(cacheKey, body, options);

            return Content(body, "application/json");
        }

        [HttpGet("GetMetricData")]
        public async Task<ActionResult<List<Route>>> GetMetricData()
        {
            var directionsEntities = await _dbService.DirectionsResponseEntities
                .AsNoTracking()
                .Include(d => d.RouteEntities)
                .ToListAsync();

            var routes = directionsEntities.SelectMany(d => d.ToDto().routes);

            //var routes = directionsDtos.Select(d => d.routes.ToList());

            //foreach (var directionEntity in directionsEntities)
            //{
            //    directionsResponses.Add(directionEntity.ToDto());
            //}

            return Ok(routes);
        }

        //[HttpGet("GetPage")]
        //public async Task<ActionResult<PagedResult>> GetPage(int pageNumber, int pageSize)
        //{
        //    // base query (before paging)
        //    var baseQuery = _dbService.DirectionsResponseEntities
        //        .Include(d => d.RouteEntities)
        //            .ThenInclude(r => r.Legs)
        //        .OrderBy(x => x.Id);

        //    // Count ALL routes so we can determine page size
        //    // Gotta be a better way to do this
        //    var count = baseQuery.Select(b => b.RouteEntities).Count();

        //    // get the page items (still using Skip / Take)
        //    var directionsEntities = await baseQuery
        //        .Skip((pageNumber - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToListAsync();

        //    // map to your DTO / flatten
        //    var routeData = directionsEntities.SelectMany(d => d.ToDto().routes).ToList();

        //    // get total count
        //    //var count = routeData.Count;

        //    var result = new PagedResult(routeData, count, pageNumber, pageSize);

        //    return Ok(result);
        //}

        [HttpGet("GetPage")]
        public async Task<ActionResult<PagedResult>> GetPage(int pageNumber, int pageSize)
        {
            var baseQuery = _dbService.DisplayRouteEntities
                .OrderBy(d => d.Id);

            var count = baseQuery.Count();

            var result = await baseQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(result);
        }
    }
}