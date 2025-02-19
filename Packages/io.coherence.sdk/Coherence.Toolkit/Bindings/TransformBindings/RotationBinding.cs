namespace Coherence.Toolkit.Bindings.TransformBindings
{
    using Coherence.Interpolation;
    using System;
    using System.Reflection;
    using UnityEngine;
    using ValueBindings;

    [Serializable]
    public class RotationBinding : QuaternionBinding
    {
        public override string CoherenceComponentName => "WorldOrientation";
        public override string MemberNameInComponentData => "value";
        public override string MemberNameInUnityComponent => nameof(CoherenceSync.coherenceRotation);
        public override string BakedSyncScriptGetter => nameof(CoherenceSync.coherenceRotation);
        public override string BakedSyncScriptSetter => nameof(CoherenceSync.coherenceRotation);

        protected RotationBinding() {}

        public RotationBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent) {}

        public override Quaternion Value
        {
            get => coherenceSync.coherenceRotation;
            set => coherenceSync.coherenceRotation = value;
        }

        public void RotateSamples(Quaternion delta, bool transformLastSampleToo)
        {
            var buffer = Interpolator.Buffer;

            var count = (transformLastSampleToo ? buffer.Count : buffer.Count - 1);

            for (var index = 0; index < count; index++)
            {
                var sample = buffer[index];

                buffer[index] = new Sample<Quaternion>(delta * sample.Value, sample.Stopped, sample.Time);
            }
        }

        public override void OnConnectedEntityChanged()
        {
            MarkAsReadyToSend();
        }

        internal override (bool, string) IsBindingValid()
        {
            bool isValid = unityComponent.transform.parent == null;
            string reason = string.Empty;

            if (!isValid)
            {
                reason = "World rotation binding shouldn't be in a child object.";
            }

            return (isValid, reason);
        }
    }
}
