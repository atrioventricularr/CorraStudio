using System.Windows.Controls;

namespace CorraStudio.Presentation.Wpf.Navigation;

public interface IViewLocator
{
    Page? GetViewForViewModel(ViewModelBase viewModel);
    Page? GetView(string viewModelName);
}

public class ViewLocator : IViewLocator
{
    private readonly IServiceProvider _serviceProvider;

    public ViewLocator(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Page? GetViewForViewModel(ViewModelBase viewModel)
    {
        var viewModelName = viewModel.GetType().Name;
        return GetView(viewModelName);
    }

    public Page? GetView(string viewModelName)
    {
        var viewName = viewModelName.Replace("ViewModel", "Page");
        var viewType = Type.GetType($"CorraStudio.Presentation.Wpf.Views.{viewName}");
        
        if (viewType != null)
            return Activator.CreateInstance(viewType) as Page;
        
        return null;
    }
}
