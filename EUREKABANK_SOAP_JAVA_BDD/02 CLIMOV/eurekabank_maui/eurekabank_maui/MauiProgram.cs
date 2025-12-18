using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Hosting;

namespace Eurekabank_Maui
{
    public static class MauiProgram
    {
        public static IServiceProvider Services { get; private set; }

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiMaps()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    handlers.AddHandler(typeof(Microsoft.Maui.Controls.Maps.Map), typeof(Microsoft.Maui.Maps.Handlers.MapHandler));
#endif
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // Configurar HttpClient global con IHttpClientFactory
            builder.Services.AddHttpClient("EurekabankClient", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            });

            // Registrar un HttpClient genérico que usa el factory
            builder.Services.AddSingleton<HttpClient>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                return factory.CreateClient("EurekabankClient");
            });

            var app = builder.Build();
            Services = app.Services;

            return app;
        }
    }
}
