using Microsoft.Extensions.Configuration;

namespace EbayPlaywrightAutomation.DataManagers
{
    /// <summary>
    /// Provides typed access to appsettings.json configuration values.
    /// </summary>
    public static class ConfigManager
    {
        private static readonly IConfiguration _config;

        static ConfigManager()
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();
        }

        public static string BaseUrl => _config["BaseUrl"] ?? "https://www.ebay.com";

        public static string Browser => _config["Browser"] ?? "Chromium";

        public static bool Headless => bool.TryParse(_config["Headless"], out var h) ? h : false;

        public static int SlowMo => int.TryParse(_config["SlowMo"], out var s) ? s : 50;

        public static int DefaultTimeoutMs => int.TryParse(_config["DefaultTimeoutMs"], out var t) ? t : 30000;

        public static int NavigationTimeoutMs => int.TryParse(_config["NavigationTimeoutMs"], out var t) ? t : 60000;

        public static string ScreenshotsPath => _config["ScreenshotsPath"] ?? "Screenshots";

        public static string AllureResultsPath => _config["AllureResultsPath"] ?? "allure-results";

        public static int DefaultSearchLimit => int.TryParse(_config["Search:DefaultLimit"], out var l) ? l : 5;

        public static bool PagingEnabled => bool.TryParse(_config["Search:PagingEnabled"], out var p) ? p : true;

        public static string LoginEmail => _config["Login:Email"] ?? string.Empty;

        public static string LoginPassword => _config["Login:Password"] ?? string.Empty;

        /// <summary>
        /// When true, the login step is skipped and the test runs as a guest.
        /// Set to false and fill Email/Password to run as a signed-in user.
        /// </summary>
        public static bool SkipLogin => bool.TryParse(_config["Login:Skip"], out var s) ? s : true;
    }
}
