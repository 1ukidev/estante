using System;

namespace Estante.Game
{
    public sealed class EpubProcessingException : Exception
    {
        public string FilePath { get; }

        public EpubProcessingException(string filePath, Exception innerException)
            : base($"Could not process the EPUB file \"{filePath}\".", innerException)
        {
            FilePath = filePath;
        }
    }
}
