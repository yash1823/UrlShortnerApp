using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Data;
using UrlShortener.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("[controller]")]
public class UrlController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<UrlController> _logger;

    public UrlController(AppDbContext db, ILogger<UrlController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpPost("shorten")]
    public async Task<IActionResult> ShortenUrl([FromBody] string originalUrl)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
            return BadRequest("Invalid URL format");

        var shortCode = GenerateShortCode();
        var domain = $"{Request.Scheme}://{Request.Host}";

        var url = new Url
        {
            ShortCode = shortCode,
            OriginalUrl = originalUrl
        };

        _db.Urls.Add(url);
        await _db.SaveChangesAsync();

        return Ok(new { shortUrl = $"{domain}/{shortCode}" });
    }

    [HttpGet("{shortCode}")]
    public async Task<IActionResult> RedirectUrl(string shortCode)
    {
        var url = await _db.Urls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);
        if (url == null) return NotFound();

        return Redirect(url.OriginalUrl);
    }

    private string GenerateShortCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 8)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}