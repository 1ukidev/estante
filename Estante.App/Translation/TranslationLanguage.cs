using System;
using System.Collections.Generic;
using System.Linq;

namespace Estante.App
{
    public sealed record TranslationLanguage(string Code, string Name)
    {
        public override string ToString() => Name;
    }

    public static class TranslationLanguages
    {
        public static IReadOnlyList<TranslationLanguage> All { get; } = new[]
        {
            new TranslationLanguage("pt", "Portuguese"),
            new TranslationLanguage("pb", "Portuguese (Brazil)"),
            new TranslationLanguage("en", "English"),
            new TranslationLanguage("es", "Spanish"),
            new TranslationLanguage("fr", "French"),
            new TranslationLanguage("de", "German"),
            new TranslationLanguage("it", "Italian"),
            new TranslationLanguage("nl", "Dutch"),
            new TranslationLanguage("pl", "Polish"),
            new TranslationLanguage("ru", "Russian"),
            new TranslationLanguage("ja", "Japanese"),
            new TranslationLanguage("zh", "Chinese"),
            new TranslationLanguage("ar", "Arabic")
        };

        public static TranslationLanguage Find(string code) =>
            All.FirstOrDefault(language => string.Equals(language.Code, code, StringComparison.OrdinalIgnoreCase));

        public static bool IsSupported(string code) => Find(code) != null;
    }
}
