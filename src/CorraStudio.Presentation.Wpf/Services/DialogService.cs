using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace CorraStudio.Presentation.Wpf.Services;

public interface IDialogService
{
    Task<DialogResult> ShowMessageAsync(string message, string title = "Information", DialogButtons buttons = DialogButtons.OK, DialogIcon icon = DialogIcon.Information);
    Task<bool> ShowConfirmationAsync(string message, string title = "Confirm");
    Task<string?> ShowInputDialogAsync(string message, string title = "Input", string defaultValue = "");
    Task<string?> ShowFileOpenDialogAsync(string filter = "All files (*.*)|*.*", string? defaultPath = null);
    Task<string?> ShowFileSaveDialogAsync(string filter = "All files (*.*)|*.*", string? defaultPath = null);
    Task<string?> ShowFolderDialogAsync(string description = "Select folder");
    void ShowError(string message, string title = "Error");
    void ShowWarning(string message, string title = "Warning");
    void ShowInfo(string message, string title = "Information");
    Task<ProgressDialogResult> ShowProgressDialogAsync(string title, Func<IProgress<string>, Task> work);
}

public enum DialogButtons
{
    OK,
    OKCancel,
    YesNo,
    YesNoCancel,
    RetryCancel,
    AbortRetryIgnore
}

public enum DialogIcon
{
    Information,
    Question,
    Warning,
    Error,
    None
}

public enum DialogResult
{
    OK,
    Cancel,
    Yes,
    No,
    Retry,
    Abort,
    Ignore
}

public class ProgressDialogResult
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public Exception? Error { get; set; }
}

public class DialogService : IDialogService
{
    private Window? GetActiveWindow()
    {
        return Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive) ?? Application.Current.MainWindow;
    }

    public Task<DialogResult> ShowMessageAsync(string message, string title = "Information", DialogButtons buttons = DialogButtons.OK, DialogIcon icon = DialogIcon.Information)
    {
        return Task.Run(() =>
        {
            var button = MessageBoxButton.OK;
            var image = MessageBoxImage.Information;
            
            switch (buttons)
            {
                case DialogButtons.OK: button = MessageBoxButton.OK; break;
                case DialogButtons.OKCancel: button = MessageBoxButton.OKCancel; break;
                case DialogButtons.YesNo: button = MessageBoxButton.YesNo; break;
                case DialogButtons.YesNoCancel: button = MessageBoxButton.YesNoCancel; break;
                case DialogButtons.RetryCancel: button = MessageBoxButton.RetryCancel; break;
                case DialogButtons.AbortRetryIgnore: button = MessageBoxButton.AbortRetryIgnore; break;
            }
            
            switch (icon)
            {
                case DialogIcon.Information: image = MessageBoxImage.Information; break;
                case DialogIcon.Question: image = MessageBoxImage.Question; break;
                case DialogIcon.Warning: image = MessageBoxImage.Warning; break;
                case DialogIcon.Error: image = MessageBoxImage.Error; break;
                case DialogIcon.None: image = MessageBoxImage.None; break;
            }
            
            var result = MessageBox.Show(GetActiveWindow(), message, title, button, image);
            
            return result switch
            {
                MessageBoxResult.OK => DialogResult.OK,
                MessageBoxResult.Cancel => DialogResult.Cancel,
                MessageBoxResult.Yes => DialogResult.Yes,
                MessageBoxResult.No => DialogResult.No,
                MessageBoxResult.Retry => DialogResult.Retry,
                MessageBoxResult.Abort => DialogResult.Abort,
                MessageBoxResult.Ignore => DialogResult.Ignore,
                _ => DialogResult.Cancel
            };
        });
    }

    public Task<bool> ShowConfirmationAsync(string message, string title = "Confirm")
    {
        return Task.Run(() =>
        {
            var result = MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            return result == MessageBoxResult.Yes;
        });
    }

    public Task<string?> ShowInputDialogAsync(string message, string title = "Input", string defaultValue = "")
    {
        return Task.Run(() =>
        {
            var inputDialog = new Window
            {
                Title = title,
                Width = 450,
                Height = 180,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                Owner = GetActiveWindow(),
                Background = new SolidColorBrush(Colors.White),
                WindowStyle = WindowStyle.ToolWindow
            };
            
            var grid = new Grid { Margin = new Thickness(10) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            
            var textBlock = new TextBlock 
            { 
                Text = message, 
                Margin = new Thickness(0, 0, 0, 10),
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap
            };
            Grid.SetRow(textBlock, 0);
            
            var textBox = new TextBox 
            { 
                Text = defaultValue, 
                Margin = new Thickness(0, 0, 0, 15),
                FontSize = 14,
                Padding = new Thickness(8, 5, 8, 5)
            };
            Grid.SetRow(textBox, 1);
            
            var buttonPanel = new StackPanel 
            { 
                Orientation = Orientation.Horizontal, 
                HorizontalAlignment = HorizontalAlignment.Right 
            };
            
            var okButton = new Button 
            { 
                Content = "OK", 
                Width = 75, 
                Height = 30, 
                Margin = new Thickness(5),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3498DB")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand
            };
            
            var cancelButton = new Button 
            { 
                Content = "Cancel", 
                Width = 75, 
                Height = 30, 
                Margin = new Thickness(5),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#BDBDBD")),
                Cursor = Cursors.Hand
            };
            
            string? result = null;
            
            okButton.Click += (s, e) =>
            {
                result = textBox.Text;
                inputDialog.Close();
            };
            
            cancelButton.Click += (s, e) => inputDialog.Close();
            
            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    result = textBox.Text;
                    inputDialog.Close();
                }
                else if (e.Key == Key.Escape)
                {
                    inputDialog.Close();
                }
            };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);
            
            grid.Children.Add(textBlock);
            grid.Children.Add(textBox);
            grid.Children.Add(buttonPanel);
            inputDialog.Content = grid;
            
            inputDialog.ShowDialog();
            return result;
        });
    }

    public Task<string?> ShowFileOpenDialogAsync(string filter = "All files (*.*)|*.*", string? defaultPath = null)
    {
        return Task.Run(() =>
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                InitialDirectory = defaultPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            
            var result = dialog.ShowDialog(GetActiveWindow());
            return result == true ? dialog.FileName : null;
        });
    }

    public Task<string?> ShowFileSaveDialogAsync(string filter = "All files (*.*)|*.*", string? defaultPath = null)
    {
        return Task.Run(() =>
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter,
                InitialDirectory = defaultPath ?? Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };
            
            var result = dialog.ShowDialog(GetActiveWindow());
            return result == true ? dialog.FileName : null;
        });
    }

    public Task<string?> ShowFolderDialogAsync(string description = "Select folder")
    {
        return Task.Run(() =>
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = description,
                UseDescriptionForTitle = true
            };
            
            var result = dialog.ShowDialog();
            return result == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
        });
    }

    public void ShowError(string message, string title = "Error")
    {
        MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Error);
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    public void ShowInfo(string message, string title = "Information")
    {
        MessageBox.Show(GetActiveWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Information);
    }

    public async Task<ProgressDialogResult> ShowProgressDialogAsync(string title, Func<IProgress<string>, Task> work)
    {
        var tcs = new TaskCompletionSource<ProgressDialogResult>();
        
        var progressWindow = new Window
        {
            Title = title,
            Width = 450,
            Height = 150,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = GetActiveWindow(),
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            ShowInTaskbar = false
        };
        
        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        
        var statusText = new TextBlock 
        { 
            Text = "Processing...", 
            Margin = new Thickness(0, 0, 0, 10),
            FontSize = 14,
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        };
        Grid.SetRow(statusText, 0);
        
        var progressBar = new ProgressBar 
        { 
            Height = 20, 
            Margin = new Thickness(0, 0, 0, 10),
            IsIndeterminate = true
        };
        Grid.SetRow(progressBar, 1);
        
        var cancelButton = new Button 
        { 
            Content = "Cancel", 
            Width = 75, 
            Height = 30,
            HorizontalAlignment = HorizontalAlignment.Center,
            Cursor = Cursors.Hand
        };
        Grid.SetRow(cancelButton, 2);
        
        grid.Children.Add(statusText);
        grid.Children.Add(progressBar);
        grid.Children.Add(cancelButton);
        progressWindow.Content = grid;
        
        var cancellationTokenSource = new CancellationTokenSource();
        
        cancelButton.Click += (s, e) =>
        {
            cancellationTokenSource.Cancel();
            cancelButton.IsEnabled = false;
            statusText.Text = "Cancelling...";
        };
        
        var progress = new Progress<string>(msg =>
        {
            statusText.Text = msg;
        });
        
        progressWindow.Loaded += async (s, e) =>
        {
            try
            {
                await work(progress);
                tcs.SetResult(new ProgressDialogResult { Success = true });
                progressWindow.Close();
            }
            catch (Exception ex)
            {
                tcs.SetResult(new ProgressDialogResult { Success = false, Error = ex, Message = ex.Message });
                progressWindow.Close();
            }
        };
        
        progressWindow.ShowDialog();
        
        return await tcs.Task;
    }
}
