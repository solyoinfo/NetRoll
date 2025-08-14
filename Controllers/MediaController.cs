using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NetRoll.Controllers
{
    [Authorize]
    [Route("media")] // pl.: /media/{...}
    public class MediaController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _http;
        public MediaController(IWebHostEnvironment env, IHttpContextAccessor http)
        {
            _env = env; _http = http;
        }

        [HttpGet("{*path}")]
        public IActionResult Get(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return NotFound();
            var userId = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            // Várt mappa: wwwroot-protected/media/{UserId}/{path}
            var root = Path.Combine(_env.ContentRootPath, "wwwroot-protected", "media", userId);
            var full = Path.GetFullPath(Path.Combine(root, path));
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return Forbid();
            if (!System.IO.File.Exists(full)) return NotFound();
            var ext = Path.GetExtension(full).ToLowerInvariant();
            var contentType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };
            return PhysicalFile(full, contentType);
        }
    }
}
