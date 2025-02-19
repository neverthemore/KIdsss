namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System;
    using System.Reflection;
    using Entities;
    using UnityEngine;
    using Utils;

    public class UShortBinding : ValueBinding<ushort>
    {
        public override string CoherenceComponentName => $"GenericFieldUShort{genericPoolIndex}";

        protected UShortBinding() { }
        public UShortBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override ushort Value
        {
            get => (ushort)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(ushort first, ushort second)
        {
            return first != second;
        }
    }
}
