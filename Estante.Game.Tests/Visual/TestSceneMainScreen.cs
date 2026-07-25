using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Screens;
using osu.Framework.Testing;

namespace Estante.Game.Tests.Visual
{
    [TestFixture]
    public partial class TestSceneMainScreen : EstanteTestScene
    {
        private readonly MainScreen screen;
        private readonly string epubFilePath;

        // Add visual tests to ensure correct behaviour of your game: https://github.com/ppy/osu-framework/wiki/Development-and-Testing
        // You can make changes to classes associated with the tests and they will recompile and update immediately.

        public TestSceneMainScreen()
        {
            epubFilePath = EpubTestFile.Create();
            Add(new ScreenStack(screen = new MainScreen(onSelected => onSelected(epubFilePath))) { RelativeSizeAxes = Axes.Both });
        }

        [Test]
        public void TestMenuOptions()
        {
            AddUntilStep("home screen is loaded", () => screen.ChildrenOfType<HomeScreen>().SingleOrDefault()?.IsLoaded == true);
            AddAssert("three menu options are present", () => screen.ChildrenOfType<ClickableContainer>().Count() == 3);
            AddAssert("open book option is present", () => screen.ChildrenOfType<ClickableContainer>().Any(button => button.Name == "Open a book"));
            AddAssert("open book option is enabled", () => screen.ChildrenOfType<ClickableContainer>().Single(button => button.Name == "Open a book").Enabled.Value);
            AddAssert("settings option is present", () => screen.ChildrenOfType<ClickableContainer>().Any(button => button.Name == "Settings"));
            AddAssert("exit option is present", () => screen.ChildrenOfType<ClickableContainer>().Any(button => button.Name == "Exit"));
        }

        [Test]
        public void TestOpenSettings()
        {
            AddUntilStep("home screen is current", () => screen.ChildrenOfType<HomeScreen>().SingleOrDefault()?.IsCurrentScreen() == true);
            AddStep("open settings", () => screen.ChildrenOfType<ClickableContainer>().Single(button => button.Name == "Settings").TriggerClick());
            AddUntilStep("settings screen is loaded", () => screen.ChildrenOfType<SettingsScreen>().SingleOrDefault()?.IsLoaded == true);
            AddAssert("construction message is absent", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                                 .ChildrenOfType<SpriteText>()
                                                                 .All(text => text.Text.ToString() != "Settings are under construction."));
            AddAssert("back button is present", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                       .ChildrenOfType<ClickableContainer>()
                                                       .Any(button => button.Name == "Back"));
            AddAssert("back button has no label", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                         .ChildrenOfType<SpriteText>()
                                                         .All(text => text.Text.ToString() != "Back"));
            AddAssert("clear history option is present", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                                  .ChildrenOfType<ClickableContainer>()
                                                                  .Any(button => button.Name == "Clear history"));
            AddAssert("LibreTranslate URL is configurable", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                                      .ChildrenOfType<TextBox>()
                                                                      .Single(textBox => textBox.Name == "LibreTranslate URL")
                                                                      .Text,
                () => Is.EqualTo(TranslationSettingsStore.DEFAULT_LIBRE_TRANSLATE_URL));
            AddStep("save LibreTranslate URL", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                           .ChildrenOfType<ClickableContainer>()
                                                           .Single(button => button.Name == "Save LibreTranslate URL")
                                                           .TriggerClick());
            AddAssert("save is confirmed", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                    .ChildrenOfType<SpriteText>()
                                                    .Any(text => text.Text.ToString() == "Saved"));
            AddAssert("save confirmation icon is shown", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                                 .ChildrenOfType<ClickableContainer>()
                                                                 .Single(button => button.Name == "Save LibreTranslate URL")
                                                                 .ChildrenOfType<SpriteIcon>()
                                                                 .Any(icon => icon.Icon.Equals(FontAwesome.Solid.Check) && icon.Alpha > 0));
            AddAssert("target language is configurable", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                                   .ChildrenOfType<Dropdown<TranslationLanguage>>()
                                                                   .Single(dropdown => dropdown.Name == "Target language")
                                                                   .Current.Value?.Code,
                () => Is.EqualTo(TranslationSettingsStore.DEFAULT_TARGET_LANGUAGE));
            AddAssert("language list overscroll is contained", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                                        .ChildrenOfType<BasicScrollContainer>()
                                                                        .Single(scroll => scroll.Name == "Target languages")
                                                                        .ClampExtension,
                () => Is.EqualTo(70));
            AddStep("clear history", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                  .ChildrenOfType<ClickableContainer>()
                                                  .Single(button => button.Name == "Clear history")
                                                  .TriggerClick());
            AddAssert("clear history is confirmed", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                            .ChildrenOfType<SpriteText>()
                                                            .Any(text => text.Text.ToString() == "History cleared"));
            AddStep("return to home", () => screen.ChildrenOfType<SettingsScreen>().Single()
                                                  .ChildrenOfType<ClickableContainer>()
                                                  .Single(button => button.Name == "Back")
                                                  .TriggerClick());
            AddUntilStep("home screen resumes", () => screen.ChildrenOfType<HomeScreen>().Single().IsCurrentScreen());
            AddAssert("recent history is empty", () => screen.ChildrenOfType<ClickableContainer>().All(button => !button.Name.StartsWith("Recent book:")));
        }

        [Test]
        public void TestOpenSelectedBook()
        {
            AddUntilStep("home screen is loaded", () => screen.ChildrenOfType<HomeScreen>().SingleOrDefault()?.IsLoaded == true);
            AddStep("open selected book", () => screen.ChildrenOfType<ClickableContainer>().Single(button => button.Name == "Open a book").TriggerClick());
            AddUntilStep("book screen is loaded", () => screen.ChildrenOfType<BookScreen>().SingleOrDefault()?.IsLoaded == true);
            AddUntilStep("epub content is processed", () => screen.ChildrenOfType<BookScreen>().Single().ChapterCount == 8);
            AddAssert("reader overscroll is contained", () => getReadingScroll().ClampExtension, () => Is.EqualTo(70));
            AddAssert("epub metadata is displayed", () => screen.ChildrenOfType<BookScreen>().Single().BookTitle == "Test book");
            AddAssert("first chapter is selected", () => screen.ChildrenOfType<BookScreen>().Single().CurrentChapterIndex == 0);
            AddStep("open next chapter", () => screen.ChildrenOfType<ClickableContainer>().Single(button => button.Name == "Next chapter").TriggerClick());
            AddAssert("next chapter is selected", () => screen.ChildrenOfType<BookScreen>().Single().CurrentChapterIndex == 1);
            AddStep("return to previous chapter", () => screen.ChildrenOfType<ClickableContainer>().Single(button => button.Name == "Previous chapter").TriggerClick());
            AddAssert("first chapter is selected again", () => screen.ChildrenOfType<BookScreen>().Single().CurrentChapterIndex == 0);
            AddStep("open last chapter from index", () => screen.ChildrenOfType<ClickableContainer>().Single(button => button.Name == "Chapter 8").TriggerClick());
            AddAssert("last chapter is selected", () => screen.ChildrenOfType<BookScreen>().Single().CurrentChapterIndex == 7);
            AddUntilStep("index follows last chapter", () => getTableOfContents().Current > 0);
            AddAssert("index overscroll is contained", () => getTableOfContents().ClampExtension, () => Is.EqualTo(70));
            AddStep("return to first chapter from index", () => screen.ChildrenOfType<ClickableContainer>().Single(button => button.Name == "Chapter 1").TriggerClick());
            AddAssert("first chapter is selected from index", () => screen.ChildrenOfType<BookScreen>().Single().CurrentChapterIndex == 0);
            AddUntilStep("index follows first chapter", () => getTableOfContents().Current < 1);
            AddStep("stop on second chapter", () => screen.ChildrenOfType<ClickableContainer>().Single(button => button.Name == "Next chapter").TriggerClick());
            AddAssert("second chapter is selected", () => screen.ChildrenOfType<BookScreen>().Single().CurrentChapterIndex == 1);
            AddUntilStep("chapter is scrollable", () => getReadingScroll().ScrollableExtent > 0);
            AddStep("scroll into chapter", () =>
            {
                BasicScrollContainer scroll = getReadingScroll();
                scroll.ScrollTo(scroll.ScrollableExtent * 0.6, false);
            });
            AddAssert("scroll position changed", () => getReadingScroll().Current / getReadingScroll().ScrollableExtent, () => Is.EqualTo(0.6).Within(0.01));
            AddAssert("back button is present", () => screen.ChildrenOfType<ClickableContainer>().Any(button => button.Name == "Back"));
            AddAssert("selection actions are present", () => screen.ChildrenOfType<ClickableContainer>().Count(button => button.Name is "Traduzir" or "Pesquisar") == 2);
            AddAssert("translation result card is present", () => screen.ChildrenOfType<Container>().Any(container => container.Name == "Translation result"));
            AddAssert("translation overscroll is contained", () => screen.ChildrenOfType<BasicScrollContainer>()
                                                                       .Single(scroll => scroll.Name == "Translation text")
                                                                       .ClampExtension,
                () => Is.EqualTo(70));
            AddStep("return from book", () => screen.ChildrenOfType<BookScreen>().Single()
                                                     .ChildrenOfType<ClickableContainer>()
                                                     .Single(button => button.Name == "Back")
                                                     .TriggerClick());
            AddUntilStep("home screen resumes", () => screen.ChildrenOfType<HomeScreen>().Single().IsCurrentScreen());
            AddAssert("book appears in recent history", () => screen.ChildrenOfType<ClickableContainer>().Any(button => button.Name == "Recent book: Test book"));
            AddStep("reopen recent book", () => screen.ChildrenOfType<ClickableContainer>().Single(button => button.Name == "Recent book: Test book").TriggerClick());
            AddUntilStep("recent book content is processed", () => screen.ChildrenOfType<BookScreen>().SingleOrDefault()?.ChapterCount == 8);
            AddAssert("last chapter is restored", () => screen.ChildrenOfType<BookScreen>().Single().CurrentChapterIndex == 1);
            AddUntilStep("last scroll position is restored", () =>
            {
                BasicScrollContainer scroll = getReadingScroll();
                return scroll.ScrollableExtent > 0 && System.Math.Abs(scroll.Current / scroll.ScrollableExtent - 0.6) < 0.01;
            });
            AddStep("return from reopened book", () => screen.ChildrenOfType<BookScreen>().Single()
                                                              .ChildrenOfType<ClickableContainer>()
                                                              .Single(button => button.Name == "Back")
                                                              .TriggerClick());
            AddUntilStep("home screen resumes again", () => screen.ChildrenOfType<HomeScreen>().Single().IsCurrentScreen());
        }

        private BasicScrollContainer getReadingScroll() =>
            screen.ChildrenOfType<BookScreen>().Single()
                  .ChildrenOfType<BasicScrollContainer>()
                  .Single(scroll => scroll.Name == "Reading content");

        private BasicScrollContainer getTableOfContents() =>
            screen.ChildrenOfType<BookScreen>().Single()
                  .ChildrenOfType<BasicScrollContainer>()
                  .Single(scroll => scroll.Name == "Table of contents");

        protected override void Dispose(bool isDisposing)
        {
            if (File.Exists(epubFilePath))
                File.Delete(epubFilePath);

            base.Dispose(isDisposing);
        }
    }
}
