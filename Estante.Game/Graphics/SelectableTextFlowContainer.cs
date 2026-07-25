using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.IO.Stores;
using osu.Framework.Text;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace Estante.Game
{
    public partial class SelectableTextFlowContainer : CompositeDrawable
    {
        private const float selection_start_tolerance = 28;

        private readonly Container highlightLayer;
        private readonly TextFlowContainer textFlow;
        private readonly Color4 selectionColour;

        [Resolved]
        private FontStore fontStore { get; set; }

        private int selectionStart = -1;
        private int selectionEnd = -1;

        public Action<RectangleF> SelectionFinished { get; set; }
        public Action SelectionCleared { get; set; }

        public bool HasSelection => selectionStart >= 0 && selectionEnd >= 0;

        public string SelectedText
        {
            get
            {
                if (!HasSelection)
                    return string.Empty;

                IReadOnlyList<SpriteText> words = getWords();
                int first = Math.Min(selectionStart, selectionEnd);
                int last = Math.Max(selectionStart, selectionEnd);

                if (first < 0 || last >= words.Count)
                    return string.Empty;

                return string.Concat(words.Skip(first)
                                          .Take(last - first + 1)
                                          .Select(word => word.Text.ToString()))
                             .Trim();
            }
        }

        public RectangleF? SelectionScreenBounds
        {
            get
            {
                if (!HasSelection)
                    return null;

                IReadOnlyList<SpriteText> words = getWords();
                int first = Math.Min(selectionStart, selectionEnd);
                int last = Math.Max(selectionStart, selectionEnd);

                if (first < 0 || last >= words.Count)
                    return null;

                RectangleF bounds = words[first].ScreenSpaceDrawQuad.AABBFloat;

                for (int i = first + 1; i <= last; i++)
                    bounds = RectangleF.Union(bounds, words[i].ScreenSpaceDrawQuad.AABBFloat);

                return bounds;
            }
        }

        public string Text
        {
            set
            {
                ClearSelection();
                textFlow.Text = value;
            }
        }

        public float ParagraphSpacing
        {
            get => textFlow.ParagraphSpacing;
            set => textFlow.ParagraphSpacing = value;
        }

        public float LineSpacing
        {
            get => textFlow.LineSpacing;
            set => textFlow.LineSpacing = value;
        }

        public Anchor TextAnchor
        {
            get => textFlow.TextAnchor;
            set => textFlow.TextAnchor = value;
        }

        public override bool HandlePositionalInput => true;

        public SelectableTextFlowContainer(Action<SpriteText> defaultCreationParameters, Color4 selectionColour)
        {
            this.selectionColour = selectionColour;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChildren = new Drawable[]
            {
                highlightLayer = new Container
                {
                    RelativeSizeAxes = Axes.X
                },
                textFlow = new TextFlowContainer(defaultCreationParameters)
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y
                }
            };
        }

        public void ClearSelection()
        {
            bool hadSelection = HasSelection;

            selectionStart = -1;
            selectionEnd = -1;
            highlightLayer.Clear();
            highlightLayer.ClearTransforms();
            highlightLayer.Alpha = 1;

            if (hadSelection)
                SelectionCleared?.Invoke();
        }

        protected override void Update()
        {
            base.Update();

            highlightLayer.Height = textFlow.DrawHeight;
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (e.Button != MouseButton.Left)
                return false;

            IReadOnlyList<SpriteText> words = getWords();
            Vector2 mouseDownPosition = ToLocalSpace(e.ScreenSpaceMouseDownPosition);
            Vector2 mousePosition = ToLocalSpace(e.ScreenSpaceMousePosition);
            int startIndex = findClosestWordIndex(words, mouseDownPosition, selection_start_tolerance);

            if (startIndex < 0)
                return false;

            ClearSelection();
            selectionStart = startIndex;
            selectionEnd = findClosestWordIndex(words, mousePosition) is int currentIndex and >= 0
                ? currentIndex
                : startIndex;

            rebuildHighlights(words);
            highlightLayer.FadeInFromZero(90, Easing.OutQuint);
            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            if (!HasSelection || e.Button != MouseButton.Left)
                return;

            IReadOnlyList<SpriteText> words = getWords();
            Vector2 mousePosition = ToLocalSpace(e.ScreenSpaceMousePosition);
            int currentIndex = findClosestWordIndex(words, mousePosition);

            if (currentIndex < 0 || currentIndex == selectionEnd)
                return;

            selectionEnd = currentIndex;
            rebuildHighlights(words);
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            base.OnDragEnd(e);

            if (e.Button != MouseButton.Left || !HasSelection)
                return;

            RectangleF? bounds = SelectionScreenBounds;

            if (bounds.HasValue)
                SelectionFinished?.Invoke(bounds.Value);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (e.Button != MouseButton.Left)
                return base.OnClick(e);

            ClearSelection();
            return true;
        }

        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            if (e.Button != MouseButton.Left)
                return base.OnDoubleClick(e);

            IReadOnlyList<SpriteText> words = getWords();
            Vector2 mousePosition = ToLocalSpace(e.ScreenSpaceMousePosition);
            int wordIndex = findClosestWordIndex(words, mousePosition, selection_start_tolerance);

            if (wordIndex < 0)
            {
                ClearSelection();
                return true;
            }

            ClearSelection();
            selectionStart = wordIndex;
            selectionEnd = wordIndex;
            rebuildHighlights(words);
            highlightLayer.FadeInFromZero(90, Easing.OutQuint);

            RectangleF? bounds = SelectionScreenBounds;

            if (bounds.HasValue)
                SelectionFinished?.Invoke(bounds.Value);

            return true;
        }

        private IReadOnlyList<SpriteText> getWords() =>
            textFlow.Children.OfType<SpriteText>().ToArray();

        private int findClosestWordIndex(IReadOnlyList<SpriteText> words, Vector2 localPosition, float maximumDistance = float.PositiveInfinity)
        {
            int closestIndex = -1;
            float closestDistanceSquared = maximumDistance * maximumDistance;

            for (int i = 0; i < words.Count; i++)
            {
                RectangleF rectangle = ToLocalSpace(words[i].ScreenSpaceDrawQuad).AABBFloat;
                float closestX = Math.Clamp(localPosition.X, rectangle.Left, rectangle.Right);
                float closestY = Math.Clamp(localPosition.Y, rectangle.Top, rectangle.Bottom);
                float distanceSquared = Vector2.DistanceSquared(localPosition, new Vector2(closestX, closestY));

                if (distanceSquared <= closestDistanceSquared)
                {
                    closestIndex = i;
                    closestDistanceSquared = distanceSquared;
                }
            }

            return closestIndex;
        }

        private void rebuildHighlights(IReadOnlyList<SpriteText> words)
        {
            highlightLayer.Clear();

            int first = Math.Min(selectionStart, selectionEnd);
            int last = Math.Max(selectionStart, selectionEnd);
            var lines = new List<RectangleF>();

            for (int i = first; i <= last && i < words.Count; i++)
            {
                RectangleF rectangle = getHighlightRectangle(words[i]);

                if (lines.Count > 0 && Math.Abs(lines[^1].Top - rectangle.Top) < 2)
                    lines[^1] = RectangleF.Union(lines[^1], rectangle);
                else
                    lines.Add(rectangle);
            }

            foreach (RectangleF line in lines)
            {
                highlightLayer.Add(new Container
                {
                    Name = "Selection highlight",
                    Position = new Vector2(line.X, line.Y),
                    Size = new Vector2(line.Width, line.Height),
                    Masking = true,
                    CornerRadius = 2,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = selectionColour
                    }
                });
            }
        }

        private RectangleF getHighlightRectangle(SpriteText word)
        {
            RectangleF rectangle = ToLocalSpace(word.ScreenSpaceDrawQuad).AABBFloat;
            string text = word.Text.ToString();

            if (string.IsNullOrEmpty(text))
                return rectangle;

            int lastVisibleCharacter = text.Length - 1;

            while (lastVisibleCharacter >= 0 && char.IsWhiteSpace(text[lastVisibleCharacter]))
                lastVisibleCharacter--;

            float rightInset = 0;

            for (int i = text.Length - 1; i > lastVisibleCharacter; i--)
            {
                ITexturedCharacterGlyph whitespaceGlyph = getGlyph(word.Font, text[i]);

                if (whitespaceGlyph != null)
                    rightInset += whitespaceGlyph.XAdvance * word.Font.Size;

                if (i > 0)
                    rightInset += word.Spacing.X;
            }

            if (lastVisibleCharacter >= 0)
            {
                ITexturedCharacterGlyph lastGlyph = getGlyph(word.Font, text[lastVisibleCharacter]);

                if (lastGlyph != null)
                {
                    float rightBearing = lastGlyph.XAdvance - lastGlyph.XOffset - lastGlyph.Width;
                    rightInset += Math.Max(0, rightBearing * word.Font.Size);
                }
            }

            rectangle.Width = Math.Max(0, rectangle.Width - rightInset);
            return rectangle;
        }

        private ITexturedCharacterGlyph getGlyph(FontUsage font, char character) =>
            fontStore.Get(font.FontName, character)
            ?? fontStore.Get(font.FontNameNoFamily, character)
            ?? fontStore.Get(string.Empty, character);
    }
}
