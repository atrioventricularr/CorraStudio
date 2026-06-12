using CorraStudio.Rendering.Models;
using System.Collections.Concurrent;

namespace CorraStudio.Rendering.RenderEngine;

public class HighQualityRenderEngine : IRenderEngine, IPrintRenderEngine, IBatchRenderEngine
{
    private readonly object _lock = new();
    private bool _isBatchPaused;
    private bool _isBatchCancelled;
    private readonly ConcurrentQueue<RenderJob> _batchQueue = new();
    private Task? _batchTask;

    public event EventHandler<RenderProgress>? ProgressChanged;

    public async Task<byte[]> RenderImageAsync(byte[] imageData, RenderSettings settings, ImageEffects? effects = null)
    {
        using var original = SKBitmap.Decode(imageData);
        
        var (targetWidth, targetHeight) = CalculateTargetDimensions(original.Width, original.Height, settings.Dpi);
        
        using var surface = SKSurface.Create(new SKImageInfo(targetWidth, targetHeight));
        var canvas = surface.Canvas;
        
        // Apply high quality settings
        if (settings.EnableAntiAlias)
        {
            canvas.ClipRect(new SKRect(0, 0, targetWidth, targetHeight), antialias: true);
        }
        
        // Draw background
        canvas.Clear(settings.BackgroundColor);
        
        // Draw and scale image
        using var paint = new SKPaint
        {
            IsAntialias = settings.EnableAntiAlias,
            FilterQuality = settings.EnableHighQuality ? SKFilterQuality.High : SKFilterQuality.Medium
        };
        
        var destRect = new SKRect(0, 0, targetWidth, targetHeight);
        var sourceRect = new SKRect(0, 0, original.Width, original.Height);
        
        canvas.DrawBitmap(original, sourceRect, destRect, paint);
        
        // Apply effects if any
        if (effects != null)
        {
            using var effectBitmap = SKBitmap.Decode(surface.Snapshot().Encode());
            var effectData = await ApplyEffectsAsync(effectBitmap.Encode().ToArray(), effects);
            using var effectBmp = SKBitmap.Decode(effectData);
            canvas.DrawBitmap(effectBmp, 0, 0);
        }
        
        using var finalImage = surface.Snapshot();
        var encoder = await GetEncoderForFormat(settings.OutputFormat);
        using var data = finalImage.Encode(encoder, settings.Quality);
        
        return data.ToArray();
    }

    public async Task<string> RenderImageToFileAsync(byte[] imageData, string outputPath, RenderSettings settings, ImageEffects? effects = null)
    {
        var rendered = await RenderImageAsync(imageData, settings, effects);
        await File.WriteAllBytesAsync(outputPath, rendered);
        return outputPath;
    }

    public async Task<BatchRenderResult> RenderBatchAsync(List<RenderJob> jobs, BatchRenderSettings settings, IProgress<RenderProgress>? progress = null)
    {
        var result = new BatchRenderResult();
        var startTime = DateTime.UtcNow;
        
        using var semaphore = new SemaphoreSlim(settings.MaxConcurrentJobs);
        var tasks = new List<Task>();
        
        var processedCount = 0;
        var totalJobs = jobs.Count;
        
        foreach (var job in jobs)
        {
            if (_isBatchCancelled) break;
            
            await semaphore.WaitAsync();
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var jobStartTime = DateTime.UtcNow;
                    
                    var outputData = await RenderImageAsync(job.ImageData, job.Settings, job.Effects);
                    
                    string outputPath;
                    if (!string.IsNullOrEmpty(job.OutputPath))
                    {
                        outputPath = job.OutputPath;
                    }
                    else
                    {
                        outputPath = GenerateOutputPath(settings, job);
                    }
                    
                    await File.WriteAllBytesAsync(outputPath, outputData);
                    
                    result.Results.Add(new RenderJobResult
                    {
                        JobId = job.Id,
                        Success = true,
                        OutputPath = outputPath,
                        ImageData = outputData,
                        ProcessingTime = DateTime.UtcNow - jobStartTime
                    });
                }
                catch (Exception ex)
                {
                    result.Results.Add(new RenderJobResult
                    {
                        JobId = job.Id,
                        Success = false,
                        ErrorMessage = ex.Message,
                        ProcessingTime = DateTime.UtcNow - jobStartTime
                    });
                    
                    if (settings.StopOnError)
                        _isBatchCancelled = true;
                }
                finally
                {
                    var current = Interlocked.Increment(ref processedCount);
                    var progressPercent = current * 100 / totalJobs;
                    
                    ProgressChanged?.Invoke(this, new RenderProgress
                    {
                        JobId = job.Id,
                        ProgressPercentage = progressPercent,
                        CurrentStep = $"Processing {current} of {totalJobs}",
                        Timestamp = DateTime.UtcNow,
                        ItemsProcessed = current,
                        TotalItems = totalJobs
                    });
                    
                    progress?.Report(new RenderProgress
                    {
                        ProgressPercentage = progressPercent,
                        CurrentStep = $"Processing {current} of {totalJobs}"
                    });
                    
                    semaphore.Release();
                }
            }));
        }
        
        await Task.WhenAll(tasks);
        
        result.TotalTime = DateTime.UtcNow - startTime;
        return result;
    }

    public async Task<byte[]> PrepareForPrintAsync(byte[] imageData, PrintSettings settings)
    {
        var result = imageData;
        
        if (settings.EnableColorCorrection)
        {
            result = await ColorCorrectAsync(result, settings);
        }
        
        if (settings.AddCropMarks)
        {
            result = await AddCropMarksAsync(result, settings);
        }
        
        if (settings.BleedSizePixels > 0)
        {
            result = await AddBleedAsync(result, settings);
        }
        
        return result;
    }

    public async Task<byte[]> AddCropMarksAsync(byte[] imageData, PrintSettings settings)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        
        canvas.DrawBitmap(bitmap, 0, 0);
        
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true
        };
        
        var markLength = 20;
        var margin = 10;
        
        // Draw crop marks at corners
        // Top-left
        canvas.DrawLine(margin, margin, margin + markLength, margin, paint);
        canvas.DrawLine(margin, margin, margin, margin + markLength, paint);
        
        // Top-right
        canvas.DrawLine(bitmap.Width - margin, margin, bitmap.Width - margin - markLength, margin, paint);
        canvas.DrawLine(bitmap.Width - margin, margin, bitmap.Width - margin, margin + markLength, paint);
        
        // Bottom-left
        canvas.DrawLine(margin, bitmap.Height - margin, margin + markLength, bitmap.Height - margin, paint);
        canvas.DrawLine(margin, bitmap.Height - margin, margin, bitmap.Height - margin - markLength, paint);
        
        // Bottom-right
        canvas.DrawLine(bitmap.Width - margin, bitmap.Height - margin, bitmap.Width - margin - markLength, bitmap.Height - margin, paint);
        canvas.DrawLine(bitmap.Width - margin, bitmap.Height - margin, bitmap.Width - margin, bitmap.Height - margin - markLength, paint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> ColorCorrectAsync(byte[] imageData, PrintSettings settings)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        
        // Apply basic color correction
        var colorMatrix = new float[]
        {
            1.1f, 0, 0, 0, 0,  // Red boost
            0, 1.05f, 0, 0, 0, // Green boost
            0, 0, 0.95f, 0, 0, // Blue reduce
            0, 0, 0, 1, 0
        };
        
        using var colorFilter = SKColorFilter.CreateColorMatrix(colorMatrix);
        using var paint = new SKPaint { ColorFilter = colorFilter };
        
        canvas.DrawBitmap(bitmap, 0, 0, paint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
        return data.ToArray();
    }

    public async Task<byte[]> AddBleedAsync(byte[] imageData, PrintSettings settings)
    {
        using var original = SKBitmap.Decode(imageData);
        var bleedSize = settings.BleedSizePixels;
        
        var newWidth = original.Width + (bleedSize * 2);
        var newHeight = original.Height + (bleedSize * 2);
        
        using var surface = SKSurface.Create(new SKImageInfo(newWidth, newHeight));
        var canvas = surface.Canvas;
        
        canvas.Clear(SKColors.White);
        
        // Draw original image with bleed extension
        using var paint = new SKPaint { FilterQuality = SKFilterQuality.High };
        canvas.DrawBitmap(original, bleedSize, bleedSize, paint);
        
        // Extend edges for bleed
        // Top strip
        var topRect = new SKRect(bleedSize, 0, original.Width + bleedSize, bleedSize);
        var topSource = new SKRect(bleedSize, bleedSize, original.Width + bleedSize, bleedSize + 1);
        canvas.DrawBitmapRect(original, topSource, topRect, paint);
        
        // Bottom strip
        var bottomRect = new SKRect(bleedSize, original.Height + bleedSize, original.Width + bleedSize, newHeight);
        var bottomSource = new SKRect(bleedSize, original.Height - 1, original.Width + bleedSize, original.Height);
        canvas.DrawBitmapRect(original, bottomSource, bottomRect, paint);
        
        // Left strip
        var leftRect = new SKRect(0, bleedSize, bleedSize, original.Height + bleedSize);
        var leftSource = new SKRect(bleedSize, bleedSize, bleedSize + 1, original.Height + bleedSize);
        canvas.DrawBitmapRect(original, leftSource, leftRect, paint);
        
        // Right strip
        var rightRect = new SKRect(original.Width + bleedSize, bleedSize, newWidth, original.Height + bleedSize);
        var rightSource = new SKRect(original.Width - 1, bleedSize, original.Width, original.Height + bleedSize);
        canvas.DrawBitmapRect(original, rightSource, rightRect, paint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> CreatePrintReadyAsync(byte[] imageData, PrintSettings settings)
    {
        var result = imageData;
        
        // Resize to target paper size
        var (targetWidth, targetHeight) = settings.PaperSize.GetDimensions(settings.Dpi);
        result = await ResizeAsync(result, targetWidth, targetHeight, true);
        
        // Apply print preparation
        result = await PrepareForPrintAsync(result, settings);
        
        return result;
    }

    public async Task<byte[]> CreateColorTestPageAsync(PrintSettings settings)
    {
        var (width, height) = settings.PaperSize.GetDimensions(settings.Dpi);
        
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        
        canvas.Clear(SKColors.White);
        
        // Draw color bars
        var colors = new[] {
            SKColors.Red, SKColors.Green, SKColors.Blue,
            SKColors.Cyan, SKColors.Magenta, SKColors.Yellow,
            SKColors.Black, SKColors.White, SKColors.Gray
        };
        
        var barWidth = width / colors.Length;
        var barHeight = height / 3;
        
        for (int i = 0; i < colors.Length; i++)
        {
            using var paint = new SKPaint { Color = colors[i] };
            canvas.DrawRect(i * barWidth, barHeight, barWidth, barHeight, paint);
        }
        
        // Add gradient
        var gradientColors = new[] { SKColors.Black, SKColors.White };
        var shader = SKShader.CreateLinearGradient(
            new SKPoint(0, barHeight * 2),
            new SKPoint(width, barHeight * 2),
            gradientColors,
            null,
            SKShaderTileMode.Clamp);
        
        using var gradientPaint = new SKPaint { Shader = shader };
        canvas.DrawRect(0, barHeight * 2, width, barHeight, gradientPaint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> CreateAlignmentPageAsync(PrintSettings settings)
    {
        var (width, height) = settings.PaperSize.GetDimensions(settings.Dpi);
        
        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        
        canvas.Clear(SKColors.White);
        
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            StrokeWidth = 2,
            IsAntialias = true
        };
        
        // Draw grid
        for (int x = 0; x < width; x += 100)
        {
            canvas.DrawLine(x, 0, x, height, paint);
        }
        
        for (int y = 0; y < height; y += 100)
        {
            canvas.DrawLine(0, y, width, y, paint);
        }
        
        // Draw alignment targets
        var targetSize = 50;
        
        // Center target
        canvas.DrawCircle(width / 2, height / 2, targetSize, paint);
        canvas.DrawCircle(width / 2, height / 2, targetSize / 2, paint);
        canvas.DrawCircle(width / 2, height / 2, 2, paint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> ApplyEffectsAsync(byte[] imageData, ImageEffects effects)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        
        canvas.DrawBitmap(bitmap, 0, 0);
        
        if (effects.Vignette != null)
        {
            await ApplyVignette(canvas, bitmap.Width, bitmap.Height, effects.Vignette);
        }
        
        if (effects.Shadow != null)
        {
            await ApplyShadow(canvas, bitmap, effects.Shadow);
        }
        
        if (effects.Border != null)
        {
            await ApplyBorder(canvas, bitmap.Width, bitmap.Height, effects.Border);
        }
        
        if (effects.CornerRadius != null)
        {
            await ApplyCornerRadius(canvas, bitmap.Width, bitmap.Height, effects.CornerRadius);
        }
        
        if (effects.ColorAdjustment != null)
        {
            await ApplyColorAdjustment(canvas, bitmap, effects.ColorAdjustment);
        }
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private async Task ApplyVignette(SKCanvas canvas, int width, int height, VignetteSettings settings)
    {
        var radius = Math.Min(width, height) / 2;
        var center = new SKPoint(width / 2, height / 2);
        
        var colors = new[] { SKColors.Transparent, settings.Color.WithAlpha((byte)(255 * settings.Intensity)) };
        var shader = SKShader.CreateRadialGradient(center, radius, colors, null, SKShaderTileMode.Clamp);
        
        using var paint = new SKPaint { Shader = shader, BlendMode = SKBlendMode.Darken };
        canvas.DrawRect(0, 0, width, height, paint);
    }

    private async Task ApplyShadow(SKCanvas canvas, SKBitmap bitmap, ShadowSettings settings)
    {
        // Draw shadow underneath
        using var shadowPaint = new SKPaint
        {
            Color = settings.Color,
            ImageFilter = SKImageFilter.CreateDropShadow(
                settings.OffsetX, settings.OffsetY,
                (float)settings.BlurRadius, (float)settings.BlurRadius,
                settings.Color)
        };
        
        canvas.DrawBitmap(bitmap, settings.OffsetX, settings.OffsetY, shadowPaint);
    }

    private async Task ApplyBorder(SKCanvas canvas, int width, int height, BorderSettings settings)
    {
        using var paint = new SKPaint
        {
            Color = settings.Color,
            StrokeWidth = settings.Width,
            IsStroke = true,
            IsAntialias = true
        };
        
        var rect = new SKRect(settings.Width / 2, settings.Width / 2, 
                              width - settings.Width / 2, height - settings.Width / 2);
        
        if (settings.Style == BorderStyle.Dashed)
        {
            paint.PathEffect = SKPathEffect.CreateDash(new float[] { 10, 10 }, 0);
        }
        else if (settings.Style == BorderStyle.Dotted)
        {
            paint.PathEffect = SKPathEffect.CreateDash(new float[] { 2, 4 }, 0);
        }
        
        canvas.DrawRect(rect, paint);
    }

    private async Task ApplyCornerRadius(SKCanvas canvas, int width, int height, CornerSettings settings)
    {
        var radius = (float)settings.Radius;
        var rect = new SKRoundRect(new SKRect(0, 0, width, height), radius);
        
        canvas.ClipRoundRect(rect, antialias: true);
    }

    private async Task ApplyColorAdjustment(SKCanvas canvas, SKBitmap bitmap, ColorAdjustment settings)
    {
        var matrix = new float[20];
        
        // Brightness adjustment
        var brightness = settings.Brightness / 100f;
        // Contrast adjustment
        var contrast = (100f + settings.Contrast) / 100f;
        contrast = contrast * contrast;
        
        matrix[0] = contrast; matrix[5] = brightness;
        matrix[6] = contrast; matrix[11] = brightness;
        matrix[12] = contrast; matrix[17] = brightness;
        matrix[18] = 1;
        
        using var colorFilter = SKColorFilter.CreateColorMatrix(matrix);
        using var paint = new SKPaint { ColorFilter = colorFilter };
        
        canvas.DrawBitmap(bitmap, 0, 0, paint);
    }

    public async Task<byte[]> ResizeAsync(byte[] imageData, int width, int height, bool highQuality = true)
    {
        using var original = SKBitmap.Decode(imageData);
        using var resized = original.Resize(new SKImageInfo(width, height), 
            highQuality ? SKFilterQuality.High : SKFilterQuality.Medium);
        
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<byte[]> CropAsync(byte[] imageData, SKRect cropRect)
    {
        using var original = SKBitmap.Decode(imageData);
        using var cropped = new SKBitmap((int)cropRect.Width, (int)cropRect.Height);
        
        using var canvas = new SKCanvas(cropped);
        canvas.DrawBitmap(original, -cropRect.Left, -cropRect.Top);
        
        using var image = SKImage.FromBitmap(cropped);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    public async Task<BatchRenderResult> RenderQueueAsync(ConcurrentQueue<RenderJob> queue, BatchRenderSettings settings)
    {
        var jobs = new List<RenderJob>();
        while (queue.TryDequeue(out var job))
        {
            jobs.Add(job);
        }
        return await RenderBatchAsync(jobs, settings, null);
    }

    public async Task<bool> PauseBatchAsync()
    {
        _isBatchPaused = true;
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> ResumeBatchAsync()
    {
        _isBatchPaused = false;
        await Task.CompletedTask;
        return true;
    }

    public async Task<bool> CancelBatchAsync()
    {
        _isBatchCancelled = true;
        await Task.CompletedTask;
        return true;
    }

    public async Task<SKEncodedImageFormat> GetEncoderForFormat(ImageFormat format)
    {
        return format switch
        {
            ImageFormat.Png => SKEncodedImageFormat.Png,
            ImageFormat.Jpeg => SKEncodedImageFormat.Jpeg,
            ImageFormat.Tiff => SKEncodedImageFormat.Tiff,
            ImageFormat.Bmp => SKEncodedImageFormat.Bmp,
            ImageFormat.Webp => SKEncodedImageFormat.Webp,
            _ => SKEncodedImageFormat.Png
        };
    }

    public byte[] EncodeImage(SKBitmap bitmap, ImageFormat format, int quality)
    {
        using var image = SKImage.FromBitmap(bitmap);
        var encoder = GetEncoderForFormat(format).Result;
        using var data = image.Encode(encoder, quality);
        return data.ToArray();
    }

    private (int Width, int Height) CalculateTargetDimensions(int originalWidth, int originalHeight, int targetDpi)
    {
        // Assume original is at 72 DPI (screen)
        var scale = targetDpi / 72.0;
        return ((int)(originalWidth * scale), (int)(originalHeight * scale));
    }

    private string GenerateOutputPath(BatchRenderSettings settings, RenderJob job)
    {
        var fileName = settings.NamingConvention switch
        {
            NamingConvention.SessionId_Order => $"{job.Id}_{DateTime.Now:yyyyMMddHHmmss}.png",
            NamingConvention.Timestamp => $"{DateTime.Now:yyyyMMdd_HHmmss_fff}.png",
            _ => $"{job.Name ?? job.Id.ToString()}.png"
        };
        
        return Path.Combine(settings.OutputDirectory, fileName);
    }
}
