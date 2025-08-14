using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats;

namespace NetRoll.Services
{
    public class ImageStorageService
    {
        private readonly IWebHostEnvironment _env;
        public ImageStorageService(IWebHostEnvironment env) { _env = env; }

    public record SaveResult(string StoredFileName, int? Width, int? Height);

        private string UserRoot(string userId) => Path.Combine(_env.ContentRootPath, "wwwroot-protected", "media", userId);
        private string EnsureDir(string path) { Directory.CreateDirectory(path); return path; }

        public async Task<SaveResult> SaveOriginalAsync(string userId, string originalFileName, Stream data)
        {
            var safeName = MakeSafeFileName(originalFileName);
            var root = UserRoot(userId);
            var originalDir = EnsureDir(Path.Combine(root, "original"));
            var full = Path.Combine(originalDir, safeName);
            // unique name if exists
            full = EnsureUnique(full);
            using (var fs = System.IO.File.Create(full)) { await data.CopyToAsync(fs); }
            try
            {
                using var img = await Image.LoadAsync(full);
                try { await CreateThumbAsync(userId, Path.GetFileName(full), img); } catch { /* bélyegkép nélkül folytatjuk */ }
                return new SaveResult(Path.GetFileName(full), img.Width, img.Height);
            }
            catch (SixLabors.ImageSharp.UnknownImageFormatException)
            {
                // Nem támogatott vagy felismerhetetlen képformátum → csak az eredetit hagyjuk meg, bélyegkép nélkül
                return new SaveResult(Path.GetFileName(full), null, null);
            }
            catch
            {
                // Bármi más képbetöltési hiba esetén is őrizzük meg az eredetit
                return new SaveResult(Path.GetFileName(full), null, null);
            }
        }

        public async Task CreateThumbAsync(string userId, string fileName, Image img)
        {
            var root = UserRoot(userId);
            var thumbs = EnsureDir(Path.Combine(root, "thumbs"));
            using var clone = img.Clone(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Max, Size = new Size(200, 200) }));
            var path = Path.Combine(thumbs, fileName);
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            IImageEncoder encoder = ext switch
            {
                ".jpg" or ".jpeg" => new JpegEncoder { Quality = 80 },
                ".png" => new PngEncoder(),
                ".webp" => new WebpEncoder { Quality = 80 },
                _ => new JpegEncoder { Quality = 80 }
            };
            await clone.SaveAsync(path, encoder);
        }

        public async Task SaveResizedAsync(string userId, string fileName, int w, int h, Stream data)
        {
            var root = UserRoot(userId);
            var dir = EnsureDir(Path.Combine(root, "resized", $"{w}x{h}"));
            var path = Path.Combine(dir, fileName);
            using var img = await Image.LoadAsync(data);
            img.Mutate(x => x.Resize(new ResizeOptions { Mode = ResizeMode.Crop, Size = new Size(w, h) }));
            await img.SaveAsync(path, new JpegEncoder { Quality = 85 });
        }

        public async Task SaveCroppedAsync(string userId, string fileName, int targetW, int targetH, SixLabors.ImageSharp.Rectangle cropRect)
            => await SaveCroppedAsync(userId, fileName, targetW, targetH, cropRect, variantFolder: null);

    public async Task SaveCroppedAsync(string userId, string fileName, int targetW, int targetH, SixLabors.ImageSharp.Rectangle cropRect, string? variantFolder)
        {
            var root = UserRoot(userId);
            var original = Path.Combine(root, "original", fileName);
            if (!System.IO.File.Exists(original)) throw new FileNotFoundException("Original not found", original);
            var baseDir = Path.Combine(root, "resized");
            if (!string.IsNullOrWhiteSpace(variantFolder)) baseDir = Path.Combine(baseDir, variantFolder);
            var dir = EnsureDir(baseDir);
            var outPath = Path.Combine(dir, fileName);
            using var img = await Image.LoadAsync(original);
            // Clamp cropRect to image bounds to avoid exceptions
            int rx = Math.Max(0, (int)Math.Floor((double)cropRect.X));
            int ry = Math.Max(0, (int)Math.Floor((double)cropRect.Y));
            int rw = Math.Max(1, (int)Math.Round((double)cropRect.Width));
            int rh = Math.Max(1, (int)Math.Round((double)cropRect.Height));
            if (rx >= img.Width) rx = img.Width - 1;
            if (ry >= img.Height) ry = img.Height - 1;
            if (rx + rw > img.Width) rw = Math.Max(1, img.Width - rx);
            if (ry + rh > img.Height) rh = Math.Max(1, img.Height - ry);
            var safeRect = new Rectangle(rx, ry, rw, rh);
            // Először kivágjuk a megadott téglalapot (felhasználó által kiválasztott kompozíció)
            img.Mutate(x => x.Crop(safeRect));
            // Ha a kivágat aránya nem egyezik pontosan a célaránnyal (>1%), akkor letterbox (Pad fehér háttérrel), különben Crop.
            double cropAr = (double)safeRect.Width / safeRect.Height;
            double targetAr = targetW / (double)targetH;
            var diff = Math.Abs(cropAr - targetAr) / targetAr;
            var mode = diff > 0.01 ? ResizeMode.Pad : ResizeMode.Crop;
            img.Mutate(x => x.Resize(new ResizeOptions {
                Mode = mode,
                Size = new Size(targetW, targetH),
                PadColor = SixLabors.ImageSharp.Color.White
            }));
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            IImageEncoder encoder = ext switch
            {
                ".jpg" or ".jpeg" => new JpegEncoder { Quality = 85 },
                ".png" => new PngEncoder(),
                ".webp" => new WebpEncoder { Quality = 85 },
                _ => new JpegEncoder { Quality = 85 }
            };
            // Írjuk átmeneti fájlba, majd cseréljük atomikusan, így mindig frissül
            var tmpPath = outPath + ".tmp";
            await img.SaveAsync(tmpPath, encoder);
            try
            {
                if (System.IO.File.Exists(outPath)) System.IO.File.Delete(outPath);
            }
            catch { }
            System.IO.File.Move(tmpPath, outPath);
            // Megjegyzés: a fájlt az eredeti kiterjesztéssel mentjük (pl. resized/1x1/filename.jpg)
        }

        private static string MakeSafeFileName(string input)
        {
            var name = string.Join("_", input.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).Trim();
            if (string.IsNullOrWhiteSpace(name)) name = $"img_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
            return name;
        }
        private static string EnsureUnique(string fullPath)
        {
            var dir = Path.GetDirectoryName(fullPath)!;
            var name = Path.GetFileNameWithoutExtension(fullPath);
            var ext = Path.GetExtension(fullPath);
            var p = fullPath; int i = 1;
            while (System.IO.File.Exists(p)) p = Path.Combine(dir, $"{name}_{i++}{ext}");
            return p;
        }
    }
}
