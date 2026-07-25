using System;
using System.IO;
using System.Threading.Tasks;
using VersOne.Epub;
using VersOne.Epub.Options;

namespace Estante.Game
{
    public sealed class EpubProcessor
    {
        private readonly EpubReaderOptionsPreset readerOptionsPreset;

        public EpubProcessor(EpubReaderOptionsPreset readerOptionsPreset = EpubReaderOptionsPreset.RELAXED)
        {
            this.readerOptionsPreset = readerOptionsPreset;
        }

        public async Task<EpubBook> ProcessAsync(string filePath)
        {
            string normalizedPath = validateFilePath(filePath);

            try
            {
                return await EpubReader.ReadBookAsync(normalizedPath, readerOptionsPreset).ConfigureAwait(false);
            }
            catch (EpubReaderException exception)
            {
                throw new EpubProcessingException(normalizedPath, exception);
            }
            catch (InvalidDataException exception)
            {
                throw new EpubProcessingException(normalizedPath, exception);
            }
        }

        public async Task<ProcessedEpubBook> ProcessForReadingAsync(string filePath)
        {
            EpubBook book = await ProcessAsync(filePath).ConfigureAwait(false);

            try
            {
                return ProcessedEpubBook.Create(book);
            }
            catch (InvalidDataException exception)
            {
                throw new EpubProcessingException(book.FilePath, exception);
            }
        }

        private static string validateFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("The EPUB file path cannot be empty.", nameof(filePath));

            string normalizedPath = Path.GetFullPath(filePath);

            if (!string.Equals(Path.GetExtension(normalizedPath), ".epub", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("The selected file must have the .epub extension.", nameof(filePath));

            if (!File.Exists(normalizedPath))
                throw new FileNotFoundException("The EPUB file was not found.", normalizedPath);

            return normalizedPath;
        }
    }
}
