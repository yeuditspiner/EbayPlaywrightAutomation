using Microsoft.Playwright;
using EbayPlaywrightAutomation.DataManagers;

namespace EbayPlaywrightAutomation.Utilities
{
    /// <summary>
    /// Centralises screenshot capture so every page screenshot is saved to a
    /// consistent location with a timestamped filename.
    /// </summary>
    public static class ScreenshotHelper
    {
        /// <summary>
        /// Takes a full-page screenshot and saves it to the configured screenshots folder.
        /// </summary>
        /// <param name="page">Active Playwright page.</param>
        /// <param name="label">Human-readable label used in the filename (spaces replaced with underscores).</param>
        /// <returns>The absolute path of the saved screenshot.</returns>
        public static async Task<string> CaptureAsync(IPage page, string label)
        {
            string folder = Path.Combine(AppContext.BaseDirectory, ConfigManager.ScreenshotsPath);
            Directory.CreateDirectory(folder);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string safeName = label.Replace(" ", "_").Replace("/", "-").Replace("\\", "-");
            string filename = $"{timestamp}_{safeName}.png";
            string fullPath = Path.Combine(folder, filename);

            await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = fullPath,
                FullPage = true
            });

            Console.WriteLine($"[Screenshot] Saved: {fullPath}");
            return fullPath;
        }

        /// <summary>
        /// Saves pre-captured screenshot bytes to a timestamped file.
        /// Used when bytes are already captured to avoid a second browser IO operation.
        /// </summary>
        public static async Task<string> SaveBytesAsync(byte[] bytes, string label)
        {
            string folder = Path.Combine(AppContext.BaseDirectory, ConfigManager.ScreenshotsPath);
            Directory.CreateDirectory(folder);

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            string safeName = label.Replace(" ", "_").Replace("/", "-").Replace("\\", "-");
            string fullPath = Path.Combine(folder, $"{timestamp}_{safeName}.png");

            await File.WriteAllBytesAsync(fullPath, bytes);
            return fullPath;
        }
        /// <summary>
        /// Takes a screenshot and returns its bytes (useful for attaching to Allure/Extent reports).
        /// Use SaveBytesAsync to persist to file without a second browser capture.
        /// </summary>
        public static async Task<byte[]> CaptureBytesAsync(IPage page)
        {
            return await page.ScreenshotAsync(new PageScreenshotOptions { FullPage = true });
        }
    }
}
