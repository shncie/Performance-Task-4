using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CareFlow.Data;
using CareFlow.Services;
using CareFlow.ViewModels;
using CareFlow.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CareFlow;

public partial class App : Application
{
    public IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var serviceCollection = new ServiceCollection();

        var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "careflow.db");
        var connectionString = $"Data Source={dbPath}";

        serviceCollection.AddDbContextFactory<CareFlowDbContext>(options =>
        {
            options.UseSqlite(connectionString);
        });

        serviceCollection.AddSingleton<IBookingService, BookingService>();
        serviceCollection.AddTransient<MainViewModel>();

        Services = serviceCollection.BuildServiceProvider();

        using (var scope = Services.CreateScope())
        {
            var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<CareFlowDbContext>>();
            using var dbContext = dbFactory.CreateDbContext();
            dbContext.Database.EnsureCreated();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainVm = Services.GetRequiredService<MainViewModel>();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}