using LeadFinder2GIS.Controllers;
using LeadFinder2GIS.Models;
using LeadFinder2GIS.Services;
using LeadFinderMaps.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace LeadFinder2GIS.Tests.Controllers
{
    public class PlacesControllerTests
    {
        private readonly Mock<IDgisPlacesService> _mockService;
        private readonly Mock<ILogger<PlacesController>> _mockLogger;
        private readonly PlacesController _controller;

        public PlacesControllerTests()
        {
            _mockService = new Mock<IDgisPlacesService>();
            _mockLogger = new Mock<ILogger<PlacesController>>();
            _controller = new PlacesController(_mockService.Object, _mockLogger.Object);
        }

        [Fact]
        public async Task Search_ReturnsOkResult_WithPlaces()
        {
            _mockService
                .Setup(x => x.FetchPlacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Place>
                {
                    new Place { Name = "Test Cafe", Address = "ул. Тестовая" }
                });

            var result = await _controller.Search(new SearchRequest
            {
                Query = "кафе",
                Location = "56.318266,44.039754",
                Radius = 500
            });

            var okResult = Assert.IsType<OkObjectResult>(result);
            var places = Assert.IsType<List<Place>>(okResult.Value);
            Assert.Equal("Test Cafe", places[0].Name);
        }

        [Fact]
        public async Task Search_ReturnsEmptyList_WhenNoResults()
        {
            _mockService
                .Setup(x => x.FetchPlacesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(new List<Place>());
            var result = await _controller.Search(new SearchRequest { Query = "несуществующее место" });
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Empty((List<Place>)okResult.Value);
        }
    }
}