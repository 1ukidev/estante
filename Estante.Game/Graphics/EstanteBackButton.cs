using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osuTK;

namespace Estante.Game
{
    internal partial class EstanteBackButton : ClickableContainer
    {
        private readonly Box backgroundBox;
        private readonly SpriteIcon icon;

        public EstanteBackButton()
        {
            Size = new Vector2(44);
            Masking = true;
            CornerRadius = 13;

            InternalChildren = new Drawable[]
            {
                backgroundBox = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = GruvboxColours.Background
                },
                icon = new SpriteIcon
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(16),
                    Icon = FontAwesome.Solid.ArrowLeft,
                    Colour = GruvboxColours.ForegroundMuted
                }
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            backgroundBox.FadeColour(GruvboxColours.Background1, 180, Easing.OutQuint);
            icon.FadeColour(GruvboxColours.Aqua, 180, Easing.OutQuint);
            icon.MoveToX(-3, 180, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            backgroundBox.FadeColour(GruvboxColours.Background, 200, Easing.OutQuint);
            icon.FadeColour(GruvboxColours.ForegroundMuted, 200, Easing.OutQuint);
            icon.MoveToX(0, 200, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            this.ScaleTo(0.97f, 70, Easing.OutQuint);
            return base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            this.ScaleTo(1, 130, Easing.OutBack);
            base.OnMouseUp(e);
        }
    }
}
