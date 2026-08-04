using Microsoft.Playwright;
using EbayPlaywrightAutomation.DataManagers;

namespace EbayPlaywrightAutomation.Infrastructure
{
    /// <summary>
    /// Base class for all Page Objects. Holds the IPage reference and
    /// exposes common Playwright helpers with configured timeouts.
    /// </summary>
    public abstract class BasePage
    {
        protected readonly IPage Page;
        protected readonly int DefaultTimeout;

        protected BasePage(IPage page)
        {
            Page = page;
            DefaultTimeout = ConfigManager.DefaultTimeoutMs;
        }

        /// <summary>Navigates to the given URL and waits until network is idle.</summary>
        public async Task GoToAsync(string url)
        {
            await Page.GotoAsync(url, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.Load,
                Timeout = ConfigManager.NavigationTimeoutMs
            });
        }

        /// <summary>Clicks a locator and waits for it to be visible first.</summary>
        protected async Task ClickAsync(ILocator locator)
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = DefaultTimeout
            });
            await locator.ClickAsync();
        }

        /// <summary>Fills an input after clearing its current value.</summary>
        protected async Task FillAsync(ILocator locator, string value)
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = DefaultTimeout
            });
            await locator.ClearAsync();
            await locator.FillAsync(value);
        }

        /// <summary>Returns inner text, trimmed.</summary>
        protected async Task<string> GetTextAsync(ILocator locator)
        {
            await locator.WaitForAsync(new LocatorWaitForOptions
            {
                State = WaitForSelectorState.Visible,
                Timeout = DefaultTimeout
            });
            return (await locator.InnerTextAsync()).Trim();
        }

        /// <summary>Checks whether a locator is visible within the timeout.</summary>
        protected async Task<bool> IsVisibleAsync(ILocator locator, int timeoutMs = 5000)
        {
            try
            {
                await locator.WaitForAsync(new LocatorWaitForOptions
                {
                    State = WaitForSelectorState.Visible,
                    Timeout = timeoutMs
                });
                return true;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }
    }
}
