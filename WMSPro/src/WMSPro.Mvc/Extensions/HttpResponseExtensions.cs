using System.Text.Json;

namespace WMSPro.Mvc.Extensions;

public static class HttpResponseExtensions
{
    public static async Task<string> GetErrorMessageAsync(this HttpResponseMessage response)
    {
        var rawContent = await response.Content.ReadAsStringAsync();
        if (string.IsNullOrWhiteSpace(rawContent))
        {
            return response.ReasonPhrase ?? "An error occurred.";
        }

        try
        {
            using var doc = JsonDocument.Parse(rawContent);
            if (doc.RootElement.TryGetProperty("error", out var errorProp))
            {
                return errorProp.GetString() ?? "An error occurred.";
            }
        }
        catch
        {
            // If it's not JSON, return the raw string
        }

        return rawContent;
    }
}
