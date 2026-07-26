using System;
using NUnit.Framework;
using osu.Framework.Testing;

namespace Estante.App.Tests.Translation
{
    [TestFixture]
    public class TranslationSettingsStoreTest
    {
        private TemporaryNativeStorage storage;

        [SetUp]
        public void SetUp()
        {
            storage = new TemporaryNativeStorage($"estante-translation-settings-{Guid.NewGuid()}");
        }

        [TearDown]
        public void TearDown()
        {
            storage.Dispose();
        }

        [Test]
        public void TestUsesDefaults()
        {
            var settings = new TranslationSettingsStore(storage);

            Assert.That(settings.LibreTranslateUrl, Is.EqualTo("127.0.0.1:5000"));
            Assert.That(settings.ApiKey, Is.Empty);
            Assert.That(settings.TargetLanguage, Is.EqualTo("pb"));
        }

        [Test]
        public void TestUrlPersists()
        {
            var settings = new TranslationSettingsStore(storage);

            Assert.That(settings.TrySetLibreTranslateUrl("https://translate.example.com/api"), Is.True);

            var reloadedSettings = new TranslationSettingsStore(storage);
            Assert.That(reloadedSettings.LibreTranslateUrl, Is.EqualTo("https://translate.example.com/api"));
        }

        [Test]
        public void TestInvalidUrlIsRejected()
        {
            var settings = new TranslationSettingsStore(storage);

            Assert.That(settings.TrySetLibreTranslateUrl("not a valid url"), Is.False);
            Assert.That(settings.LibreTranslateUrl, Is.EqualTo(TranslationSettingsStore.DEFAULT_LIBRE_TRANSLATE_URL));
        }

        [Test]
        public void TestApiKeyPersists()
        {
            var settings = new TranslationSettingsStore(storage);

            settings.SetApiKey("  secret-key  ");

            var reloadedSettings = new TranslationSettingsStore(storage);
            Assert.That(reloadedSettings.ApiKey, Is.EqualTo("secret-key"));
        }

        [Test]
        public void TestTargetLanguagePersists()
        {
            var settings = new TranslationSettingsStore(storage);

            Assert.That(settings.TrySetTargetLanguage("en"), Is.True);

            var reloadedSettings = new TranslationSettingsStore(storage);
            Assert.That(reloadedSettings.TargetLanguage, Is.EqualTo("en"));
        }

        [Test]
        public void TestInvalidTargetLanguageIsRejected()
        {
            var settings = new TranslationSettingsStore(storage);

            Assert.That(settings.TrySetTargetLanguage("invalid"), Is.False);
            Assert.That(settings.TargetLanguage, Is.EqualTo(TranslationSettingsStore.DEFAULT_TARGET_LANGUAGE));
        }
    }
}
