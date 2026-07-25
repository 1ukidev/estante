using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Framework.Testing.Input;
using osuTK.Graphics;
using osuTK.Input;

namespace Estante.Game.Tests.Visual
{
    [TestFixture]
    public partial class TestSceneSelectableTextFlowContainer : ManualInputManagerTestScene
    {
        private SelectableTextFlowContainer selectableText;
        private bool selectionFinished;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create selectable text", () =>
            {
                selectionFinished = false;
                Child = selectableText = new SelectableTextFlowContainer(
                    sprite => sprite.Font = FontUsage.Default.With(size: 24),
                    Color4.Yellow)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Width = 500,
                    Text = "First second third",
                    SelectionFinished = _ => selectionFinished = true
                };
            });
            AddUntilStep("text is laid out", () => selectableText.ChildrenOfType<SpriteText>().Any(text => text.Text.ToString().Contains("second")));
        }

        [Test]
        public void TestDoubleClickSelectsWord()
        {
            AddStep("double click second word", () =>
            {
                SpriteText secondWord = selectableText.ChildrenOfType<SpriteText>()
                                                      .First(text => text.Text.ToString().Contains("second"));

                InputManager.MoveMouseTo(secondWord);
                InputManager.Click(MouseButton.Left);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("second word is selected", () => selectableText.SelectedText, () => Is.EqualTo("second"));
            AddAssert("selection callback is invoked", () => selectionFinished);
            AddAssert("trailing space is excluded from highlight", () =>
            {
                SpriteText secondWord = selectableText.ChildrenOfType<SpriteText>()
                                                      .First(text => text.Text.ToString().Contains("second"));
                Container highlight = selectableText.ChildrenOfType<Container>()
                                                    .Single(container => container.Name == "Selection highlight");

                return highlight.DrawWidth < secondWord.DrawWidth;
            });
        }
    }
}
