using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using UrlShortener.Data;
using UrlShortener.Models;
using UrlShortnerApp.Models;

[ApiController]
[Route("api/url")]
public class UrlController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<UrlController> _logger;
    private readonly IOptions<UrlOptions> _urlOptions;

    public UrlController(AppDbContext db, ILogger<UrlController> logger, IOptions<UrlOptions> urlOptions)
    {
        _db = db;
        _logger = logger;
        _urlOptions = urlOptions;
    }

    [HttpPost("shorten")]
    public async Task<ActionResult<UrlResponse>> ShortenUrl([FromBody] string originalUrl)
    {
        try
        {
            if (!Uri.TryCreate(originalUrl, UriKind.Absolute, out _))
                return BadRequest(new { Message = "Invalid URL format" });

            var shortCode = GenerateShortCode();
            return await CreateUrlEntry(originalUrl, shortCode, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error shortening URL");
            return StatusCode(500, new { Message = "Error processing request" });
        }
    }

    [HttpPost("custom")]
    public async Task<ActionResult<UrlResponse>> CreateCustomUrl([FromBody] CustomUrlRequest request)
    {
        try
        {
            if (!Uri.TryCreate(request.OriginalUrl, UriKind.Absolute, out _))
                return BadRequest(new { Message = "Invalid URL format" });

            if (string.IsNullOrWhiteSpace(request.CustomCode) ||
                !System.Text.RegularExpressions.Regex.IsMatch(request.CustomCode, "^[a-zA-Z0-9]{3,20}$"))
            {
                return BadRequest(new
                {
                    Message = "Custom code must be 3-20 alphanumeric characters",
                    Requirements = "Only letters and numbers allowed"
                });
            }

            if (await _db.Urls.AnyAsync(u => u.ShortCode == request.CustomCode))
            {
                return Conflict(new
                {
                    Message = "Custom code already in use",
                    Suggestion = "Try a different code"
                });
            }

            return await CreateUrlEntry(request.OriginalUrl, request.CustomCode, true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating custom URL");
            return StatusCode(500, new { Message = "Error processing request" });
        }
    }

    private async Task<ActionResult<UrlResponse>> CreateUrlEntry(string originalUrl, string shortCode, bool isCustom)
    {
        try
        {
            var url = new Url
            {
                ShortCode = shortCode,
                OriginalUrl = originalUrl,
                IsCustom = isCustom
            };

            _db.Urls.Add(url);
            await _db.SaveChangesAsync();

            var domain = _urlOptions.Value.BaseUrl?.TrimEnd('/') ?? throw new InvalidOperationException("BaseUrl is not configured.");
            return Ok(new UrlResponse
            {
                ShortUrl = $"{domain}/{shortCode}",
                OriginalUrl = originalUrl,
                IsCustom = isCustom
            });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Database error creating URL entry");
            return StatusCode(500, new
            {
                Message = "Error saving URL",
                Suggestion = "Please try again"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating URL entry");
            throw; // Will be caught by global handler
        }
    }

    [HttpGet("{shortCode:regex(^[[a-zA-Z0-9]]{{3,20}}$)}", Order = 1)]
    public async Task<IActionResult> RedirectUrl(string shortCode)
    {
        try
        {
            var url = await _db.Urls.FirstOrDefaultAsync(u => u.ShortCode == shortCode);
            if (url == null)
            {
                _logger.LogWarning("Short code not found: {ShortCode}", shortCode);
                return NotFound(new
                {
                    Message = "Short URL not found",
                    Action = "Check the URL or create a new one"
                });
            }

            return Redirect(url.OriginalUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redirecting URL");
            return StatusCode(500, new { Message = "Error processing redirect" });
        }
    }

    private string GenerateShortCode()
    {
        try
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 8)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating short code");
            throw;
        }
    }
}