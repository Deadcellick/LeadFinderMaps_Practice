using LeadFinder2GIS.Models;
using LeadFinder2GIS.Services;
using LeadFinderMaps.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace LeadFinder2GIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PlacesController : ControllerBase
    {
        private readonly IDgisPlacesService _service;
        private readonly ILogger<PlacesController> _logger;

        public PlacesController(IDgisPlacesService service, ILogger<PlacesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchRequest request)
        {
            _logger.LogInformation("Search request: Query={Query}, Location={Location}, Radius={Radius}",
                request.Query, request.Location, request.Radius);

            try
            {
                var results = await _service.FetchPlacesAsync(
                    request.Query,
                    request.Location,
                    request.Radius
                );

                _logger.LogDebug("Found {Count} places", results.Count);
                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search failed");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}