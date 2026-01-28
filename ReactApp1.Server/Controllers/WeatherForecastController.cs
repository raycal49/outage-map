using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using ReactApp1.Server.Models;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using ReactApp1.Server.Dtos;
using System.Net.NetworkInformation;
using NetTopologySuite;
using ReactApp1.Server.Infrastructure.Http;
using System.Threading.Tasks;

using static ReactApp1.Server.Models.GeoJsonConversions;
using Route = ReactApp1.Server.Dtos.Route;

namespace ReactApp1.Server.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class MetroRouteController : ControllerBase
    {
        private readonly IDirectionsService _directionsService;
        //private readonly IOutageService _outageService;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IDistributedCache _cache;
        private readonly AppDbContext _dbService;
        private readonly IConfiguration _config;

        public MetroRouteController(IHttpClientFactory clientFactory, IDistributedCache cache, AppDbContext dbService,
            IDirectionsService directionsService, IOutageService outageService)
        {
            _clientFactory = clientFactory;
            _cache = cache;
            _dbService = dbService;
            _directionsService = directionsService;
            //_outageService = outageService;
        }

        //[HttpGet("OutageData")]
        //public async Task<ActionResult> GetOutageData()
        //{
        //    var outageDto = await _outageService.GetOutageData();

        //    var outageGeoJson = ConvertToFeatureCollection(outageDto);

        //    return Ok(outageGeoJson);
        //}

        //[HttpGet("Directions")]
        //public async Task<ActionResult<DirectionsResponseDto>> GetDirections(string coordinates)
        //{
        //    var directions = await _directionsService.GetDirections(coordinates);

        //    return Ok(directions);
        //}

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

        [HttpGet("Forward")]
        public async Task<IActionResult> Forward(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query is required.");

            var token = _config["Mapbox:SecretToken"];

            if (string.IsNullOrWhiteSpace(token))
                return StatusCode(500, "Mapbox access token is not configured.");

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

            var routes = directionsEntities.SelectMany(d => d.ToDto().routes);

            return Ok(routes);
        }

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