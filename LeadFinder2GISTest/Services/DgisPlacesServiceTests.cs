using FluentAssertions;
using LeadFinder2GIS.Models;
using LeadFinder2GIS.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace LeadFinder2GIS.Tests.Services
{
    public class DgisPlacesServiceTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpHandler;
        private readonly HttpClient _httpClient;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly Mock<ILogger<DgisPlacesService>> _mockLogger;
        private readonly DgisPlacesService _service;

        public DgisPlacesServiceTests()
        {
            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpHandler.Object);
            _mockConfig = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<DgisPlacesService>>();

            _mockConfig.Setup(c => c["DgisApiKey"]).Returns("test_key");
            _service = new DgisPlacesService(_httpClient, _mockConfig.Object, _mockLogger.Object);
        }

        [Fact]
        public void ParseLocation_ValidInput_ReturnsCoordinates()
        {
            var input = "56.318266,44.039754";
            var (lon, lat) = _service.ParseLocation(input);
            lon.Should().BeApproximately(44.039754, 0.000001);
            lat.Should().BeApproximately(56.318266, 0.000001);
        }

        [Fact]
        public void ParseLocation_InvalidInput_ThrowsException()
        {
            var invalidInput = "invalid_coords";
            Assert.Throws<ArgumentException>(() => _service.ParseLocation(invalidInput));
        }

        [Fact]
        public async Task FetchPlacesAsync_ValidRequest_ReturnsPlaces()
        {
            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(@"{
                ""result"": {
                    ""items"": [
                        { 
                            ""id"": ""1"", 
                            ""name"": ""Test Cafe"", 
                            ""addressName"": ""ул. Тестовая, 1"",
                            ""address"": {
                                ""components"": [
                                    { ""type"": ""street"", ""name"": ""ул. Тестовая"" },
                                    { ""type"": ""street_number"", ""number"": ""1"" }
                                ]
                            },
                            ""contact_groups"": []
                        }
                    ]
                }
            }")
                });

            var result = await _service.FetchPlacesAsync("кафе", "56.318266,44.039754", 500);
            result.Should().ContainSingle();
            result[0].Should().BeEquivalentTo(new Place
            {
                Id = "1",
                Name = "Test Cafe",
                Address = "ул. Тестовая, 1",
                Phone = "",
                Website = "",
                Rating = null
            }, options => options
                .ExcludingMissingMembers() 
            );
        }
    }
}