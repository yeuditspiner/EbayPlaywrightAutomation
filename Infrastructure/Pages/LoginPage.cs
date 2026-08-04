using Microsoft.Playwright;
using EbayPlaywrightAutomation.Infrastructure;

namespace EbayPlaywrightAutomation.Infrastructure.Pages
{
    /// <summary>
    /// Page Object for the eBay sign-in page.
    /// All locators use XPath.
    /// </summary>
    public class LoginPage : BasePage
    {
        #region XPath Locators
        private ILocator EmailInput     => Page.Locator("xpath=//input[@id='userid']");
        private ILocator ContinueButton => Page.Locator("xpath=//button[@id='signin-continue-btn']");
        private ILocator PasswordInput  => Page.Locator("xpath=//input[@id='pass']");
        private ILocator SignInButton   => Page.Locator("xpath=//button[@id='sgnBt']");
        private ILocator SignInNavLink  => Page.Locator("xpath=//a[contains(@href,'signin') or normalize-space(text())='Sign in']").First;

        // Login success — user greeting in header
        private ILocator UserGreeting   => Page.Locator("xpath=//span[contains(@class,'gh-ug')] | //a[contains(@href,'myebay')] | //*[@id='gh-ug']").First;

        // Error message on wrong credentials
        private ILocator ErrorMessage   => Page.Locator("xpath=//span[@id='errMsg'] | //*[contains(@class,'fld-txt')]").First;
        #endregion

        public LoginPage(IPage page) : base(page) { }

        /// <summary>
        /// Navigates to eBay sign-in and performs the full email → password login flow.
        /// </summary>
        public async Task<bool> LoginAsync(string email, string password)
        {
            Console.WriteLine("[Login] Navigating to sign-in page...");

            bool linkVisible = await IsVisibleAsync(SignInNavLink, 5000);
            if (linkVisible)
                await ClickAsync(SignInNavLink);
            else
                await GoToAsync("https://signin.ebay.com/signin/");

            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Step 1 — email
            await FillAsync(EmailInput, email);
            await ClickAsync(ContinueButton);
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Step 2 — password
            bool passwordVisible = await IsVisibleAsync(PasswordInput, 10000);
            if (!passwordVisible)
            {
                Console.WriteLine("[Login] Password field not found after email step.");
                return false;
            }

            await FillAsync(PasswordInput, password);
            await ClickAsync(SignInButton);
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            // Verify
            bool loggedIn = await IsVisibleAsync(UserGreeting, 8000);
            if (loggedIn)
                Console.WriteLine("[Login] Login successful.");
            else
            {
                string errorText = await IsVisibleAsync(ErrorMessage, 3000)
                    ? await GetTextAsync(ErrorMessage)
                    : "Unknown error";
                Console.WriteLine($"[Login] Login failed: {errorText}");
            }

            return loggedIn;
        }

        /// <summary>Returns true if the user is already signed in.</summary>
        public async Task<bool> IsLoggedInAsync()
            => await IsVisibleAsync(UserGreeting, 3000);
    }
}
