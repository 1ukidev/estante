using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Estante.App.Tests
{
    [TestFixture]
    public class EpubProcessorTest
    {
        private string epubFilePath;

        [SetUp]
        public void SetUp()
        {
            epubFilePath = EpubTestFile.Create();
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(epubFilePath))
                File.Delete(epubFilePath);
        }

        [Test]
        public async Task TestProcessEpub()
        {
            var processor = new EpubProcessor();

            var book = await processor.ProcessAsync(epubFilePath);

            Assert.That(book.Title, Is.EqualTo("Test book"));
            Assert.That(book.Author, Is.EqualTo("Test author"));
            Assert.That(book.ReadingOrder, Has.Count.EqualTo(8));
            Assert.That(book.ReadingOrder[0].Content, Does.Contain("Chapter content"));
        }

        [Test]
        public async Task TestPrepareEpubForReading()
        {
            var processor = new EpubProcessor();

            ProcessedEpubBook book = await processor.ProcessForReadingAsync(epubFilePath);

            Assert.That(book.Title, Is.EqualTo("Test book"));
            Assert.That(book.Author, Is.EqualTo("Test author"));
            Assert.That(book.CoverImage, Is.Not.Empty);
            Assert.That(book.Chapters, Has.Count.EqualTo(8));
            Assert.That(book.Chapters[0].Title, Is.EqualTo("Chapter 1"));
            Assert.That(book.Chapters[0].Text, Does.Contain("Chapter content."));
            Assert.That(book.Chapters[0].Text, Does.Contain("Second paragraph for reading."));
            Assert.That(book.Chapters[0].Text, Does.Not.StartWith("Chapter 1"));
            Assert.That(book.Chapters[1].Title, Is.EqualTo("Chapter 2"));
            Assert.That(book.Chapters[1].Text, Does.Contain("Second chapter content."));
        }

        [Test]
        public void TestRejectsNonEpubFile()
        {
            var processor = new EpubProcessor();

            Assert.ThrowsAsync<System.ArgumentException>(() => processor.ProcessAsync(Path.ChangeExtension(epubFilePath, ".txt")));
        }

        [Test]
        public void TestRejectsMissingFile()
        {
            var processor = new EpubProcessor();
            string missingFilePath = Path.Combine(Path.GetTempPath(), $"{Path.GetRandomFileName()}.epub");

            Assert.ThrowsAsync<FileNotFoundException>(() => processor.ProcessAsync(missingFilePath));
        }

    }
}
