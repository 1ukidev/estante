using System;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Screens;

namespace Estante.Game
{
    public partial class EstanteGame : EstanteGameBase
    {
        private readonly Action<Action<string>> selectBook;

        private ScreenStack screenStack;

        public EstanteGame(Action<Action<string>> selectBook = null)
        {
            this.selectBook = selectBook;
        }

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            // Add your top-level game components here.
            // A screen stack and sample screen has been provided for convenience, but you can replace it if you don't want to use screens.
            Child = screenStack = new ScreenStack { RelativeSizeAxes = Axes.Both };

            // FrameworkConfigManager
            config.SetValue(FrameworkSetting.Renderer, RendererType.Automatic);
            config.SetValue(FrameworkSetting.FrameSync, FrameSync.VSync);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            screenStack.Push(new MainScreen(selectBook));
        }
    }
}
