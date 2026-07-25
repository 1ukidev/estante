using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK;

namespace Estante.App
{
    internal enum EstanteBackgroundStyle
    {
        Home,
        Reader
    }

    internal partial class EstanteBackground : CompositeDrawable
    {
        private readonly Circle primaryGlow;
        private readonly Circle secondaryGlow;
        private readonly double pulseDuration;
        private readonly bool animateSecondaryGlow;

        public EstanteBackground(EstanteBackgroundStyle style)
        {
            RelativeSizeAxes = Axes.Both;

            Vector2 primaryPosition;
            Vector2 primarySize;
            float primaryAlpha;
            Vector2 secondaryPosition;
            Vector2 secondarySize;
            float secondaryAlpha;

            if (style == EstanteBackgroundStyle.Home)
            {
                primaryPosition = new Vector2(50, -70);
                primarySize = new Vector2(390);
                primaryAlpha = 0.055f;
                secondaryPosition = new Vector2(-80, 105);
                secondarySize = new Vector2(520);
                secondaryAlpha = 0.035f;
                pulseDuration = 6500;
                animateSecondaryGlow = true;
            }
            else
            {
                primaryPosition = new Vector2(40, -120);
                primarySize = new Vector2(520);
                primaryAlpha = 0.04f;
                secondaryPosition = new Vector2(-80, 100);
                secondarySize = new Vector2(420);
                secondaryAlpha = 0.025f;
                pulseDuration = 7000;
            }

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = GruvboxColours.BackgroundHard
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    Children = new Drawable[]
                    {
                        primaryGlow = new Circle
                        {
                            Anchor = Anchor.TopRight,
                            Origin = Anchor.Centre,
                            Position = primaryPosition,
                            Size = primarySize,
                            Colour = GruvboxColours.Aqua,
                            Alpha = primaryAlpha
                        },
                        secondaryGlow = new Circle
                        {
                            Anchor = Anchor.BottomLeft,
                            Origin = Anchor.Centre,
                            Position = secondaryPosition,
                            Size = secondarySize,
                            Colour = GruvboxColours.Yellow,
                            Alpha = secondaryAlpha
                        }
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            primaryGlow
                .ScaleTo(1.08f, pulseDuration, Easing.InOutSine)
                .Then()
                .ScaleTo(1, pulseDuration, Easing.InOutSine)
                .Loop();

            if (animateSecondaryGlow)
            {
                secondaryGlow
                    .MoveToOffset(new Vector2(45, -24), 8000, Easing.InOutSine)
                    .Then()
                    .MoveToOffset(new Vector2(-45, 24), 8000, Easing.InOutSine)
                    .Loop();
            }
        }
    }
}
