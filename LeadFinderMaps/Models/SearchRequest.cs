namespace LeadFinderMaps.Models
{
    public class SearchRequest
    {
        public string Query { get; set; }      
        public string Location { get; set; }   
        public int Radius { get; set; }        
    }
}