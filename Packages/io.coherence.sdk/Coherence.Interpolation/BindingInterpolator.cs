// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Interpolation
{
    using Coherence.SimulationFrame;
    using UnityEngine;

    /// <summary>
    /// BindingInterpolator is used to smoothly interpolate between samples received over the network.
    /// The following types are supported for interpolation: float, bool, int, long, Vector2, Vector3, Quaternion, Color, string and EntityReferences.
    /// Additional <see cref="Smoothing"/> (using SmoothDamp) is supported for types: float, Vector2, Vector3 and Quaternion.
    /// </summary>
    public sealed class BindingInterpolator<T>
    {
        /// <summary>
        /// Smooth time used for smoothing <see cref="Delay"/> towards <see cref="TargetDelay"/>.
        /// </summary>
        private const float DelaySmoothTime = 0.5f;

        /// <summary>
        /// Determines how far the interpolation is allowed to enter into overshooting.
        /// Overshooting occurs when samples do not arrive within the expected time (and stop marker wasn't received).
        /// A value of 1 means the binding is allowed to extrapolate 1 sample into the future.
        /// </summary>
        public const float MaxOvershootAllowed = 1.5f;

        /// <summary>
        /// The frequency at which samples are generated and transmitted from the entity owner. Higher sample rates incur lower <see cref="Delay"/>.
        /// Value is automatically initialize by the corresponding <see cref="Binding"/>.
        /// Use the Optimize window to configure the sample rate for a binding.
        /// </summary>
        public double SampleRate
        {
            get => sampleRate > 0 ? sampleRate : InterpolationSettings.DefaultSampleRate;
            set => sampleRate = value;
        }
        private double sampleRate = InterpolationSettings.DefaultSampleRate;

        /// <summary>
        /// Interpolation settings which specify behaviour and type of the interpolator.
        /// </summary>
        public InterpolationSettings Settings;

        /// <inheritdoc cref="InterpolationSettings.IsInterpolationNone"/>
        public bool IsInterpolationNone => Settings.IsInterpolationNone;

        /// <summary>
        /// The current time for this binding. It will trail behind <see cref="NetworkTime"/> by <see cref="Delay"/> seconds in order to produce smooth movement.
        /// </summary>
        public double Time;

        /// <summary>
        /// The duration between samples at which samples are actually generated and transmitted from the entity owner.
        /// This is measured from incoming samples by finding the longest duration between samples in the current <see cref="SampleBuffer{T}"/>.
        /// </summary>
        public double MeasuredSampleInterval { get; private set; }

        /// <summary>
        /// Latency between this client and the authority client that owns this entity, in seconds.
        /// This value is updated when new samples arrives so that <see cref="Delay"/> stays tuned to current network conditions.
        /// Network latency is scaled with <see cref="LatencySettings.networkLatencyFactor"/>.
        /// </summary>
        public double NetworkLatency { get; private set; }

        /// <summary>
        /// The current internal latency for this binding, i.e., how many seconds this binding's <see cref="Time"/> trails behind <see cref="NetworkTime.Time"/>.
        /// Delay is lerped towards <see cref="TargetDelay"/> over time to avoid sudden jumps in movement.
        /// </summary>
        public double Delay { get; private set; }

        /// <summary>
        /// The target internal latency for this binding, i.e., how many seconds this binding's <see cref="Time"/> trails behind <see cref="NetworkTime.Time"/>.
        /// TargetDelay is computed from a number of factors, including sampling frequency, network latency and blending method.
        /// </summary>
        /// <seealso cref="SampleRate"/>
        /// <seealso cref="NetworkLatency"/>
        /// <seealso cref="LatencySettings"/>
        /// <seealso cref="Interpolator.NumberOfSamplesToStayBehind"/>
        public double TargetDelay => Settings.interpolator.NumberOfSamplesToStayBehind * MeasuredSampleInterval // keep 1 or 2 samples of headroom at all times
                               + NetworkLatency * Settings.latencySettings.networkLatencyFactor // network latency, scaled with delay fudge factor
                               + Settings.latencySettings.additionalLatency // additional delay
                               + 1 / InterpolationSettings.SimulationFramesPerSecond; // account for 60hz quantization errors

        /// <summary>
        /// When we are completely done with interpolation and there is nothing else to interpolate over, interpolation is marked as stopped.
        /// In other words, when we completely interpolated (t = 1) to the latest received sample (which must be stopped!), IsStopped is set true,
        /// and reset to false when we receive another sample.
        /// </summary>
        public bool IsStopped { get; private set; }

        /// <summary>
        /// Used for SmoothDamp calculation of <see cref="Delay"/> towards <see cref="TargetDelay"/>.
        /// </summary>
        private double delayVelocity;

        /// <summary>
        /// Used for SmoothDamp calculation of <see cref="Delay"/> towards <see cref="TargetDelay"/>.
        /// </summary>
        private double? lastDelaySmoothTime;

        private readonly SampleBuffer<T> buffer = new SampleBuffer<T>();
        public SampleBuffer<T> Buffer => buffer;

        private readonly IInterpolator<T> interpolator;

        /// <summary>
        /// After applying interpolation, smoothing is also applied to additionally smooth movement
        /// </summary>
        private readonly ISmoothing<T> smoothing;

        public BindingInterpolator(InterpolationSettings settings, double sampleRate)
        {
            this.Settings = settings;
            this.sampleRate = sampleRate;
            this.smoothing = new Smoothing() as ISmoothing<T>;

            this.interpolator = settings.interpolator as IInterpolator<T>;

            if (this.interpolator == null)
            {
                throw new System.Exception("Interpolator doesn't implement IInterpolator<T> for type: " + typeof(T));
            }

            // setting the MeasuredSampleInterval to the expected duration as default
            MeasuredSampleInterval = 1 / SampleRate;
        }

        /// <summary>
        /// Return the last sample in the sample buffer, casted to the given type.
        /// </summary>
        /// <returns>The last sample in the sample buffer.</returns>
        public Sample<T>? GetLastSample()
        {
            if (buffer == null || buffer.Count == 0)
            {
                return null;
            }

            return buffer.Last;
        }

        /// <summary>
        /// Adds a sample to the sample buffer at the given frame.
        /// </summary>
        /// <param name="value">The sample data.</param>
        /// <param name="stopped">The samples have stopped with this value.</param>
        /// <param name="sampleFrame">The simulation frame at which the sample was generated by the entity authority.</param>
        /// <param name="localFrame">The current simulation frame on this machine, i.e. <see cref="NetworkTime.ClientSimulationFrame"/>.</param>
        public void AppendSample(T value, bool stopped, AbsoluteSimulationFrame sampleFrame, AbsoluteSimulationFrame localFrame)
        {
            AppendSample(value,
                stopped,
                isSampleTimeValid: sampleFrame != AbsoluteSimulationFrame.Invalid,
                sampleFrame / InterpolationSettings.SimulationFramesPerSecond,
                localFrame / InterpolationSettings.SimulationFramesPerSecond);
        }

        /// <summary>
        /// Adds a sample to the sample buffer at the given time.
        /// </summary>
        /// <param name="value">The sample data.</param>
        /// <param name="stopped">The samples have stopped with this value.</param>
        /// <param name="isSampleTimeValid">False if received sampleFrame is invalid. Possibly because the sample wasn't streamed
        ///     directly from the authority client, but was actually sent from the RS cache when we moved our live query or floating origin</param>
        /// <param name="sampleTime">The time at which the sample was generated by the entity authority.</param>
        /// <param name="localTime">The current time on this machine, i.e. <see cref="NetworkTime.TimeAsDouble"/>.</param>
        public void AppendSample(T value, bool stopped, bool isSampleTimeValid, double sampleTime, double localTime)
        {
            if (Settings.IsInterpolationNone)
            {
                buffer.SetLast(new Sample<T>(value, stopped, sampleTime));
                return;
            }

            if (IsBeyondTeleportDistance(value))
            {
                Reset();
            }

            var sampleBuffer = buffer;

            // Update network latency only if the sample is not stale (did not come from the RS cache).
            //  1. isSampleTimeValid => deserialized simFrame delta is not -byte.MaxValue
            //  2. sampleBuffer.Count > 0 => first sample is always from the RS cache
            //  3. sampleTime > sampleBuffer.Last.Time => if we receive two samples with the same simulation frame it means that the second one is stale
            if (isSampleTimeValid && sampleBuffer.Count > 0 && sampleTime > sampleBuffer.Last.Value.Time)
            {
                UpdateNetworkLatency(sampleTime, localTime);
            }

            // If we are stopped, we have to create a "virtual" sample. This is because we weren't receiving samples
            // while the binding wasn't changing, and now when we received a sample, time gap between them is huge.
            // To fix this, we take the last sample and move it in time to the expected time, which is at sampleTime - MeasuredSampleInterval.
            // This will be wrong only in case the actual sample interval is greater than MeasuredSampleInterval.
            if (IsStopped && sampleBuffer.Last.HasValue)
            {
                var lastSample = sampleBuffer.Last.Value;
                sampleBuffer.SetLast(new Sample<T>(lastSample.Value, true, sampleTime - MeasuredSampleInterval));
            }

            sampleBuffer.PushFront(new Sample<T>(value, stopped, sampleTime));
            if (sampleBuffer.TryMeasureMaxSampleInterval(out var measuredSampleInterval))
            {
                this.MeasuredSampleInterval = measuredSampleInterval;
            }

            this.IsStopped = false;
        }

        /// <inheritdoc cref="SampleBuffer{T}.RemoveOutdatedSamples(double, int)"/>
        public void RemoveOutdatedSamples(double time)
        {
            buffer.RemoveOutdatedSamples(time, Settings.interpolator.NumberOfSamplesToStayBehind);
        }

        /// <summary>
        /// Resets the buffer and state variables. This is useful, e.g. when teleporting and re-parenting.
        /// </summary>
        public void Reset()
        {
            buffer.Reset();
            Time = default;
            delayVelocity = default;
            lastDelaySmoothTime = default;
            MeasuredSampleInterval = 1 / SampleRate;
        }

        /// <summary>
        /// Queries the sample buffer for samples adjacent to the given time
        /// and performs blending between those samples using the <see cref="InterpolationSettings.interpolator"/>.
        /// </summary>
        /// <param name="time">The time at which to query the interpolation. Usually <see cref="Time"/>.</param>
        /// <returns>Returns the interpolated value at the given time,
        /// or default if the buffer is empty,
        /// or the single sample value if the buffer holds a single sample.</returns>
        public T GetValueAt(double time)
        {
            var result = CalculateInterpolationPercentage(time);
            return interpolator.Interpolate(result.value0, result.value1, result.value2, result.value3, result.t);
        }

        /// <summary>
        /// Performs interpolation on the given binding and returns its new value.
        /// Increments <see cref="Time"/> for this binding, taking <see cref="Delay"/> into account.
        /// Calculates the interpolated value for the latency-adjusted time using the <see cref="InterpolationSettings.interpolator"/>.
        /// Applies additional <see cref="smoothing"/> for types that support it (float, double, Vector2, Vector3, Quaternion).
        /// If the sample buffer is empty, the currentValue will be returned.
        /// If the sample buffer contains a single sample, that sample value is returned with no blending or smoothing performed.
        /// </summary>
        /// <param name="currentValue">The current value for the binding.</param>
        /// <param name="time">The current game time, usually <see cref="NetworkTime"/>.</param>
        /// <returns>The interpolated value for this binding at the given time,
        /// or <see cref="currentValue"/> if the sample buffer is empty.</returns>
        public T PerformInterpolation(T currentValue, double time)
        {
            if (buffer.Count == 0)
            {
                return currentValue;
            }

            var newValue = GetValueAt(Step(time));

            if (buffer.Count > 1 && smoothing != null)
            {
                return smoothing.Smooth(Settings.smoothing, currentValue, newValue, time);
            }

            return newValue;
        }

        private double Step(double newTime)
        {
            UpdateDelay(newTime);

            Time = newTime - Delay;

            // Remove outdated samples
            RemoveOutdatedSamples(Time);

            return Time;
        }

        private void UpdateDelay(double time)
        {
            // at startup we want Delay to be set to TargetDelay instead of slowly lerping from zero
            if (lastDelaySmoothTime == null)
            {
                this.Delay = this.TargetDelay;
            }
            else
            {
                var deltaTime = time - lastDelaySmoothTime;

                if (deltaTime > 0)
                {
                    // Anything above 1 will allow Delay to increase faster than 1 second per second which interpolates backwards.
                    const double maxSpeed = 1;

                    this.Delay = InterpolationUtils.SmoothMixDouble(Delay, TargetDelay, ref delayVelocity, DelaySmoothTime, maxSpeed, (float)deltaTime);
                }
            }

            lastDelaySmoothTime = time;
        }

        private void UpdateNetworkLatency(double remoteTime, double localTime)
        {
            this.NetworkLatency = localTime - remoteTime;

            // If calculated network latency is negative, it means our clientSimulationFrame is behind serverSimulationFrame
            // which means we are currently simulating history.
            // In that case timeScale will be over 1 and we will be simulating (replaying) our data at a faster speed.
            // Also we don't want to have negative latency because that will move our InterpolationSettings.Time back to real time
            // instead of keeping it in history time
            if (this.NetworkLatency < 0)
            {
                this.NetworkLatency = 0;
            }

            // If we are stopped and new network latency is bigger than the old one, we can freely snap Delay to TargetDelay.
            // It will not cause any movement jitter because we are stopped, and it makes sure that Delay is where it should be,
            // instead of letting it lerp slowly towards the TargetDelay.
            if (IsStopped && this.Delay < TargetDelay)
            {
                this.Delay = TargetDelay;
            }
        }

        private bool IsBeyondTeleportDistance(T value)
        {
            if (Settings.maxDistance <= 0)
            {
                return false;
            }

            var newestSampleInBuffer = buffer.Last;
            if (!newestSampleInBuffer.HasValue)
            {
                return false;
            }

            return (newestSampleInBuffer.Value.Value, value) switch
            {
                (int prev, int next) => IsBeyondTeleportDistance(next, prev),
                (float prev, float next) => IsBeyondTeleportDistance(next, prev),
                (Vector2 prev, Vector2 next) => IsBeyondTeleportDistance(next, prev),
                (Vector3 prev, Vector3 next) => IsBeyondTeleportDistance(next, prev),
                (Quaternion prev, Quaternion next) => IsBeyondTeleportDistance(next, prev),
                _ => false
            };
        }

        public bool IsBeyondTeleportDistance(int a, int b)
        {
            return Settings.maxDistance > 0 && Mathf.Abs(a - b) >= Settings.maxDistance;
        }

        public bool IsBeyondTeleportDistance(float a, float b)
        {
            return Settings.maxDistance > 0 && Mathf.Abs(a - b) >= Settings.maxDistance;
        }

        public bool IsBeyondTeleportDistance(Vector2 a, Vector2 b)
        {
            return Settings.maxDistance > 0 && (a - b).sqrMagnitude >= Settings.maxDistance * Settings.maxDistance;
        }

        public bool IsBeyondTeleportDistance(Vector3 a, Vector3 b)
        {
            return Settings.maxDistance > 0 && (a - b).sqrMagnitude >= Settings.maxDistance * Settings.maxDistance;
        }

        public bool IsBeyondTeleportDistance(Quaternion a, Quaternion b)
        {
            return Settings.maxDistance > 0 && Quaternion.Angle(a, b) >= Settings.maxDistance;
        }

        public InterpolationResult<T> CalculateInterpolationPercentage(double time)
        {
            var result = new InterpolationResult<T>()
            {
                t = -1,
                delay = this.Delay,
                targetDelay = this.TargetDelay,
                networkLatency = this.NetworkLatency,
                measuredSampleInterval = this.MeasuredSampleInterval
            };

            // Retrieve adjacent samples
            var adjecentSamples = buffer.GetAdjacentSamples(time);

            // no samples in the buffer
            if (adjecentSamples.Sample1Index == -1)
            {
                result.isStopped = IsStopped = true;
                return result;
            }

            result.sample0 = buffer[Mathf.Clamp(adjecentSamples.Sample1Index - 1, 0, buffer.Count - 1)];
            result.sample1 = adjecentSamples.Sample1;
            result.sample2 = adjecentSamples.Sample2;
            result.sample3 = buffer[Mathf.Clamp(adjecentSamples.Sample1Index + 2, 0, buffer.Count - 1)];

            var sampleDeltaTime = adjecentSamples.Sample2.Time - adjecentSamples.Sample1.Time;

            if (sampleDeltaTime == 0)
            {
                result.t = 0;
                result.isStopped = IsStopped = adjecentSamples.IsLastSample;
                return result;
            }

            // Calculate the time fraction
            var timePassed = time - adjecentSamples.Sample1.Time;

            var stopped = adjecentSamples.Sample2.Stopped;

            result.t = (float)(timePassed / sampleDeltaTime);

            // received sample stop mark
            if (stopped)
            {
                result.t = Mathf.Min(result.t, 1f);
            }

            if (stopped && adjecentSamples.IsLastSample && result.t == 1)
            {
                result.isStopped = IsStopped = true;
            }

            if (result.t > 1 + MaxOvershootAllowed)
            {
                result.t = 1;
                result.isStopped = IsStopped = true;
            }

            return result;
        }
    }

    public struct InterpolationResult<T>
    {
        public Sample<T> sample0;
        public Sample<T> sample1;
        public Sample<T> sample2;
        public Sample<T> sample3;
        public float t;

        public double delay;
        public double targetDelay;
        public double networkLatency;
        public double measuredSampleInterval;
        public bool isStopped;

        public T value0 => sample0.Value;
        public T value1 => sample1.Value;
        public T value2 => sample2.Value;
        public T value3 => sample3.Value;

        public override string ToString()
        {
            return $"1:{sample1} 2:{sample2} t:{t} delay:{delay} targetDelay:{targetDelay} networkLatency{networkLatency} measuredSampleInterval:{measuredSampleInterval} isStopped:{isStopped}";
        }
    }
}
