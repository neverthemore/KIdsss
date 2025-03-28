// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

#nullable enable
namespace Coherence.Utils
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using Formatting = Newtonsoft.Json.Formatting;

    // Mirror of public static JsonConvert API, with the difference that this one has no static DefaultSettings property.
    // All the context as to why this class needs to exist and be used over the JsonConvert API is here: https://github.com/coherence/unity/issues/4522
    internal static class CoherenceJson
    {
        public static string SerializeObject(object? value) => SerializeObject(value, type: null, settings: null);

        [DebuggerStepThrough]
        public static string SerializeObject(object? value, Formatting formatting) => SerializeObject(value, formatting, settings: null);

        [DebuggerStepThrough]
        public static string SerializeObject(object? value, params JsonConverter[]? converters)
        {
            JsonSerializerSettings? serializerSettings;
            if (converters == null || converters.Length == 0)
            {
                serializerSettings = null;
            }
            else
            {
                serializerSettings = new JsonSerializerSettings()
                {
                    Converters = (IList<JsonConverter>) converters
                };
            }

            JsonSerializerSettings? settings = serializerSettings;
            return SerializeObject(value, null, settings);
        }

        [DebuggerStepThrough]
        public static string SerializeObject(object? value, Formatting formatting, params JsonConverter[]? converters)
        {
            JsonSerializerSettings? serializerSettings;
            if (converters == null || converters.Length == 0)
            {
                serializerSettings = null;
            }
            else
            {
                serializerSettings = new JsonSerializerSettings()
                {
                    Converters = (IList<JsonConverter>) converters
                };
            }

            JsonSerializerSettings? settings = serializerSettings;
            return SerializeObject(value, null, formatting, settings);
        }

        [DebuggerStepThrough]
        public static string SerializeObject(object? value, JsonSerializerSettings? settings) => SerializeObject(value, type: null, settings);

        [DebuggerStepThrough]
        public static string SerializeObject(object? value, Type? type, JsonSerializerSettings? settings)
        {
            JsonSerializer jsonSerializer = JsonSerializer.Create(settings);
            return SerializeObjectInternal(value, type, jsonSerializer);
        }

        [DebuggerStepThrough]
        public static string SerializeObject(object? value, Formatting formatting, JsonSerializerSettings? settings)
        {
            return SerializeObject(value, type: null, formatting, settings);
        }

        [DebuggerStepThrough]
        public static string SerializeObject(object? value, Type? type, Formatting formatting, JsonSerializerSettings? settings)
        {
            JsonSerializer jsonSerializer = JsonSerializer.Create(settings);
            jsonSerializer.Formatting = formatting;
            return SerializeObjectInternal(value, type, jsonSerializer);
        }

        private static string SerializeObjectInternal(object? value, Type? type, JsonSerializer jsonSerializer)
        {
            StringWriter stringWriter = new StringWriter(new StringBuilder(256), (IFormatProvider) CultureInfo.InvariantCulture);

            using (JsonTextWriter jsonTextWriter = new JsonTextWriter((TextWriter) stringWriter))
            {
                jsonTextWriter.Formatting = jsonSerializer.Formatting;
                jsonSerializer.Serialize((JsonWriter) jsonTextWriter, value, type);
            }

            return stringWriter.ToString();
        }

        [DebuggerStepThrough]
        public static object? DeserializeObject(string value) => DeserializeObject(value, type: null, settings: null);

        [DebuggerStepThrough]
        public static object? DeserializeObject(string value, JsonSerializerSettings settings) => DeserializeObject(value, type: null, settings);

        [DebuggerStepThrough]
        public static object? DeserializeObject(string value, Type type) => DeserializeObject(value, type, settings: null);

        [DebuggerStepThrough]
        public static T? DeserializeObject<T>(string value) => DeserializeObject<T>(value, settings: null);

        [DebuggerStepThrough]
        public static T? DeserializeAnonymousType<T>(string value, T anonymousTypeObject) => DeserializeObject<T>(value);

        [DebuggerStepThrough]
        public static T? DeserializeAnonymousType<T>(string value, T anonymousTypeObject, JsonSerializerSettings settings)
        {
            return DeserializeObject<T>(value, settings);
        }

        [DebuggerStepThrough]
        public static T? DeserializeObject<T>(string value, params JsonConverter[] converters) => (T?) DeserializeObject(value, typeof (T), converters);

        [DebuggerStepThrough]
        public static T? DeserializeObject<T>(string value, JsonSerializerSettings? settings) => (T?) DeserializeObject(value, typeof (T), settings);

        [DebuggerStepThrough]
        public static object? DeserializeObject(string value, Type type, params JsonConverter[]? converters)
        {
            JsonSerializerSettings? serializerSettings;

            if (converters == null || converters.Length == 0)
            {
                serializerSettings = null;
            }
            else
            {
                serializerSettings = new JsonSerializerSettings()
                {
                    Converters = (IList<JsonConverter>) converters
                };
            }

            JsonSerializerSettings? settings = serializerSettings;
            return DeserializeObject(value, type, settings);
        }

        public static object? DeserializeObject(string value, Type? type, JsonSerializerSettings? settings)
        {
            JsonSerializer jsonSerializer = JsonSerializer.Create(settings);
            using JsonTextReader reader = new JsonTextReader((TextReader) new StringReader(value));
            return jsonSerializer.Deserialize((JsonReader) reader, type);
        }
    }
}

