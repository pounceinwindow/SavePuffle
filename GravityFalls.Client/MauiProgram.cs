using Microsoft.Extensions.Logging;

namespace SavePuffle
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
            builder.Services.AddSingleton<GravityFallsClient.AppShell>();

            builder.Services.AddTransient<GravityFallsClient.Pages.MainMenuPage>();
            builder.Services.AddTransient<GravityFallsClient.Pages.CharacterSelectionPage>();
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
