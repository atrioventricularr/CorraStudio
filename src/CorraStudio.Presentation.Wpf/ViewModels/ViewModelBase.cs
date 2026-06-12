using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CorraStudio.Presentation.Wpf.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;
        
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    // Navigation lifecycle methods
    public virtual void OnNavigatedTo(object? parameter) { }
    public virtual void OnNavigatedFrom() { }
    public virtual void OnNavigatingFrom() { }
    
    // Loading states
    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetField(ref _isLoading, value);
    }
    
    private string _errorMessage = string.Empty;
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetField(ref _errorMessage, value);
    }
    
    private bool _hasError;
    public bool HasError
    {
        get => _hasError;
        set => SetField(ref _hasError, value);
    }
    
    protected void SetError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }
    
    protected void ClearError()
    {
        ErrorMessage = string.Empty;
        HasError = false;
    }
}
