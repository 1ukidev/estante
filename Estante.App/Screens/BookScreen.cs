using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Input.Events;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace Estante.App
{
    public partial class BookScreen : EstanteSubScreen
    {
        private static readonly Color4 background = GruvboxColours.BackgroundHard;
        private static readonly Color4 surface = GruvboxColours.Background;
        private static readonly Color4 elevatedSurface = GruvboxColours.Background1;
        private static readonly Color4 accent = GruvboxColours.Aqua;
        private static readonly Color4 warmAccent = GruvboxColours.Yellow;
        private static readonly Color4 textPrimary = GruvboxColours.Foreground;
        private static readonly Color4 textSecondary = GruvboxColours.ForegroundMuted;
        private static readonly Color4 paper = GruvboxColours.Background;
        private static readonly Color4 paperText = GruvboxColours.Foreground;
        private static readonly Color4 paperMuted = GruvboxColours.ForegroundMuted;

        private readonly string filePath;
        private readonly EpubProcessor epubProcessor;
        private readonly List<ChapterButton> chapterButtons = new List<ChapterButton>();

        public string BookTitle { get; private set; }
        public int CurrentChapterIndex { get; private set; }
        public int ChapterCount => book?.Chapters.Count ?? 0;

        private IRenderer renderer;
        private GameHost host;
        private BookHistoryStore historyStore;
        private TranslationSettingsStore translationSettings;
        private LibreTranslateClient libreTranslateClient;
        private ProcessedEpubBook book;
        private Texture coverTexture;

        private Container interfaceContent;
        private Container readingPage;
        private Container sidebar;
        private Container readerContent;
        private Container loadingOverlay;
        private Container errorOverlay;
        private Container coverPlaceholder;
        private Box readingProgress;
        private Sprite coverSprite;
        private SpriteIcon loadingIcon;
        private SpriteText topTitle;
        private SpriteText topSubtitle;
        private SpriteText sidebarTitle;
        private SpriteText sidebarAuthor;
        private SpriteText chapterPosition;
        private SpriteText chapterTitle;
        private SpriteText pageIndicator;
        private SpriteText errorMessage;
        private SelectableTextFlowContainer chapterText;
        private FillFlowContainer chapterList;
        private BasicScrollContainer chapterListScroll;
        private BasicScrollContainer readingScroll;
        private ReaderNavigationButton previousButton;
        private ReaderNavigationButton nextButton;
        private SelectionToolbar selectionToolbar;
        private TranslationPopup translationPopup;

        private int loadGeneration;
        private int translationGeneration;
        private CancellationTokenSource translationCancellation;
        private bool isDisposed;
        private bool hasDisplayedChapter;
        private double lastProgressSaveTime;
        private int lastSavedChapterIndex = -1;
        private double lastSavedScrollProgress = -1;
        private double? pendingScrollRestore;
        private int pendingScrollRestoreChapterIndex = -1;
        private int pendingScrollRestoreFrames;

        public BookScreen(string filePath, EpubProcessor epubProcessor = null)
        {
            this.filePath = filePath;
            this.epubProcessor = epubProcessor ?? new EpubProcessor();
            BookTitle = getFallbackTitle(filePath);
        }

        [BackgroundDependencyLoader]
        private void load(
            IRenderer renderer,
            GameHost host,
            BookHistoryStore historyStore,
            TranslationSettingsStore translationSettings,
            LibreTranslateClient libreTranslateClient)
        {
            this.renderer = renderer;
            this.host = host;
            this.historyStore = historyStore;
            this.translationSettings = translationSettings;
            this.libreTranslateClient = libreTranslateClient;

            InternalChildren = new Drawable[]
            {
                new EstanteBackground(EstanteBackgroundStyle.Reader),
                interfaceContent = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(1180, 680),
                    Children = new Drawable[]
                    {
                        createTopBar(),
                        sidebar = createSidebar(),
                        createReadingArea()
                    }
                }
            };
        }

        private Container createTopBar() =>
            new Container
            {
                Width = 1180,
                Height = 62,
                Children = new Drawable[]
                {
                    new EstanteBackButton
                    {
                        Name = "Back",
                        Action = this.Exit
                    },
                    topTitle = new SpriteText
                    {
                        X = 62,
                        Y = 7,
                        Text = BookTitle,
                        Font = FontUsage.Default.With(size: 20, weight: "Bold"),
                        Colour = textPrimary,
                        MaxWidth = 570,
                        Truncate = true
                    },
                    topSubtitle = new SpriteText
                    {
                        X = 62,
                        Y = 36,
                        Text = "Loading book...",
                        Font = FontUsage.Default.With(size: 12),
                        Colour = textSecondary,
                        MaxWidth = 570,
                        Truncate = true
                    },
                    new Box
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Width = 1180,
                        Height = 1,
                        Colour = textSecondary,
                        Alpha = 0.055f
                    }
                }
            };

        private Container createSidebar() =>
            new Container
            {
                Y = 80,
                Width = 242,
                Height = 600,
                Masking = true,
                CornerRadius = 20,
                BorderThickness = 1,
                BorderColour = textSecondary.Opacity(0.1f),
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Radius = 18,
                    Colour = background.Opacity(0.65f),
                    Offset = new Vector2(0, 8)
                },
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = surface
                    },
                    new Container
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Y = 24,
                        Width = 92,
                        Height = 124,
                        Masking = true,
                        CornerRadius = 9,
                        BorderThickness = 1,
                        BorderColour = warmAccent.Opacity(0.35f),
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Shadow,
                            Radius = 16,
                            Colour = background.Opacity(0.75f),
                            Offset = new Vector2(0, 7)
                        },
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = elevatedSurface
                            },
                            coverPlaceholder = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box
                                    {
                                        RelativeSizeAxes = Axes.Y,
                                        Width = 7,
                                        Colour = warmAccent,
                                        Alpha = 0.8f
                                    },
                                    new SpriteIcon
                                    {
                                        Anchor = Anchor.Centre,
                                        Origin = Anchor.Centre,
                                        Size = new Vector2(31),
                                        Icon = FontAwesome.Solid.BookOpen,
                                        Colour = warmAccent
                                    }
                                }
                            },
                            coverSprite = new Sprite
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.Both,
                                FillMode = FillMode.Fill,
                                Alpha = 0
                            }
                        }
                    },
                    sidebarTitle = new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Y = 170,
                        Text = BookTitle,
                        Font = FontUsage.Default.With(size: 15, weight: "Bold"),
                        Colour = textPrimary,
                        MaxWidth = 202,
                        Truncate = true
                    },
                    sidebarAuthor = new SpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Y = 196,
                        Text = "Loading...",
                        Font = FontUsage.Default.With(size: 11),
                        Colour = textSecondary,
                        MaxWidth = 202,
                        Truncate = true
                    },
                    new Box
                    {
                        Y = 230,
                        X = 22,
                        Width = 198,
                        Height = 1,
                        Colour = textSecondary,
                        Alpha = 0.055f
                    },
                    new SpriteText
                    {
                        X = 22,
                        Y = 250,
                        Text = "TABLE OF CONTENTS",
                        Font = FontUsage.Default.With(size: 10, weight: "Bold"),
                        Colour = textSecondary
                    },
                    chapterListScroll = new BasicScrollContainer
                    {
                        Name = "Table of contents",
                        X = 12,
                        Y = 278,
                        Width = 218,
                        Height = 240,
                        ScrollbarVisible = false,
                        ClampExtension = 70,
                        Child = chapterList = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 6)
                        }
                    },
                    new Container
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        X = 22,
                        Y = -24,
                        Width = 198,
                        Height = 42,
                        Children = new Drawable[]
                        {
                            new SpriteText
                            {
                                Text = "PROGRESS",
                                Font = FontUsage.Default.With(size: 10, weight: "Bold"),
                                Colour = textSecondary
                            },
                            new Box
                            {
                                Y = 25,
                                Width = 198,
                                Height = 4,
                                Colour = elevatedSurface
                            },
                            readingProgress = new Box
                            {
                                Y = 25,
                                Width = 0,
                                Height = 4,
                                Colour = accent
                            }
                        }
                    }
                }
            };

        private Container createReadingArea() =>
            new Container
            {
                X = 262,
                Y = 80,
                Width = 918,
                Height = 600,
                Masking = true,
                CornerRadius = 20,
                BorderThickness = 1,
                BorderColour = textSecondary.Opacity(0.1f),
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = background
                    },
                    readingPage = createReadingPage(),
                    createPageControls()
                }
            };

        private Container createReadingPage() =>
            new Container
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Y = 18,
                Width = 748,
                Height = 520,
                Masking = true,
                CornerRadius = 6,
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Radius = 28,
                    Colour = background.Opacity(0.8f),
                    Offset = new Vector2(0, 12)
                },
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = paper
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 5,
                        Colour = warmAccent,
                        Alpha = 0.45f
                    },
                    readerContent = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        Child = readingScroll = new BasicScrollContainer
                        {
                            Name = "Reading content",
                            RelativeSizeAxes = Axes.Both,
                            ScrollbarVisible = true,
                            ClampExtension = 70,
                            Padding = new MarginPadding
                            {
                                Top = 42,
                                Bottom = 38,
                                Left = 62,
                                Right = 64
                            },
                            Child = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 18),
                                Children = new Drawable[]
                                {
                                    chapterPosition = new SpriteText
                                    {
                                        Text = "CHAPTER",
                                        Font = FontUsage.Default.With(size: 10, weight: "Bold"),
                                        Colour = GruvboxColours.Yellow
                                    },
                                    chapterTitle = new SpriteText
                                    {
                                        Text = "Loading...",
                                        Font = FontUsage.Default.With(size: 30, weight: "Bold"),
                                        Colour = paperText,
                                        MaxWidth = 620
                                    },
                                    new Box
                                    {
                                        Width = 54,
                                        Height = 3,
                                        Colour = warmAccent
                                    },
                                    chapterText = new SelectableTextFlowContainer(sprite =>
                                    {
                                        sprite.Font = FontUsage.Default.With(size: 17);
                                        sprite.Colour = paperText;
                                    }, accent.Opacity(0.28f))
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        ParagraphSpacing = 0.75f,
                                        LineSpacing = 0.35f,
                                        TextAnchor = Anchor.TopLeft,
                                        SelectionFinished = showSelectionToolbar,
                                        SelectionCleared = hideSelectionToolbar
                                    }
                                }
                            }
                        }
                    },
                    loadingOverlay = createLoadingOverlay(),
                    errorOverlay = createErrorOverlay(),
                    selectionToolbar = new SelectionToolbar(translateSelection, searchSelection)
                    {
                        Alpha = 0
                    },
                    translationPopup = new TranslationPopup
                    {
                        Alpha = 0
                    }
                }
            };

        private Container createLoadingOverlay() =>
            new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    loadingIcon = new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = -24,
                        Size = new Vector2(40),
                        Icon = FontAwesome.Solid.BookOpen,
                        Colour = GruvboxColours.Yellow
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = 28,
                        Text = "Preparing your book...",
                        Font = FontUsage.Default.With(size: 16, weight: "Bold"),
                        Colour = paperText
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = 54,
                        Text = "Organizing chapters and content",
                        Font = FontUsage.Default.With(size: 12),
                        Colour = paperMuted
                    }
                }
            };

        private Container createErrorOverlay() =>
            new Container
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0,
                Children = new Drawable[]
                {
                    new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = -54,
                        Size = new Vector2(36),
                        Icon = FontAwesome.Solid.ExclamationTriangle,
                        Colour = GruvboxColours.Red
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = -8,
                        Text = "Could not open this book",
                        Font = FontUsage.Default.With(size: 17, weight: "Bold"),
                        Colour = paperText
                    },
                    errorMessage = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = 22,
                        Text = "Make sure the file is a valid EPUB.",
                        Font = FontUsage.Default.With(size: 12),
                        Colour = paperMuted,
                        MaxWidth = 560,
                        Truncate = true
                    },
                    new ReaderTextButton("Try again", retryLoading)
                    {
                        Name = "Try again",
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Y = 72
                    }
                }
            };

        private Container createPageControls() =>
            new Container
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                Y = -9,
                Width = 430,
                Height = 44,
                Children = new Drawable[]
                {
                    previousButton = new ReaderNavigationButton(FontAwesome.Solid.ChevronLeft, showPreviousChapter)
                    {
                        Name = "Previous chapter",
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft
                    },
                    pageIndicator = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = "— / —",
                        Font = FontUsage.Default.With(size: 12, weight: "Bold"),
                        Colour = textSecondary
                    },
                    nextButton = new ReaderNavigationButton(FontAwesome.Solid.ChevronRight, showNextChapter)
                    {
                        Name = "Next chapter",
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight
                    }
                }
            };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            interfaceContent.Alpha = 0;
            interfaceContent.Y = 24;
            interfaceContent.FadeIn(500, Easing.OutQuint);
            interfaceContent.MoveToY(0, 650, Easing.OutQuint);

            sidebar.X = -18;
            sidebar.FadeInFromZero(480, Easing.OutQuint);
            sidebar.MoveToX(0, 600, Easing.OutQuint);

            readingPage.Alpha = 0;
            readingPage.Scale = new Vector2(0.975f);
            readingPage.Delay(100).FadeIn(520, Easing.OutQuint);
            readingPage.Delay(100).ScaleTo(1, 620, Easing.OutQuint);

            loadingIcon
                .ScaleTo(1.08f, 850, Easing.InOutSine)
                .Then()
                .ScaleTo(1, 850, Easing.InOutSine)
                .Loop();

            setNavigationEnabled(false, false);
            beginLoading();
        }

        private void beginLoading()
        {
            int generation = ++loadGeneration;

            loadingOverlay.Show();
            loadingOverlay.FadeIn(180);
            errorOverlay.Hide();
            readerContent.Hide();
            topSubtitle.Text = "Loading book...";
            sidebarAuthor.Text = "Loading...";
            setNavigationEnabled(false, false);

            _ = loadBookAsync(generation);
        }

        private async Task loadBookAsync(int generation)
        {
            try
            {
                ProcessedEpubBook processedBook = await Task.Run(() => epubProcessor.ProcessForReadingAsync(filePath)).ConfigureAwait(false);

                if (isDisposed)
                    return;

                Schedule(() =>
                {
                    if (generation == loadGeneration)
                        displayBook(processedBook);
                });
            }
            catch (Exception exception)
            {
                if (isDisposed)
                    return;

                Schedule(() =>
                {
                    if (generation == loadGeneration)
                        displayError(exception);
                });
            }
        }

        private void displayBook(ProcessedEpubBook processedBook)
        {
            book = processedBook;
            BookTitle = processedBook.Title;
            CurrentChapterIndex = 0;

            topTitle.Text = processedBook.Title;
            topSubtitle.Text = processedBook.Author;
            sidebarTitle.Text = processedBook.Title;
            sidebarAuthor.Text = processedBook.Author;

            BookHistoryEntry historyEntry = historyStore.Get(filePath);
            historyStore.RecordOpened(filePath, processedBook.Title, processedBook.Author);

            loadCover(processedBook.CoverImage);
            populateChapterList();

            loadingOverlay.FadeOut(220);
            errorOverlay.Hide();
            readerContent.FadeInFromZero(350, Easing.OutQuint);

            int initialChapterIndex = Math.Clamp(historyEntry?.ChapterIndex ?? 0, 0, processedBook.Chapters.Count - 1);
            showChapter(initialChapterIndex, false, historyEntry?.ScrollProgress);
        }

        private void loadCover(byte[] coverImage)
        {
            if (coverImage == null || coverImage.Length == 0)
                return;

            try
            {
                coverTexture?.Dispose();

                using var stream = new MemoryStream(coverImage);
                var upload = new TextureUpload(stream);
                coverTexture = renderer.CreateTexture(upload.Width, upload.Height);
                coverTexture.SetData(upload);
                coverSprite.Texture = coverTexture;
                coverSprite.FadeIn(280, Easing.OutQuint);
                coverPlaceholder.FadeOut(200);
            }
            catch
            {
                coverSprite.Hide();
                coverPlaceholder.Show();
            }
        }

        private void populateChapterList()
        {
            chapterList.Clear();
            chapterButtons.Clear();

            for (int i = 0; i < book.Chapters.Count; i++)
            {
                int chapterIndex = i;
                var button = new ChapterButton(i + 1, book.Chapters[i].Title, () => showChapter(chapterIndex));
                chapterButtons.Add(button);
                chapterList.Add(button);
            }
        }

        private void showChapter(int index, bool animate = true, double? restoredScrollProgress = null)
        {
            if (book == null || index < 0 || index >= book.Chapters.Count)
                return;

            if (hasDisplayedChapter && index != CurrentChapterIndex && !pendingScrollRestore.HasValue)
                saveReadingProgress();

            cancelPendingScrollRestore();
            CurrentChapterIndex = index;
            ProcessedEpubChapter chapter = book.Chapters[index];

            chapterPosition.Text = $"CHAPTER {index + 1} OF {book.Chapters.Count}";
            chapterTitle.Text = chapter.Title;
            chapterText.Text = chapter.Text;
            pageIndicator.Text = $"{index + 1} / {book.Chapters.Count}";
            readingScroll.ScrollToStart(false);

            if (restoredScrollProgress.HasValue)
            {
                pendingScrollRestore = Math.Clamp(restoredScrollProgress.Value, 0, 1);
                pendingScrollRestoreChapterIndex = index;
            }

            float progressWidth = 198f * (index + 1) / book.Chapters.Count;
            readingProgress.ResizeWidthTo(progressWidth, animate ? 350 : 0, Easing.OutQuint);

            for (int i = 0; i < chapterButtons.Count; i++)
                chapterButtons[i].SetSelected(i == index);

            ChapterButton selectedChapterButton = chapterButtons[index];
            ScheduleAfterChildren(() =>
            {
                if (CurrentChapterIndex == index)
                    chapterListScroll.ScrollIntoView(selectedChapterButton, animate);
            });

            setNavigationEnabled(index > 0, index < book.Chapters.Count - 1);
            hasDisplayedChapter = true;

            if (animate)
            {
                readerContent.ClearTransforms();
                readerContent.Alpha = 0;
                readerContent.X = 12;
                readerContent.FadeIn(240, Easing.OutQuint);
                readerContent.MoveToX(0, 300, Easing.OutQuint);
            }
        }

        private void showPreviousChapter() => showChapter(CurrentChapterIndex - 1);

        private void showNextChapter() => showChapter(CurrentChapterIndex + 1);

        private void showSelectionToolbar(RectangleF selectionBounds)
        {
            cancelTranslation();
            translationPopup.Dismiss();

            Vector2 position = readingPage.ToLocalSpace(new Vector2(selectionBounds.Centre.X, selectionBounds.Top));
            float toolbarX = Math.Clamp(position.X, selectionToolbar.Width / 2 + 12, readingPage.DrawWidth - selectionToolbar.Width / 2 - 12);
            float toolbarY = Math.Clamp(position.Y - 10, selectionToolbar.Height + 8, readingPage.DrawHeight - 12);

            selectionToolbar.ClearTransforms();
            selectionToolbar.Position = new Vector2(toolbarX, toolbarY + 7);
            selectionToolbar.Scale = new Vector2(0.94f);
            selectionToolbar.Alpha = 0;
            selectionToolbar.FadeIn(150, Easing.OutQuint);
            selectionToolbar.MoveToY(toolbarY, 190, Easing.OutQuint);
            selectionToolbar.ScaleTo(1, 190, Easing.OutBack);
        }

        private void hideSelectionToolbar()
        {
            cancelTranslation();

            selectionToolbar.ClearTransforms();
            selectionToolbar.FadeOut(110, Easing.OutQuint);
            selectionToolbar.MoveToY(selectionToolbar.Y - 4, 140, Easing.OutQuint);
            selectionToolbar.ScaleTo(0.96f, 140, Easing.OutQuint);
            translationPopup.Dismiss();
        }

        private void translateSelection()
        {
            string selectedText = chapterText.SelectedText;
            RectangleF? selectionBounds = chapterText.SelectionScreenBounds;

            if (string.IsNullOrWhiteSpace(selectedText) || !selectionBounds.HasValue)
                return;

            cancelTranslation();
            int generation = ++translationGeneration;
            translationCancellation = new CancellationTokenSource();

            Vector2 position = readingPage.ToLocalSpace(new Vector2(selectionBounds.Value.Centre.X, selectionBounds.Value.Top));
            float popupX = Math.Clamp(position.X, translationPopup.Width / 2 + 12, readingPage.DrawWidth - translationPopup.Width / 2 - 12);
            float popupY = Math.Clamp(position.Y - 10, translationPopup.Height + 8, readingPage.DrawHeight - 12);

            selectionToolbar.ClearTransforms();
            selectionToolbar.FadeOut(100, Easing.OutQuint);
            selectionToolbar.ScaleTo(0.96f, 130, Easing.OutQuint);

            translationPopup.Position = new Vector2(popupX, popupY);
            translationPopup.ShowLoading();

            _ = performTranslation(selectedText, generation, translationCancellation.Token);
        }

        private async Task performTranslation(string selectedText, int generation, CancellationToken cancellationToken)
        {
            try
            {
                string translatedText = await libreTranslateClient.TranslateAsync(
                    translationSettings.LibreTranslateUrl,
                    selectedText,
                    translationSettings.TargetLanguage,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                if (isDisposed || cancellationToken.IsCancellationRequested)
                    return;

                Schedule(() =>
                {
                    if (generation == translationGeneration && chapterText.HasSelection)
                        translationPopup.ShowResult(translatedText);
                });
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                if (isDisposed)
                    return;

                Schedule(() =>
                {
                    if (generation == translationGeneration && chapterText.HasSelection)
                        translationPopup.ShowError("Translation timed out.");
                });
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception exception) when (exception is HttpRequestException or TranslationException)
            {
                if (isDisposed || cancellationToken.IsCancellationRequested)
                    return;

                string message = exception is HttpRequestException
                    ? "Could not reach LibreTranslate."
                    : exception.Message;

                Schedule(() =>
                {
                    if (generation == translationGeneration && chapterText.HasSelection)
                        translationPopup.ShowError(message);
                });
            }
        }

        private void cancelTranslation()
        {
            translationGeneration++;
            translationCancellation?.Cancel();
            translationCancellation?.Dispose();
            translationCancellation = null;
        }

        private void searchSelection()
        {
            string selectedText = chapterText.SelectedText;

            if (!string.IsNullOrWhiteSpace(selectedText))
                host.OpenUrlExternally(GoogleSearch.CreateUrl(selectedText));
        }

        private void setNavigationEnabled(bool previous, bool next)
        {
            previousButton.SetEnabled(previous);
            nextButton.SetEnabled(next);
        }

        protected override void Update()
        {
            base.Update();

            if (!hasDisplayedChapter || pendingScrollRestore.HasValue || Time.Current - lastProgressSaveTime < 1000)
                return;

            double scrollProgress = getScrollProgress();

            if (CurrentChapterIndex != lastSavedChapterIndex || Math.Abs(scrollProgress - lastSavedScrollProgress) >= 0.005)
                saveReadingProgress();
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!pendingScrollRestore.HasValue)
                return;

            if (CurrentChapterIndex != pendingScrollRestoreChapterIndex)
            {
                cancelPendingScrollRestore();
                return;
            }

            pendingScrollRestoreFrames++;

            if (readingScroll.ScrollableExtent <= 0 && pendingScrollRestore.Value > 0 && pendingScrollRestoreFrames < 30)
                return;

            double progress = pendingScrollRestore.Value;
            readingScroll.ScrollTo(readingScroll.ScrollableExtent * progress, false);
            lastSavedChapterIndex = CurrentChapterIndex;
            lastSavedScrollProgress = progress;
            lastProgressSaveTime = Time.Current;
            cancelPendingScrollRestore();
        }

        private double getScrollProgress() =>
            readingScroll.ScrollableExtent <= 0
                ? 0
                : Math.Clamp(readingScroll.Target / readingScroll.ScrollableExtent, 0, 1);

        private void saveReadingProgress()
        {
            if (!hasDisplayedChapter || historyStore == null || pendingScrollRestore.HasValue)
                return;

            double scrollProgress = getScrollProgress();
            historyStore.UpdateProgress(filePath, CurrentChapterIndex, scrollProgress);
            lastSavedChapterIndex = CurrentChapterIndex;
            lastSavedScrollProgress = scrollProgress;
            lastProgressSaveTime = Time.Current;
        }

        private void cancelPendingScrollRestore()
        {
            pendingScrollRestore = null;
            pendingScrollRestoreChapterIndex = -1;
            pendingScrollRestoreFrames = 0;
        }

        private void displayError(Exception exception)
        {
            loadingOverlay.FadeOut(180);
            readerContent.Hide();
            errorMessage.Text = getFriendlyErrorMessage(exception);
            errorOverlay.FadeInFromZero(260, Easing.OutQuint);
            topSubtitle.Text = "Failed to load EPUB";
            sidebarAuthor.Text = "File unavailable";
            setNavigationEnabled(false, false);
        }

        private void retryLoading()
        {
            errorOverlay.FadeOut(120);
            beginLoading();
        }

        private static string getFriendlyErrorMessage(Exception exception)
        {
            if (exception is FileNotFoundException)
                return "The selected file is no longer available.";

            if (exception is UnauthorizedAccessException)
                return "Estante does not have permission to access this file.";

            if (exception is EpubProcessingException)
                return "The file is corrupted or contains no readable chapters.";

            return "An unexpected error occurred while processing the file.";
        }

        private static string getFallbackTitle(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Untitled book";

            string title = Path.GetFileNameWithoutExtension(path);
            return string.IsNullOrWhiteSpace(title) ? "Untitled book" : title;
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Left)
            {
                showPreviousChapter();
                return true;
            }

            if (e.Key == Key.Right)
            {
                showNextChapter();
                return true;
            }

            return base.OnKeyDown(e);
        }

        protected override void PrepareExitTransition()
        {
            saveReadingProgress();
            interfaceContent.MoveToY(10, 260, Easing.OutQuint);
        }

        protected override void Dispose(bool isDisposing)
        {
            if (isDisposing && !isDisposed)
                saveReadingProgress();

            isDisposed = true;
            loadGeneration++;
            cancelTranslation();
            coverTexture?.Dispose();
            base.Dispose(isDisposing);
        }

        private partial class TranslationPopup : Container
        {
            private readonly SpriteIcon stateIcon;
            private readonly SpriteText title;
            private readonly TextFlowContainer body;

            public TranslationPopup()
            {
                Name = "Translation result";
                Width = 430;
                Height = 140;
                Origin = Anchor.BottomCentre;

                InternalChildren = new Drawable[]
                {
                    new SpriteIcon
                    {
                        Anchor = Anchor.BottomCentre,
                        Origin = Anchor.BottomCentre,
                        Y = 3,
                        Size = new Vector2(19),
                        Icon = FontAwesome.Solid.CaretDown,
                        Colour = elevatedSurface
                    },
                    new Container
                    {
                        Width = 430,
                        Height = 132,
                        Masking = true,
                        CornerRadius = 14,
                        BorderThickness = 1,
                        BorderColour = textSecondary.Opacity(0.18f),
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Shadow,
                            Radius = 16,
                            Colour = background.Opacity(0.75f),
                            Offset = new Vector2(0, 7)
                        },
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = elevatedSurface
                            },
                            new Box
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = 4,
                                Colour = accent,
                                Alpha = 0.75f
                            },
                            stateIcon = new SpriteIcon
                            {
                                X = 18,
                                Y = 17,
                                Size = new Vector2(15),
                                Icon = FontAwesome.Solid.Language,
                                Colour = accent
                            },
                            title = new SpriteText
                            {
                                X = 43,
                                Y = 19,
                                Text = "TRANSLATION",
                                Font = FontUsage.Default.With(size: 11, weight: "Bold"),
                                Colour = accent
                            },
                            new BasicScrollContainer
                            {
                                Name = "Translation text",
                                X = 18,
                                Y = 45,
                                Width = 394,
                                Height = 70,
                                ScrollbarVisible = false,
                                ClampExtension = 70,
                                Child = body = new TextFlowContainer(sprite =>
                                {
                                    sprite.Font = FontUsage.Default.With(size: 14);
                                    sprite.Colour = textPrimary;
                                })
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    LineSpacing = 0.2f
                                }
                            }
                        }
                    }
                };
            }

            public void ShowLoading()
            {
                stateIcon.ClearTransforms();
                stateIcon.Icon = FontAwesome.Solid.Language;
                stateIcon.Colour = warmAccent;
                title.Text = "TRANSLATING";
                title.Colour = warmAccent;
                body.Text = "Contacting LibreTranslate...";

                ClearTransforms();
                Alpha = 0;
                Scale = new Vector2(0.96f);
                Y += 7;
                this.FadeIn(180, Easing.OutQuint);
                this.MoveToY(Y - 7, 230, Easing.OutQuint);
                this.ScaleTo(1, 230, Easing.OutBack);

                stateIcon.ScaleTo(0.82f)
                         .Then()
                         .ScaleTo(1.12f, 520, Easing.InOutSine)
                         .Then()
                         .ScaleTo(0.82f, 520, Easing.InOutSine)
                         .Loop();
            }

            public void ShowResult(string translatedText)
            {
                stateIcon.ClearTransforms();
                stateIcon.Scale = Vector2.One;
                stateIcon.Icon = FontAwesome.Solid.Check;
                stateIcon.Colour = accent;
                title.Text = "TRANSLATION";
                title.Colour = accent;

                body.ClearTransforms();
                body.Text = translatedText;
                body.Alpha = 0;
                body.Y = 5;
                body.FadeIn(220, Easing.OutQuint);
                body.MoveToY(0, 280, Easing.OutQuint);

                this.ScaleTo(1.015f, 100, Easing.OutQuint)
                    .Then()
                    .ScaleTo(1, 220, Easing.OutBack);
            }

            public void ShowError(string message)
            {
                stateIcon.ClearTransforms();
                stateIcon.Scale = Vector2.One;
                stateIcon.Icon = FontAwesome.Solid.ExclamationCircle;
                stateIcon.Colour = GruvboxColours.Red;
                title.Text = "TRANSLATION FAILED";
                title.Colour = GruvboxColours.Red;

                body.ClearTransforms();
                body.Text = message;
                body.Alpha = 0;
                body.FadeIn(220, Easing.OutQuint);
            }

            public void Dismiss()
            {
                stateIcon.ClearTransforms();
                ClearTransforms();
                this.FadeOut(120, Easing.OutQuint);
                this.MoveToY(Y - 4, 150, Easing.OutQuint);
                this.ScaleTo(0.97f, 150, Easing.OutQuint);
            }
        }

        private partial class SelectionToolbar : Container
        {
            public SelectionToolbar(Action translateAction, Action searchAction)
            {
                Name = "Text selection actions";
                Width = 184;
                Height = 50;
                Origin = Anchor.BottomCentre;

                InternalChildren = new Drawable[]
                {
                    new SpriteIcon
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Y = 34,
                        Size = new Vector2(17),
                        Icon = FontAwesome.Solid.CaretDown,
                        Colour = elevatedSurface
                    },
                    new Container
                    {
                        Width = 184,
                        Height = 42,
                        Masking = true,
                        CornerRadius = 12,
                        BorderThickness = 1,
                        BorderColour = textSecondary.Opacity(0.16f),
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Shadow,
                            Radius = 14,
                            Colour = background.Opacity(0.72f),
                            Offset = new Vector2(0, 6)
                        },
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = elevatedSurface
                            },
                            new SelectionActionButton("Translate")
                            {
                                X = 4,
                                Y = 4,
                                Action = translateAction
                            },
                            new Box
                            {
                                X = 91,
                                Y = 9,
                                Width = 1,
                                Height = 24,
                                Colour = textSecondary,
                                Alpha = 0.12f
                            },
                            new SelectionActionButton("Search")
                            {
                                X = 94,
                                Y = 4,
                                Action = searchAction
                            }
                        }
                    }
                };
            }
        }

        private partial class SelectionActionButton : ClickableContainer
        {
            private readonly Box backgroundBox;
            private readonly SpriteText label;

            public SelectionActionButton(string text)
            {
                Name = text;
                Action = () => { };
                Width = 86;
                Height = 34;
                Masking = true;
                CornerRadius = 9;

                InternalChildren = new Drawable[]
                {
                    backgroundBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = accent,
                        Alpha = 0
                    },
                    label = new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = text,
                        Font = FontUsage.Default.With(size: 11, weight: "Bold"),
                        Colour = textPrimary
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                backgroundBox.FadeTo(0.16f, 130, Easing.OutQuint);
                label.FadeColour(accent, 130, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                backgroundBox.FadeOut(150, Easing.OutQuint);
                label.FadeColour(textPrimary, 150, Easing.OutQuint);
                base.OnHoverLost(e);
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                this.ScaleTo(0.96f, 70, Easing.OutQuint);
                return base.OnMouseDown(e);
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                this.ScaleTo(1, 130, Easing.OutBack);
                base.OnMouseUp(e);
            }
        }

        private partial class ReaderNavigationButton : ClickableContainer
        {
            private readonly Box backgroundBox;
            private readonly SpriteIcon icon;

            public ReaderNavigationButton(IconUsage iconUsage, Action action)
            {
                Action = action;
                Size = new Vector2(42);
                Masking = true;
                CornerRadius = 21;

                InternalChildren = new Drawable[]
                {
                    backgroundBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = elevatedSurface
                    },
                    icon = new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(13),
                        Icon = iconUsage,
                        Colour = textSecondary
                    }
                };
            }

            public void SetEnabled(bool enabled)
            {
                Enabled.Value = enabled;
                this.FadeTo(enabled ? 1 : 0.35f, 180, Easing.OutQuint);
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (!Enabled.Value)
                    return base.OnHover(e);

                backgroundBox.FadeColour(accent, 160, Easing.OutQuint);
                icon.FadeColour(background, 160, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                backgroundBox.FadeColour(elevatedSurface, 180, Easing.OutQuint);
                icon.FadeColour(textSecondary, 180, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }

        private partial class ReaderTextButton : ClickableContainer
        {
            private readonly Box backgroundBox;

            public ReaderTextButton(string text, Action action)
            {
                Action = action;
                Width = 150;
                Height = 38;
                Masking = true;
                CornerRadius = 12;

                InternalChildren = new Drawable[]
                {
                    backgroundBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = GruvboxColours.Yellow
                    },
                    new SpriteText
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Text = text,
                        Font = FontUsage.Default.With(size: 12, weight: "Bold"),
                        Colour = paperText
                    }
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                backgroundBox.FadeColour(warmAccent, 160, Easing.OutQuint);
                this.ScaleTo(1.03f, 160, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                backgroundBox.FadeColour(GruvboxColours.Yellow, 180, Easing.OutQuint);
                this.ScaleTo(1, 180, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }

        private partial class ChapterButton : ClickableContainer
        {
            private readonly Box backgroundBox;
            private readonly SpriteText number;
            private readonly SpriteText title;
            private bool selected;

            public ChapterButton(int chapterNumber, string chapterTitle, Action action)
            {
                Name = $"Chapter {chapterNumber}";
                Action = action;
                Width = 218;
                Height = 42;
                Masking = true;
                CornerRadius = 10;

                InternalChildren = new Drawable[]
                {
                    backgroundBox = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = elevatedSurface,
                        Alpha = 0
                    },
                    number = new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.Centre,
                        X = 17,
                        Text = chapterNumber.ToString("00"),
                        Font = FontUsage.Default.With(size: 10, weight: "Bold"),
                        Colour = textSecondary
                    },
                    title = new SpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        X = 45,
                        Text = chapterTitle,
                        Font = FontUsage.Default.With(size: 12),
                        Colour = textSecondary,
                        MaxWidth = 154,
                        Truncate = true
                    }
                };
            }

            public void SetSelected(bool selected)
            {
                this.selected = selected;
                backgroundBox.FadeTo(selected ? 0.13f : 0, 180, Easing.OutQuint);
                backgroundBox.FadeColour(selected ? accent : elevatedSurface, 180, Easing.OutQuint);
                number.FadeColour(selected ? accent : textSecondary, 180, Easing.OutQuint);
                title.FadeColour(selected ? textPrimary : textSecondary, 180, Easing.OutQuint);
            }

            protected override bool OnHover(HoverEvent e)
            {
                backgroundBox.FadeTo(0.1f, 150, Easing.OutQuint);
                title.FadeColour(textPrimary, 150, Easing.OutQuint);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                backgroundBox.FadeTo(selected ? 0.13f : 0, 180, Easing.OutQuint);
                backgroundBox.FadeColour(selected ? accent : elevatedSurface, 180, Easing.OutQuint);
                title.FadeColour(selected ? textPrimary : textSecondary, 180, Easing.OutQuint);
                base.OnHoverLost(e);
            }
        }
    }
}
