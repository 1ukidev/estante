using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;

namespace Estante.App
{
    public partial class SettingsScreen : EstanteSubScreen
    {
        private static readonly Color4 textPrimary = GruvboxColours.Foreground;
        private static readonly Color4 dropdownBackground = new Color4(20, 22, 23, 255);

        private EstanteBackButton backButton;
        private Container settingsPanel;
        private LibreTranslateUrlTextBox urlTextBox;
        private LibreTranslateUrlTextBox apiKeyTextBox;
        private SaveSettingButton saveUrlButton;
        private SaveSettingButton saveApiKeyButton;
        private TargetLanguageDropdown targetLanguageDropdown;
        private ClearHistoryButton clearHistoryButton;
        private BookHistoryStore historyStore;
        private TranslationSettingsStore translationSettings;

        public SettingsScreen()
        {
            InternalChildren = new Drawable[]
            {
                new EstanteBackground(EstanteBackgroundStyle.Reader),
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        backButton = new EstanteBackButton
                        {
                            Name = "Back",
                            Anchor = Anchor.TopLeft,
                            Origin = Anchor.TopLeft,
                            Position = new Vector2(38),
                            Action = this.Exit
                        },
                        settingsPanel = createSettingsPanel(),
                        clearHistoryButton = new ClearHistoryButton
                        {
                            Name = "Clear history",
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Y = 190,
                            Action = clearHistory
                        }
                    }
                }
            };
        }

        private Container createSettingsPanel() =>
            new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Y = -30,
                Width = 570,
                Height = 330,
                Depth = -1,
                Children = new Drawable[]
                {
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Masking = true,
                        CornerRadius = 18,
                        BorderThickness = 1,
                        BorderColour = GruvboxColours.ForegroundMuted.Opacity(0.12f),
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = GruvboxColours.Background
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = 4,
                                Colour = GruvboxColours.Aqua,
                                Alpha = 0.7f
                            }
                        }
                    },
                    new SpriteIcon
                    {
                        X = 28,
                        Y = 28,
                        Size = new Vector2(18),
                        Icon = FontAwesome.Solid.Language,
                        Colour = GruvboxColours.Aqua
                    },
                    new SpriteText
                    {
                        X = 58,
                        Y = 28,
                        Text = "Translation",
                        Font = FontUsage.Default.With(size: 18, weight: "Bold"),
                        Colour = textPrimary
                    },
                    new SpriteText
                    {
                        X = 28,
                        Y = 61,
                        Text = "LibreTranslate URL",
                        Font = FontUsage.Default.With(size: 12, weight: "Bold"),
                        Colour = GruvboxColours.ForegroundMuted
                    },
                    urlTextBox = new LibreTranslateUrlTextBox
                    {
                        Name = "LibreTranslate URL",
                        X = 28,
                        Y = 86,
                        Width = 414,
                        Height = 44,
                        PlaceholderText = TranslationSettingsStore.DEFAULT_LIBRE_TRANSLATE_URL
                    },
                    saveUrlButton = new SaveSettingButton("Save LibreTranslate URL")
                    {
                        X = 452,
                        Y = 86,
                        Action = saveTranslationUrl
                    },
                    new SpriteText
                    {
                        X = 28,
                        Y = 151,
                        Text = "LibreTranslate API key",
                        Font = FontUsage.Default.With(size: 12, weight: "Bold"),
                        Colour = GruvboxColours.ForegroundMuted
                    },
                    apiKeyTextBox = new LibreTranslateUrlTextBox
                    {
                        Name = "LibreTranslate API key",
                        X = 28,
                        Y = 176,
                        Width = 414,
                        Height = 44,
                        PlaceholderText = "Optional",
                        InputProperties = new TextInputProperties(TextInputType.Password)
                    },
                    saveApiKeyButton = new SaveSettingButton("Save LibreTranslate API key")
                    {
                        X = 452,
                        Y = 176,
                        Action = saveApiKey
                    },
                    new SpriteText
                    {
                        X = 28,
                        Y = 241,
                        Text = "Target language",
                        Font = FontUsage.Default.With(size: 12, weight: "Bold"),
                        Colour = GruvboxColours.ForegroundMuted
                    },
                    targetLanguageDropdown = new TargetLanguageDropdown
                    {
                        Name = "Target language",
                        X = 28,
                        Y = 266,
                        Width = 514,
                        Depth = -1,
                        Items = TranslationLanguages.All
                    }
                }
            };

        [BackgroundDependencyLoader]
        private void load(BookHistoryStore historyStore, TranslationSettingsStore translationSettings)
        {
            this.historyStore = historyStore;
            this.translationSettings = translationSettings;
            urlTextBox.Text = translationSettings.LibreTranslateUrl;
            urlTextBox.OnCommit += (_, _) => saveTranslationUrl();
            urlTextBox.Current.BindValueChanged(_ => saveUrlButton.Reset(), false);
            apiKeyTextBox.Text = translationSettings.ApiKey;
            apiKeyTextBox.OnCommit += (_, _) => saveApiKey();
            apiKeyTextBox.Current.BindValueChanged(_ => saveApiKeyButton.Reset(), false);

            TranslationLanguage selectedLanguage = TranslationLanguages.Find(translationSettings.TargetLanguage);
            targetLanguageDropdown.Current.Value = selectedLanguage;
            targetLanguageDropdown.Current.BindValueChanged(change => saveTargetLanguage(change.NewValue), false);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            backButton.Alpha = 0;
            backButton.X = 22;
            backButton.FadeIn(320, Easing.OutQuint);
            backButton.MoveToX(38, 420, Easing.OutQuint);

            settingsPanel.Alpha = 0;
            settingsPanel.Y = -18;
            settingsPanel.Delay(50).FadeIn(380, Easing.OutQuint);
            settingsPanel.Delay(50).MoveToY(-30, 460, Easing.OutQuint);

            clearHistoryButton.Alpha = 0;
            clearHistoryButton.Y = 200;
            clearHistoryButton.Delay(80).FadeIn(380, Easing.OutQuint);
            clearHistoryButton.Delay(80).MoveToY(190, 460, Easing.OutQuint);
        }

        private void saveTranslationUrl()
        {
            if (translationSettings.TrySetLibreTranslateUrl(urlTextBox.Text))
                saveUrlButton.ConfirmSaved();
            else
            {
                saveUrlButton.Reset();
                urlTextBox.FlashError();
            }
        }

        private void saveApiKey()
        {
            translationSettings.SetApiKey(apiKeyTextBox.Text);
            saveApiKeyButton.ConfirmSaved();
        }

        private void saveTargetLanguage(TranslationLanguage language)
        {
            if (language != null)
                translationSettings.TrySetTargetLanguage(language.Code);
        }

        private void clearHistory()
        {
            historyStore.Clear();
            clearHistoryButton.ConfirmCleared();
        }

        private partial class ClearHistoryButton : ClickableContainer
        {
            private readonly Box backgroundBox;
            private readonly SpriteIcon icon;
            private readonly SpriteText label;

            public ClearHistoryButton()
            {
                Width = 188;
                Height = 44;
                Masking = true;
                CornerRadius = 13;
                BorderThickness = 1;
                BorderColour = GruvboxColours.Red.Opacity(0.3f);

                InternalChildren = new Drawable[]
                {
                    backgroundBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = GruvboxColours.Red,
                        Alpha = 0.12f
                    },
                    icon = new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.Centre,
                        X = 27,
                        Size = new Vector2(14),
                        Icon = FontAwesome.Solid.TrashAlt,
                        Colour = GruvboxColours.Red
                    },
                    label = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        X = 10,
                        Text = "Clear history",
                        Font = FontUsage.Default.With(size: 13, weight: "Bold"),
                        Colour = textPrimary
                    }
                };
            }

            public void ConfirmCleared()
            {
                label.Text = "History cleared";
                icon.Icon = FontAwesome.Solid.Check;

                backgroundBox.ClearTransforms();
                backgroundBox.FadeTo(0.24f, 100, Easing.OutQuint)
                             .Then()
                             .FadeTo(0.12f, 300, Easing.OutQuint);
                this.ScaleTo(1.04f, 100, Easing.OutQuint)
                    .Then()
                    .ScaleTo(1, 220, Easing.OutBack);
            }

            protected override bool OnHover(HoverEvent e)
            {
                backgroundBox.FadeTo(0.22f, 140, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                backgroundBox.FadeTo(0.12f, 160, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }

        private partial class LibreTranslateUrlTextBox : BasicTextBox
        {
            protected override Color4 SelectionColour => GruvboxColours.Aqua;

            public LibreTranslateUrlTextBox()
            {
                FontSize = 15;
                Masking = true;
                CornerRadius = 11;
                BorderThickness = 1;
                BorderColour = GruvboxColours.ForegroundMuted.Opacity(0.14f);
                BackgroundFocused = GruvboxColours.Background2;
                BackgroundUnfocused = GruvboxColours.BackgroundHard;
                BackgroundCommit = GruvboxColours.Green;
            }

            public void FlashError() => this.FlashColour(GruvboxColours.Red, 260, Easing.OutQuint);

            protected override Drawable GetDrawableCharacter(char c) =>
                new SpriteText
                {
                    Text = c.ToString(),
                    Font = FontUsage.Default.With(size: 15),
                    Colour = textPrimary
                };

            protected override SpriteText CreatePlaceholder() =>
                new SpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Scale = new Vector2(0.9f),
                    Font = FontUsage.Default,
                    Colour = GruvboxColours.ForegroundMuted
                };
        }

        private partial class TargetLanguageDropdown : BasicDropdown<TranslationLanguage>
        {
            protected override DropdownHeader CreateHeader() => new GruvboxDropdownHeader();

            protected override DropdownMenu CreateMenu() => new GruvboxDropdownMenu();

            private partial class GruvboxDropdownHeader : BasicDropdownHeader
            {
                public GruvboxDropdownHeader()
                {
                    Masking = true;
                    CornerRadius = 11;
                    BorderThickness = 1;
                    BorderColour = GruvboxColours.ForegroundMuted.Opacity(0.14f);
                    BackgroundColour = GruvboxColours.BackgroundHard;
                    BackgroundColourHover = GruvboxColours.Background2;
                    Foreground.Padding = new MarginPadding
                    {
                        Horizontal = 14,
                        Vertical = 10
                    };
                    Foreground.Colour = textPrimary;
                }
            }

            private partial class GruvboxDropdownMenu : BasicDropdownMenu
            {
                public GruvboxDropdownMenu()
                {
                    BackgroundColour = dropdownBackground;
                    MaxHeight = 180;
                    ScrollbarVisible = true;
                    MaskingContainer.CornerRadius = 10;
                }

                protected override ScrollContainer<Drawable> CreateScrollContainer(Direction direction) =>
                    new BasicScrollContainer(direction)
                    {
                        Name = "Target languages",
                        ClampExtension = 70
                    };

                protected override DrawableDropdownMenuItem CreateDrawableDropdownMenuItem(MenuItem item) =>
                    new GruvboxDropdownMenuItem(item);

                private partial class GruvboxDropdownMenuItem : DrawableDropdownMenuItem
                {
                    public GruvboxDropdownMenuItem(MenuItem item)
                        : base(item)
                    {
                        BackgroundColour = dropdownBackground;
                        BackgroundColourHover = GruvboxColours.Background2;
                        BackgroundColourSelected = GruvboxColours.Background1;
                        ForegroundColour = textPrimary;
                        ForegroundColourHover = GruvboxColours.Aqua;
                        ForegroundColourSelected = GruvboxColours.Aqua;
                        Foreground.Padding = new MarginPadding
                        {
                            Horizontal = 12,
                            Vertical = 8
                        };
                    }

                    protected override Drawable CreateContent() =>
                        new SpriteText
                        {
                            Font = FontUsage.Default.With(size: 13),
                            Colour = Color4.White
                        };
                }
            }
        }

        private partial class SaveSettingButton : ClickableContainer
        {
            private readonly Box background;
            private readonly SpriteIcon icon;
            private readonly SpriteText label;

            public SaveSettingButton(string name)
            {
                Name = name;
                Width = 90;
                Height = 44;
                Masking = true;
                CornerRadius = 11;

                InternalChildren = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = GruvboxColours.Aqua,
                        Alpha = 0.18f
                    },
                    icon = new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.Centre,
                        X = 19,
                        Size = new Vector2(13),
                        Icon = FontAwesome.Solid.Check,
                        Colour = GruvboxColours.Aqua,
                        Alpha = 0
                    },
                    label = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "Save",
                        Font = FontUsage.Default.With(size: 12, weight: "Bold"),
                        Colour = textPrimary
                    }
                };
            }

            public void ConfirmSaved()
            {
                label.Text = "Saved";
                label.MoveToX(9, 180, Easing.OutQuint);

                icon.ClearTransforms();
                icon.Alpha = 0;
                icon.Scale = new Vector2(0.55f);
                icon.FadeIn(160, Easing.OutQuint);
                icon.ScaleTo(1, 240, Easing.OutBack);

                background.ClearTransforms();
                background.FadeTo(0.34f, 100, Easing.OutQuint)
                          .Then()
                          .FadeTo(0.18f, 300, Easing.OutQuint);
                this.ScaleTo(1.05f, 100, Easing.OutQuint)
                    .Then()
                    .ScaleTo(1, 220, Easing.OutBack);
            }

            public void Reset()
            {
                label.Text = "Save";
                label.MoveToX(0, 140, Easing.OutQuint);
                icon.ClearTransforms();
                icon.FadeOut(100, Easing.OutQuint);
            }

            protected override bool OnHover(HoverEvent e)
            {
                background.FadeTo(0.3f, 140, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                background.FadeTo(0.18f, 160, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
