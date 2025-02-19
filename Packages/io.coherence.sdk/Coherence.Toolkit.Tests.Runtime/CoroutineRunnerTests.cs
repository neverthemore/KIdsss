namespace Coherence.Toolkit.Tests
{
    using System.Collections;
    using System.Linq;
    using Coherence.Tests;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using UnityEngine.TestTools;

    /// <summary>
    /// Runtime tests for <see cref="CoroutineRunner"/>.
    /// </summary>
    public sealed class CoroutineRunnerTests : CoherenceTest
    {
        private const string EmptySceneName = "EmptyScene";

        [UnityTest]
        public IEnumerator StartCoroutine_Starts_Coroutine()
        {
            var coroutineHolder = new CoroutineHolder();
            CoroutineRunner.StartCoroutine(coroutineHolder.Coroutine());

            yield return coroutineHolder.WaitForCoroutineToFinish();

            Assert.That(coroutineHolder.CoroutineHasFinished, Is.True);
        }

        [UnityTest]
        public IEnumerator StartCoroutine_Survives_Unloading_Active_Scenes()
        {
            var coroutineHolder = new CoroutineHolder();
            CoroutineRunner.StartCoroutine(coroutineHolder.Coroutine());

            SceneManager.LoadScene(EmptySceneName);

            yield return coroutineHolder.WaitForCoroutineToFinish();

            Assert.That(coroutineHolder.CoroutineHasFinished, Is.True);
        }

        [Test]
        public void GameObject_Has_HideFlags_HideAndDontSave()
        {
            // Start any coroutine to cause the CoroutineRunner to get created.
            CoroutineRunner.StartCoroutine(new CoroutineHolder().Coroutine());

            var coroutineRunner = FindCoroutineRunner();
            var gameObject = coroutineRunner.gameObject;
            var hideFlags = gameObject.hideFlags;

            Assert.That(hideFlags, Is.EqualTo(HideFlags.HideAndDontSave));
        }

        [Test]
        public void Multiple_StartCoroutine_Calls_Result_In_Exactly_One_CoroutineRunner_Being_Created()
        {
            CoroutineRunner.StartCoroutine(new CoroutineHolder().Coroutine());
            CoroutineRunner.StartCoroutine(new CoroutineHolder().Coroutine());

            var instances = FindAllCoroutineRunners();

            Assert.That(instances, Has.Length.EqualTo(1));
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
            CoroutineRunner.DisposeSharedInstance(true);
        }

        private static CoroutineRunner FindCoroutineRunner() => FindAllCoroutineRunners().Single();

        private static CoroutineRunner[] FindAllCoroutineRunners()
            => Resources.FindObjectsOfTypeAll<CoroutineRunner>()
                .Where(c => c) // ignore destroyed instances
                .ToArray();

        private sealed class CoroutineHolder
        {
            private const int CoroutineDurationInFrames = 1;

            public bool CoroutineHasFinished { get; private set; }

            public IEnumerator Coroutine()
            {
                yield return null;
                CoroutineHasFinished = true;
            }

            public IEnumerator WaitForCoroutineToFinish()
            {
                for (var framesWaited = 0; !CoroutineHasFinished; framesWaited++)
                {
                    Assert.That(framesWaited, Is.LessThanOrEqualTo(CoroutineDurationInFrames));
                    yield return null;
                }
            }
        }
    }
}
