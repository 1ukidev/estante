using osu.Framework.iOS;
using Estante.App;

namespace Estante.iOS
{
    /// <inheritdoc />
    public class AppDelegate : GameApplicationDelegate
    {
        /// <inheritdoc />
        protected override osu.Framework.Game CreateGame() => new EstanteApp();
    }
}
