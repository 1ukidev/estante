using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Screens;

namespace Estante.App
{
    public partial class MainScreen : Screen
    {
        private readonly Action<Action<string>> selectBook;

        private ScreenStack screenStack;

        public MainScreen(Action<Action<string>> selectBook = null)
        {
            this.selectBook = selectBook;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = screenStack = new ScreenStack
            {
                RelativeSizeAxes = Axes.Both
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            screenStack.Push(new HomeScreen(selectBook));
        }
    }
}
