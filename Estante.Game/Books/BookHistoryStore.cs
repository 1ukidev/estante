using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using osu.Framework.Platform;

namespace Estante.Game
{
    public sealed class BookHistoryStore
    {
        private const string history_file = "book-history.json";
        private const int maximum_entries = 10;

        private static readonly JsonSerializerOptions serializer_options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        private readonly Storage storage;
        private readonly List<BookHistoryEntry> entries;

        public BookHistoryStore(Storage storage)
        {
            this.storage = storage.GetStorageForDirectory("state");
            entries = loadEntries();
        }

        public IReadOnlyList<BookHistoryEntry> GetRecentBooks(int count = 3) =>
            entries.OrderByDescending(entry => entry.LastOpenedUtc)
                   .Take(Math.Max(0, count))
                   .Select(entry => entry.Clone())
                   .ToArray();

        public BookHistoryEntry Get(string filePath)
        {
            string normalizedPath = normalizePath(filePath);
            return entries.FirstOrDefault(entry => pathsEqual(entry.FilePath, normalizedPath))?.Clone();
        }

        public void RecordOpened(string filePath, string title, string author)
        {
            string normalizedPath = normalizePath(filePath);
            BookHistoryEntry entry = findOrCreate(normalizedPath);

            entry.Title = string.IsNullOrWhiteSpace(title) ? Path.GetFileNameWithoutExtension(normalizedPath) : title.Trim();
            entry.Author = string.IsNullOrWhiteSpace(author) ? "Unknown author" : author.Trim();
            entry.LastOpenedUtc = DateTimeOffset.UtcNow;

            trimEntries();
            save();
        }

        public void UpdateProgress(string filePath, int chapterIndex, double scrollProgress)
        {
            string normalizedPath = normalizePath(filePath);
            BookHistoryEntry entry = findOrCreate(normalizedPath);

            entry.ChapterIndex = Math.Max(0, chapterIndex);
            entry.ScrollProgress = Math.Clamp(scrollProgress, 0, 1);
            save();
        }

        public void Clear()
        {
            entries.Clear();
            save();
        }

        private BookHistoryEntry findOrCreate(string normalizedPath)
        {
            BookHistoryEntry entry = entries.FirstOrDefault(candidate => pathsEqual(candidate.FilePath, normalizedPath));

            if (entry != null)
                return entry;

            entry = new BookHistoryEntry
            {
                FilePath = normalizedPath,
                Title = Path.GetFileNameWithoutExtension(normalizedPath),
                Author = "Unknown author",
                LastOpenedUtc = DateTimeOffset.UtcNow
            };
            entries.Add(entry);
            return entry;
        }

        private List<BookHistoryEntry> loadEntries()
        {
            if (!storage.Exists(history_file))
                return new List<BookHistoryEntry>();

            try
            {
                using Stream stream = storage.GetStream(history_file, FileAccess.Read, FileMode.Open);
                var data = JsonSerializer.Deserialize<BookHistoryData>(stream, serializer_options);

                return data?.Books?
                           .Where(entry => !string.IsNullOrWhiteSpace(entry.FilePath))
                           .OrderByDescending(entry => entry.LastOpenedUtc)
                           .Take(maximum_entries)
                           .ToList()
                       ?? new List<BookHistoryEntry>();
            }
            catch
            {
                return new List<BookHistoryEntry>();
            }
        }

        private void trimEntries()
        {
            if (entries.Count <= maximum_entries)
                return;

            entries.RemoveAll(entry => !entries.OrderByDescending(candidate => candidate.LastOpenedUtc)
                                               .Take(maximum_entries)
                                               .Contains(entry));
        }

        private void save()
        {
            using Stream stream = storage.CreateFileSafely(history_file);
            JsonSerializer.Serialize(stream, new BookHistoryData { Books = entries }, serializer_options);
        }

        private static string normalizePath(string filePath) =>
            Path.GetFullPath(filePath);

        private static bool pathsEqual(string first, string second) =>
            string.Equals(first, second, StringComparison.OrdinalIgnoreCase);

        private sealed class BookHistoryData
        {
            public List<BookHistoryEntry> Books { get; set; } = new List<BookHistoryEntry>();
        }
    }

    public sealed class BookHistoryEntry
    {
        public string FilePath { get; set; }
        public string Title { get; set; }
        public string Author { get; set; }
        public DateTimeOffset LastOpenedUtc { get; set; }
        public int ChapterIndex { get; set; }
        public double ScrollProgress { get; set; }

        internal BookHistoryEntry Clone() =>
            new BookHistoryEntry
            {
                FilePath = FilePath,
                Title = Title,
                Author = Author,
                LastOpenedUtc = LastOpenedUtc,
                ChapterIndex = ChapterIndex,
                ScrollProgress = ScrollProgress
            };
    }
}
