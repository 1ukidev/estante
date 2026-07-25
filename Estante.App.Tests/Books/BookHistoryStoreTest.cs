using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;

namespace Estante.App.Tests.Books
{
    [TestFixture]
    public class BookHistoryStoreTest
    {
        private TemporaryNativeStorage storage;

        [SetUp]
        public void SetUp()
        {
            storage = new TemporaryNativeStorage($"estante-history-{Guid.NewGuid()}");
        }

        [TearDown]
        public void TearDown()
        {
            storage.Dispose();
        }

        [Test]
        public void TestRecordsRecentBooks()
        {
            var history = new BookHistoryStore(storage);

            history.RecordOpened("/books/first.epub", "First book", "First author");
            history.RecordOpened("/books/second.epub", "Second book", "Second author");

            BookHistoryEntry[] recentBooks = history.GetRecentBooks().ToArray();

            Assert.That(recentBooks, Has.Length.EqualTo(2));
            Assert.That(recentBooks[0].Title, Is.EqualTo("Second book"));
            Assert.That(recentBooks[1].Title, Is.EqualTo("First book"));
        }

        [Test]
        public void TestProgressPersists()
        {
            const string filePath = "/books/progress.epub";

            var history = new BookHistoryStore(storage);
            history.RecordOpened(filePath, "Progress book", "Author");
            history.UpdateProgress(filePath, 4, 0.625);

            var reloadedHistory = new BookHistoryStore(storage);
            BookHistoryEntry entry = reloadedHistory.Get(filePath);

            Assert.That(entry, Is.Not.Null);
            Assert.That(entry.ChapterIndex, Is.EqualTo(4));
            Assert.That(entry.ScrollProgress, Is.EqualTo(0.625).Within(0.0001));
        }

        [Test]
        public void TestClearRemovesHistory()
        {
            var history = new BookHistoryStore(storage);
            history.RecordOpened("/books/book.epub", "Book", "Author");

            history.Clear();

            Assert.That(history.GetRecentBooks(), Is.Empty);
            Assert.That(new BookHistoryStore(storage).GetRecentBooks(), Is.Empty);
        }
    }
}
