using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class UserSanitizer
{
    public static void SanitizeUsersFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            Console.WriteLine("❌ File not found.");
            return;
        }

        string rawContent = File.ReadAllText(filePath);

        // Remove trailing commas and wrap in array if not already
        string cleanedJson = rawContent.Trim().Trim(',');

        // Ensure it becomes a valid JSON array
        cleanedJson = $"[{cleanedJson}]";

        try
        {
            // Validate that it’s a valid JSON array
            var parsed = JArray.Parse(cleanedJson);

            // Reformat it neatly
            string formattedJson = JsonConvert.SerializeObject(parsed, Formatting.Indented);

            // Overwrite original file
            File.WriteAllText(filePath, formattedJson);

            Console.WriteLine("✅ Users.txt sanitized and saved.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to parse and save file: {ex.Message}");
        }
    }
}
