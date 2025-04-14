using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UrlShortener.Data;
using UrlShortener.Models;
using UrlShortnerApp.Models;

namespace UrlShortener.Controllers;

[ApiController]
[Route("api/url")] 
public class UrlController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<UrlController> _logger;

    public UrlController(AppDbContext db, ILogger<UrlController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // Existing auto-shorten endpoint
    [HttpPost("shorten")]
    public async Task<ActionResult<UrlResponse>> ShortenUrl([FromBody] string originalUrl)
    {
        if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
            return BadRequest("Invalid URL format");

        var shortCode = GenerateShortCode();
        return await CreateUrlEntry(originalUrl, shortCode, false);
    }

    // Custom URL endpoint
    [HttpPost("custom")]
    public async Task<ActionResult<UrlResponse>> CreateCustomUrl([FromBody] CustomUrlRequest request)
    {
        // Validate URL
        if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out _))
            return BadRequest("Invalid URL format");

        // Validate custom code (alphanumeric, 3-20 chars)
        if (string.IsNullOrWhiteSpace(request.CustomCode) ||
            !System.Text.RegularExpressions.Regex.IsMatch(request.CustomCode, "^[a-zA-Z0-9]{3,20}$"))
        {
            return BadRequest("Custom code must be 3-20 alphanumeric characters");
        }

        // Check if custom code exists
        if (await _db.Urls.AnyAsync(u => u.ShortCode == request.CustomCode))
        {
            return Conflict("Custom code already in use");
        }

        return await CreateUrlEntry(request.OriginalUrl, request.CustomCode, true);
    }

    // Shared creation logic
    private async Task<ActionResult<UrlResponse>> CreateUrlEntry(string originalUrl, string shortCode, bool isCustom)
    {
        var url = new Url
        {
            ShortCode = shortCode,
            OriginalUrl = originalUrl,
            IsCustom = isCustom
        };

        _db.Urls.Add(url);
        await _db.SaveChangesAsync();

        var domain = $"{Request.Scheme}://{Request.Host}";
        return Ok(new UrlResponse
        {
            ShortUrl = $"{domain}/{shortCode}",
            OriginalUrl = originalUrl,
            IsCustom = isCustom
        });
    }

    // Redirect endpoint
    [HttpGet("{shortCode:regex(^[[a-zA-Z0-9]]{{3,20}}$)}", Order = 1)]
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