namespace Coherence.Toolkit.Bindings.TransformBindings
{
    using Coherence.Entities;
    using Coherence.SimulationFrame;
    using Interpolation;
    using System;
    using UnityEngine;
    using ValueBindings;

    [Serializable]
    public class PositionBinding : Vector3Binding
    {
        public override string CoherenceComponentName => "WorldPosition";
        public override string MemberNameInComponentData => "value";
        public override string MemberNameInUnityComponent => nameof(CoherenceSync.coherencePosition);
        public override string BakedSyncScriptGetter => nameof(CoherenceSync.coherencePosition);
        public override string BakedSyncScriptSetter => nameof(CoherenceSync.coherencePosition);

        protected PositionBinding() { }

        public PositionBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent) { }

        public override Vector3 Value
        {
            get => coherenceSync.coherencePosition;
            set => coherenceSync.coherencePosition = value;
        }

        protected override (Vector3 value, AbsoluteSimulationFrame simFrame) ReadComponentData(ICoherenceComponentData coherenceComponent, Vector3 floatingOriginDelta)
        {
            var (position, simFrame) = base.ReadComponentData(coherenceComponent, floatingOriginDelta);

            if (!coherenceSync.HasParentWithCoherenceSync)
            {
                position += floatingOriginDelta;
            }

            return (position, simFrame);
        }

        public void ShiftSamples(Vector3 delta)
        {
            var buffer = Interpolator.Buffer;
            for (int index = 0; index < buffer.Count; index++)
            {
                var sample = buffer[index];
                buffer[index] = new Sample<Vector3>(sample.Value - delta, sample.Stopped, sample.Time);
            }
        }

        public void TransformSamples(Matrix4x4 transform, bool transformLastSampleToo)
        {
            var buffer = Interpolator.Buffer;

            var count = (transformLastSampleToo ? buffer.Count : buffer.Count - 1);

            for (var index = 0; index < count; index++)
            {
                var sample = buffer[index];
                buffer[index] = new Sample<Vector3>(transform.MultiplyPoint3x4(sample.Value), sample.Stopped, sample.Time);
            }
        }

        public override void OnConnectedEntityChanged()
        {
            MarkAsReadyToSend();
        }

        internal override (bool IsValid, string Reason) IsBindingValid()
        {
            var isValid = unityComponent.TryGetComponent(out CoherenceSync _);
            var reason = isValid ? string.Empty : "World position binding shouldn't be in a child object.";

            return (isValid, reason);
        }
    }
}
