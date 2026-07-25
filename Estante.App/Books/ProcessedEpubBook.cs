using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using VersOne.Epub;

namespace Estante.App
{
    public sealed class ProcessedEpubBook
    {
        public string FilePath { get; }
        public string Title { get; }
        public string Author { get; }
        public string Description { get; }
        public byte[] CoverImage { get; }
        public IReadOnlyList<ProcessedEpubChapter> Chapters { get; }

        private ProcessedEpubBook(
            string filePath,
            string title,
            string author,
            string description,
            byte[] coverImage,
            IReadOnlyList<ProcessedEpubChapter> chapters)
        {
            FilePath = filePath;
            Title = title;
            Author = author;
            Description = description;
            CoverImage = coverImage;
            Chapters = chapters;
        }

        internal static ProcessedEpubBook Create(EpubBook book)
        {
            var navigationTitles = new Dictionary<string, string>(StringComparer.Ordinal);
            collectNavigationTitles(book.Navigation, navigationTitles);

            var chapters = new List<ProcessedEpubChapter>();

            for (int i = 0; i < book.ReadingOrder.Count; i++)
            {
                EpubLocalTextContentFile contentFile = book.ReadingOrder[i];
                ParsedContent parsedContent = parseContent(contentFile.Content);

                if (string.IsNullOrWhiteSpace(parsedContent.Text))
                    continue;

                navigationTitles.TryGetValue(contentFile.FilePath, out string navigationTitle);
                string title = firstNotEmpty(navigationTitle, parsedContent.Title, $"Chapter {chapters.Count + 1}");

                chapters.Add(new ProcessedEpubChapter(contentFile.FilePath, title, parsedContent.Text));
            }

            if (chapters.Count == 0)
                throw new InvalidDataException("The EPUB contains no readable text chapters.");

            string fallbackTitle = Path.GetFileNameWithoutExtension(book.FilePath);

            return new ProcessedEpubBook(
                book.FilePath,
                firstNotEmpty(book.Title, fallbackTitle, "Untitled book"),
                firstNotEmpty(book.Author, "Unknown author"),
                book.Description,
                book.CoverImage,
                new ReadOnlyCollection<ProcessedEpubChapter>(chapters));
        }

        private static void collectNavigationTitles(IEnumerable<EpubNavigationItem> items, IDictionary<string, string> titles)
        {
            if (items == null)
                return;

            foreach (EpubNavigationItem item in items)
            {
                if (item.HtmlContentFile != null && !string.IsNullOrWhiteSpace(item.Title))
                    titles[item.HtmlContentFile.FilePath] = item.Title.Trim();

                collectNavigationTitles(item.NestedItems, titles);
            }
        }

        private static ParsedContent parseContent(string html)
        {
            if (string.IsNullOrWhiteSpace(html))
                return new ParsedContent(null, string.Empty);

            try
            {
                XDocument document = XDocument.Parse(html, LoadOptions.PreserveWhitespace);
                XElement body = document.Descendants().FirstOrDefault(element => element.Name.LocalName.Equals("body", StringComparison.OrdinalIgnoreCase))
                                ?? document.Root;
                string title = findTitle(document);
                var builder = new StringBuilder();

                appendNode(body, builder);
                return new ParsedContent(title, removeLeadingTitle(normalizeText(builder.ToString()), title));
            }
            catch (XmlException)
            {
                return parseMalformedHtml(html);
            }
        }

        private static string findTitle(XDocument document)
        {
            string[] preferredElements = { "h1", "h2", "title" };

            foreach (string elementName in preferredElements)
            {
                XElement element = document.Descendants()
                                           .FirstOrDefault(candidate => candidate.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));
                string value = normalizeInlineText(element?.Value);

                if (!string.IsNullOrWhiteSpace(value))
                    return value;
            }

            return null;
        }

        private static void appendNode(XNode node, StringBuilder builder)
        {
            if (node is XText text)
            {
                appendInlineText(builder, text.Value);
                return;
            }

            if (!(node is XElement element))
                return;

            string name = element.Name.LocalName.ToLowerInvariant();

            if (name is "head" or "script" or "style" or "svg" or "nav")
                return;

            bool block = name is "address" or "article" or "aside" or "blockquote" or "div" or "figcaption"
                or "figure" or "footer" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6"
                or "header" or "hr" or "li" or "main" or "ol" or "p" or "pre" or "section" or "table" or "ul";

            if (block)
                appendLineBreak(builder);

            if (name == "li")
                builder.Append("• ");

            if (name == "br")
                appendLineBreak(builder);

            foreach (XNode child in element.Nodes())
                appendNode(child, builder);

            if (block)
                appendLineBreak(builder);
        }

        private static ParsedContent parseMalformedHtml(string html)
        {
            string title = null;
            Match titleMatch = Regex.Match(html, @"<(h1|h2|title)\b[^>]*>(.*?)</\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (titleMatch.Success)
                title = normalizeInlineText(stripTags(titleMatch.Groups[2].Value));

            string text = Regex.Replace(html, @"<(script|style|svg|nav)\b[^>]*>.*?</\1>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            text = Regex.Replace(text, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, @"</?(address|article|aside|blockquote|div|figcaption|figure|footer|h[1-6]|header|hr|li|main|ol|p|pre|section|table|ul)\b[^>]*>", "\n", RegexOptions.IgnoreCase);
            text = stripTags(text);

            return new ParsedContent(title, removeLeadingTitle(normalizeText(WebUtility.HtmlDecode(text)), title));
        }

        private static string stripTags(string value) => Regex.Replace(value, "<[^>]+>", string.Empty);

        private static void appendInlineText(StringBuilder builder, string value)
        {
            string normalized = normalizeInlineText(value);

            if (string.IsNullOrEmpty(normalized))
                return;

            bool startsWithPunctuation = char.IsPunctuation(normalized[0]) && normalized[0] is not '(' and not '[' and not '{';

            if (!startsWithPunctuation && builder.Length > 0 && builder[^1] != '\n' && builder[^1] != ' ')
                builder.Append(' ');

            builder.Append(normalized);
        }

        private static void appendLineBreak(StringBuilder builder)
        {
            while (builder.Length > 0 && builder[^1] == ' ')
                builder.Length--;

            if (builder.Length > 0 && builder[^1] != '\n')
                builder.Append('\n');
        }

        private static string normalizeInlineText(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : Regex.Replace(WebUtility.HtmlDecode(value), @"\s+", " ").Trim();

        private static string normalizeText(string value)
        {
            string[] lines = value.Replace("\r", string.Empty)
                                  .Split('\n')
                                  .Select(normalizeInlineText)
                                  .Where(line => !string.IsNullOrWhiteSpace(line))
                                  .ToArray();

            return string.Join("\n\n", lines);
        }

        private static string removeLeadingTitle(string text, string title)
        {
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(text))
                return text;

            if (text.Equals(title, StringComparison.OrdinalIgnoreCase))
                return string.Empty;

            string prefix = $"{title}\n\n";
            return text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? text[prefix.Length..] : text;
        }

        private static string firstNotEmpty(params string[] values) =>
            values.First(value => !string.IsNullOrWhiteSpace(value)).Trim();

        private readonly struct ParsedContent
        {
            public string Title { get; }
            public string Text { get; }

            public ParsedContent(string title, string text)
            {
                Title = title;
                Text = text;
            }
        }
    }

    public sealed class ProcessedEpubChapter
    {
        public string FilePath { get; }
        public string Title { get; }
        public string Text { get; }

        internal ProcessedEpubChapter(string filePath, string title, string text)
        {
            FilePath = filePath;
            Title = title;
            Text = text;
        }
    }
}
