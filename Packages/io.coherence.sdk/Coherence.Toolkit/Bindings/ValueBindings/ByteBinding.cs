namespace Coherence.Toolkit.Bindings.ValueBindings
{
    using System;
    using System.Reflection;
    using Entities;
    using UnityEngine;
    using Utils;

    public class ByteBinding : ValueBinding<byte>
    {
        public override string CoherenceComponentName => $"GenericFieldByte{genericPoolIndex}";

        protected ByteBinding() { }
        public ByteBinding(Descriptor descriptor, Component unityComponent) : base(descriptor, unityComponent)
        {
        }

        public override byte Value
        {
            get => (byte)GetValueUsingReflection();
            set => SetValueUsingReflection(value);
        }

        protected override bool DiffersFrom(byte first, byte second)
        {
            return first != second;
        }
    }
}
