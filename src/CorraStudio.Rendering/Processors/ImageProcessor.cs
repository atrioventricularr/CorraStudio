namespace CorraStudio.Rendering.Processors;

public interface IImageProcessor
{
    Task<byte[]> ConvertToGrayscaleAsync(byte[] imageData);
    Task<byte[]> ConvertToSepiaAsync(byte[] imageData);
    Task<byte[]> AdjustBrightnessContrastAsync(byte[] imageData, float brightness, float contrast);
    Task<byte[]> ApplySharpenAsync(byte[] imageData, double amount = 1.0);
    Task<byte[]> ApplyBlurAsync(byte[] imageData, double radius = 5.0);
    Task<byte[]> RemoveBackgroundAsync(byte[] imageData, SKColor? keyColor = null, double tolerance = 0.1);
    Task<byte[]> AutoCropAsync(byte[] imageData);
    Task<byte[]> RotateAsync(byte[] imageData, float degrees);
    Task<byte[]> FlipAsync(byte[] imageData, bool horizontal, bool vertical);
}

public class ImageProcessor : IImageProcessor
{
    public async Task<byte[]> ConvertToGrayscaleAsync(byte[] imageData)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        
        var matrix = new float[]
        {
            0.3f, 0.59f, 0.11f, 0, 0,
            0.3f, 0.59f, 0.11f, 0, 0,
            0.3f, 0.59f, 0.11f, 0, 0,
            0, 0, 0, 1, 0
        };
        
        using var colorFilter = SKColorFilter.CreateColorMatrix(matrix);
        using var paint = new SKPaint { ColorFilter = colorFilter };
        
        canvas.DrawBitmap(bitmap, 0, 0, paint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> ConvertToSepiaAsync(byte[] imageData)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        
        var matrix = new float[]
        {
            0.393f, 0.769f, 0.189f, 0, 0,
            0.349f, 0.686f, 0.168f, 0, 0,
            0.272f, 0.534f, 0.131f, 0, 0,
            0, 0, 0, 1, 0
        };
        
        using var colorFilter = SKColorFilter.CreateColorMatrix(matrix);
        using var paint = new SKPaint { ColorFilter = colorFilter };
        
        canvas.DrawBitmap(bitmap, 0, 0, paint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> AdjustBrightnessContrastAsync(byte[] imageData, float brightness, float contrast)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        
        var b = brightness / 100f;
        var c = (100f + contrast) / 100f;
        c = c * c;
        
        var matrix = new float[]
        {
            c, 0, 0, 0, b,
            0, c, 0, 0, b,
            0, 0, c, 0, b,
            0, 0, 0, 1, 0
        };
        
        using var colorFilter = SKColorFilter.CreateColorMatrix(matrix);
        using var paint = new SKPaint { ColorFilter = colorFilter };
        
        canvas.DrawBitmap(bitmap, 0, 0, paint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> ApplySharpenAsync(byte[] imageData, double amount = 1.0)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        
        var sigma = 0.5;
        var radius = 1;
        
        using var image = SKImage.FromBitmap(bitmap);
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        
        // Apply sharpening through convolution
        using var sharpen = SKImageFilter.CreateConvolution(
            new SKSizeI(3, 3),
            new float[] { 0, -1, 0, -1, 5, -1, 0, -1, 0 },
            1, 0, SKPointI.Empty, SKFilterTileMode.Decal, true);
        
        using var paint = new SKPaint { ImageFilter = sharpen };
        canvas.DrawImage(image, 0, 0, paint);
        
        using var resultImage = surface.Snapshot();
        using var data = resultImage.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> ApplyBlurAsync(byte[] imageData, double radius = 5.0)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var image = SKImage.FromBitmap(bitmap);
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        
        using var blur = SKImageFilter.CreateBlur((float)radius, (float)radius);
        using var paint = new SKPaint { ImageFilter = blur };
        
        canvas.DrawImage(image, 0, 0, paint);
        
        using var resultImage = surface.Snapshot();
        using var data = resultImage.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> RemoveBackgroundAsync(byte[] imageData, SKColor? keyColor = null, double tolerance = 0.1)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var result = new SKBitmap(bitmap.Width, bitmap.Height);
        
        var targetColor = keyColor ?? SKColors.Green;
        
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                
                // Check if pixel is close to key color
                var diffR = Math.Abs(pixel.Red - targetColor.Red) / 255.0;
                var diffG = Math.Abs(pixel.Green - targetColor.Green) / 255.0;
                var diffB = Math.Abs(pixel.Blue - targetColor.Blue) / 255.0;
                var diff = (diffR + diffG + diffB) / 3;
                
                if (diff < tolerance)
                {
                    result.SetPixel(x, y, SKColors.Transparent);
                }
                else
                {
                    result.SetPixel(x, y, pixel);
                }
            }
        }
        
        using var image = SKImage.FromBitmap(result);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> AutoCropAsync(byte[] imageData)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        
        int left = bitmap.Width, right = 0, top = bitmap.Height, bottom = 0;
        var bgColor = bitmap.GetPixel(0, 0);
        
        for (int y = 0; y < bitmap.Height; y++)
        {
            for (int x = 0; x < bitmap.Width; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel != bgColor)
                {
                    left = Math.Min(left, x);
                    right = Math.Max(right, x);
                    top = Math.Min(top, y);
                    bottom = Math.Max(bottom, y);
                }
            }
        }
        
        if (left < right && top < bottom)
        {
            var width = right - left + 1;
            var height = bottom - top + 1;
            
            using var cropped = new SKBitmap(width, height);
            using var canvas = new SKCanvas(cropped);
            canvas.DrawBitmap(bitmap, -left, -top);
            
            using var image = SKImage.FromBitmap(cropped);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
        
        return imageData;
    }

    public async Task<byte[]> RotateAsync(byte[] imageData, float degrees)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var rotated = new SKBitmap(bitmap.Height, bitmap.Width);
        
        using var canvas = new SKCanvas(rotated);
        canvas.Translate(rotated.Width / 2, rotated.Height / 2);
        canvas.RotateDegrees(degrees);
        canvas.Translate(-bitmap.Width / 2, -bitmap.Height / 2);
        canvas.DrawBitmap(bitmap, 0, 0);
        
        using var image = SKImage.FromBitmap(rotated);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> FlipAsync(byte[] imageData, bool horizontal, bool vertical)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var flipped = new SKBitmap(bitmap.Width, bitmap.Height);
        
        using var canvas = new SKCanvas(flipped);
        canvas.Scale(horizontal ? -1 : 1, vertical ? -1 : 1, 
                     horizontal ? bitmap.Width / 2 : 0,
                     vertical ? bitmap.Height / 2 : 0);
        canvas.DrawBitmap(bitmap, 0, 0);
        
        using var image = SKImage.FromBitmap(flipped);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
