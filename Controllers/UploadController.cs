using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetRoll.Data;
using NetRoll.Models;
using NetRoll.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace NetRoll.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/images")]
    public class UploadController : ControllerBase
    {
        private readonly IHttpContextAccessor _http;
        private readonly ImageStorageService _storage;
        private readonly ApplicationDbContext _db;
        private readonly ILogger<UploadController> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly NetRoll.Services.PlanService _plans;
        private readonly IStringLocalizer<UploadController> _L;
        public UploadController(IHttpContextAccessor http, ImageStorageService storage, ApplicationDbContext db, ILogger<UploadController> logger, IWebHostEnvironment env, NetRoll.Services.PlanService plans, IStringLocalizer<UploadController> localizer)
        {
            _http = http; _storage = storage; _db = db; _logger = logger; _env = env; _plans = plans; _L = localizer;
        }

    [HttpPost("upload")]
    [IgnoreAntiforgeryToken]
        [RequestSizeLimit(long.MaxValue)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upload()
        {
            try
            {
                var userId = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                if (Request.Form?.Files == null || Request.Form.Files.Count == 0) return BadRequest(_L["NoFiles"]);

                // Lekérjük vagy létrehozzuk a usage rekordot
                var usage = await _db.UserUsages.FirstOrDefaultAsync(u => u.OwnerUserId == userId);
                if (usage == null)
                {
                    usage = new NetRoll.Models.UserUsage { OwnerUserId = userId, FileCount = 0, StorageBytes = 0, ProductCount = 0, UpdatedUtc = DateTime.UtcNow };
                    _db.UserUsages.Add(usage);
                }
                // Felhasználó csomag beolvasása (ApplicationUser.PlanName) ha van
                var userEntity = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var planName = userEntity?.PlanName;
                var plan = _plans.GetPlans().FirstOrDefault(p => p.Name == planName) ?? _plans.GetDefaultPlan();
                // Fájlméretek előzetes összegzése a requestből (feltételezve stream elérhető)
                long incomingBytes = 0;
                foreach (var f in Request.Form.Files) incomingBytes += f.Length;
                // Limit ellenőrzések
                if (usage.FileCount + Request.Form.Files.Count > plan.MaxFileCount)
                    return BadRequest(_L["FileLimitExceeded", plan.MaxFileCount]);
                if (usage.StorageBytes + incomingBytes > plan.MaxStorageBytes)
                    return BadRequest(_L["StorageLimitExceeded", (plan.MaxStorageBytes/1024/1024)]);
                // ProductCount későbbi bekötéshez: ha lesz Product entitás és a feltöltés termékhez kötött, itt lehet ellenőrizni
                // if (usage.ProductCount >= plan.MaxProductCount) return BadRequest(_L["ProductLimitExceeded", plan.MaxProductCount]);

                var added = new List<ImageAsset>();
                foreach (var file in Request.Form.Files)
                {
                    if (file.Length == 0) continue;
                    await using var stream = file.OpenReadStream();
                    var saved = await _storage.SaveOriginalAsync(userId, file.FileName, stream);
                    var asset = new ImageAsset
                    {
                        OwnerUserId = userId,
                        OriginalFileName = file.FileName,
                        FileName = saved.StoredFileName,
                        ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
                        UploadedAt = DateTime.UtcNow,
                        Width = saved.Width,
                        Height = saved.Height
                    };
                    _db.ImageAssets.Add(asset);
                    added.Add(asset);
                    usage.FileCount += 1;
                    usage.StorageBytes += file.Length; // eredeti feltöltött méret hozzáadása
                }
                usage.UpdatedUtc = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                return Ok(added.Select(a => new { a.Id, a.OriginalFileName, a.FileName, a.UploadedAt }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Image upload failed");
                return StatusCode(500, _L["InternalError", ex.GetType().Name, ex.Message]);
            }
        }

        public record CropSaveDto(string fileName, string aspect, double x, double y, double width, double height);
    [HttpPost("save-crop")]
    [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SaveCrop([FromBody] CropSaveDto dto)
        {
            try
            {
                var userId = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var rx = (int)Math.Round(dto.x);
                var ry = (int)Math.Round(dto.y);
                var rw = (int)Math.Round(dto.width);
                var rh = (int)Math.Round(dto.height);
                if (rw <= 0 || rh <= 0)
                {
                    // Biztonságos alapérték: a kép 80%-a középre vágva
                    var userRoot = Path.Combine(_env.ContentRootPath, "wwwroot-protected", "media", userId);
                    var originalPath = Path.Combine(userRoot, "original", dto.fileName);
                    if (System.IO.File.Exists(originalPath))
                    {
                        using var img0 = await SixLabors.ImageSharp.Image.LoadAsync(originalPath);
                        rw = Math.Max(1, (int)Math.Round(img0.Width * 0.8));
                        rh = Math.Max(1, (int)Math.Round(img0.Height * 0.8));
                        rx = Math.Max(0, (img0.Width - rw) / 2);
                        ry = Math.Max(0, (img0.Height - rh) / 2);
                    }
                    else
                    {
                        rw = Math.Max(1, rw);
                        rh = Math.Max(1, rh);
                        rx = Math.Max(0, rx);
                        ry = Math.Max(0, ry);
                    }
                }
                var rect = new SixLabors.ImageSharp.Rectangle(rx, ry, rw, rh);
                _logger.LogInformation("SaveCrop request: file={file}, aspect={aspect}, rect=({x},{y},{w},{h})", dto.fileName, dto.aspect, rect.X, rect.Y, rect.Width, rect.Height);
                // Dinamikus alap képarány lista (felhasználói beállításból) pl: "1:1,4:3,3:4,16:9"
                var aspectListSetting = await _db.UserSettings.FirstOrDefaultAsync(s => s.OwnerUserId == userId && s.Key == "Image.Aspects");
                var allowedAspects = (aspectListSetting?.Value ?? "1:1,4:3,3:4")
                    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(a => a.Contains(':'))
                    .Distinct()
                    .Take(8)
                    .ToList();
                var aspect = dto.aspect;
                if (!allowedAspects.Contains(aspect)) aspect = allowedAspects.FirstOrDefault() ?? "1:1";
                // Felhasználói maximumok (UserImageSettings) – ezek a felső korlátok
                int maxW = 900, maxH = 900;
                try
                {
                    var userSettings = await _db.UserImageSettings.FirstOrDefaultAsync(s => s.OwnerUserId == userId);
                    if (userSettings != null)
                    {
                        maxW = userSettings.MaxWidth;
                        maxH = userSettings.MaxHeight;
                    }
                }
                catch (Exception exQuery)
                {
                    // Migrations should ensure the table exists; just log and continue with defaults
                    _logger.LogWarning(exQuery, "UserImageSettings lookup failed; using defaults {w}x{h}", maxW, maxH);
                }
                // Általános: arány szöveg -> szám (a:b)
                int aw = 1, ah = 1;
                var parts = aspect.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[0], out var p0) && int.TryParse(parts[1], out var p1) && p0 > 0 && p1 > 0)
                {
                    aw = p0; ah = p1;
                }
                // Cél méret: kitöltse a lehetséges legnagyobb teret úgy, hogy ne lépje túl a max-okat
                // Skála = min(maxW/aw, maxH/ah)
                double scale = Math.Min((double)maxW / aw, (double)maxH / ah);
                if (scale <= 0) scale = 1; // fallback
                int w = (int)Math.Floor(aw * scale);
                int h = (int)Math.Floor(ah * scale);
                // Biztonság: ne lépje túl a korlátokat (kerekítési hiba esetén)
                if (w > maxW) w = maxW;
                if (h > maxH) h = maxH;
                if (w < 1) w = 1; if (h < 1) h = 1;
                // Mentés resized mappába szolgáltatáson keresztül
                var folder = aspect.Replace(':', 'x'); // pl. 16:9 -> 16x9
                await _storage.SaveCroppedAsync(userId, dto.fileName, w, h, rect, folder);
                // Ellenőrzésként: állítsuk össze a várt path-ot és jelezzük vissza
                var dir = Path.Combine(_env.ContentRootPath, "wwwroot-protected", "media", userId, "resized", folder);
                var outPath = Path.Combine(dir, dto.fileName);
                var exists = System.IO.File.Exists(outPath);
                if (!exists)
                {
                    _logger.LogWarning("SaveCrop reported success, but output not found: {path}", outPath);
                }
                return Ok(new { path = outPath, folder, exists, width = w, height = h, rect = new { rect.X, rect.Y, rect.Width, rect.Height } });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SaveCrop failed");
                return StatusCode(500, $"SaveCrop error: {ex.GetType().Name}: {ex.Message}");
            }
        }

        public record MetaDto(int id, string? title, string? altText);
    [HttpPost("update-meta")]
    [IgnoreAntiforgeryToken]
        public async Task<IActionResult> UpdateMeta([FromBody] MetaDto dto)
        {
            try
            {
                var userId = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var asset = await _db.ImageAssets.FindAsync(dto.id);
                if (asset is null || asset.OwnerUserId != userId) return NotFound();
                asset.Title = dto.title;
                asset.AltText = dto.altText;
                await _db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UpdateMeta failed");
                return StatusCode(500, ex.Message);
            }
        }

    [HttpDelete("delete/{id:int}")]
    [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var userId = _http.HttpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                var asset = await _db.ImageAssets.FindAsync(id);
                if (asset is null || asset.OwnerUserId != userId) return NotFound();

                // Delete files: original, thumb, and all resized variants + usage update
                var root = Path.Combine(_env.ContentRootPath, "wwwroot-protected", "media", userId);
                var orig = Path.Combine(root, "original", asset.FileName);
                var thumb = Path.Combine(root, "thumbs", asset.FileName);
                long reclaimedBytes = 0;
                try { if (System.IO.File.Exists(orig)) { var fi = new FileInfo(orig); reclaimedBytes += fi.Length; System.IO.File.Delete(orig); } } catch { }
                try { if (System.IO.File.Exists(thumb)) { var fi = new FileInfo(thumb); reclaimedBytes += fi.Length; System.IO.File.Delete(thumb); } } catch { }
                var resizedRoot = Path.Combine(root, "resized");
                if (Directory.Exists(resizedRoot))
                {
                    foreach (var dir in Directory.GetDirectories(resizedRoot))
                    {
                        var p = Path.Combine(dir, asset.FileName);
                        try { if (System.IO.File.Exists(p)) { var fi = new FileInfo(p); reclaimedBytes += fi.Length; System.IO.File.Delete(p); } } catch { }
                    }
                }

                _db.ImageAssets.Remove(asset);
                var usage = await _db.UserUsages.FirstOrDefaultAsync(u => u.OwnerUserId == userId);
                if (usage != null)
                {
                    if (usage.FileCount > 0) usage.FileCount -= 1;
                    if (reclaimedBytes > 0 && usage.StorageBytes >= reclaimedBytes) usage.StorageBytes -= reclaimedBytes;
                    usage.UpdatedUtc = DateTime.UtcNow;
                }
                await _db.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete image failed");
                return StatusCode(500, ex.Message);
            }
        }
    }
}
