using System.Windows;
using CorraStudio.Licensing.Services;
using CorraStudio.Licensing.Mayar;
using CorraStudio.Deployment.Kiosk;
using CorraStudio.Deployment.Updater;
using CorraStudio.Presentation.Wpf.ViewModels.FirstRun;
using CorraStudio.Presentation.Wpf.Views.FirstRun;
using Microsoft.Extensions.DependencyInjection;

namespace CorraStudio.Presentation.Wpf;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private static ServiceProvider? _staticServiceProvider;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        
        var services = new ServiceCollection();
        
        // Load environment variables
        LoadEnvironmentVariables();
        
        // Register licensing
        var mayarConfig = new MayarConfig
        {
            ApiKey = Environment.GetEnvironmentVariable("MAYAR_API_KEY") ?? "",
            ApiUrl = Environment.GetEnvironmentVariable("MAYAR_API_URL") ?? "https://api.mayar.id/v1",
            IsProduction = Environment.GetEnvironmentVariable("MAYAR_PRODUCTION") == "true",
            ValidationIntervalHours = 24,
            EnableHardwareLock = true
        };
        
        services.AddSingleton(mayarConfig);
        services.AddSingleton<IMayarLicenseService, MayarLicenseService>();
        services.AddSingleton<ILicenseValidationService, LicenseValidationService>();
        
        // Register deployment services
        var updateUrl = Environment.GetEnvironmentVariable("UPDATE_URL") ?? "https://updates.corrastudio.com";
        services.AddSingleton<IAutoUpdater>(sp => new AutoUpdater(updateUrl));
        services.AddSingleton<IKioskMode, KioskMode>();
        
        // Register ViewModels
        services.AddTransient<FirstRunWizardViewModel>();
        services.AddTransient<LicenseActivationViewModel>();
        services.AddTransient<LicenseStatusViewModel>();
        services.AddTransient<Admin.LicenseSettingsViewModel>();
        services.AddTransient<ShellViewModel>();
        
        // Register other services (existing)
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService, DialogService>();
        
        _serviceProvider = services.BuildServiceProvider();
        _staticServiceProvider = _serviceProvider;
        
        // Initialize license service
        var licenseService = _serviceProvider.GetRequiredService<ILicenseValidationService>();
        var isInitialized = await licenseService.InitializeAsync();
        
        // Check if first run
        var isFirstRun = await IsFirstRunAsync();
        
        if (isFirstRun)
        {
            // Show first run wizard
            var wizardVm = _serviceProvider.GetRequiredService<FirstRunWizardViewModel>();
            var wizard = new FirstRunWizard(wizardVm);
            wizard.ShowDialog();
        }
        
        // Check license
        if (!licenseService.IsLicenseValid)
        {
            var activationVm = _serviceProvider.GetRequiredService<LicenseActivationViewModel>();
            var activationWindow = new LicenseActivationWindow(activationVm);
            
            var tcs = new TaskCompletionSource<bool>();
            activationVm.ActivationCompleted += (success) => tcs.SetResult(success);
            
            activationWindow.ShowDialog();
            
            var activated = await tcs.Task;
            if (!activated)
            {
                Shutdown();
                return;
            }
        }
        
        // Check for updates in background
        _ = Task.Run(async () =>
        {
            await Task.Delay(5000);
            var updater = _serviceProvider!.GetRequiredService<IAutoUpdater>();
            var result = await updater.CheckForUpdatesAsync();
            
            if (result.HasUpdate && result.LatestVersion != null)
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    ShowUpdateNotification(result.LatestVersion);
                });
            }
        });
        
        // Apply kiosk mode if enabled
        var kioskMode = _serviceProvider.GetRequiredService<IKioskMode>();
        if (kioskMode.IsKioskModeEnabled)
        {
            // Apply fullscreen window style
            var shell = new MainWindow();
            shell.WindowState = WindowState.Maximized;
            shell.WindowStyle = WindowStyle.None;
            shell.Topmost = true;
            shell.DataContext = _serviceProvider.GetRequiredService<ShellViewModel>();
            shell.Show();
        }
        else
        {
            var shell = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<ShellViewModel>()
            };
            shell.Show();
        }
        
        var navigationService = _serviceProvider.GetRequiredService<INavigationService>();
        navigationService.NavigateTo<WelcomeViewModel>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
    
    public static IServiceProvider? ServiceProvider => _staticServiceProvider;

    private void LoadEnvironmentVariables()
    {
        var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadAllLines(envPath))
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;
                
                var parts = line.Split('=', 2);
                if (parts.Length == 2)
                {
                    Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
                }
            }
        }
    }

    private async Task<bool> IsFirstRunAsync()
    {
        var setupPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CorraStudio", "setup.dat");
        
        return await Task.FromResult(!File.Exists(setupPath));
    }

    private void ShowUpdateNotification(UpdateInfo update)
    {
        var result = MessageBox.Show(
            $"New version {update.Version} available!\n\n{update.ReleaseNotes}\n\nInstall now?",
            "Update Available",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);
        
        if (result == MessageBoxResult.Yes)
        {
            var updater = _serviceProvider!.GetRequiredService<IAutoUpdater>();
            _ = Task.Run(async () =>
            {
                await updater.DownloadUpdateAsync(update);
                await updater.InstallUpdateAsync();
            });
        }
    }
}
