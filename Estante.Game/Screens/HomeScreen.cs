using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;

namespace Estante.Game
{
    public partial class HomeScreen : Screen
    {
        private static readonly string[] supported_book_extensions = { ".epub" };

        private static readonly Color4 surface = GruvboxColours.Background;
        private static readonly Color4 surfaceHover = GruvboxColours.Background2;
        private static readonly Color4 accent = GruvboxColours.Aqua;
        private static readonly Color4 warmAccent = GruvboxColours.Yellow;
        private static readonly Color4 textPrimary = GruvboxColours.Foreground;
        private static readonly Color4 textSecondary = GruvboxColours.ForegroundMuted;

        private readonly MenuButton[] menuButtons = new MenuButton[3];
        private readonly Action<Action<string>> selectBook;

        private BookHistoryStore historyStore;
        private ISystemFileSelector systemFileSelector;
        private Container brandContent;
        private Container menuContent;
        private FillFlowContainer recentBooksList;

        public HomeScreen(Action<Action<string>> selectBook = null)
        {
            this.selectBook = selectBook;
        }

        [BackgroundDependencyLoader]
        private void load(TextureStore textures, osu.Framework.Game game, GameHost host, BookHistoryStore historyStore)
        {
            this.historyStore = historyStore;

            Action openBookAction = selectBook == null
                ? () => presentSystemFileSelector(host)
                : () => selectBook(openSelectedBook);

            InternalChildren = new Drawable[]
            {
                new EstanteBackground(EstanteBackgroundStyle.Home),
                new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(1140, 590),
                    Children = new Drawable[]
                    {
                        brandContent = createBrandContent(textures),
                        new Box
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            X = 86,
                            Width = 2,
                            Height = 430,
                            Colour = textSecondary,
                            Alpha = 0.055f
                        },
                        menuContent = createMenuContent(game, openBookAction)
                    }
                }
            };

            refreshRecentBooks();
        }

        private Container createBrandContent(TextureStore textures) =>
            new Container
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Width = 625,
                Height = 500,
                Children = new Drawable[]
                {
                    new Container
                    {
                        Size = new Vector2(118),
                        Masking = true,
                        CornerRadius = 28,
                        BorderThickness = 1.5f,
                        BorderColour = accent,
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Shadow,
                            Radius = 28,
                            Colour = accent.Opacity(0.12f)
                        },
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = surface
                            },
                            new Sprite
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(88),
                                Texture = textures.Get("logo"),
                                Colour = textPrimary
                            }
                        }
                    },
                    new SpriteText
                    {
                        Y = 155,
                        Text = "Estante",
                        Font = FontUsage.Default.With(size: 68, weight: "Bold"),
                        Colour = textPrimary
                    },
                    new Box
                    {
                        Y = 246,
                        Width = 62,
                        Height = 5,
                        Colour = accent
                    },
                    new SpriteText
                    {
                        Y = 278,
                        Text = "Your next story starts here.",
                        Font = FontUsage.Default.With(size: 23),
                        Colour = textPrimary
                    },
                    new SpriteText
                    {
                        Y = 321,
                        Text = "A dynamic book reader.",
                        Font = FontUsage.Default.With(size: 17),
                        Colour = textSecondary
                    }
                }
            };

        private Container createMenuContent(osu.Framework.Game game, Action openBookAction)
        {
            menuButtons[0] = new MenuButton(
                "Open a book",
                "Choose an EPUB file",
                FontAwesome.Solid.BookOpen,
                accent)
            {
                Name = "Open a book",
                Action = openBookAction
            };

            menuButtons[1] = new MenuButton(
                "Settings",
                "Customize your reading experience",
                FontAwesome.Solid.Cog,
                warmAccent)
            {
                Name = "Settings",
                Action = openSettings
            };

            menuButtons[2] = new MenuButton(
                "Exit",
                "Close Estante",
                FontAwesome.Solid.SignOutAlt,
                GruvboxColours.Red)
            {
                Name = "Exit",
                Action = game.RequestExit
            };

            return new Container
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                Width = 420,
                Height = 500,
                Children = new Drawable[]
                {
                    new SpriteText
                    {
                        Text = "What shall we read today?",
                        Font = FontUsage.Default.With(size: 27, weight: "Bold"),
                        Colour = textPrimary
                    },
                    new SpriteText
                    {
                        Y = 43,
                        Text = "Choose an option to get started.",
                        Font = FontUsage.Default.With(size: 15),
                        Colour = textSecondary
                    },
                    new FillFlowContainer
                    {
                        Y = 90,
                        Width = 420,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 14),
                        Children = menuButtons
                    },
                    new SpriteText
                    {
                        Y = 354,
                        Text = "RECENT BOOKS",
                        Font = FontUsage.Default.With(size: 10, weight: "Bold"),
                        Colour = textSecondary
                    },
                    recentBooksList = new FillFlowContainer
                    {
                        Y = 378,
                        Width = 420,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 5)
                    }
                }
            };
        }

        private void refreshRecentBooks()
        {
            recentBooksList.Clear();
            IReadOnlyList<BookHistoryEntry> recentBooks = historyStore.GetRecentBooks();

            if (recentBooks.Count == 0)
            {
                recentBooksList.Add(new SpriteText
                {
                    Text = "No recently opened books.",
                    Font = FontUsage.Default.With(size: 12),
                    Colour = textSecondary
                });
                return;
            }

            foreach (BookHistoryEntry entry in recentBooks)
            {
                recentBooksList.Add(new RecentBookButton(entry, () => openSelectedBook(entry.FilePath)));
            }
        }

        private void presentSystemFileSelector(GameHost host)
        {
            systemFileSelector?.Dispose();
            systemFileSelector = host.CreateSystemFileSelector(supported_book_extensions);

            if (systemFileSelector == null)
                return;

            systemFileSelector.Selected += file => openSelectedBook(file.FullName);
            systemFileSelector.Present();
        }

        private void openSelectedBook(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return;

            Schedule(() =>
            {
                if (this.IsCurrentScreen())
                    this.Push(new BookScreen(path));
            });
        }

        private void openSettings()
        {
            if (this.IsCurrentScreen())
                this.Push(new SettingsScreen());
        }

        protected override void Dispose(bool isDisposing)
        {
            systemFileSelector?.Dispose();
            base.Dispose(isDisposing);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            brandContent.Alpha = 0;
            brandContent.X = -28;
            brandContent.FadeIn(650, Easing.OutQuint);
            brandContent.MoveToX(0, 700, Easing.OutQuint);

            menuContent.Alpha = 0;
            menuContent.X = 24;
            menuContent.Delay(120).FadeIn(600, Easing.OutQuint);
            menuContent.Delay(120).MoveToX(0, 650, Easing.OutQuint);

            for (int i = 0; i < menuButtons.Length; i++)
            {
                MenuButton button = menuButtons[i];
                button.Alpha = 0;
                button.X = 20;
                button.Delay(220 + i * 90).FadeIn(420, Easing.OutQuint);
                button.Delay(220 + i * 90).MoveToX(0, 500, Easing.OutQuint);
            }
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);

            refreshRecentBooks();
            recentBooksList.FadeInFromZero(220, Easing.OutQuint);
        }

        private partial class MenuButton : ClickableContainer
        {
            private readonly Color4 buttonAccent;
            private readonly Color4 restingColour;
            private readonly Box backgroundBox;
            private readonly Box accentBar;
            private readonly Box flash;
            private readonly Container iconContainer;
            private readonly SpriteIcon chevron;

            public MenuButton(string title, string description, IconUsage icon, Color4 buttonAccent)
            {
                this.buttonAccent = buttonAccent;
                restingColour = surface;

                Width = 420;
                Height = 72;
                Masking = true;
                CornerRadius = 16;
                BorderThickness = 1;
                BorderColour = textSecondary.Opacity(0.1f);
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Radius = 14,
                    Colour = GruvboxColours.BackgroundHard.Opacity(0.55f),
                    Offset = new Vector2(0, 6)
                };

                InternalChildren = new Drawable[]
                {
                    backgroundBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = restingColour
                    },
                    accentBar = new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 5,
                        Colour = buttonAccent,
                        Alpha = 0.45f
                    },
                    iconContainer = new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.Centre,
                        X = 42,
                        Size = new Vector2(40),
                        Masking = true,
                        CornerRadius = 20,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = buttonAccent,
                                Alpha = 0.13f
                            },
                            new SpriteIcon
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(18),
                                Icon = icon,
                                Colour = buttonAccent
                            }
                        }
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.BottomLeft,
                        Position = new Vector2(78, -2),
                        Text = title,
                        Font = FontUsage.Default.With(size: 17, weight: "Bold"),
                        Colour = textPrimary
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.TopLeft,
                        Position = new Vector2(78, 5),
                        Text = description,
                        Font = FontUsage.Default.With(size: 12),
                        Colour = textSecondary
                    },
                    chevron = new SpriteIcon
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        X = -22,
                        Size = new Vector2(13),
                        Icon = FontAwesome.Solid.ChevronRight,
                        Colour = textSecondary
                    },
                    flash = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = buttonAccent,
                        Alpha = 0
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                backgroundBox.FadeColour(surfaceHover, 180, Easing.OutQuint);
                accentBar.FadeTo(1, 180, Easing.OutQuint);
                iconContainer.ScaleTo(1.1f, 180, Easing.OutQuint);
                chevron.FadeColour(buttonAccent, 180, Easing.OutQuint);
                chevron.MoveToX(-17, 180, Easing.OutQuint);

                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                backgroundBox.FadeColour(restingColour, 220, Easing.OutQuint);
                accentBar.FadeTo(restingColour == surface ? 0.45f : 1, 220, Easing.OutQuint);
                iconContainer.ScaleTo(1, 220, Easing.OutQuint);
                chevron.FadeColour(textSecondary, 220, Easing.OutQuint);
                chevron.MoveToX(-22, 220, Easing.OutQuint);

                base.OnHoverLost(e);
            }

            protected override bool OnClick(ClickEvent e)
            {
                flash.ClearTransforms();
                flash.Alpha = 0.18f;
                flash.FadeOut(320, Easing.OutQuint);
                iconContainer.ScaleTo(0.9f, 70, Easing.OutQuint)
                             .Then()
                             .ScaleTo(1.1f, 160, Easing.OutQuint);

                return base.OnClick(e);
            }
        }

        private partial class RecentBookButton : ClickableContainer
        {
            private readonly Box backgroundBox;
            private readonly SpriteText title;
            private readonly SpriteText metadata;

            public RecentBookButton(BookHistoryEntry entry, Action action)
            {
                Name = $"Recent book: {entry.Title}";
                Action = action;
                Width = 420;
                Height = 34;
                Masking = true;
                CornerRadius = 9;

                InternalChildren = new Drawable[]
                {
                    backgroundBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = surface,
                        Alpha = 0.72f
                    },
                    new SpriteIcon
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.Centre,
                        X = 17,
                        Size = new Vector2(12),
                        Icon = FontAwesome.Solid.Book,
                        Colour = accent
                    },
                    title = new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        X = 36,
                        Text = entry.Title,
                        Font = FontUsage.Default.With(size: 11, weight: "Bold"),
                        Colour = textPrimary,
                        MaxWidth = 228,
                        Truncate = true
                    },
                    metadata = new SpriteText
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        X = -14,
                        Text = $"Chapter {entry.ChapterIndex + 1} · {entry.Author}",
                        Font = FontUsage.Default.With(size: 10),
                        Colour = textSecondary,
                        MaxWidth = 142,
                        Truncate = true
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                backgroundBox.FadeColour(surfaceHover, 140, Easing.OutQuint);
                title.FadeColour(accent, 140, Easing.OutQuint);
                metadata.FadeColour(textPrimary, 140, Easing.OutQuint);
                this.MoveToX(4, 160, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                backgroundBox.FadeColour(surface, 160, Easing.OutQuint);
                title.FadeColour(textPrimary, 160, Easing.OutQuint);
                metadata.FadeColour(textSecondary, 160, Easing.OutQuint);
                this.MoveToX(0, 180, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
