using LeadFinder2GIS.Models;
using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LeadFinder2GIS.Services
{
    public class DgisPlacesService : IDgisPlacesService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;
        private readonly ILogger<DgisPlacesService> _logger;

        public DgisPlacesService(HttpClient httpClient, IConfiguration config, ILogger<DgisPlacesService> logger)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _apiKey = config?["DgisApiKey"] ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient.BaseAddress = new Uri("https://catalog.api.2gis.ru/");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        public async Task<List<Place>> FetchPlacesAsync(string query, string location, int radius)
        {
            try
            {
                var (lon, lat) = ParseLocation(location);
                var url = $"3.0/items?q={Uri.EscapeDataString(query)}" +
                         $"&point={lat.ToString(CultureInfo.InvariantCulture)},{lon.ToString(CultureInfo.InvariantCulture)}" +
                         $"&radius={radius}" +
                         $"&fields=items.point,items.contact_groups,items.reviews,items.address,items.adm_div,items.flags,items.name,items.address_name,items.contacts" +
                         $"&key={_apiKey}";

                _logger.LogInformation("Request to 2GIS: {Url}", url);
                var response = await _httpClient.GetStringAsync(url);
                _logger.LogDebug("Full API response: {Response}", response);

                var data = JsonConvert.DeserializeObject<DgisResponse>(response);

                if (data?.Error != null)
                {
                    _logger.LogError("2GIS API error: {Message}", data.Error.Message);
                    return new List<Place>();
                }

                if (data?.Result?.Items == null || !data.Result.Items.Any())
                {
                    _logger.LogWarning("No items found in 2GIS response");
                    return new List<Place>();
                }

                return data.Result.Items
                    .Where(item => item != null && !string.IsNullOrWhiteSpace(item.Name))
                    .Select(item => new Place
                    {
                        Id = item.Id ?? "unknown",
                        Name = item.Name ?? "Без названия",
                        Address = GetFullAddress(item),
                        Phone = GetPrimaryContact(item.ContactGroups, "phone"),
                        Website = GetPrimaryContact(item.ContactGroups, "website"),
                        Rating = item.Reviews?.Rating
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching places from 2GIS");
                return new List<Place>();
            }
        }

        public (double lon, double lat) ParseLocation(string location)
        {
            try
            {
                var parts = location.Replace(" ", "").Split(',');

                if (parts.Length != 2)
                    throw new ArgumentException("Неверный формат координат. Ожидается 'широта,долгота'");

                var lon = double.Parse(parts[0], CultureInfo.InvariantCulture);
                var lat = double.Parse(parts[1], CultureInfo.InvariantCulture);
                if (Math.Abs(lon) > 180 || Math.Abs(lat) > 90)
                {
                    _logger.LogWarning("Координаты переставлены местами (было: {Lon}, {Lat})", lon, lat);
                    (lon, lat) = (lat, lon);
                }

                return (lon, lat);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Location parsing failed for input: {Location}", location);
                throw new ArgumentException(
                    $"Ошибка парсинга координат: {location}. Пример: '56.328878,44.016631' (широта,долгота)",
                    ex
                );
            }
        }

        private string GetFullAddress(PlaceItem item)
        {
            if (item == null)
            {
                return "Адрес не указан";
            }

            if (!string.IsNullOrWhiteSpace(item.AddressName))
            {
                return item.AddressName;
            }

            var components = new List<string>();

            if (item.Address?.Components != null)
            {
                var street = item.Address.Components.FirstOrDefault(c => c?.Type == "street");
                var number = item.Address.Components.FirstOrDefault(c => c?.Type == "street_number");

                if (street != null && !string.IsNullOrWhiteSpace(street.Name))
                {
                    components.Add(street.Name);
                }

                if (number != null && !string.IsNullOrWhiteSpace(number.Number))
                {
                    components.Add(number.Number);
                }
            }

            if (components.Any())
                return string.Join(", ", components);

            if (item.AdmDiv != null)
            {
                var city = item.AdmDiv.FirstOrDefault(d => d?.Type == "city");
                if (city != null && !string.IsNullOrWhiteSpace(city.Name))
                    return city.Name;
            }

            return "Адрес не указан";
        }

        private string GetPrimaryContact(List<ContactGroup> groups, string contactType)
        {
            if (groups == null)
                return string.Empty;
            var typeVariants = contactType.ToLower() switch
            {
                "phone" => new[] { "phone", "phones", "телефон", "телефоны", "call", "мобильный" },
                "website" => new[] { "website", "site", "url", "сайт", "вебсайт", "web" },
                _ => new[] { contactType.ToLower() }
            };

            var contact = groups
                .Where(g => g != null && typeVariants.Contains(g.Type?.ToLower()))
                .SelectMany(g => g.Contacts ?? new List<Contact>())
                .FirstOrDefault(c => c != null && !string.IsNullOrWhiteSpace(c.Value));

            return contact?.Value ?? string.Empty;
        }
    }

    internal class DgisResponse
    {
        public string Api_version { get; set; }
        public ResultData Result { get; set; }
        public ErrorData Error { get; set; }
    }

    internal class ResultData
    {
        public List<PlaceItem> Items { get; set; }
    }

    internal class PlaceItem
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string AddressName { get; set; }
        public AddressInfo Address { get; set; }
        public List<ContactGroup> ContactGroups { get; set; } = new List<ContactGroup>();
        public ReviewInfo Reviews { get; set; }
        public List<AdminDivision> AdmDiv { get; set; } = new List<AdminDivision>();
    }

    internal class AddressInfo
    {
        public string Name { get; set; }
        public string BuildingId { get; set; }
        public List<AddressComponent> Components { get; set; }
        public string Postcode { get; set; }
    }

    internal class AddressComponent
    {
        public string Type { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string StreetId { get; set; }
        public string Comment { get; set; }
    }

    internal class AdminDivision
    {
        public string Name { get; set; }
        public string Type { get; set; }
    }

    internal class ContactGroup
    {
        public string Type { get; set; }  
        public List<Contact> Contacts { get; set; }
    }

    internal class Contact
    {
        public string Value { get; set; }
        public string Type { get; set; } 
    }

    internal class ReviewInfo
    {
        public double? Rating { get; set; }
    }

    internal class ErrorData
    {
        public string Code { get; set; }
        public string Message { get; set; }
    }
}