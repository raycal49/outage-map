using NetTopologySuite.Features;
using ReactApp1.Server.Dtos;
using ReactApp1.Server.Models;
using System.Text.Json;

namespace ReactApp1.Server.Infrastructure.Http
{
    public class OutageService : IOutageService
    {
        private readonly HttpClient _httpClient;
        private readonly string _token;

        public OutageService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<OutageDataDto>> GetOutageData()
        {
            var response = await _httpClient.GetAsync(_httpClient.BaseAddress);

            System.Text.Json.Nodes.JsonObject responseBody = new();

            if (response.IsSuccessStatusCode == false)
            {
                responseBody = await response.Content.ReadFromJsonAsync<System.Text.Json.Nodes.JsonObject>();
                var message = responseBody!["message"]!.ToString();
                throw new HttpRequestException(message);
            }

            var dataDto = JsonSerializer.Deserialize<List<OutageDataDto>>(responseBody);

            return dataDto;
        }
    }
}
