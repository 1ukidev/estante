using System.IO;
using System.Text.Json;
using osu.Framework.Platform;

namespace Estante.App
{
    public sealed class TranslationSettingsStore
    {
        public const string DEFAULT_LIBRE_TRANSLATE_URL = "127.0.0.1:5000";
        public const string DEFAULT_TARGET_LANGUAGE = "pb";

        private const string settings_file = "translation-settings.json";

        private static readonly JsonSerializerOptions serializer_options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private readonly Storage storage;

        public string LibreTranslateUrl { get; private set; } = DEFAULT_LIBRE_TRANSLATE_URL;
        public string ApiKey { get; private set; } = string.Empty;
        public string TargetLanguage { get; private set; } = DEFAULT_TARGET_LANGUAGE;

        public TranslationSettingsStore(Storage storage)
        {
            this.storage = storage.GetStorageForDirectory("state");
            load();
        }

        public bool TrySetLibreTranslateUrl(string value)
        {
            string trimmedValue = value?.Trim();

            if (!LibreTranslateClient.TryCreateTranslateEndpoint(trimmedValue, out _))
                return false;

            LibreTranslateUrl = trimmedValue;
            save();
            return true;
        }

        public void SetApiKey(string value)
        {
            ApiKey = value?.Trim() ?? string.Empty;
            save();
        }

        public bool TrySetTargetLanguage(string languageCode)
        {
            if (!TranslationLanguages.IsSupported(languageCode))
                return false;

            TargetLanguage = languageCode.ToLowerInvariant();
            save();
            return true;
        }

        private void load()
        {
            if (!storage.Exists(settings_file))
                return;

            try
            {
                using Stream stream = storage.GetStream(settings_file, FileAccess.Read, FileMode.Open);
                TranslationSettingsData data = JsonSerializer.Deserialize<TranslationSettingsData>(stream, serializer_options);

                if (LibreTranslateClient.TryCreateTranslateEndpoint(data?.LibreTranslateUrl, out _))
                    LibreTranslateUrl = data.LibreTranslateUrl.Trim();

                ApiKey = data?.ApiKey?.Trim() ?? string.Empty;

                if (TranslationLanguages.IsSupported(data?.TargetLanguage))
                    TargetLanguage = data.TargetLanguage.ToLowerInvariant();
            }
            catch
            {
                LibreTranslateUrl = DEFAULT_LIBRE_TRANSLATE_URL;
                ApiKey = string.Empty;
                TargetLanguage = DEFAULT_TARGET_LANGUAGE;
            }
        }

        private void save()
        {
            using Stream stream = storage.CreateFileSafely(settings_file);
            JsonSerializer.Serialize(stream, new TranslationSettingsData
            {
                LibreTranslateUrl = LibreTranslateUrl,
                ApiKey = ApiKey,
                TargetLanguage = TargetLanguage
            }, serializer_options);
        }

        private sealed class TranslationSettingsData
        {
            public string LibreTranslateUrl { get; set; }
            public string ApiKey { get; set; }
            public string TargetLanguage { get; set; }
        }
    }
}
