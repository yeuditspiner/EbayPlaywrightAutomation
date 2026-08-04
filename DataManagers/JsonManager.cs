using Newtonsoft.Json;

namespace EbayPlaywrightAutomation.DataManagers
{
    /// <summary>
    /// Generic JSON file reader for data-driven test scenarios.
    /// </summary>
    public static class JsonManager
    {
        /// <summary>
        /// Deserializes a JSON file into the specified type T.
        /// </summary>
        /// <typeparam name="T">Target deserialization type.</typeparam>
        /// <param name="relativePath">Path relative to the test output directory (e.g. "TestData/ebay_search.json").</param>
        public static T LoadFromFile<T>(string relativePath)
        {
            string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);

            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Test data file not found: {fullPath}");

            string json = File.ReadAllText(fullPath);
            var result = JsonConvert.DeserializeObject<T>(json);

            if (result == null)
                throw new InvalidOperationException($"Failed to deserialize JSON from: {fullPath}");

            return result;
        }

        /// <summary>
        /// Serializes an object and writes it to a JSON file (useful for saving test state).
        /// </summary>
        public static void SaveToFile<T>(T data, string relativePath)
        {
            string fullPath = Path.Combine(AppContext.BaseDirectory, relativePath);
            string? dir = Path.GetDirectoryName(fullPath);
            if (dir != null) Directory.CreateDirectory(dir);

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(fullPath, json);
        }
    }
}
