using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using ReactApp1.Server.Dtos;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace ReactApp1.Server.Infrastructure.Http
{
    public class DirectionsService : IDirectionsService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;

        public DirectionsService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _token = config["Mapbox"];
        }

        // okay... so this works but... is this even testable?
        public async Task<DirectionsResponseDto> GetDirections(string coordinates)
        {
            var queryString = new Dictionary<string, string?>()
            {
                ["access_token"] = _token,
                ["geometries"] = "geojson",
                ["overview"] = "full",
                ["annotations"] = "duration,distance,speed,congestion,congestion_numeric",
                ["steps"] = "true",
                //["waypoints_per_route"] = "true"
            };

            var url = QueryHelpers.AddQueryString(coordinates, queryString);

            var res = await _httpClient.GetFromJsonAsync<DirectionsResponseDto>(url);

            return res;
        }

    }
}    
