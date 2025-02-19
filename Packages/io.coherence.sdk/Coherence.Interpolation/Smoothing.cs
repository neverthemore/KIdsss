// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

using System;
using UnityEngine;
using Coherence.Common;

namespace Coherence.Interpolation
{
    /// <summary>
    /// ISmoothing interface defines methods used for smoothing value towards a target value.
    /// </summary>
    /// <typeparam name="T">Type of the smoothed value</typeparam>
    internal interface ISmoothing<T>
    {
        /// <summary>
        /// Smoothly lerps the current value towards the target value.
        /// </summary>
        /// <param name="settings">Settings used to define smoothing parameters</param>
        /// <param name="currentValue">The current value</param>
        /// <param name="targetValue">The target value which needs to be smoothly reached</param>
        /// <param name="time">Current time (eg. NetworkTime.TimeAsDouble)</param>
        /// <returns>New value which is smoothly lerped towards the target value</returns>
        public T Smooth(SmoothingSettings settings, T currentValue, T targetValue, double time);
    }

    /// <summary>
    /// Provides smoothing using SmoothDamp for types: float, Vector2, Vector3 and Quaternion.
    /// Smoothing unlike ordinary interpolation, cannot be rewinded, so keep smoothing at a minimum for accurate hitbox rewind.
    /// Quaternion smoothing uses SmoothDampAngle for its three euler angles, which may cause a performance hit for large number of objects.
    /// </summary>
    public class Smoothing
        : ISmoothing<double>, ISmoothing<float>, ISmoothing<Vector2>, ISmoothing<Vector3>, ISmoothing<Quaternion>
    {
        private double lastTime;
        private double doubleVelocity;
        private Vector4d velocity;

        private float GetDeltaTime(double time)
        {
            var deltaTime = (float)(time - lastTime);

            if (deltaTime < 0f)
            {
                throw new Exception("deltaTime should only be positive. Possibly passed in InterpolationSettings.Time instead of NetworkTime into a smoothing function.");
            }

            lastTime = time;
            return deltaTime;
        }

        public double SmoothDouble(SmoothingSettings settings, double currentValue, double targetValue, double time)
        {
            return InterpolationUtils.SmoothMixDouble(currentValue, targetValue, ref doubleVelocity, settings.smoothTime, settings.maxSpeed, GetDeltaTime(time));
        }

        public float SmoothFloat(SmoothingSettings settings, float currentValue, float targetValue, double time)
        {
            var smoothValue = InterpolationUtils.SmoothMixDouble(currentValue, targetValue, ref doubleVelocity, settings.smoothTime, settings.maxSpeed, GetDeltaTime(time));

            return InterpolationUtils.ClampFloat((float)smoothValue);
        }

        public Vector2 SmoothVector2(SmoothingSettings settings, Vector2 currentValue, Vector2 targetValue, double time)
        {
            var deltaTime = GetDeltaTime(time);
            var smoothX = InterpolationUtils.SmoothMixDouble(currentValue.x, targetValue.x, ref velocity.x, settings.smoothTime, settings.maxSpeed, deltaTime);
            var smoothY = InterpolationUtils.SmoothMixDouble(currentValue.y, targetValue.y, ref velocity.y, settings.smoothTime, settings.maxSpeed, deltaTime);
            return new Vector2(
                InterpolationUtils.ClampFloat((float)smoothX),
                InterpolationUtils.ClampFloat((float)smoothY)
            );
        }

        public Vector3 SmoothVector3(SmoothingSettings settings, Vector3 currentValue, Vector3 targetValue, double time)
        {
            var deltaTime = GetDeltaTime(time);
            var smoothX = InterpolationUtils.SmoothMixDouble(currentValue.x, targetValue.x, ref velocity.x, settings.smoothTime, settings.maxSpeed, deltaTime);
            var smoothY = InterpolationUtils.SmoothMixDouble(currentValue.y, targetValue.y, ref velocity.y, settings.smoothTime, settings.maxSpeed, deltaTime);
            var smoothZ = InterpolationUtils.SmoothMixDouble(currentValue.z, targetValue.z, ref velocity.z, settings.smoothTime, settings.maxSpeed, deltaTime);
            return new Vector3(
                InterpolationUtils.ClampFloat((float)smoothX),
                InterpolationUtils.ClampFloat((float)smoothY),
                InterpolationUtils.ClampFloat((float)smoothZ)
            );
        }

        public Quaternion SmoothQuaternion(SmoothingSettings settings, Quaternion currentValue, Quaternion targetValue, double time)
        {
            return InterpolationUtils.SmoothDampQuaternion(currentValue, targetValue, ref velocity, settings.smoothTime, settings.maxSpeed, GetDeltaTime(time));
        }

        // ISmoothing<T> implementations
        public double Smooth(SmoothingSettings settings, double currentValue, double targetValue, double time) => SmoothDouble(settings, currentValue, targetValue, time);
        public float Smooth(SmoothingSettings settings, float currentValue, float targetValue, double time) => SmoothFloat(settings, currentValue, targetValue, time);
        public Vector2 Smooth(SmoothingSettings settings, Vector2 currentValue, Vector2 targetValue, double time) => SmoothVector2(settings, currentValue, targetValue, time);
        public Vector3 Smooth(SmoothingSettings settings, Vector3 currentValue, Vector3 targetValue, double time) => SmoothVector3(settings, currentValue, targetValue, time);
        public Quaternion Smooth(SmoothingSettings settings, Quaternion currentValue, Quaternion targetValue, double time) => SmoothQuaternion(settings, currentValue, targetValue, time);
    }
}
