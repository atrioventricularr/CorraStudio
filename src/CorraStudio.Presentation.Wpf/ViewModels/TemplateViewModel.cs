using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CorraStudio.Rendering.Models;
using CorraStudio.Rendering.Services;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public class TemplateViewModel : ViewModelBase
{
    private readonly ITemplateEngine _templateEngine;
    private ObservableCollection<TemplateModel> _templates = new();
    private ObservableCollection<LayoutModel> _layouts = new();
    private TemplateModel? _selectedTemplate;
    private LayoutModel? _selectedLayout;
    private BitmapImage? _previewImage;
    private string _searchText = string.Empty;
    private bool _isRendering;

    public TemplateViewModel(ITemplateEngine templateEngine)
    {
        _templateEngine = templateEngine;
        
        LoadTemplatesCommand = new RelayCommand(async () => await LoadTemplatesAsync());
        LoadLayoutsCommand = new RelayCommand(async () => await LoadLayoutsAsync());
        SelectTemplateCommand = new RelayCommand<TemplateModel>(SelectTemplate);
        SelectLayoutCommand = new RelayCommand<LayoutModel>(SelectLayout);
        RenderPreviewCommand = new RelayCommand(async () => await RenderPreviewAsync(), () => SelectedTemplate != null && !IsRendering);
        
        _templateEngine.RenderingProgress += OnRenderingProgress;
        
        Task.Run(async () => await LoadTemplatesAsync());
        Task.Run(async () => await LoadLayoutsAsync());
    }

    public ObservableCollection<TemplateModel> Templates
    {
        get => _templates;
        set => SetField(ref _templates, value);
    }

    public ObservableCollection<LayoutModel> Layouts
    {
        get => _layouts;
        set => SetField(ref _layouts, value);
    }

    public TemplateModel? SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetField(ref _selectedTemplate, value);
    }

    public LayoutModel? SelectedLayout
    {
        get => _selectedLayout;
        set => SetField(ref _selectedLayout, value);
    }

    public BitmapImage? PreviewImage
    {
        get => _previewImage;
        set => SetField(ref _previewImage, value);
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            SetField(ref _searchText, value);
            Task.Run(async () => await FilterItemsAsync());
        }
    }

    public bool IsRendering
    {
        get => _isRendering;
        set => SetField(ref _isRendering, value);
    }

    public ICommand LoadTemplatesCommand { get; }
    public ICommand LoadLayoutsCommand { get; }
    public ICommand SelectTemplateCommand { get; }
    public ICommand SelectLayoutCommand { get; }
    public ICommand RenderPreviewCommand { get; }

    private async Task LoadTemplatesAsync()
    {
        try
        {
            IsLoading = true;
            var templates = await _templateEngine.GetAllTemplatesAsync(Guid.Empty);
            Templates = new ObservableCollection<TemplateModel>(templates);
        }
        catch (Exception ex)
        {
            SetError($"Failed to load templates: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadLayoutsAsync()
    {
        try
        {
            IsLoading = true;
            var layouts = await _templateEngine.GetAllLayoutsAsync(Guid.Empty);
            Layouts = new ObservableCollection<LayoutModel>(layouts);
        }
        catch (Exception ex)
        {
            SetError($"Failed to load layouts: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void SelectTemplate(TemplateModel? template)
    {
        SelectedTemplate = template;
    }

    private void SelectLayout(LayoutModel? layout)
    {
        SelectedLayout = layout;
    }

    private async Task RenderPreviewAsync()
    {
        if (SelectedTemplate == null) return;
        
        try
        {
            IsRendering = true;
            StatusMessage = "Rendering preview...";
            
            // Create sample photos for preview
            var samplePhotos = new List<byte[]>();
            for (int i = 0; i < 4; i++)
            {
                samplePhotos.Add(CreateSamplePhoto());
            }
            
            var result = await _templateEngine.RenderTemplateAsync(SelectedTemplate, samplePhotos);
            
            if (result.Success && result.ImageData != null)
            {
                using var stream = new MemoryStream(result.ImageData);
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                PreviewImage = bitmap;
                StatusMessage = "Preview rendered successfully";
            }
            else
            {
                SetError($"Render failed: {result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            SetError($"Render preview failed: {ex.Message}");
        }
        finally
        {
            IsRendering = false;
        }
    }

    private async Task FilterItemsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadTemplatesAsync();
            return;
        }
        
        var filtered = Templates.Where(t => 
            t.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
            t.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase)).ToList();
        
        Templates = new ObservableCollection<TemplateModel>(filtered);
    }

    private byte[] CreateSamplePhoto()
    {
        // Create a simple gradient image for preview
        var width = 400;
        var height = 400;
        
        using var bitmap = new SkiaSharp.SKBitmap(width, height);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        
        // Draw gradient
        var colors = new[] { SkiaSharp.SKColors.LightBlue, SkiaSharp.SKColors.DarkBlue };
        var shader = SkiaSharp.SKShader.CreateLinearGradient(
            new SkiaSharp.SKPoint(0, 0),
            new SkiaSharp.SKPoint(width, height),
            colors,
            null,
            SkiaSharp.SKShaderTileMode.Clamp);
        
        using var paint = new SkiaSharp.SKPaint { Shader = shader };
        canvas.DrawRect(0, 0, width, height, paint);
        
        // Draw sample text
        using var textPaint = new SkiaSharp.SKPaint
        {
            Color = SkiaSharp.SKColors.White,
            TextSize = 24,
            IsAntialias = true
        };
        
        canvas.DrawText("Sample", width / 2 - 50, height / 2, textPaint);
        
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private void OnRenderingProgress(object? sender, RenderingProgressEventArgs e)
    {
        App.Current?.Dispatcher.Invoke(() =>
        {
            Progress = e.ProgressPercentage;
            StatusMessage = e.CurrentStep;
        });
    }
}
