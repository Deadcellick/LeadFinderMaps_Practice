using LeadFinder2GIS.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LeadFinder2GIS.Services
{
    public interface IDgisPlacesService
    {
        Task<List<Place>> FetchPlacesAsync(string query, string location, int radius);
        (double lon, double lat) ParseLocation(string location);
    }
}