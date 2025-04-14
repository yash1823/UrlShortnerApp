namespace UrlShortnerApp.Models
{
    public class UrlResponse
    {
        public string ShortUrl { get; set; }
        public string OriginalUrl { get; set; }
        public bool IsCustom { get; set; }  
    }
}
