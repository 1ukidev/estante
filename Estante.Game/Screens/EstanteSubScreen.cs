using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Screens;
using osuTK.Input;

namespace Estante.Game
{
    public abstract partial class EstanteSubScreen : Screen
    {
        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.Escape)
            {
                this.Exit();
                return true;
            }

            return base.OnKeyDown(e);
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            PrepareExitTransition();

            this.FadeOut(260, Easing.OutQuint);
            this.MoveToX(52, 300, Easing.OutQuint);
            return base.OnExiting(e);
        }

        protected virtual void PrepareExitTransition()
        {
        }
    }
}
