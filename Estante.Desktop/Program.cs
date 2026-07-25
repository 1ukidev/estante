using System;
using Estante.App;
using NativeFileDialogNET;
using osu.Framework;
using osu.Framework.Platform;

namespace Estante.Desktop
{
    public static class Program
    {
        public static void Main()
        {
            using (GameHost host = Host.GetSuitableDesktopHost(@"Estante"))
            using (osu.Framework.Game game = new EstanteApp(onSelected => openBookSelector(host, onSelected)))
                host.Run(game);
        }

        private static void openBookSelector(GameHost host, Action<string> onSelected)
        {
            host.InputThread.Scheduler.Add(() =>
            {
                using var dialog = new NativeFileDialog()
                                   .SelectFile()
                                   .AddFilter("EPUB Books", "*.epub");

                dialog.Open(out string selectedPath, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

                if (!string.IsNullOrWhiteSpace(selectedPath))
                    onSelected(selectedPath);
            });
        }
    }
}
