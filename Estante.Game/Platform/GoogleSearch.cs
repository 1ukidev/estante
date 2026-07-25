using System;

namespace Estante.Game
{
    public static class GoogleSearch
    {
        public static string CreateUrl(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                throw new ArgumentException("A search query is required.", nameof(query));

            return $"https://www.google.com/search?q={Uri.EscapeDataString(query.Trim())}";
        }
    }
}
