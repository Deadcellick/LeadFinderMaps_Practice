using FluentAssertions;
using LeadFinder2GIS.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;

async Task FetchPlacesAsync_ValidRequest_ReturnsPlaces()
{
    var mockHttp = new Mock<HttpMessageHandler>();
    mockHttp
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
                        { ""id"": ""1"", ""name"": ""Test Cafe"", ""addressName"": ""ул. Тестовая, 1"" }
                    ]
                }
            }")
        });

    var httpClient = new HttpClient(mockHttp.Object);
    var configMock = new Mock<IConfiguration>();
    configMock.Setup(c => c["DgisApiKey"]).Returns("fake_key");
    var loggerMock = new Mock<ILogger<DgisPlacesService>>();
    var service = new DgisPlacesService(httpClient, configMock.Object, loggerMock.Object);
    var result = await service.FetchPlacesAsync("кафе", "56.318266,44.039754", 500);
    result.Should().HaveCount(1);
    result[0].Name.Should().Be("Test Cafe");
}