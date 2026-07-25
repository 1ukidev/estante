using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Estante.Game.Tests.Translation
{
    [TestFixture]
    public class LibreTranslateClientTest
    {
        [Test]
        public async Task TestTranslatesUsingExpectedRequest()
        {
            var handler = new RecordingHandler(HttpStatusCode.OK, """{"translatedText":"Olá, mundo!"}""");
            var client = new LibreTranslateClient(new HttpClient(handler));

            string result = await client.TranslateAsync("127.0.0.1:5000", "Hello, world!", "es");

            Assert.That(result, Is.EqualTo("Olá, mundo!"));
            Assert.That(handler.RequestUri?.ToString(), Is.EqualTo("http://127.0.0.1:5000/translate"));
            Assert.That(handler.RequestBody, Does.Contain("\"q\":\"Hello, world!\""));
            Assert.That(handler.RequestBody, Does.Contain("\"source\":\"auto\""));
            Assert.That(handler.RequestBody, Does.Contain("\"target\":\"es\""));
        }

        [Test]
        public void TestReturnsApiError()
        {
            var handler = new RecordingHandler(HttpStatusCode.BadRequest, """{"error":"Invalid target language"}""");
            var client = new LibreTranslateClient(new HttpClient(handler));

            TranslationException exception = Assert.ThrowsAsync<TranslationException>(
                async () => await client.TranslateAsync("http://localhost:5000", "Hello", "es"));

            Assert.That(exception?.Message, Is.EqualTo("Invalid target language"));
        }

        private sealed class RecordingHandler : HttpMessageHandler
        {
            private readonly HttpStatusCode statusCode;
            private readonly string responseBody;

            public System.Uri RequestUri { get; private set; }
            public string RequestBody { get; private set; }

            public RecordingHandler(HttpStatusCode statusCode, string responseBody)
            {
                this.statusCode = statusCode;
                this.responseBody = responseBody;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                RequestUri = request.RequestUri;
                RequestBody = await request.Content.ReadAsStringAsync(cancellationToken);

                return new HttpResponseMessage(statusCode)
                {
                    Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
                };
            }
        }
    }
}
