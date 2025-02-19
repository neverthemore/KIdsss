// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation.Tests
{
    using System;
    using NUnit.Framework;
    using UnityEngine;
    using UnityEngine.TestTools.Utils;
    using Coherence.Tests;

    public class OvershootingTests : CoherenceTest
    {
        public const float Epsilon = 10e-3f;
        private static readonly Vector3EqualityComparer Comparer = new Vector3EqualityComparer(Epsilon);

        private static readonly Sample<Vector3> SampleA = new Sample<Vector3>(new Vector3(1, 2, 3), false, 0);
        private static readonly Sample<Vector3> SampleB = new Sample<Vector3>(new Vector3(4, 5, 6), false, 1);
        private static readonly Sample<Vector3> SampleBStopped = new Sample<Vector3>(new Vector3(4, 5, 6), true, 2);
        private static readonly Sample<Vector3> NewSampleStoppedEarly = new Sample<Vector3>(new Vector3(7, 8, 9), true, 1.5);
        private static readonly Sample<Vector3> NewSampleStoppedOnTime = new Sample<Vector3>(new Vector3(7, 8, 9), true, 2);
        private static readonly Sample<Vector3> NewSampleStoppedLate = new Sample<Vector3>(new Vector3(7, 8, 9), true, 2.25);
        private static readonly Sample<Vector3> SampleD = new Sample<Vector3>(new Vector3(4.1f, 5.1f, 6.1f), true, 2);

        public readonly struct SampleTestData
        {
            public SampleTestData(float time, double sampleRate, Vector3 expectedPosition, Sample<Vector3>[] samples)
            {
                Time = time;
                ExpectedPosition = expectedPosition;
                Samples = samples;
                SampleRate = sampleRate;
            }

            public Sample<Vector3>[] Samples { get; }
            public double Time { get; }
            public double SampleRate { get; }
            public Vector3? ExpectedPosition { get; }

            public override string ToString()
            {
                return $"{nameof(Samples)}: {Samples}, {nameof(Time)}: {Time}, {nameof(SampleRate)}: {SampleRate}, " +
                    $"{nameof(ExpectedPosition)}: {ExpectedPosition}";
            }
        }

        private static SampleTestData[] testSource = {
            // Small overshooting
            new SampleTestData(1.1f, 1, SampleA.Value + (SampleB.Value - SampleA.Value) * 1.1f, new []{SampleA, SampleB}),
            new SampleTestData(2.1f, 1, NewSampleStoppedEarly.Value, new []{SampleA, SampleB, NewSampleStoppedEarly}),
            new SampleTestData(2.1f, 1, NewSampleStoppedOnTime.Value, new []{SampleA, SampleB, NewSampleStoppedOnTime}),

            // Big overshooting
            new SampleTestData(2.5f, 1, SampleA.Value + (SampleB.Value - SampleA.Value) * 2.5f, new []{SampleA, SampleB}),
            new SampleTestData(2.5f, 1, NewSampleStoppedEarly.Value, new []{SampleA, SampleB, NewSampleStoppedEarly}),
            new SampleTestData(2.5f, 1, NewSampleStoppedOnTime.Value, new []{SampleA, SampleB, NewSampleStoppedOnTime}),
            new SampleTestData(2.5f, 1, NewSampleStoppedLate.Value, new []{SampleA, SampleB, NewSampleStoppedLate}),

            // Mid-overshoot
            new SampleTestData(1.25f, 1, SampleB.Value + (SampleB.Value - SampleA.Value) * 0.25f, new []{SampleA, SampleB}),
            new SampleTestData(1.25f, 1, SampleB.Value + (NewSampleStoppedOnTime.Value - SampleB.Value) * 0.25f, new []{SampleA, SampleB, NewSampleStoppedOnTime}), // new sample arrived mid overshoot
            new SampleTestData(1.25f, 1, SampleBStopped.Value, new []{SampleA, SampleB, SampleBStopped}), // repeat sample with stop mid overshoot

            // Mid-overshoot retraction - pops to the last sample?
            new SampleTestData(2f, 1, SampleA.Value + (SampleB.Value - SampleA.Value) * 2f, new []{SampleA, SampleB}),
            new SampleTestData(2.26f, 1, NewSampleStoppedLate.Value, new []{SampleA, SampleB, NewSampleStoppedLate}),

            // Max-overshoot (retraction)
            new SampleTestData(1.01f + BindingInterpolator<Vector3>.MaxOvershootAllowed, 1, SampleB.Value, new []{SampleA, SampleB}),
        };

        [Test]
        [TestCaseSource(nameof(testSource))]
        public void TestOvershooting(SampleTestData testData)
        {
            // Arrange
            var interpolator = new BindingInterpolator<Vector3>(InterpolationSettings.CreateDefault(), testData.SampleRate);

            // Act
            foreach (var sample in testData.Samples)
            {
                interpolator.AppendSample(sample.Value, sample.Stopped, sample.Frame, sample.Frame);
            }

            // Assert
            var actualPosition = interpolator.GetValueAt(testData.Time);
            Assert.That(actualPosition, Is.EqualTo(testData.ExpectedPosition).Using(Comparer));
        }
    }
}
