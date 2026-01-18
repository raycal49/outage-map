using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using ReactApp1.Server.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Route = ReactApp1.Server.Dtos.DirectionsResponseDto;
using ReactApp1.Server.Dtos;

namespace ReactApp1.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MapController : ControllerBase
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly IDistributedCache _cache;
        private readonly AppDbContext _dbService;
        private readonly IConfiguration _config;

        public MapController(IHttpClientFactory clientFactory, IDistributedCache cache, AppDbContext dbService, IConfiguration config)
        {
            _clientFactory = clientFactory;
            _cache = cache;
            _dbService = dbService;
            _config = config;
        }

        [HttpGet("RouteEntities")]
        public async Task<ActionResult<string>> GetRoutes()
        {
            var cachedRouteData = await _cache.GetStringAsync("RouteEntities");

            if (cachedRouteData is not null)
            {
                Console.WriteLine("Found routes data in cache.");
                return Ok(cachedRouteData);
            }

            var client = _clientFactory.CreateClient("MetroTransit");

            var response = await client.GetStringAsync("Routes");

            await _cache.SetStringAsync("RouteEntities", response);

            Console.WriteLine("Did not find routes data in cache.");
            return Ok(response);
        }

        [HttpGet("Directions")]
        public async Task<ActionResult<DirectionsResponseDto>> GetDirections(string coordinates)
        {
            var cached = await _cache.GetStringAsync(coordinates);

            //if (cached is not null)
            //{
            //    Console.WriteLine("Found direction data in cache.");
            //    var deserializedCache = JsonSerializer.Deserialize<DirectionsResponseDto>(cached);
            //    return Ok(deserializedCache);
            //}

            var mapClient = _clientFactory.CreateClient("MapBox");

            var url = $"/directions/v5/mapbox/driving-traffic/{coordinates}";

            var response = await mapClient.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to retrieve directions from MapBox.");
            }

            var responseContent = await response.Content.ReadAsStringAsync();

            var directionsData = JsonSerializer.Deserialize<DirectionsResponseDto>(responseContent);

            if (directionsData == null)
            {
                return NotFound("DirectionsResponseDto could not be deserialized.");
            }

            await _cache.SetStringAsync(coordinates, responseContent);

            Console.WriteLine("Did not find directions data in cache.");

            var entity = directionsData.ToEntity();

            if (_dbService.Find<DirectionsResponseEntity>(entity.Uuid) is null)
            {
                await _dbService.AddAsync(entity);
                await _dbService.SaveChangesAsync();
            }

            return Ok(directionsData);
        }

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

            return Content(body, "application/json");
        }

        [HttpGet("GetMetricData")]
        public async Task<ActionResult<List<Route>>> GetMetricData()
        {
            var directionsEntities = await _dbService.DirectionsResponseEntities
                .AsNoTracking()
                .Include(d => d.RouteEntities)
                .ToListAsync();

            var routeData = directionsEntities.SelectMany(d => d.ToDto().routes);

            return Ok(routeData);
        }

        [HttpGet("GetPage")]
        public async Task<ActionResult<List<Route>>> GetMetricData(int pageNumber, int pageSize)
        {
            var directionsEntities = await _dbService.DirectionsResponseEntities
                .OrderBy(x => x.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var routeData = directionsEntities.SelectMany(d => d.ToDto().routes);

            return Ok(routeData);
        }
    }
}