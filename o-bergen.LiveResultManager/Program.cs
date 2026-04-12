using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using o_bergen.LiveResultManager.Configuration;
using WinFormsApp = System.Windows.Forms.Application;

namespace o_bergen.LiveResultManager;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Register encoding provider for Windows-1252 and other code pages
        // Required for WinSplits/World of O compatibility
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();

        // Build the host with DI and configuration
        var host = CreateHostBuilder().Build();

        // Get the main form from DI container
        var mainForm = host.Services.GetRequiredService<Form1>();

        // Run the application
        WinFormsApp.Run(mainForm);
    }

    /// <summary>
    /// Creates and configures the host builder
    /// </summary>
    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Set base path to application directory
                config.SetBasePath(AppDomain.CurrentDomain.BaseDirectory);

                // Add JSON configuration files
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: true);

                // Add environment variables (useful for production deployment)
                config.AddEnvironmentVariables(prefix: "LIVERESULT_");
            })
            .ConfigureServices((context, services) =>
            {
                // Register all application services
                services.AddLiveResultManagerServices(context.Configuration);

                // Register Forms (Transient - new instance each time)
                services.AddTransient<Form1>();
            });
    }
}
