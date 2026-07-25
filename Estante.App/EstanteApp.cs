using System;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Framework.Screens;

namespace Estante.App
{
    public partial class EstanteApp : EstanteAppBase
    {
        private readonly Action<Action<string>> selectBook;

        private ScreenStack screenStack;

        public EstanteApp(Action<Action<string>> selectBook = null)
        {
            this.selectBook = selectBook;
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            // Add your top-level game components here.
            // A screen stack and sample screen has been provided for convenience, but you can replace it if you don't want to use screens.
            Children = new Drawable[]
            {
                screenStack = new ScreenStack { RelativeSizeAxes = Axes.Both },
                new EstanteCursorContainer()
            };

            // FrameworkConfigManager
            config.SetValue(FrameworkSetting.Renderer, RendererType.Automatic);
            config.SetValue(FrameworkSetting.FrameSync, FrameSync.VSync);
        }

        public override void SetHost(GameHost host)
        {
            base.SetHost(host);

            if (host.Window != null)
                host.Window.CursorState |= CursorState.Hidden;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            screenStack.Push(new MainScreen(selectBook));
        }
    }
}
