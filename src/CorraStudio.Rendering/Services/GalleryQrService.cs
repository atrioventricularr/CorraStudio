using SkiaSharp;

namespace CorraStudio.Rendering.Services;

public interface IGalleryQrService
{
    Task<byte[]> GenerateGalleryQrCodeAsync(string galleryUrl, string sessionCode, string? logoPath = null);
    Task<byte[]> GenerateQrWithBrandingAsync(string galleryUrl, string sessionCode, byte[] logoData);
}

public class GalleryQrService : IGalleryQrService
{
    public async Task<byte[]> GenerateGalleryQrCodeAsync(string galleryUrl, string sessionCode, string? logoPath = null)
    {
        return await Task.Run(() =>
        {
            using var qrGenerator = new SkiaSharp.QrCode.SKQrCode();
            var qrCode = qrGenerator.Generate(galleryUrl);
            var matrix = qrCode.GetMatrix();
            
            var size = matrix.Size;
            var scale = 8; // pixels per module
            var imageSize = size * scale;
            var logoSize = imageSize / 4;
            
            using var surface = SKSurface.Create(new SKImageInfo(imageSize, imageSize));
            var canvas = surface.Canvas;
            
            canvas.Clear(SKColors.White);
            
            // Draw QR code
            using var blackPaint = new SKPaint { Color = SKColors.Black };
            using var whitePaint = new SKPaint { Color = SKColors.White };
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var isBlack = matrix[x, y];
                    var rect = new SKRect(x * scale, y * scale, (x + 1) * scale, (y + 1) * scale);
                    canvas.DrawRect(rect, isBlack ? blackPaint : whitePaint);
                }
            }
            
            // Draw text below QR
            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                TextSize = 16,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };
            
            var textY = imageSize + 25;
            canvas.DrawText($"Gallery Code: {sessionCode}", imageSize / 2, textY, textPaint);
            
            using var smallPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = 12,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true
            };
            
            canvas.DrawText("Scan to view & download your photos", imageSize / 2, textY + 20, smallPaint);
            
            // Add logo if provided
            if (!string.IsNullOrEmpty(logoPath) && File.Exists(logoPath))
            {
                using var logo = SKBitmap.Decode(logoPath);
                var logoRect = new SKRect(
                    (imageSize - logoSize) / 2,
                    (imageSize - logoSize) / 2,
                    (imageSize + logoSize) / 2,
                    (imageSize + logoSize) / 2
                );
                canvas.DrawBitmap(logo, logoRect);
            }
            
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        });
    }

    public async Task<byte[]> GenerateQrWithBrandingAsync(string galleryUrl, string sessionCode, byte[] logoData)
    {
        return await Task.Run(() =>
        {
            using var qrGenerator = new SkiaSharp.QrCode.SKQrCode();
            var qrCode = qrGenerator.Generate(galleryUrl);
            var matrix = qrCode.GetMatrix();
            
            var size = matrix.Size;
            var scale = 8;
            var imageSize = size * scale;
            var logoSize = imageSize / 4;
            
            using var surface = SKSurface.Create(new SKImageInfo(imageSize + 100, imageSize + 80));
            var canvas = surface.Canvas;
            
            canvas.Clear(SKColors.White);
            
            // Draw decorative border
            using var borderPaint = new SKPaint { Color = SKColors.FromHsl(200, 80, 50), StrokeWidth = 5 };
            canvas.DrawRoundRect(10, 10, imageSize + 80, imageSize + 60, 20, 20, borderPaint);
            
            // Draw QR code
            using var blackPaint = new SKPaint { Color = SKColors.Black };
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    if (matrix[x, y])
                    {
                        var rect = new SKRect(50 + x * scale, 50 + y * scale, 50 + (x + 1) * scale, 50 + (y + 1) * scale);
                        canvas.DrawRect(rect, blackPaint);
                    }
                }
            }
            
            // Add logo
            using var logo = SKBitmap.Decode(logoData);
            var logoRect = new SKRect(
                50 + (imageSize - logoSize) / 2,
                50 + (imageSize - logoSize) / 2,
                50 + (imageSize + logoSize) / 2,
                50 + (imageSize + logoSize) / 2
            );
            canvas.DrawBitmap(logo, logoRect);
            
            // Add title
            using var titlePaint = new SKPaint
            {
                Color = SKColors.FromHsl(200, 80, 40),
                TextSize = 18,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true,
                Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
            };
            canvas.DrawText("Corra Studio Gallery", 50 + imageSize / 2, imageSize + 30, titlePaint);
            
            // Add instruction
            using var instructionPaint = new SKPaint
            {
                Color = SKColors.Gray,
                TextSize = 12,
                TextAlign = SKTextAlign.Center,
                IsAntialias = true
            };
            canvas.DrawText($"Code: {sessionCode}", 50 + imageSize / 2, imageSize + 50, instructionPaint);
            
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        });
    }
}
