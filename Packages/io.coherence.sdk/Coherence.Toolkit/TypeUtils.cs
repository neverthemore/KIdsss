// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Toolkit
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using Bindings.ValueBindings;
    using Entities;
    using Serializer;
    using UnityEngine;
    using Object = UnityEngine.Object;

    internal static class TypeUtils
    {
        private class TypeData
        {
            public SchemaType schemaType;

            // how many members of this type we allow on a generic command?
            public int commandPoolSize;

            // how many members of this type we allow on generic/reflection mode?
            public int reflectedPoolSize;

            public Type bindingType;

            public string schemaComponentFieldName;

            private int commandCount;


            public bool IncrementCommandCount()
            {
                if (commandCount >= commandPoolSize)
                {
                    return false;
                }

                commandCount++;
                return true;
            }

            public void ResetCommandCount()
            {
                commandCount = 0;
            }
        }

        [Flags]
        public enum MethodCompatibility
        {
            None = 0,
            Reflected = 1,
            Baked = 2,
            Full = ~0,
        }

        private static Type[] obscuredTypes =
        {
            typeof(object),
            typeof(Component),
            typeof(MonoBehaviour),
            typeof(Object),
        };

        // For commands, the pool size is no longer an accurate account of how much data can be sent in a command.  Now, the number of parameters isn't
        // important, it's the size of the parameter and the total size of the command can not exceed one byte[] field which, at the time of writing,
        // is 511 bytes. See OutProtocolBitStream.BYTES_LIST_MAX_LENGTH
        // Except the Entities.  They are translated by the RS, so they are pooled.
        private static readonly int MaxEntityRefs = GenericNetworkCommandArgs.MAX_ENTITY_REFS;

        private static readonly Dictionary<SchemaType, TypeData> typeDataList = new Dictionary<SchemaType, TypeData>
        {
            [SchemaType.Bool] =
                new TypeData
                {
                    schemaType = SchemaType.Bool,
                    commandPoolSize = 999,
                    reflectedPoolSize = 10,
                    bindingType = typeof(BoolBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.Int8] =
                new TypeData
                {
                    schemaType = SchemaType.Int8,
                    commandPoolSize = 999,
                    reflectedPoolSize = 10,
                    bindingType = typeof(SByteBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.UInt8] =
                new TypeData
                {
                    schemaType = SchemaType.UInt8,
                    commandPoolSize = 999,
                    reflectedPoolSize = 10,
                    bindingType = typeof(ByteBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.Int16] =
                new TypeData
                {
                    schemaType = SchemaType.Int16,
                    commandPoolSize = 999,
                    reflectedPoolSize = 10,
                    bindingType = typeof(ShortBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.UInt16] =
                new TypeData
                {
                    schemaType = SchemaType.UInt16,
                    commandPoolSize = 999,
                    reflectedPoolSize = 10,
                    bindingType = typeof(UShortBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.Char] =
                new TypeData
                {
                    schemaType = SchemaType.Char,
                    commandPoolSize = 999,
                    reflectedPoolSize = 10,
                    bindingType = typeof(CharBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.Int] =
                new TypeData
                {
                    schemaType = SchemaType.Int,
                    commandPoolSize = 999,
                    reflectedPoolSize = 10,
                    bindingType = typeof(IntBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.UInt] =
                new TypeData
                {
                    schemaType = SchemaType.UInt,
                    commandPoolSize = 999,
                    reflectedPoolSize = 10,
                    bindingType = typeof(UIntBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.Float] =
                new TypeData
                {
                    schemaType = SchemaType.Float,
                    commandPoolSize = 999,
                    reflectedPoolSize = 10,
                    bindingType = typeof(FloatBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.String] =
                new TypeData
                {
                    schemaType = SchemaType.String,
                    commandPoolSize = 999,
                    reflectedPoolSize = 5,
                    bindingType = typeof(StringBinding),
                    schemaComponentFieldName = "name"
                },
            [SchemaType.Vector2] =
                new TypeData
                {
                    schemaType = SchemaType.Vector2,
                    commandPoolSize = 999,
                    reflectedPoolSize = 4,
                    bindingType = typeof(Vector2Binding),
                    schemaComponentFieldName = "value"
                },
            [SchemaType.Vector3] =
                new TypeData
                {
                    schemaType = SchemaType.Vector3,
                    commandPoolSize = 999,
                    reflectedPoolSize = 4,
                    bindingType = typeof(Vector3Binding),
                    schemaComponentFieldName = "value"
                },
            [SchemaType.Quaternion] =
                new TypeData
                {
                    schemaType = SchemaType.Quaternion,
                    commandPoolSize = 999,
                    reflectedPoolSize = 1,
                    bindingType = typeof(QuaternionBinding),
                    schemaComponentFieldName = "value"
                },
            [SchemaType.Entity] =
                new TypeData
                {
                    schemaType = SchemaType.Entity,
                    commandPoolSize = MaxEntityRefs,
                    reflectedPoolSize = 10,
                    bindingType = typeof(ReferenceBinding),
                    schemaComponentFieldName = "value"
                },
            [SchemaType.Bytes] =
                new TypeData
                {
                    schemaType = SchemaType.Bytes,
                    commandPoolSize = 999,
                    reflectedPoolSize = 1,
                    bindingType = typeof(ByteArrayBinding),
                    schemaComponentFieldName = "bytes"
                },
            [SchemaType.Int64] =
                new TypeData
                {
                    schemaType = SchemaType.Int64,
                    commandPoolSize = 999,
                    reflectedPoolSize = 4,
                    bindingType = typeof(LongBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.UInt64] =
                new TypeData
                {
                    schemaType = SchemaType.UInt64,
                    commandPoolSize = 999,
                    reflectedPoolSize = 4,
                    bindingType = typeof(ULongBinding),
                    schemaComponentFieldName = "number"
                },
            [SchemaType.Color] =
                new TypeData
                {
                    schemaType = SchemaType.Color,
                    commandPoolSize = 999,
                    reflectedPoolSize = 2,
                    bindingType = typeof(ColorBinding),
                    schemaComponentFieldName = "value"
                },
            [SchemaType.Float64] = new TypeData
            {
                schemaType = SchemaType.Float64,
                commandPoolSize = 999,
                reflectedPoolSize = 10,
                bindingType = typeof(DoubleBinding),
                schemaComponentFieldName = "number"
            },
        };

        private static readonly Dictionary<Type, TypeData> typeHash = new Dictionary<Type, TypeData>
        {
            [typeof(bool)] = typeDataList[SchemaType.Bool],
            [typeof(byte)] = typeDataList[SchemaType.UInt8],
            [typeof(sbyte)] = typeDataList[SchemaType.Int8],
            [typeof(short)] = typeDataList[SchemaType.Int16],
            [typeof(ushort)] = typeDataList[SchemaType.UInt16],
            [typeof(char)] = typeDataList[SchemaType.Char],
            [typeof(int)] = typeDataList[SchemaType.Int],
            [typeof(uint)] = typeDataList[SchemaType.UInt],
            [typeof(float)] = typeDataList[SchemaType.Float],
            [typeof(string)] = typeDataList[SchemaType.String],
            [typeof(Vector2)] = typeDataList[SchemaType.Vector2],
            [typeof(Vector3)] = typeDataList[SchemaType.Vector3],
            [typeof(Quaternion)] = typeDataList[SchemaType.Quaternion],
            [typeof(GameObject)] = typeDataList[SchemaType.Entity],
            [typeof(Transform)] = typeDataList[SchemaType.Entity],
            [typeof(RectTransform)] = typeDataList[SchemaType.Entity],
            [typeof(CoherenceSync)] = typeDataList[SchemaType.Entity],
            [typeof(Entity)] = typeDataList[SchemaType.Entity],
            [typeof(byte[])] = typeDataList[SchemaType.Bytes],
            [typeof(long)] = typeDataList[SchemaType.Int64],
            [typeof(ulong)] = typeDataList[SchemaType.UInt64],
            [typeof(Int64)] = typeDataList[SchemaType.Int64],
            [typeof(UInt64)] = typeDataList[SchemaType.UInt64],
            [typeof(Color)] = typeDataList[SchemaType.Color],
            [typeof(double)] = typeDataList[SchemaType.Float64],
        };

        private static readonly Dictionary<Type, string> niceTypeHash = new Dictionary<Type, string>
        {
            [typeof(byte)] = "byte",
            [typeof(sbyte)] = "sbyte",
            [typeof(short)] = "short",
            [typeof(ushort)] = "ushort",
            [typeof(char)] = "char",
            [typeof(int)] = "int",
            [typeof(uint)] = "uint",
            [typeof(long)] = "long",
            [typeof(ulong)] = "ulong",
            [typeof(float)] = "float",
            [typeof(double)] = "double",
            [typeof(bool)] = "bool",
            [typeof(string)] = "string",
            [typeof(object)] = "object",
            [typeof(void)] = "void",
        };

        private static readonly Dictionary<string, Type> schemaTypeToCSharp = new Dictionary<string, Type>
        {
            ["Vector3"] = typeof(Vector3),
            ["Vector2"] = typeof(Vector2),
            ["Quaternion"] = typeof(Quaternion),
            ["Int"] = typeof(int),
            ["Float"] = typeof(float),
            ["UInt"] = typeof(uint),
            ["Entity"] = typeof(Entity),
            ["String"] = typeof(string),
            ["Bool"] = typeof(bool),
            ["Int64"] = typeof(Int64),
            ["Bytes"] = typeof(byte[]),
            ["Float64"] = typeof(double),
            ["UInt64"] = typeof(UInt64),
            ["Color"] = typeof(Color),
            ["UInt8"] = typeof(byte),
            ["Int16"] = typeof(short),
            ["Int8"] = typeof(sbyte),
            ["UInt16"] = typeof(ushort),
            ["Char"] = typeof(char),
        };

        private static readonly Dictionary<SchemaType, int> schemaTypeToFieldSize = new Dictionary<SchemaType, int>
        {
            [SchemaType.Int] = 4,
            [SchemaType.Int64] = 8,
            [SchemaType.Bool] = 1,
            [SchemaType.Float] = 4,
            [SchemaType.String] = 16,
            [SchemaType.Vector2] = 4 * 2,
            [SchemaType.Vector3] = 4 * 3,
            [SchemaType.Quaternion] = 4 * 4,
            [SchemaType.Entity] = 4,
            [SchemaType.Bytes] = 16,
            [SchemaType.Color] = 4 * 4,
            [SchemaType.UInt] = 4,
            [SchemaType.UInt64] = 8,
            [SchemaType.Float64] = 8,
            [SchemaType.Int8] = 1,
            [SchemaType.UInt8] = 1,
            [SchemaType.Int16] = 2,
            [SchemaType.UInt16] = 2,
            [SchemaType.Char] = 4,
        };

        private static readonly Dictionary<string, Field.Type> schemaTypeToInputType =
            new Dictionary<string, Field.Type>
            {
                ["Vector3"] = Field.Type.Axis3D,
                ["Vector2"] = Field.Type.Axis2D,
                ["Quaternion"] = Field.Type.Rotation,
                ["Int"] = Field.Type.Integer,
                ["Float"] = Field.Type.Axis,
                ["Bool"] = Field.Type.Button,
                ["String"] = Field.Type.String,
            };

        private static readonly Dictionary<Field.Type, SchemaType> inputTypeToSchemaType =
            new Dictionary<Field.Type, SchemaType>
            {
                [Field.Type.Axis3D] = SchemaType.Vector3,
                [Field.Type.Axis2D] = SchemaType.Vector2,
                [Field.Type.Rotation] = SchemaType.Quaternion,
                [Field.Type.Integer] = SchemaType.Int,
                [Field.Type.Axis] = SchemaType.Float,
                [Field.Type.Button] = SchemaType.Bool,
                [Field.Type.String] = SchemaType.String,
            };

        private static readonly Regex oldValueParamRegex =
            new Regex("^([Oo]ld|[Ll]ast|[Pp]rev|[Oo]rig|[Rr]ecent)", RegexOptions.Compiled);

        private static readonly Regex newValueParamRegex =
            new Regex("^([Nn]ew|[Ff]resh|[Nn]ext|[Cc]urr)", RegexOptions.Compiled);


        public static int GetFieldOffsetForSchemaType(SchemaType type)
        {
            return schemaTypeToFieldSize.TryGetValue(type, out var fieldOffset) ? fieldOffset :
                throw new Exception("Unexpected field type : " + type);
        }

        public static Type GetCSharpTypeForSchemaType(string type)
        {
            return schemaTypeToCSharp.TryGetValue(type, out var typeData) ? typeData : null;
        }

        public static Field.Type GetInputTypeForSchemaType(string type)
        {
            return schemaTypeToInputType.TryGetValue(type, out var typeData) ? typeData : default;
        }

        public static SchemaType GetSchemaTypeForInputType(Field.Type type)
        {
            return inputTypeToSchemaType.TryGetValue(type, out var typeData) ? typeData : default;
        }

        public static string GetStringifiedBitMask(int bitMask)
        {
            return Convert.ToString(bitMask, 2).PadLeft(32, '0');
        }

        public static int GetReflectedTypePoolSize(Type t)
        {
            return typeHash.TryGetValue(t, out var typeData) ? typeData.reflectedPoolSize : 0;
        }

        public static string GetNiceTypeString(Type t)
        {
            if (t == null)
            {
                return "unknown";
            }

            return niceTypeHash.TryGetValue(t, out string nt) ? nt : t.Name;
        }

        public static MethodCompatibility GetMethodCompatibility(MethodInfo methodInfo, string componentName)
        {
            if (methodInfo.ReturnType != typeof(void))
            {
                return MethodCompatibility.None;
            }

            foreach (var td in typeDataList.Values)
            {
                td.ResetCommandCount();
            }

            MethodCompatibility mask = CheckLength(componentName, methodInfo.Name);

            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                if (parameterInfo.IsIn || parameterInfo.IsOut || parameterInfo.IsRetval)
                {
                    return MethodCompatibility.None;
                }

                if (!typeHash.TryGetValue(parameterInfo.ParameterType, out TypeData td))
                {
                    return MethodCompatibility.None;
                }

                if (!td.IncrementCommandCount())
                {
                    mask |= MethodCompatibility.Reflected;
                }
            }

            return MethodCompatibility.Full & ~mask;
        }

        private static MethodCompatibility CheckLength(string componentName, string methodName)
        {
            MethodCompatibility mask = 0;

            if (componentName.Length + methodName.Length + 1 > OutProtocolBitStream.SHORT_STRING_MAX_SIZE)
            {
                mask |= MethodCompatibility.Reflected;
            }

            return mask;
        }

        internal static BindingState IsMethodCompatible(MethodInfo methodInfo)
        {
            if (methodInfo.IsGenericMethod)
            {
                return BindingState.Incompatible;
            }

            if (methodInfo.ReturnType != typeof(void))
            {
                return BindingState.Incompatible;
            }

            foreach (var parameterInfo in methodInfo.GetParameters())
            {
                if (parameterInfo.IsIn || parameterInfo.IsOut || parameterInfo.IsRetval)
                {
                    return BindingState.Incompatible;
                }

                if (!IsTypeSupported(parameterInfo.ParameterType))
                {
                    return BindingState.Incompatible;
                }
            }

            return BindingState.Valid;
        }

        public static SchemaType GetSchemaType(Type t)
        {
            if (t == null)
            {
                return SchemaType.Unknown;
            }

            return typeHash.TryGetValue(t, out TypeData typeData) ? typeData.schemaType : SchemaType.Unknown;
        }

        public static bool IsObscuredType(Type t)
        {
            return Array.IndexOf(obscuredTypes, t) >= 0;
        }

        private static Dictionary<Type, bool> nonBindableTypeResultCache = new Dictionary<Type, bool>();

        public static bool IsNonBindableType(Type t)
        {
            if (!nonBindableTypeResultCache.TryGetValue(t, out var result))
            {
                result = Attribute.IsDefined(t, typeof(NonBindableAttribute));
                nonBindableTypeResultCache[t] = result;
            }

            return result;
        }

        public static bool IsTypeSupported(Type t)
        {
            return typeHash.ContainsKey(t);
        }

        private static Dictionary<string, string> tidyAssemblyTypeNames = new Dictionary<string, string>();

        internal static string TidyAssemblyTypeName(string assemblyTypeName)
        {
            if (string.IsNullOrEmpty(assemblyTypeName))
            {
                return assemblyTypeName;
            }

            if (tidyAssemblyTypeNames.TryGetValue(assemblyTypeName, out var cachedTypeName))
            {
                return cachedTypeName;
            }

            var originalAssemblyTypeName = assemblyTypeName;

            int min = int.MaxValue;
            int i = assemblyTypeName.IndexOf(", Version=");
            if (i != -1)
            {
                min = Math.Min(i, min);
            }

            i = assemblyTypeName.IndexOf(", Culture=");
            if (i != -1)
            {
                min = Math.Min(i, min);
            }

            i = assemblyTypeName.IndexOf(", PublicKeyToken=");
            if (i != -1)
            {
                min = Math.Min(i, min);
            }

            if (min != int.MaxValue)
            {
                assemblyTypeName = assemblyTypeName.Substring(0, min);
            }

            // Strip module assembly name.
            // The non-modular version will always work, due to type forwarders.
            i = assemblyTypeName.IndexOf(", UnityEngine.");
            if (i != -1 && assemblyTypeName.EndsWith("Module"))
            {
                assemblyTypeName = assemblyTypeName.Substring(0, i) + ", UnityEngine";
            }

            tidyAssemblyTypeNames[originalAssemblyTypeName] = assemblyTypeName;

            return assemblyTypeName;
        }

        // used by schema creator
        public static string NamespacedMethodName(MethodInfo methodInfo)
        {
            return $"{methodInfo.DeclaringType}.{methodInfo.Name}";
        }

        internal static string CommandName(Type targetScriptType, string methodName)
        {
            return $"{targetScriptType}.{methodName}";
        }

        internal static string CommandNameWithSignatureSuffix(string commandName, IReadOnlyCollection<Type> arguments,
            bool prettify = false)
        {
            return $"{commandName}{ObjectTypesAsString(arguments, prettify)}";
        }

        internal static readonly string CommandArgsPrefix = "(";
        internal static readonly string CommandArgsSuffix = ")";

        private static string ObjectTypesAsString(IReadOnlyCollection<Type> types, bool prettify = false)
        {
            var stringBuilder = new StringBuilder();
            _ = stringBuilder.Append(CommandArgsPrefix);

            foreach (var o in types)
            {
                if (o != types.First() && prettify)
                    _ = stringBuilder.Append(", ");
                _ = stringBuilder.Append(o.FullName);
            }

            _ = stringBuilder.Append(CommandArgsSuffix);

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Get the value of a field on a given object.
        /// </summary>
        /// <param name="obj">The object that owns the field</param>
        /// <param name="fieldName">The name of the field</param>
        /// <param name="flags">Binding flags</param>
        /// <typeparam name="T">The type of the field value</typeparam>
        /// <returns>The field's value or the type's default</returns>
        internal static T GetFieldValue<T>(object obj, string fieldName, BindingFlags flags)
        {
            var fieldValue = obj?.GetType().GetField(fieldName, flags)?.GetValue(obj);
            return fieldValue is T value ? value : default;
        }

        public static Type GetFieldOrPropertyType(MemberInfo memberInfo)
        {
            return memberInfo.MemberType switch
            {
                MemberTypes.Field => ((FieldInfo)memberInfo).FieldType,
                MemberTypes.Property => ((PropertyInfo)memberInfo).PropertyType,
                _ => throw new Exception("Unexpected member type: " + memberInfo.MemberType +
                                         ". Expected Field or Property.")
            };
        }

        public static bool IsValidBinding(this FieldInfo fi)
        {
            var isObsolete = fi.GetCustomAttribute<ObsoleteAttribute>() != null;

            return
                !IsObscuredType(fi.DeclaringType) &&
                IsTypeSupported(fi.FieldType) && !isObsolete;
        }

        public static bool IsValidBinding(this PropertyInfo pi)
        {
            var isObsolete = pi.GetCustomAttribute<ObsoleteAttribute>() != null;

            return
                pi.CanRead &&
                pi.CanWrite &&
                pi.GetMethod.IsPublic &&
                pi.SetMethod.IsPublic &&
                !IsObscuredType(pi.DeclaringType) &&
                IsTypeSupported(pi.PropertyType) &&
                !isObsolete;
        }

        private static bool IsValidBinding(this MethodInfo mi)
        {
            var isObsolete = mi.IsDefined(typeof(ObsoleteAttribute));

            return
                !mi.IsSpecialName &&
                !IsObscuredType(mi.DeclaringType) &&
                IsMethodCompatible(mi) == BindingState.Valid &&
                !isObsolete;
        }

        public static bool IsValidBinding(this MemberInfo memberInfo)
        {
            return memberInfo switch
            {
                FieldInfo fieldInfo => IsValidBinding(fieldInfo),
                MethodInfo methodInfo => IsValidBinding(methodInfo),
                PropertyInfo propertyInfo => IsValidBinding(propertyInfo),
                _ => false,
            };
        }

        public static BindingState GetBindingState(this MethodInfo mi)
        {
            if (mi.IsSpecialName)
            {
                return BindingState.SpecialName;
            }

            if (IsObscuredType(mi.DeclaringType))
            {
                return BindingState.UnsuppotedType;
            }

            if (IsMethodCompatible(mi) != BindingState.Valid)
            {
                return BindingState.Incompatible;
            }

            return mi.IsDefined(typeof(ObsoleteAttribute)) ? BindingState.Obsolete : BindingState.Valid;
        }

        private static readonly Dictionary<string, Type> memoizedTypeCache = new Dictionary<string, Type>();

        public static Type GetMemoizedType(string tidyAssemblyTypeName)
        {
            if (string.IsNullOrEmpty(tidyAssemblyTypeName))
            {
                return null;
            }

            if (!memoizedTypeCache.TryGetValue(tidyAssemblyTypeName, out var type))
            {
                type = Type.GetType(tidyAssemblyTypeName);
                memoizedTypeCache[tidyAssemblyTypeName] = type;
            }

            return type;
        }

        public static string CheckCallbackParameterOrder(MethodInfo callbackMethod)
        {
            ParameterInfo firstParam = callbackMethod.GetParameters()[0];
            ParameterInfo secondParam = callbackMethod.GetParameters()[1];

            string errorFormat =
                $"A part of the {{0}} parameter ({firstParam.Name}) of the '{callbackMethod.Name}' method indicates a possible ordering problem. " +
                "Please ensure the following parameter order: 'public void MyCallback({1} oldValue, {1} newValue)'. " +
                $"This error can be suppressed by setting the {nameof(OnValueSyncedAttribute)}.{nameof(OnValueSyncedAttribute.SuppressParamOrderError)} flag.";

            if (newValueParamRegex.IsMatch(firstParam.Name))
            {
                string paramTypeName = GetNiceTypeString(firstParam.ParameterType);
                return string.Format(errorFormat, "first", paramTypeName, paramTypeName);
            }

            if (oldValueParamRegex.IsMatch(secondParam.Name))
            {
                string paramTypeName = GetNiceTypeString(secondParam.ParameterType);
                return string.Format(errorFormat, "second", paramTypeName, paramTypeName);
            }

            return null;
        }

        public static bool IsBindingSupportingCallbacks(Type memberBindingType)
        {
            return memberBindingType != typeof(Entity) &&
                   memberBindingType != typeof(CoherenceScene);
        }

        public static bool CallbackHasValidSignature(MethodInfo methodInfo, Type memberBindingType)
        {
            var parameters = methodInfo.GetParameters();
            if (parameters.Length != 2)
            {
                return false;
            }

            return parameters[0].ParameterType == memberBindingType && parameters[1].ParameterType == memberBindingType;
        }

        public static bool IsUnsigned(Type type)
        {
            return type == typeof(ushort) ||
                   type == typeof(uint) ||
                   type == typeof(ulong) ||
                   type == typeof(byte) ||
                   type == typeof(char);
        }

        public static Type GetBindingType(Type fieldOrPropertyType)
        {
            return typeHash.TryGetValue(fieldOrPropertyType, out var typeData) ? typeData.bindingType : null;
        }

        public static string GetSchemaFieldName(Type fieldOrPropertyType)
        {
            return typeHash.TryGetValue(fieldOrPropertyType, out var typeData)
                ? typeData.schemaComponentFieldName
                : null;
        }
    }
}
