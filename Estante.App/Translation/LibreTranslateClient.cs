using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Estante.App
{
    public sealed class LibreTranslateClient
    {
        private static readonly HttpClient shared_http_client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        private readonly HttpClient httpClient;

        public LibreTranslateClient(HttpClient httpClient = null)
        {
            this.httpClient = httpClient ?? shared_http_client;
        }

        public async Task<string> TranslateAsync(string serverUrl, string text, string targetLanguage, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
                throw new ArgumentException("Text cannot be empty.", nameof(text));

            if (!TryCreateTranslateEndpoint(serverUrl, out Uri endpoint))
                throw new TranslationException("The LibreTranslate URL is invalid.");

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
            {
                Content = JsonContent.Create(new TranslationRequest
                {
                    Q = text,
                    Source = "auto",
                    Target = targetLanguage,
                    Format = "text"
                })
            };
            using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
                throw new TranslationException(readError(responseBody) ?? $"LibreTranslate returned HTTP {(int)response.StatusCode}.");

            try
            {
                TranslationResponse translation = JsonSerializer.Deserialize<TranslationResponse>(responseBody);

                if (string.IsNullOrWhiteSpace(translation?.TranslatedText))
                    throw new TranslationException("LibreTranslate returned an empty translation.");

                return translation.TranslatedText.Trim();
            }
            catch (JsonException)
            {
                throw new TranslationException("LibreTranslate returned an invalid response.");
            }
        }

        public static bool TryCreateTranslateEndpoint(string serverUrl, out Uri endpoint)
        {
            endpoint = null;

            if (string.IsNullOrWhiteSpace(serverUrl))
                return false;

            string normalizedUrl = serverUrl.Trim();

            if (!normalizedUrl.Contains("://", StringComparison.Ordinal))
                normalizedUrl = $"http://{normalizedUrl}";

            if (!Uri.TryCreate(normalizedUrl, UriKind.Absolute, out Uri baseUri)
                || (baseUri.Scheme != Uri.UriSchemeHttp && baseUri.Scheme != Uri.UriSchemeHttps)
                || string.IsNullOrWhiteSpace(baseUri.Host))
                return false;

            string baseUrl = baseUri.AbsoluteUri.TrimEnd('/') + "/";
            endpoint = new Uri(new Uri(baseUrl), "translate");
            return true;
        }

        private static string readError(string responseBody)
        {
            try
            {
                return JsonSerializer.Deserialize<TranslationError>(responseBody)?.Error;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private sealed class TranslationRequest
        {
            [JsonPropertyName("q")]
            public string Q { get; set; }

            [JsonPropertyName("source")]
            public string Source { get; set; }

            [JsonPropertyName("target")]
            public string Target { get; set; }

            [JsonPropertyName("format")]
            public string Format { get; set; }
        }

        private sealed class TranslationResponse
        {
            [JsonPropertyName("translatedText")]
            public string TranslatedText { get; set; }
        }

        private sealed class TranslationError
        {
            [JsonPropertyName("error")]
            public string Error { get; set; }
        }
    }

    public sealed class TranslationException : Exception
    {
        public TranslationException(string message)
            : base(message)
        {
        }
    }
}
