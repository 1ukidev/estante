using System.Linq;
using osu.Framework.Allocation;
using NUnit.Framework;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Testing;

namespace Estante.App.Tests.Visual
{
    [TestFixture]
    public partial class TestSceneEstanteGame : EstanteTestScene
    {
        // Add visual tests to ensure correct behaviour of your game: https://github.com/ppy/osu-framework/wiki/Development-and-Testing
        // You can make changes to classes associated with the tests and they will recompile and update immediately.

        [BackgroundDependencyLoader]
        private void load()
        {
            AddGame(new EstanteApp());
        }

        [Test]
        public void TestCustomCursorIsPresent()
        {
            AddUntilStep("custom cursor is present", () => this.ChildrenOfType<CursorContainer>()
                                                               .Any(cursor => cursor.Name == "Estante cursor"));
        }
    }
}
