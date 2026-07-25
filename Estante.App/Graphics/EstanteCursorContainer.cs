using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Input.StateChanges;
using osuTK;
using osuTK.Input;

namespace Estante.App
{
    internal partial class EstanteCursorContainer : CursorContainer
    {
        private EstanteCursor cursor;
        private bool cursorVisible = true;

        public EstanteCursorContainer()
        {
            Name = "Estante cursor";
        }

        protected override Drawable CreateCursor() => cursor = new EstanteCursor();

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            bool handled = base.OnMouseMove(e);
            bool shouldBeVisible = e.CurrentState.Mouse.LastSource is not ISourcedFromTouch;

            if (shouldBeVisible != cursorVisible)
            {
                cursorVisible = shouldBeVisible;
                cursor.FadeTo(shouldBeVisible ? 1 : 0, 80, Easing.OutQuint);
            }

            return handled;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button == MouseButton.Left && e.CurrentState.Mouse.LastSource is not ISourcedFromTouch)
                cursor.Press();

            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (e.Button == MouseButton.Left && e.CurrentState.Mouse.LastSource is not ISourcedFromTouch)
                cursor.Release();

            base.OnMouseUp(e);
        }

        private partial class EstanteCursor : CompositeDrawable
        {
            private readonly CircularContainer ring;
            private readonly Circle centre;

            public EstanteCursor()
            {
                Size = new Vector2(24);
                Origin = Anchor.Centre;

                InternalChildren = new Drawable[]
                {
                    ring = new CircularContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(20),
                        Masking = true,
                        BorderThickness = 2,
                        BorderColour = GruvboxColours.Aqua,
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Glow,
                            Radius = 8,
                            Colour = GruvboxColours.Aqua.Opacity(0.16f)
                        },
                        Child = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = GruvboxColours.BackgroundHard,
                            Alpha = 0.18f
                        }
                    },
                    centre = new Circle
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(4),
                        Colour = GruvboxColours.Aqua
                    }
                };
            }

            public void Press()
            {
                ring.ClearTransforms();
                centre.ClearTransforms();

                ring.ScaleTo(0.78f, 70, Easing.OutQuint);
                centre.ScaleTo(1.55f, 70, Easing.OutQuint);
                centre.FadeColour(GruvboxColours.Yellow, 70, Easing.OutQuint);
            }

            public void Release()
            {
                ring.ClearTransforms();
                centre.ClearTransforms();

                ring.ScaleTo(1, 190, Easing.OutBack);
                centre.ScaleTo(1, 170, Easing.OutBack);
                centre.FadeColour(GruvboxColours.Aqua, 160, Easing.OutQuint);
            }
        }
    }
}
