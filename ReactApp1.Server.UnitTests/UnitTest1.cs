using Microsoft.Extensions.Options;
using ReactApp1.Server.Infrastructure.Http;
using RichardSzalay.MockHttp;

namespace ReactApp1.Server.UnitTests
{
    public class DirectionsServiceClientTests
    {
        private readonly DirectionsService _sut;
        private readonly MockHttpMessageHandler _handlerMock = new();
        private readonly HttpClient _clientMock;
        private readonly IOptions<MapboxOptions> _options = Options.Create(new MapboxOptions
        {
            MapboxToken = "pk.1"
        });

        public DirectionsServiceClientTests ()
        {
            _handlerMock.When("https://api.mapbox.com/directions/v5/mapbox/driving-traffic/*")
                    .Respond("application/json", "{'name' : 'value'}");

            var _clientMock = _handlerMock.ToHttpClient();

            _sut = new DirectionsService(_clientMock, _options);
        }

        [Fact]
        public void Test1()
        {
            Assert.Equal(1, 1);
        }
    }
}