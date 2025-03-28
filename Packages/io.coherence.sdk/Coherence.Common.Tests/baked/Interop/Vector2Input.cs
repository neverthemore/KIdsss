// Copyright (c) coherence ApS.
// For all coherence generated code, the coherence SDK license terms apply. See the license file in the coherence Package root folder for more information.

// <auto-generated>
// Generated file. DO NOT EDIT!
// </auto-generated>
namespace Coherence.Generated
{
    using Coherence.ProtocolDef;
    using Coherence.Serializer;
    using Coherence.Brook;
    using Coherence.Entities;
    using Coherence.Log;
    using Coherence.Core;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System;
    using UnityEngine;

    public struct Vector2Input : IEntityInput, IEquatable<Vector2Input>
    {
        [StructLayout(LayoutKind.Explicit)]
        public struct Interop
        {
            [FieldOffset(0)]
            public Vector2 vector2Field;
        }

        public static unsafe Vector2Input FromInterop(System.IntPtr data, System.Int32 dataSize)
        {
            if (dataSize != 8) {
                throw new System.Exception($"Given data size is not equal to the struct size. ({dataSize} != 8) " +
                    "for input with ID 4");
            }

            var orig = new Vector2Input();
            var comp = (Interop*)data;
            orig.vector2Field = comp->vector2Field;
            return orig;
        }

        public uint GetComponentType() => 4;

        public Entity Entity { get; set; }
        public Coherence.ChannelID ChannelID { get; set; }
        public MessageTarget Routing { get; set; }
        public uint Sender { get; set; }
        public long Frame { get; set; }
        private bool isRemoteInput;

        public Vector2 vector2Field;

        public Vector2Input(
        Entity entity,
        long frame,
        Vector2 vector2Field,
        bool isRemoteInput)
        {
            this.Entity = entity;
            this.ChannelID = Coherence.ChannelID.Default;
            this.Routing = MessageTarget.All;
            this.Sender = 0;
            this.Frame = frame;
            this.isRemoteInput = isRemoteInput;
            this.vector2Field = vector2Field;
        }

        public override string ToString()
        {
            return $"Entity: {Entity}, Frame: {Frame}, Inputs: [vector2Field:{vector2Field}]";
        }

        public IEntityMessage Clone()
        {
            // This is a struct, so we can safely return
            // a struct copy.
            return this;
        }

        public IEntityMapper.Error MapToAbsolute(IEntityMapper mapper, Coherence.Log.Logger logger)
        {
            var err = mapper.MapToAbsoluteEntity(Entity, false, out var absoluteEntity);
            if (err != IEntityMapper.Error.None)
            {
                return err;
            }
            Entity = absoluteEntity;
            return IEntityMapper.Error.None;
        }

        public IEntityMapper.Error MapToRelative(IEntityMapper mapper, Coherence.Log.Logger logger)
        {
            var err = mapper.MapToRelativeEntity(Entity, false, out var relativeEntity);
            if (err != IEntityMapper.Error.None)
            {
                return err;
            }
            Entity = relativeEntity;
            return IEntityMapper.Error.None;
        }

        public HashSet<Entity> GetEntityRefs() => default;

        public void NullEntityRefs(Entity entity) { }

        public bool Equals(Vector2Input other)
        {
            return
                this.vector2Field == other.vector2Field;
        }

        public static void Serialize(Vector2Input inputData, IOutProtocolBitStream bitStream)
        {
            var converted_vector2Field = inputData.vector2Field.ToCoreVector2();
            bitStream.WriteVector2(converted_vector2Field, FloatMeta.NoCompression());
        }

        public static Vector2Input Deserialize(IInProtocolBitStream bitStream, Entity entity, long frame)
        {
            var converted_vector2Field = bitStream.ReadVector2(FloatMeta.NoCompression());
            var datavector2Field = converted_vector2Field.ToUnityVector2();

            return new Vector2Input()
            {
                Entity = entity,
                Frame = frame,
                vector2Field = datavector2Field,
                isRemoteInput = true
            };
        }
    }

}
