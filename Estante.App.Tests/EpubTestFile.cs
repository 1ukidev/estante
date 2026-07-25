using System.IO;
using System.IO.Compression;
using System.Text;

namespace Estante.App.Tests
{
    internal static class EpubTestFile
    {
        public static string Create()
        {
            string filePath = Path.Combine(Path.GetTempPath(), $"estante-test-{Path.GetRandomFileName()}.epub");

            using var file = File.Create(filePath);
            using var archive = new ZipArchive(file, ZipArchiveMode.Create);

            addEntry(archive, "mimetype", "application/epub+zip", CompressionLevel.NoCompression);
            addEntry(
                archive,
                "META-INF/container.xml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <container version="1.0" xmlns="urn:oasis:names:tc:opendocument:xmlns:container">
                  <rootfiles>
                    <rootfile full-path="OEBPS/content.opf" media-type="application/oebps-package+xml"/>
                  </rootfiles>
                </container>
                """);
            addEntry(
                archive,
                "OEBPS/content.opf",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <package version="3.0" unique-identifier="book-id" xmlns="http://www.idpf.org/2007/opf">
                  <metadata xmlns:dc="http://purl.org/dc/elements/1.1/">
                    <dc:identifier id="book-id">urn:uuid:estante-test</dc:identifier>
                    <dc:title>Test book</dc:title>
                    <dc:creator>Test author</dc:creator>
                    <dc:language>pt-BR</dc:language>
                    <meta property="dcterms:modified">2026-07-25T00:00:00Z</meta>
                  </metadata>
                  <manifest>
                    <item id="cover" href="cover.png" media-type="image/png" properties="cover-image"/>
                    <item id="chapter" href="chapter.xhtml" media-type="application/xhtml+xml"/>
                    <item id="chapter-2" href="chapter-2.xhtml" media-type="application/xhtml+xml"/>
                    <item id="chapter-3" href="chapter-3.xhtml" media-type="application/xhtml+xml"/>
                    <item id="chapter-4" href="chapter-4.xhtml" media-type="application/xhtml+xml"/>
                    <item id="chapter-5" href="chapter-5.xhtml" media-type="application/xhtml+xml"/>
                    <item id="chapter-6" href="chapter-6.xhtml" media-type="application/xhtml+xml"/>
                    <item id="chapter-7" href="chapter-7.xhtml" media-type="application/xhtml+xml"/>
                    <item id="chapter-8" href="chapter-8.xhtml" media-type="application/xhtml+xml"/>
                    <item id="navigation" href="navigation.xhtml" media-type="application/xhtml+xml" properties="nav"/>
                  </manifest>
                  <spine>
                    <itemref idref="chapter"/>
                    <itemref idref="chapter-2"/>
                    <itemref idref="chapter-3"/>
                    <itemref idref="chapter-4"/>
                    <itemref idref="chapter-5"/>
                    <itemref idref="chapter-6"/>
                    <itemref idref="chapter-7"/>
                    <itemref idref="chapter-8"/>
                  </spine>
                </package>
                """);
            addEntry(
                archive,
                "OEBPS/chapter.xhtml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <html xmlns="http://www.w3.org/1999/xhtml">
                  <head><title>Chapter 1</title></head>
                  <body>
                    <h1>Chapter 1</h1>
                    <p>Chapter content.</p>
                    <p>Second paragraph for reading.</p>
                  </body>
                </html>
                """);
            addBinaryEntry(
                archive,
                "OEBPS/cover.png",
                System.Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII="));
            addEntry(
                archive,
                "OEBPS/chapter-2.xhtml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <html xmlns="http://www.w3.org/1999/xhtml">
                  <head><title>Chapter 2</title></head>
                  <body>
                    <h1>Chapter 2</h1>
                    <p>Second chapter content.</p>
                    <p>Reading position test paragraph 01. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 02. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 03. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 04. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 05. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 06. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 07. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 08. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 09. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 10. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 11. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 12. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 13. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 14. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 15. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 16. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 17. This line intentionally makes the chapter long enough to scroll.</p>
                    <p>Reading position test paragraph 18. This line intentionally makes the chapter long enough to scroll.</p>
                  </body>
                </html>
                """);

            for (int chapter = 3; chapter <= 8; chapter++)
            {
                addEntry(
                    archive,
                    $"OEBPS/chapter-{chapter}.xhtml",
                    $"""
                     <?xml version="1.0" encoding="UTF-8"?>
                     <html xmlns="http://www.w3.org/1999/xhtml">
                       <head><title>Chapter {chapter}</title></head>
                       <body>
                         <h1>Chapter {chapter}</h1>
                         <p>Chapter {chapter} content.</p>
                       </body>
                     </html>
                     """);
            }

            addEntry(
                archive,
                "OEBPS/navigation.xhtml",
                """
                <?xml version="1.0" encoding="UTF-8"?>
                <html xmlns="http://www.w3.org/1999/xhtml" xmlns:epub="http://www.idpf.org/2007/ops">
                  <head><title>Table of Contents</title></head>
                  <body>
                    <nav epub:type="toc">
                      <ol>
                        <li><a href="chapter.xhtml">Chapter 1</a></li>
                        <li><a href="chapter-2.xhtml">Chapter 2</a></li>
                        <li><a href="chapter-3.xhtml">Chapter 3</a></li>
                        <li><a href="chapter-4.xhtml">Chapter 4</a></li>
                        <li><a href="chapter-5.xhtml">Chapter 5</a></li>
                        <li><a href="chapter-6.xhtml">Chapter 6</a></li>
                        <li><a href="chapter-7.xhtml">Chapter 7</a></li>
                        <li><a href="chapter-8.xhtml">Chapter 8</a></li>
                      </ol>
                    </nav>
                  </body>
                </html>
                """);

            return filePath;
        }

        private static void addEntry(ZipArchive archive, string path, string content, CompressionLevel compressionLevel = CompressionLevel.Optimal)
        {
            ZipArchiveEntry entry = archive.CreateEntry(path, compressionLevel);

            using Stream stream = entry.Open();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.Write(content);
        }

        private static void addBinaryEntry(ZipArchive archive, string path, byte[] content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);

            using Stream stream = entry.Open();
            stream.Write(content);
        }
    }
}
