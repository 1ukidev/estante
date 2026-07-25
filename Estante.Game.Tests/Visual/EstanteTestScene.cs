using osu.Framework.Testing;

namespace Estante.Game.Tests.Visual
{
    public abstract partial class EstanteTestScene : TestScene
    {
        protected override ITestSceneTestRunner CreateRunner() => new EstanteTestSceneTestRunner();

        private partial class EstanteTestSceneTestRunner : EstanteGameBase, ITestSceneTestRunner
        {
            private TestSceneTestRunner.TestRunner runner;

            protected override void LoadAsyncComplete()
            {
                base.LoadAsyncComplete();
                Add(runner = new TestSceneTestRunner.TestRunner());
            }

            public void RunTestBlocking(TestScene test) => runner.RunTestBlocking(test);
        }
    }
}
