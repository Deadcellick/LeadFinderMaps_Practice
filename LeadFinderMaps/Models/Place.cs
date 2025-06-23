namespace LeadFinder2GIS.Models
{
    public class Place
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public double? Rating { get; set; }
        public string Website { get; set; }
    }
    public class AddressInfo
    {
        public string Name { get; set; }
        public string BuildingId { get; set; }
        public List<AddressComponent> Components { get; set; }
        public string Postcode { get; set; }
    }
    public class AddressComponent
    {
        public string Type { get; set; }
        public string Number { get; set; }
        public string Name { get; set; }
        public string StreetId { get; set; }
    }
}