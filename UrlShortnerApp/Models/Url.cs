using System.ComponentModel.DataAnnotations;

namespace UrlShortener.Models;

public class Url
{
    public int Id { get; set; }

    [Required]
    [StringLength(10)]
    public string ShortCode { get; set; }

    [Required]
    [Url]
    public string OriginalUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}