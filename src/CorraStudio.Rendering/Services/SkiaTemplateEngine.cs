using SkiaSharp;
using System.Text.Json;
using CorraStudio.Rendering.Models;

namespace CorraStudio.Rendering.Services;

public class SkiaTemplateEngine : ITemplateEngine
{
    private readonly Dictionary<Guid, TemplateModel> _templateCache;
    private readonly Dictionary<Guid, LayoutModel> _layoutCache;
    private readonly object _lock = new();

    public event EventHandler<RenderingProgressEventArgs>? RenderingProgress;

    public SkiaTemplateEngine()
    {
        _templateCache = new Dictionary<Guid, TemplateModel>();
        _layoutCache = new Dictionary<Guid, LayoutModel>();
        
        // Initialize default templates
        InitializeDefaultTemplates();
        InitializeDefaultLayouts();
    }

    public async Task<TemplateModel> LoadTemplateAsync(Guid templateId)
    {
        lock (_lock)
        {
            if (_templateCache.TryGetValue(templateId, out var template))
                return template;
        }
        
        // In production, load from database
        var defaultTemplate = GetDefaultTemplate();
        return await Task.FromResult(defaultTemplate);
    }

    public async Task<TemplateModel> LoadTemplateAsync(string templatePath)
    {
        var json = await File.ReadAllTextAsync(templatePath);
        var template = JsonSerializer.Deserialize<TemplateModel>(json);
        return template ?? throw new Exception("Failed to load template");
    }

    public async Task<bool> SaveTemplateAsync(TemplateModel template)
    {
        lock (_lock)
        {
            _templateCache[template.Id] = template;
        }
        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteTemplateAsync(Guid templateId)
    {
        lock (_lock)
        {
            return _templateCache.Remove(templateId);
        }
    }

    public async Task<List<TemplateModel>> GetAllTemplatesAsync(Guid tenantId)
    {
        lock (_lock)
        {
            return _templateCache.Values.ToList();
        }
    }

    public async Task<LayoutModel> LoadLayoutAsync(Guid layoutId)
    {
        lock (_lock)
        {
            if (_layoutCache.TryGetValue(layoutId, out var layout))
                return layout;
        }
        
        var defaultLayout = GetDefaultLayout();
        return await Task.FromResult(defaultLayout);
    }

    public async Task<LayoutModel> LoadLayoutAsync(string layoutPath)
    {
        var json = await File.ReadAllTextAsync(layoutPath);
        var layout = JsonSerializer.Deserialize<LayoutModel>(json);
        return layout ?? throw new Exception("Failed to load layout");
    }

    public async Task<bool> SaveLayoutAsync(LayoutModel layout)
    {
        lock (_lock)
        {
            _layoutCache[layout.Id] = layout;
        }
        return await Task.FromResult(true);
    }

    public async Task<bool> DeleteLayoutAsync(Guid layoutId)
    {
        lock (_lock)
        {
            return _layoutCache.Remove(layoutId);
        }
    }

    public async Task<List<LayoutModel>> GetAllLayoutsAsync(Guid tenantId)
    {
        lock (_lock)
        {
            return _layoutCache.Values.ToList();
        }
    }

    public async Task<RenderingResult> RenderTemplateAsync(TemplateModel template, List<byte[]> photos, Dictionary<string, string>? variables = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            ReportingProgress(10, "Loading template...");
            
            using var surface = SKSurface.Create(new SKImageInfo(template.Width, template.Height));
            var canvas = surface.Canvas;
            
            // Clear canvas
            canvas.Clear(SKColors.White);
            
            ReportingProgress(20, "Processing layers...");
            
            // Sort layers by ZIndex
            var sortedLayers = template.Layers.OrderBy(l => l.ZIndex).ToList();
            var photoIndex = 0;
            
            foreach (var layer in sortedLayers)
            {
                if (!layer.IsVisible) continue;
                
                ReportingProgress(20 + (int)(60 * (sortedLayers.IndexOf(layer) / (double)sortedLayers.Count)), 
                    $"Rendering layer: {layer.Name}");
                
                switch (layer.Type)
                {
                    case LayerType.Background:
                        await RenderBackgroundLayer(canvas, layer, template.Width, template.Height);
                        break;
                    case LayerType.Photo:
                        if (photoIndex < photos.Count)
                        {
                            await RenderPhotoLayer(canvas, layer, photos[photoIndex]);
                            photoIndex++;
                        }
                        break;
                    case LayerType.Text:
                        await RenderTextLayer(canvas, layer, variables);
                        break;
                    case LayerType.Shape:
                        await RenderShapeLayer(canvas, layer);
                        break;
                    case LayerType.Image:
                        await RenderImageLayer(canvas, layer);
                        break;
                    case LayerType.Frame:
                        await RenderFrameLayer(canvas, layer);
                        break;
                    case LayerType.Overlay:
                        await RenderOverlayLayer(canvas, layer);
                        break;
                    case LayerType.QrCode:
                        await RenderQrCodeLayer(canvas, layer, variables);
                        break;
                    case LayerType.Date:
                        await RenderDateLayer(canvas, layer);
                        break;
                }
            }
            
            ReportingProgress(90, "Finalizing...");
            
            // Capture the result
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var resultBytes = data.ToArray();
            
            stopwatch.Stop();
            
            ReportingProgress(100, "Complete!");
            
            return RenderingResult.SuccessResult(resultBytes, template.Width, template.Height);
        }
        catch (Exception ex)
        {
            return RenderingResult.FailResult($"Render failed: {ex.Message}");
        }
    }

    public async Task<RenderingResult> RenderLayoutAsync(LayoutModel layout, List<byte[]> photos, Dictionary<string, string>? variables = null)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            using var surface = SKSurface.Create(new SKImageInfo(layout.Width, layout.Height));
            var canvas = surface.Canvas;
            
            canvas.Clear(SKColors.White);
            
            // Render background first
            foreach (var element in layout.DesignElements.Where(e => e.Type == ElementType.Background))
            {
                await RenderDesignElement(canvas, element);
            }
            
            // Render photo slots
            foreach (var slot in layout.PhotoSlots.OrderBy(s => s.OrderIndex))
            {
                var slotIndex = layout.PhotoSlots.IndexOf(slot);
                if (slotIndex < photos.Count)
                {
                    await RenderPhotoSlot(canvas, slot, photos[slotIndex]);
                }
            }
            
            // Render other design elements
            foreach (var element in layout.DesignElements.Where(e => e.Type != ElementType.Background))
            {
                await RenderDesignElement(canvas, element);
            }
            
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var resultBytes = data.ToArray();
            
            stopwatch.Stop();
            
            return RenderingResult.SuccessResult(resultBytes, layout.Width, layout.Height);
        }
        catch (Exception ex)
        {
            return RenderingResult.FailResult($"Layout render failed: {ex.Message}");
        }
    }

    public async Task<RenderingResult> RenderPhotoStripAsync(List<byte[]> photos, TemplateModel? template = null)
    {
        if (template == null)
        {
            template = GetDefaultPhotoStripTemplate();
        }
        
        return await RenderTemplateAsync(template, photos);
    }

    public async Task<GifRenderingResult> RenderGifAsync(List<byte[]> photos, int frameDelayMs = 500, bool loop = true)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // For GIF rendering, we'll use a more complex approach with SkiaSharp
            // This is a simplified version
            var firstImage = SKBitmap.Decode(photos[0]);
            var width = firstImage.Width;
            var height = firstImage.Height;
            
            // In production, use a proper GIF encoder
            // For now, return first frame as placeholder
            var result = new GifRenderingResult
            {
                Success = true,
                ImageData = photos[0],
                Width = width,
                Height = height,
                FrameCount = photos.Count,
                FrameDelayMs = frameDelayMs,
                IsLooping = loop,
                ProcessingTime = stopwatch.Elapsed
            };
            
            return result;
        }
        catch (Exception ex)
        {
            return new GifRenderingResult
            {
                Success = false,
                ErrorMessage = $"GIF render failed: {ex.Message}"
            };
        }
    }

    public async Task<byte[]> GenerateThumbnailAsync(byte[] imageData, int maxWidth = 300, int maxHeight = 300)
    {
        using var original = SKBitmap.Decode(imageData);
        
        float ratio = Math.Min((float)maxWidth / original.Width, (float)maxHeight / original.Height);
        int newWidth = (int)(original.Width * ratio);
        int newHeight = (int)(original.Height * ratio);
        
        using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        
        return data.ToArray();
    }

    public async Task<byte[]> ResizeImageAsync(byte[] imageData, int width, int height, bool maintainAspectRatio = true)
    {
        using var original = SKBitmap.Decode(imageData);
        
        int newWidth = width;
        int newHeight = height;
        
        if (maintainAspectRatio)
        {
            float ratio = Math.Min((float)width / original.Width, (float)height / original.Height);
            newWidth = (int)(original.Width * ratio);
            newHeight = (int)(original.Height * ratio);
        }
        
        using var resized = original.Resize(new SKImageInfo(newWidth, newHeight), SKFilterQuality.High);
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        
        return data.ToArray();
    }

    public async Task<byte[]> ApplyFilterAsync(byte[] imageData, ImageFilter filter)
    {
        using var bitmap = SKBitmap.Decode(imageData);
        using var surface = SKSurface.Create(new SKImageInfo(bitmap.Width, bitmap.Height));
        var canvas = surface.Canvas;
        
        canvas.DrawBitmap(bitmap, 0, 0);
        
        switch (filter)
        {
            case ImageFilter.Grayscale:
                using (var colorFilter = SKColorFilter.CreateColorMatrix(new float[]
                {
                    0.3f, 0.59f, 0.11f, 0, 0,
                    0.3f, 0.59f, 0.11f, 0, 0,
                    0.3f, 0.59f, 0.11f, 0, 0,
                    0, 0, 0, 1, 0
                }))
                {
                    var paint = new SKPaint { ColorFilter = colorFilter };
                    canvas.DrawBitmap(bitmap, 0, 0, paint);
                }
                break;
            case ImageFilter.Sepia:
                using (var colorFilter = SKColorFilter.CreateColorMatrix(new float[]
                {
                    0.393f, 0.769f, 0.189f, 0, 0,
                    0.349f, 0.686f, 0.168f, 0, 0,
                    0.272f, 0.534f, 0.131f, 0, 0,
                    0, 0, 0, 1, 0
                }))
                {
                    var paint = new SKPaint { ColorFilter = colorFilter };
                    canvas.DrawBitmap(bitmap, 0, 0, paint);
                }
                break;
        }
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    public async Task<byte[]> AddTextOverlayAsync(byte[] imageData, string text, TextPosition position)
    {
        using var original = SKBitmap.Decode(imageData);
        using var surface = SKSurface.Create(new SKImageInfo(original.Width, original.Height));
        var canvas = surface.Canvas;
        
        canvas.DrawBitmap(original, 0, 0);
        
        using var paint = new SKPaint
        {
            Color = SKColors.White,
            TextSize = 24,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Bold)
        };
        
        var bounds = new SKRect();
        paint.MeasureText(text, ref bounds);
        
        float x = 10, y = 40;
        switch (position)
        {
            case TextPosition.TopCenter:
                x = (original.Width - bounds.Width) / 2;
                y = 40;
                break;
            case TextPosition.TopRight:
                x = original.Width - bounds.Width - 10;
                y = 40;
                break;
            case TextPosition.BottomLeft:
                x = 10;
                y = original.Height - 20;
                break;
            case TextPosition.BottomCenter:
                x = (original.Width - bounds.Width) / 2;
                y = original.Height - 20;
                break;
            case TextPosition.BottomRight:
                x = original.Width - bounds.Width - 10;
                y = original.Height - 20;
                break;
        }
        
        // Draw shadow
        paint.Color = SKColors.Black.WithAlpha(128);
        canvas.DrawText(text, x + 2, y + 2, paint);
        
        // Draw text
        paint.Color = SKColors.White;
        canvas.DrawText(text, x, y, paint);
        
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    #region Private Rendering Methods

    private async Task RenderBackgroundLayer(SKCanvas canvas, TemplateLayer layer, int width, int height)
    {
        var color = layer.Properties.ContainsKey("Color") 
            ? SKColor.Parse(layer.Properties["Color"].ToString() ?? "#FFFFFF")
            : SKColors.White;
        
        canvas.Clear(color);
        await Task.CompletedTask;
    }

    private async Task RenderPhotoLayer(SKCanvas canvas, TemplateLayer layer, byte[] photoData)
    {
        using var photo = SKBitmap.Decode(photoData);
        var bounds = layer.Bounds;
        var rect = new SKRect(bounds.X, bounds.Y, bounds.X + bounds.Width, bounds.Y + bounds.Height);
        
        var fitMode = layer.Properties.ContainsKey("FitMode")
            ? Enum.Parse<PhotoFitMode>(layer.Properties["FitMode"].ToString() ?? "Cover")
            : PhotoFitMode.Cover;
        
        var paint = new SKPaint();
        
        // Apply corner radius
        if (layer.Properties.ContainsKey("CornerRadius"))
        {
            var radius = Convert.ToSingle(layer.Properties["CornerRadius"]);
            canvas.Save();
            canvas.ClipRoundRect(new SKRoundRect(rect, radius));
        }
        
        switch (fitMode)
        {
            case PhotoFitMode.Fill:
                canvas.DrawBitmap(photo, rect, paint);
                break;
            case PhotoFitMode.Fit:
                var fitRect = GetFitRect(photo.Width, photo.Height, bounds.Width, bounds.Height);
                fitRect.Offset(bounds.X, bounds.Y);
                canvas.DrawBitmap(photo, fitRect, paint);
                break;
            case PhotoFitMode.Cover:
                var coverRect = GetCoverRect(photo.Width, photo.Height, bounds.Width, bounds.Height);
                coverRect.Offset(bounds.X, bounds.Y);
                canvas.DrawBitmap(photo, coverRect, paint);
                break;
            case PhotoFitMode.Stretch:
                canvas.DrawBitmap(photo, rect, paint);
                break;
        }
        
        // Apply border
        if (layer.Properties.ContainsKey("BorderColor") && layer.Properties.ContainsKey("BorderWidth"))
        {
            var borderColor = SKColor.Parse(layer.Properties["BorderColor"].ToString() ?? "#000000");
            var borderWidth = Convert.ToSingle(layer.Properties["BorderWidth"]);
            
            var borderPaint = new SKPaint
            {
                Color = borderColor,
                StrokeWidth = borderWidth,
                IsStroke = true,
                IsAntialias = true
            };
            
            canvas.DrawRect(rect, borderPaint);
        }
        
        canvas.Restore();
        await Task.CompletedTask;
    }

    private async Task RenderTextLayer(SKCanvas canvas, TemplateLayer layer, Dictionary<string, string>? variables)
    {
        var text = layer.Properties.ContainsKey("Text") 
            ? layer.Properties["Text"].ToString() ?? ""
            : "";
        
        // Replace variables
        if (variables != null)
        {
            foreach (var variable in variables)
            {
                text = text.Replace($"{{{{{variable.Key}}}}}", variable.Value);
            }
        }
        
        var fontFamily = layer.Properties.ContainsKey("FontFamily") 
            ? layer.Properties["FontFamily"].ToString() 
            : "Arial";
        var fontSize = layer.Properties.ContainsKey("FontSize") 
            ? Convert.ToSingle(layer.Properties["FontSize"]) 
            : 12;
        var fontColor = layer.Properties.ContainsKey("FontColor") 
            ? SKColor.Parse(layer.Properties["FontColor"].ToString() ?? "#000000")
            : SKColors.Black;
        var isBold = layer.Properties.ContainsKey("IsBold") && (bool)layer.Properties["IsBold"];
        
        using var paint = new SKPaint
        {
            Color = fontColor,
            TextSize = fontSize,
            IsAntialias = true,
            Typeface = SKTypeface.FromFamilyName(fontFamily, isBold ? SKFontStyle.Bold : SKFontStyle.Normal)
        };
        
        var bounds = new SKRect();
        paint.MeasureText(text, ref bounds);
        
        var x = layer.Bounds.X;
        var y = layer.Bounds.Y + bounds.Height;
        
        // Handle alignment
        if (layer.Properties.ContainsKey("Alignment"))
        {
            var alignment = layer.Properties["Alignment"].ToString();
            if (alignment == "Center")
                x = layer.Bounds.X + (layer.Bounds.Width - bounds.Width) / 2;
            else if (alignment == "Right")
                x = layer.Bounds.X + layer.Bounds.Width - bounds.Width;
        }
        
        canvas.DrawText(text, x, y, paint);
        await Task.CompletedTask;
    }

    private async Task RenderShapeLayer(SKCanvas canvas, TemplateLayer layer)
    {
        var shapeType = layer.Properties.ContainsKey("ShapeType") 
            ? layer.Properties["ShapeType"].ToString() 
            : "Rectangle";
        var color = layer.Properties.ContainsKey("Color")
            ? SKColor.Parse(layer.Properties["Color"].ToString() ?? "#000000")
            : SKColors.Black;
        
        var rect = new SKRect(layer.Bounds.X, layer.Bounds.Y, 
                              layer.Bounds.X + layer.Bounds.Width, 
                              layer.Bounds.Y + layer.Bounds.Height);
        
        using var paint = new SKPaint { Color = color, IsAntialias = true };
        
        switch (shapeType)
        {
            case "Rectangle":
                canvas.DrawRect(rect, paint);
                break;
            case "Ellipse":
                canvas.DrawOval(rect, paint);
                break;
            case "RoundedRectangle":
                var radius = layer.Properties.ContainsKey("Radius") 
                    ? Convert.ToSingle(layer.Properties["Radius"]) 
                    : 10;
                canvas.DrawRoundRect(rect, radius, radius, paint);
                break;
        }
        
        await Task.CompletedTask;
    }

    private async Task RenderImageLayer(SKCanvas canvas, TemplateLayer layer)
    {
        // In production, load image from path in Properties
        await Task.CompletedTask;
    }

    private async Task RenderFrameLayer(SKCanvas canvas, TemplateLayer layer)
    {
        var color = layer.Properties.ContainsKey("Color")
            ? SKColor.Parse(layer.Properties["Color"].ToString() ?? "#000000")
            : SKColors.Black;
        var width = layer.Properties.ContainsKey("Width")
            ? Convert.ToSingle(layer.Properties["Width"])
            : 5;
        
        var rect = new SKRect(layer.Bounds.X, layer.Bounds.Y,
                              layer.Bounds.X + layer.Bounds.Width,
                              layer.Bounds.Y + layer.Bounds.Height);
        
        using var paint = new SKPaint
        {
            Color = color,
            StrokeWidth = width,
            IsStroke = true,
            IsAntialias = true
        };
        
        canvas.DrawRect(rect, paint);
        await Task.CompletedTask;
    }

    private async Task RenderOverlayLayer(SKCanvas canvas, TemplateLayer layer)
    {
        // For gradient overlays, patterns, etc.
        await Task.CompletedTask;
    }

    private async Task RenderQrCodeLayer(SKCanvas canvas, TemplateLayer layer, Dictionary<string, string>? variables)
    {
        // QR code rendering - will be implemented with a QR library
        await Task.CompletedTask;
    }

    private async Task RenderDateLayer(SKCanvas canvas, TemplateLayer layer)
    {
        var dateFormat = layer.Properties.ContainsKey("Format") 
            ? layer.Properties["Format"].ToString() 
            : "yyyy-MM-dd";
        var dateText = DateTime.Now.ToString(dateFormat);
        
        var textLayer = new TemplateLayer
        {
            Bounds = layer.Bounds,
            Properties = new Dictionary<string, object>
            {
                ["Text"] = dateText,
                ["FontSize"] = layer.Properties.GetValueOrDefault("FontSize", 12),
                ["FontColor"] = layer.Properties.GetValueOrDefault("FontColor", "#000000"),
                ["Alignment"] = layer.Properties.GetValueOrDefault("Alignment", "Center")
            }
        };
        
        await RenderTextLayer(canvas, textLayer, null);
    }

    private async Task RenderPhotoSlot(SKCanvas canvas, PhotoSlot slot, byte[] photoData)
    {
        var layer = new TemplateLayer
        {
            Bounds = slot.Bounds,
            Type = LayerType.Photo,
            Properties = new Dictionary<string, object>
            {
                ["FitMode"] = slot.FitMode.ToString(),
                ["CornerRadius"] = slot.CornerRadius,
                ["BorderColor"] = slot.BorderColor ?? "",
                ["BorderWidth"] = slot.BorderWidth
            }
        };
        
        await RenderPhotoLayer(canvas, layer, photoData);
    }

    private async Task RenderDesignElement(SKCanvas canvas, DesignElement element)
    {
        var layer = new TemplateLayer
        {
            Bounds = element.Bounds,
            Properties = element.Properties
        };
        
        switch (element.Type)
        {
            case ElementType.Text:
                await RenderTextLayer(canvas, layer, null);
                break;
            case ElementType.Shape:
                await RenderShapeLayer(canvas, layer);
                break;
            case ElementType.Background:
                await RenderBackgroundLayer(canvas, layer, 0, 0);
                break;
        }
    }

    #endregion

    #region Helper Methods

    private SKRect GetFitRect(int imageWidth, int imageHeight, int targetWidth, int targetHeight)
    {
        float ratio = Math.Min((float)targetWidth / imageWidth, (float)targetHeight / imageHeight);
        int newWidth = (int)(imageWidth * ratio);
        int newHeight = (int)(imageHeight * ratio);
        
        float x = (targetWidth - newWidth) / 2f;
        float y = (targetHeight - newHeight) / 2f;
        
        return new SKRect(x, y, x + newWidth, y + newHeight);
    }

    private SKRect GetCoverRect(int imageWidth, int imageHeight, int targetWidth, int targetHeight)
    {
        float ratio = Math.Max((float)targetWidth / imageWidth, (float)targetHeight / imageHeight);
        int newWidth = (int)(imageWidth * ratio);
        int newHeight = (int)(imageHeight * ratio);
        
        float x = (targetWidth - newWidth) / 2f;
        float y = (targetHeight - newHeight) / 2f;
        
        return new SKRect(x, y, x + newWidth, y + newHeight);
    }

    private void ReportingProgress(int percentage, string step)
    {
        RenderingProgress?.Invoke(this, new RenderingProgressEventArgs
        {
            ProgressPercentage = percentage,
            CurrentStep = step,
            Timestamp = DateTime.UtcNow
        });
    }

    #endregion

    #region Default Templates

    private void InitializeDefaultTemplates()
    {
        var defaultTemplate = GetDefaultTemplate();
        _templateCache[defaultTemplate.Id] = defaultTemplate;
        
        var photoStripTemplate = GetDefaultPhotoStripTemplate();
        _templateCache[photoStripTemplate.Id] = photoStripTemplate;
    }

    private TemplateModel GetDefaultTemplate()
    {
        return new TemplateModel
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "Standard Photo",
            Description = "Standard single photo template",
            Type = TemplateType.SinglePhoto,
            Width = 1800,
            Height = 2400,
            Dpi = 300,
            Layers = new List<TemplateLayer>
            {
                new TemplateLayer
                {
                    Id = "bg",
                    Name = "Background",
                    Type = LayerType.Background,
                    ZIndex = 0,
                    Properties = new Dictionary<string, object> { ["Color"] = "#FFFFFF" }
                },
                new TemplateLayer
                {
                    Id = "photo",
                    Name = "Photo",
                    Type = LayerType.Photo,
                    ZIndex = 1,
                    Bounds = new Rectangle { X = 100, Y = 200, Width = 1600, Height = 2000 },
                    Properties = new Dictionary<string, object>
                    {
                        ["FitMode"] = "Cover",
                        ["CornerRadius"] = 20,
                        ["BorderColor"] = "#DDDDDD",
                        ["BorderWidth"] = 3
                    }
                },
                new TemplateLayer
                {
                    Id = "date",
                    Name = "Date",
                    Type = LayerType.Date,
                    ZIndex = 2,
                    Bounds = new Rectangle { X = 100, Y = 2150, Width = 1600, Height = 50 },
                    Properties = new Dictionary<string, object>
                    {
                        ["Format"] = "MMMM dd, yyyy",
                        ["FontSize"] = 24,
                        ["FontColor"] = "#666666",
                        ["Alignment"] = "Center"
                    }
                }
            }
        };
    }

    private TemplateModel GetDefaultPhotoStripTemplate()
    {
        var photoHeight = 400;
        var spacing = 20;
        var startY = 200;
        
        var layers = new List<TemplateLayer>
        {
            new TemplateLayer
            {
                Id = "bg",
                Name = "Background",
                Type = LayerType.Background,
                ZIndex = 0,
                Properties = new Dictionary<string, object> { ["Color"] = "#FFFFFF" }
            }
        };
        
        // Add 4 photo slots
        for (int i = 0; i < 4; i++)
        {
            var y = startY + (i * (photoHeight + spacing));
            layers.Add(new TemplateLayer
            {
                Id = $"photo_{i}",
                Name = $"Photo {i + 1}",
                Type = LayerType.Photo,
                ZIndex = 1,
                Bounds = new Rectangle { X = 150, Y = y, Width = 600, Height = photoHeight },
                Properties = new Dictionary<string, object>
                {
                    ["FitMode"] = "Cover",
                    ["CornerRadius"] = 10,
                    ["BorderColor"] = "#CCCCCC",
                    ["BorderWidth"] = 2
                }
            });
        }
        
        return new TemplateModel
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Name = "Photo Strip 4x6",
            Description = "Classic photo strip with 4 photos",
            Type = TemplateType.PhotoStrip,
            Width = 900,
            Height = 2200,
            Dpi = 300,
            Layers = layers
        };
    }

    private void InitializeDefaultLayouts()
    {
        var defaultLayout = GetDefaultLayout();
        _layoutCache[defaultLayout.Id] = defaultLayout;
    }

    private LayoutModel GetDefaultLayout()
    {
        return new LayoutModel
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "Standard 4x6",
            Description = "Standard layout for 4x6 prints",
            Width = 1800,
            Height = 1200,
            Orientation = LayoutOrientation.Landscape,
            PhotoSlots = new List<PhotoSlot>
            {
                new PhotoSlot
                {
                    Id = "slot_1",
                    OrderIndex = 0,
                    Bounds = new Rectangle { X = 50, Y = 50, Width = 850, Height = 1100 },
                    FitMode = PhotoFitMode.Cover,
                    CornerRadius = 10
                },
                new PhotoSlot
                {
                    Id = "slot_2",
                    OrderIndex = 1,
                    Bounds = new Rectangle { X = 900, Y = 50, Width = 850, Height = 1100 },
                    FitMode = PhotoFitMode.Cover,
                    CornerRadius = 10
                }
            }
        };
    }

    #endregion
}
