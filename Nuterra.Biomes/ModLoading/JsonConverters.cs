using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Nuterra.World.Biomes;
using UnityEngine;

namespace Nuterra.World.JsonConverters
{
    public class BiomeConverter : UnityObjectConverter<Biome>
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Biome).IsAssignableFrom(objectType);
        }
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return base.ReadJson(reader, typeof(SuperBiome), existingValue, serializer);
        }
    }
    public class UnityObjectConverter<T> : JsonConverter where T : UnityEngine.Object
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return NuterraRes.GetObjectFromResources(objectType, reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = value as T;
            if (obj)
            {
                var name = JToken.FromObject(obj.name);
                name.WriteTo(writer);
            }
        }

        public override bool CanWrite => true;
        public override bool CanRead => true;
    }

    public class ScriptableObjectConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(ScriptableObject).IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var res = ScriptableObject.CreateInstance(objectType);

            var resJSON = JObject.ReadFrom(reader);

            var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                try
                {
                    if (resJSON[field.Name] != null)
                    {
                        field.SetValue(res, resJSON[field.Name].ToObject(field.FieldType, serializer));
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(field.Name);
                    Console.WriteLine(e);
                }
            }

            return res;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite => false;
        public override bool CanRead => true;
    }

    public class ArrayConverter<T> : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(T).MakeArrayType().IsAssignableFrom(objectType);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return JArray.FromObject(reader.Value).ToObject(typeof(T).MakeArrayType(), serializer);
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite => false;
        public override bool CanRead => true;
    }

    public class ColorConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Color) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            return JObject.ReadFrom(reader).ToObject<Color>(JsonSerializer.CreateDefault(new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore
            }));
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = new JObject();
            var color = (Color)value;
            obj.Add("r", color.r);
            obj.Add("g", color.g);
            obj.Add("b", color.b);
            obj.Add("a", color.a);

            obj.WriteTo(writer);
        }

        public override bool CanWrite => true;
        public override bool CanRead => true;
    }

    public class PrefabGroupConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(Biome.SceneryDistributor.PrefabGroup) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.ReadFrom(reader);
            var res = new Biome.SceneryDistributor.PrefabGroup();

            res.weightingTag = obj["weightingTag"].ToObject<string>();
            res.weightingTagHash = res.weightingTag.GetHashCode();
            res.terrainObject = obj["terrainObject"].ToObject<TerrainObject[]>(serializer ?? JsonSerializer.CreateDefault(new JsonSerializerSettings()
            {
                Converters = {
                    new UnityObjectConverter<TerrainObject>()
                }
            }));

            return res;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var obj = JObject.FromObject(value, JsonSerializer.CreateDefault(new JsonSerializerSettings()
            {
                Converters = {
                    new UnityObjectConverter<TerrainObject>()
                }
            }));
            obj.Remove("weightingTagHash");
            obj.WriteTo(writer);
        }

        public override bool CanWrite => true;
        public override bool CanRead => true;
    }

    public class AnimationCurveConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return typeof(AnimationCurve) == objectType;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            var obj = JObject.ReadFrom(reader);
            AnimationCurve res = null;

            if (obj["keys"] != null)
            {
                res = new AnimationCurve(obj["keys"].ToObject<Keyframe[]>());
            }
            else if (obj["constant"] != null)
            {
                var constant = obj["constant"];
                var timeStart = constant["timeStart"].ToObject<float>();
                var timeEnd = constant["timeEnd"].ToObject<float>();
                var value = constant["value"].ToObject<float>();
                res = AnimationCurve.Constant(timeStart, timeEnd, value);
            }
            else if (obj["linear"] != null)
            {
                var linear = obj["linear"];
                var timeStart = linear["timeStart"].ToObject<float>();
                var valueStart = linear["valueStart"].ToObject<float>();
                var timeEnd = linear["timeEnd"].ToObject<float>();
                var valueEnd = linear["valueEnd"].ToObject<float>();
                res = AnimationCurve.Linear(timeStart, valueStart, timeEnd, valueEnd);
            }
            else if (obj["easeInOut"] != null)
            {
                var easeInOut = obj["easeInOut"];
                var timeStart = easeInOut["timeStart"].ToObject<float>();
                var valueStart = easeInOut["valueStart"].ToObject<float>();
                var timeEnd = easeInOut["timeEnd"].ToObject<float>();
                var valueEnd = easeInOut["valueEnd"].ToObject<float>();
                res = AnimationCurve.EaseInOut(timeStart, valueStart, timeEnd, valueEnd);
            }

            return res;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }

        public override bool CanWrite => false;
        public override bool CanRead => true;
    }

    public class ContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
    {
        public Dictionary<string, List<string>> Members = new Dictionary<string, List<string>>();

        public ContractResolver() : base()
        {
            DefaultMembersSearchFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        }

        protected override List<MemberInfo> GetSerializableMembers(Type objectType)
        {
            List<MemberInfo> res = null;
            if (Members.TryGetValue(objectType.Name, out var memberNames))
            {
                res = objectType.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                    .Where(m => memberNames.Contains(m.Name))
                    .ToList();
            }
            else
            {
                var fields = objectType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(f => !f.IsAssembly).ToArray();
                if (fields.Length == 0)
                {
                    res = objectType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .Where(p => p.CanWrite)
                        .ToList<MemberInfo>();
                } 
                else
                {
                    res = fields.ToList<MemberInfo>();
                }
                
            }

            return res;
        }
    }
}
