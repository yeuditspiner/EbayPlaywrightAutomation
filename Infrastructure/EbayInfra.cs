using Microsoft.Playwright;
using EbayPlaywrightAutomation.DataManagers;
using EbayPlaywrightAutomation.Infrastructure.BusinessProcesses;

namespace EbayPlaywrightAutomation.Infrastructure
{
    /// <summary>
    /// Root automation facade. Owns the browser/context/page lifecycle
    /// and exposes PagesFactory and BusinessProcesses.
    /// </summary>
    public class EbayInfra : IAsyncDisposable
    {
        private IPlaywright? _playwright;
        private IBrowser? _browser;
        private IBrowserContext? _context;
        private IPage? _page;

        private PagesFactory? _pages;
        private EbayBusinessProcesses? _businessProcesses;

        public PagesFactory Pages => _pages
            ?? throw new InvalidOperationException("Call InitAsync() before accessing Pages.");

        public EbayBusinessProcesses BusinessProcesses => _businessProcesses
            ?? throw new InvalidOperationException("Call InitAsync() before accessing BusinessProcesses.");

        public IPage Page => _page
            ?? throw new InvalidOperationException("Call InitAsync() before accessing Page.");

        /// <summary>
        /// Launches the browser, opens a context and page, navigates to eBay.
        /// Uses a persistent browser profile to bypass eBay's bot detection.
        /// </summary>
        public async Task InitAsync()
        {
            _playwright = await Playwright.CreateAsync();

            var launchOptions = new BrowserTypeLaunchOptions
            {
                Headless = ConfigManager.Headless,
                SlowMo = ConfigManager.SlowMo,
                Args = new[]
                {
                    "--disable-blink-features=AutomationControlled",
                    "--no-sandbox"
                }
            };

            // Use a persistent context (saved profile) so eBay doesn't see a fresh browser
            string profilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "EbayPlaywrightProfile");

            Directory.CreateDirectory(profilePath);

            _context = ConfigManager.Browser.ToLower() switch
            {
                "firefox" => await _playwright.Firefox.LaunchPersistentContextAsync(profilePath,
                    new BrowserTypeLaunchPersistentContextOptions
                    {
                        Headless = ConfigManager.Headless,
                        SlowMo = ConfigManager.SlowMo,
                        ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0",
                        Locale = "en-US",
                        TimezoneId = "America/New_York"
                    }),
                _ => await _playwright.Chromium.LaunchPersistentContextAsync(profilePath,
                    new BrowserTypeLaunchPersistentContextOptions
                    {
                        Headless = ConfigManager.Headless,
                        SlowMo = ConfigManager.SlowMo,
                        ViewportSize = new ViewportSize { Width = 1440, Height = 900 },
                        UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36",
                        Locale = "en-US",
                        TimezoneId = "America/New_York",
                        JavaScriptEnabled = true,
                        Args = new[]
                        {
                            "--disable-blink-features=AutomationControlled"
                        },
                        ExtraHTTPHeaders = new Dictionary<string, string>
                        {
                            ["Accept-Language"] = "en-US,en;q=0.9"
                        }
                    })
            };

            _context.SetDefaultTimeout(ConfigManager.DefaultTimeoutMs);
            _context.SetDefaultNavigationTimeout(ConfigManager.NavigationTimeoutMs);

            _page = await _context.NewPageAsync();

            // Remove the webdriver property that eBay uses to detect automation
            await _page.AddInitScriptAsync(@"
                Object.defineProperty(navigator, 'webdriver', { get: () => undefined });
                Object.defineProperty(navigator, 'plugins', { get: () => [1, 2, 3] });
                Object.defineProperty(navigator, 'languages', { get: () => ['en-US', 'en'] });
            ");

            _pages = new PagesFactory(_page);
            _businessProcesses = new EbayBusinessProcesses(_pages, _page);

            await _page.GotoAsync(ConfigManager.BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = ConfigManager.NavigationTimeoutMs
            });

            // If eBay shows CAPTCHA, wait up to 30s for user to solve it manually
            string title = await _page.TitleAsync();
            if (title.Contains("Interruption") || title.Contains("challenge") || _page.Url.Contains("challenge"))
            {
                Console.WriteLine("[EbayInfra] CAPTCHA detected! Please solve it manually in the browser window.");
                Console.WriteLine("[EbayInfra] Waiting up to 30 seconds...");
                await _page.WaitForURLAsync(u => !u.Contains("challenge"), new PageWaitForURLOptions { Timeout = 30000 });
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_context != null) await _context.DisposeAsync();
            // _browser is null when using persistent context — that's fine
            if (_browser != null) await _browser.DisposeAsync();
            _playwright?.Dispose();
        }
    }
}
