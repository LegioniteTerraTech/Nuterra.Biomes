using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuterra.World.PatchBatch;
using TerraTechETCUtil;
using UnityEngine;
using static Circuits;

namespace Nuterra.World.Biomes
{
    /// <summary>
    /// <para>This is the merged <b>Nuterra.Biomes.Bootstrapper</b> with <b>Nuterra.Biomes.Resources</b></para>
    /// Will be converting some of <see cref="ManModBiomes"/> to use 
    /// <see cref="TerraTechETCUtil.ResourcesHelper"/> which has support for 
    /// <see cref="AssetBundle"/>s from <see cref="ModBase"/>s
    /// </summary>
    public class ManModBiomes
    {
        public const string Tag = ".Biomes";
        public const string ModBiomesName = ManModWorld.ModName + Tag;
        internal static BiomeSelector selector;
        internal static BiomeExtractor extractor;
        private static bool Init = false;

        public static readonly string BiomesFolderPath = Path.Combine(Application.dataPath, "../Custom Biomes");
        public static readonly string BiomesExtension = ".biome.json";
        public static readonly string TerrainLayerExtension = ".layer.json";
        public static readonly string MapGeneratorExtension = ".generator.json";
        public static readonly string BiomeGroupsExtension = ".group.json";

        public static readonly DirectoryInfo BiomesFolder = Directory.CreateDirectory(BiomesFolderPath);

        internal static Dictionary<Type, Dictionary<string, UnityEngine.Object>> userResources = new Dictionary<Type, Dictionary<string, UnityEngine.Object>>();
        internal static Dictionary<Type, Dictionary<string, UnityEngine.Object>> gameResources = new Dictionary<Type, Dictionary<string, UnityEngine.Object>>();
        internal static List<BiomeWrapper> biomeWrappers = new List<BiomeWrapper>();

        internal static readonly string AssetsTag = "/Assets";
        internal static readonly string MetaTag = AssetsTag + "/Meta";
        internal static readonly string AudioTag = AssetsTag + "/Audio";
        internal static readonly string TexturesTag = AssetsTag + "/Textures";
        internal static readonly string TerrainLayersTag = AssetsTag + "/TerrainLayers";
        internal static readonly string MapGeneratorsTag = AssetsTag + "/MapGenerators";
        internal static readonly string BiomesTag = AssetsTag + "/Biomes";
        internal static readonly string BiomeGroupsTag = AssetsTag + "/BiomeGroups";

        internal static Type BiomeGroup_T = default;
        internal static FieldInfo[] BiomeGroup_fields = null;


        internal static void InsureSelector()
        {
            if (selector != null)
                return;
            var holder = new GameObject();
            extractor = holder.AddComponent<BiomeExtractor>();
            selector = holder.AddComponent<BiomeSelector>();
            selector.enabled = true;
            selector.useGUILayout = false;
            UnityEngine.Object.DontDestroyOnLoad(holder);
        }

        public static void Load()
        {
            LogAsset("Nuterra Biome Injector Library started");
            BiomeGroup_T = typeof(BiomeGroup);
            if (BiomeGroup_T == null)
                throw new NullReferenceException(nameof(BiomeGroup_T));
            BiomeGroup_fields = BiomeGroup_T?.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (BiomeGroup_fields == null)
                throw new NullReferenceException(nameof(BiomeGroup_fields));

            if (Init || MassPatcher.MassPatchAllWithin(ManModWorld.harmonyInst, typeof(BiomePatches), ManModWorld.ModName, true))
            {
                InsureSelector();

                if (!Init)
                {
                    InitiateMassLoading();
                    Init = true;
                }
            }
            else
                DebugWorld.LogError(Tag, "Rebuilding lookup FAILED! " + nameof(BiomePatches) + " didn't load!");
        }

        internal static string LogAsset(string value, string tag = "")
        {
            string outS = string.Format("[" + ModBiomesName + "{0}] {1}", tag, value);
            Console.WriteLine(outS);
            return outS;
        }

        internal static void LogError(string value, string tag = "")
        {
            string error = LogAsset("Error: " + value, tag);
            if (ManModWorld.DisplayErrorsToUser)
                ManModGUI.ShowErrorPopup(error, false);
        }

        internal static void LogFileError(FileInfo file, string e, string tag = "")
        {
            string error = LogAsset(string.Format("Error while loading \"{0}\" :\n{1}", Path.Combine(file.Directory.Name, file.Name), e), tag);
            if (ManModWorld.DisplayErrorsToUser)
                ManModGUI.ShowErrorPopup(error, false);
        }
        internal static void LogAssetBundleError(ModDataHandle handle, string e, string tag = "")
        {
            string error = LogAsset(string.Format("Error while loading \"{0}\" :\n{1}", handle.ModID, e), tag);
            if (ManModWorld.DisplayErrorsToUser)
                ManModGUI.ShowErrorPopup(error, false);
        }

        public static string StripComments(string input)
        {
            // JavaScriptSerializer doesn't accept commented-out JSON,
            // so we'll strip them out ourselves;
            // NOTE: for safety and simplicity, we only support comments on their own lines,
            // not sharing lines with real JSON
            input = Regex.Replace(input, @"^\s*//.*$", "", RegexOptions.Multiline);  // removes comments like this
            input = Regex.Replace(input, @"^\s*/\*(\s|\S)*?\*/\s*$", "", RegexOptions.Multiline); /* comments like this */

            return input;
        }

        public static void AddObjectToUserResources(Type type, UnityEngine.Object obj, string name)
        {
            if (!userResources.ContainsKey(type))
            {
                userResources.Add(type, new Dictionary<string, UnityEngine.Object>());
            }
            /*if (userResources[type].ContainsKey(name))
            {
                userResources[type][name] = obj;
            }
            else
            {*/
            userResources[type].Add(name, obj);
            //}

            LogAsset(string.Format("Added {0} {1}", type.Name, name), MetaTag);
        }

        public static void AddObjectToUserResources<T>(T obj, string name) where T : UnityEngine.Object
        {
            AddObjectToUserResources(typeof(T), obj, name);
        }

        public static bool UserResourcesContainsKey(Type type, string name)
        {
            return userResources.ContainsKey(type) && userResources[type].ContainsKey(name);
        }

        public static bool UserResourcesContainsKey<T>(string name)
        {
            return UserResourcesContainsKey(typeof(T), name);
        }

        public static T GetObjectFromUserResources<T>(string name) where T : UnityEngine.Object
        {
            return (T)GetObjectFromUserResources(typeof(T), name);
        }

        public static UnityEngine.Object GetObjectFromUserResources(Type type, string name)
        {
            if (userResources.TryGetValue(type, out var bucket) && bucket.TryGetValue(name, out var item))
            {
                return item;
            }

            return null;
        }

        public static UnityEngine.Object GetObjectFromGameResources(Type type, string name)
        {
            if (gameResources.TryGetValue(type, out var CacheLookup))
            {
                if (CacheLookup.TryGetValue(name, out var result))
                    return result;
            }
            else
            {
                gameResources.Add(type, new Dictionary<string, UnityEngine.Object>());
            }

            UnityEngine.Object searchresult = null;
            var search = UnityEngine.Resources.FindObjectsOfTypeAll(type);
            foreach (var item in search)
            {
                if (item.name == name)
                {
                    searchresult = item;
                    break;
                }
            }
            if (searchresult == null)
            {
                foreach (var item in search)
                {
                    if (item.name.StartsWith(name))
                    {
                        searchresult = item;
                        break;
                    }
                }
            }

            gameResources[type].Add(name, searchresult);
            return searchresult;
        }

        public static T GetObjectFromGameResources<T>(string name) where T : UnityEngine.Object
        {
            return (T)GetObjectFromGameResources(typeof(T), name);
        }

        public static UnityEngine.Object GetObjectFromResources(Type type, string name)
        {
            var res = GetObjectFromUserResources(type, name);

            if (!res)
            {
                res = GetObjectFromGameResources(type, name);
            }

            return res;
        }

        public static T GetObjectFromResources<T>(string name) where T : UnityEngine.Object
        {
            return (T)GetObjectFromResources(typeof(T), name);
        }

        internal static bool DoingReboot = false;
        public static void InitiateMassLoading()
        {
            if (DoingReboot)
                return;
            DoingReboot = true;
            ReadyTextures = false;
            ReadyTerrainLayers = false;
            ReadyMapGenerators = false;
            ReadyBiomes = false;
            ReadyBiomeGroups = false;
            InvokeHelper.InvokeCoroutine(LoadAllTextures());
        }

        private static bool ReadyTextures = false;
        internal static IEnumerator LoadAllTextures()
        {
            LogAsset("Loading PNGs from assetBundles...", TexturesTag);
            foreach (var item in ResourcesHelper.IterateAllModAssetsBundle<Texture2D>())
            {
                var name = item.Value?.name;
                if (name != null)
                {
                    if (!UserResourcesContainsKey<Texture>(name))
                    {
                        try
                        {
                            var tex = item.Value;

                            AddObjectToUserResources<Texture2D>(tex, name);
                            AddObjectToUserResources<Texture>(tex, name);
                        }
                        catch (Exception e)
                        {
                            LogAssetBundleError(item.Key, e.ToString(), TexturesTag);
                        }
                        yield return new WaitForEndOfFrame();
                    }
                    else
                    {
                        LogError(string.Format("Texture(assetBundle) \"{0}\" already exists!", name), TexturesTag);
                    }
                }
                else
                {
                    LogError(string.Format("Texture(assetBundle) name is NULL"), TexturesTag);
                }
            }

            var PNGs = BiomesFolder.GetFiles("*.png", SearchOption.AllDirectories);
            LogAsset("Loading PNGs from disk...", TexturesTag);
            foreach (var file in PNGs)
            {
                var name = file.Name;
                if (!UserResourcesContainsKey<Texture>(name))
                {
                    try
                    {
                        var tex = FileUtils.LoadTexture(file.FullName);

                        AddObjectToUserResources<Texture2D>(tex, name);
                        AddObjectToUserResources<Texture>(tex, name);
                    }
                    catch (Exception e)
                    {
                        LogFileError(file, e.ToString(), TexturesTag);
                    }
                    yield return new WaitForEndOfFrame();
                }
                else
                {
                    LogError(string.Format("Texture(file) \"{0}\" already exists!", name), TexturesTag);
                }
            }
            ReadyTextures = true;
            InvokeHelper.InvokeCoroutine(LoadAllTerrainLayers());
            yield break;
        }

        // TerraTechETCUtil does this now with FMOD.Sounds, and AudioClips no longer work due to how FMOD intrusively disables all AudioClips and forces crashes with them
        //  Should be relatively close, but it is now restricted to AssetBundled audio...
        /*
        public static IEnumerator LoadAllAudioClips()
        {
            var MP3s = BiomesFolder.GetFiles("*.mp3", SearchOption.AllDirectories);
            LogAsset("Loading MP3s", AudioTag);
            yield return LoadAudioClipsFromFiles(MP3s, AudioType.MPEG);

            var WAVs = BiomesFolder.GetFiles("*.wav", SearchOption.AllDirectories);
            LogAsset("Loading WAVs", AudioTag);
            yield return LoadAudioClipsFromFiles(WAVs, AudioType.WAV);

            yield break;
        }

        static IEnumerator LoadAudioClipsFromFiles(FileInfo[] files, AudioType type)
        {
            foreach (var file in files)
            {
                var name = file.Name;
                if (!UserResourcesContainsKey<AudioClip>(name))
                {
                    var path = Path.Combine("file://", file.FullName);
                    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(path, type))
                    {
                        yield return www.SendWebRequest();

                        try
                        {
                            if (www.isNetworkError)
                            {
                                LogFileError(file, www.error, AudioTag);
                            }
                            else
                            {
                                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                                AddObjectToUserResources(clip, name);
                            }
                        }
                        catch (Exception e)
                        {
                            LogFileError(file, e.ToString(), AudioTag);
                        }
                    }
                }
                else
                {
                    LogError(string.Format("Audio \"{0}\" already exists!", name), AudioTag);
                }
            }

            yield break;
        }
        */

        private static bool ReadyTerrainLayers = false;
        internal static IEnumerator LoadAllTerrainLayers()
        {
            var layers = BiomesFolder.GetFiles("*" + TerrainLayerExtension, SearchOption.AllDirectories);
            LogAsset("Loading TerrainLayers", TerrainLayersTag);

            foreach (var file in layers)
            {
                var fileName = file.Name;
                try
                {
                    var layerJSON = JObject.Parse(File.ReadAllText(file.FullName));

                    if (layerJSON["name"] != null)
                    {
                        var name = layerJSON["name"].ToString();
                        if (!UserResourcesContainsKey<TerrainLayer>(name))
                        {
                            var layer = layerJSON.ToObject<TerrainLayer>(JsonSerializer.CreateDefault(new JsonSerializerSettings()
                            {
                                Converters = {
                                    new JsonConverters.UnityObjectConverter<Texture>(),
                                },
                                MissingMemberHandling = MissingMemberHandling.Ignore
                            }));

                            AddObjectToUserResources(layer, name);
                        }
                        else
                        {
                            LogError(string.Format("TerrainLayer \"{0}\" already exists!", name), TerrainLayersTag);
                        }
                    }
                    else
                    {
                        LogError(string.Format("TerrainLayer in file \"{0}\" has no name!", fileName), TerrainLayersTag);
                    }
                }
                catch (Exception e)
                {
                    LogFileError(file, e.ToString(), TerrainLayersTag);
                }

                yield return new WaitForEndOfFrame();
            }

            ReadyTerrainLayers = true;
            LogAsset("TerrainLayers loaded", TerrainLayersTag);
            InvokeHelper.InvokeCoroutine(LoadAllMapGenerators());
            yield break;
        }

        private static bool ReadyMapGenerators = false;
        internal static IEnumerator LoadAllMapGenerators()
        {
            var m_Layers = typeof(MapGenerator).GetField("m_Layers", BindingFlags.Instance | BindingFlags.NonPublic);
            var generators = BiomesFolder.GetFiles("*" + MapGeneratorExtension, SearchOption.AllDirectories);
            LogAsset("Loading MapGenerators", MapGeneratorsTag);

            GameObject generators_holder = new GameObject();

            foreach (var file in generators)
            {
                var fileName = file.Name;
                try
                {
                    var generatorJSON = JObject.Parse(File.ReadAllText(file.FullName));

                    if (generatorJSON["name"] != null)
                    {
                        var name = generatorJSON["name"].ToString();
                        if (!UserResourcesContainsKey<MapGenerator>(name))
                        {
                            var generator_go = new GameObject(name);
                            generator_go.transform.SetParent(generators_holder.transform);
                            var generator_base = generator_go.AddComponent<MapGenerator>();
                            generator_base.name = name;
                            JsonConvert.PopulateObject(generatorJSON.ToString(), generator_base, new JsonSerializerSettings()
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                Converters = {
                                    new Newtonsoft.Json.Converters.StringEnumConverter()
                                    {
                                        AllowIntegerValues = true
                                    }
                                }
                            });

                            var layers = ((JArray)generatorJSON["m_Layers"]).ToObject<MapGenerator.Layer[]>();

                            m_Layers.SetValue(generator_base, layers);

                            AddObjectToUserResources(generator_base, name);
                        }
                        else
                        {
                            LogError(string.Format("MapGenerator \"{0}\" already exists!", name), MapGeneratorsTag);
                        }
                    }
                    else
                    {
                        LogError(string.Format("MapGenerator in file \"{0}\" has no name!", fileName), MapGeneratorsTag);
                    }
                }
                catch (Exception e)
                {
                    LogFileError(file, e.ToString(), MapGeneratorsTag);
                }

                yield return new WaitForEndOfFrame();
            }

            ReadyMapGenerators = true;
            LogAsset("MapGenerators loaded", MapGeneratorsTag);
            InvokeHelper.InvokeCoroutine(LoadAllBiomes());
            yield break;
        }

        private static bool ReadyBiomes = false;
        internal static IEnumerator LoadAllBiomes()
        {
            var Biome_T = typeof(Biome);
            var fields = Biome_T.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            var biomes = BiomesFolder.GetFiles("*" + BiomesExtension, SearchOption.AllDirectories);
            LogAsset("Loading Biomes", BiomesTag);

            var settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Converters = {
                    new JsonConverters.ScriptableObjectConverter(),
                    //new JsonConverters.UnityObjectConverter<AudioClip>(),
                    new JsonConverters.UnityObjectConverter<TerrainLayer>(),
                    new JsonConverters.UnityObjectConverter<MapGenerator>(),
                    new JsonConverters.UnityObjectConverter<TerrainObject>(),
                    new JsonConverters.PrefabGroupConverter(),
                    new Newtonsoft.Json.Converters.StringEnumConverter()
                    {
                        AllowIntegerValues = true
                    }
                }
            };

            var serializer = JsonSerializer.CreateDefault(settings);

            foreach (var file in biomes)
            {
                var fileName = file.Name;
                try
                {
                    var biomeJSON = JObject.Parse(File.ReadAllText(file.FullName));

                    if (biomeJSON["name"] != null)
                    {
                        var name = biomeJSON["name"].ToString();
                        if (!UserResourcesContainsKey<Biome>(name))
                        {
                            var biome = ScriptableObject.CreateInstance<Biome>();

                            if (biomeJSON["Reference"] != null)
                            {
                                var refName = biomeJSON["Reference"].ToString();
                                var reference = GetObjectFromGameResources<Biome>(refName);
                                if (!reference)
                                {
                                    LogError(string.Format("Biome reference \"{0}\" for Biome \"{1}\" doesn't exists!", refName, name), BiomesTag);
                                    continue;
                                }

                                biome = UnityEngine.Object.Instantiate(reference);
                            }

                            biome.name = name;

                            foreach (var field in fields)
                            {
                                try
                                {
                                    if (biomeJSON[field.Name] != null)
                                    {
                                        field.SetValue(biome, biomeJSON[field.Name].ToObject(field.FieldType, serializer));
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(field.Name);
                                    Console.WriteLine(e);
                                }
                            }

                            if (biomeJSON["BiomeGroupNames"] != null)
                            {
                                var names = biomeJSON["BiomeGroupNames"].ToObject<string[]>();
                                var weights = new float[0];
                                if (biomeJSON["BiomeWeights"] == null) {
                                    weights = new float[names.Length];
                                    for (int i = 0; i < weights.Length; i++)
                                    {
                                        weights[i] = 1f;
                                    }
                                }
                                else
                                {
                                    weights = biomeJSON["BiomeWeights"].ToObject<float[]>();
                                    var oldLength = weights.Length;
                                    Array.Resize(ref weights, names.Length);
                                    for (int i = oldLength; i < weights.Length; i++)
                                    {
                                        weights[i] = 1f;
                                    }
                                }
                                biomeWrappers.Add(new BiomeWrapper()
                                {
                                    biome = biome,
                                    biomeGroupNames = names,
                                    biomeWeights = weights
                                });
                            }

                            AddObjectToUserResources(biome, name);
                        }
                        else
                        {
                            LogError(string.Format("Biome \"{0}\" already exists!", name), BiomesTag);
                        }
                    }
                    else
                    {
                        LogError(string.Format("Biome in file \"{0}\" has no name!", fileName), BiomesTag);
                    }
                }
                catch (Exception e)
                {
                    LogFileError(file, e.ToString(), BiomesTag);
                }

                yield return new WaitForEndOfFrame();
            }
            ReadyBiomes = true;
            LogAsset("Biomes loaded", BiomesTag);
            InvokeHelper.InvokeCoroutine(LoadAllBiomeGroups());
        }

        private static bool ReadyBiomeGroups = false;
        internal static IEnumerator LoadAllBiomeGroups()
        {
            var biomeGroups = BiomesFolder.GetFiles("*" + BiomeGroupsExtension, SearchOption.AllDirectories);
            LogAsset("Loading BiomesGroups", BiomesTag);

            var settings = new JsonSerializerSettings()
            {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Converters = {
                    new JsonConverters.UnityObjectConverter<Biome>(),
                    new JsonConverters.AnimationCurveConverter()
                }
            };

            var serializer = JsonSerializer.CreateDefault(settings);

            foreach (var file in biomeGroups)
            {
                var fileName = file.Name;
                try
                {
                    var biomeGroupJSON = JObject.Parse(File.ReadAllText(file.FullName));

                    if (biomeGroupJSON["name"] != null)
                    {
                        var name = biomeGroupJSON["name"].ToString();
                        if (!UserResourcesContainsKey<BiomeGroup>(name))
                        {
                            var biomeGroup = ScriptableObject.CreateInstance<BiomeGroup>();
                            biomeGroup.name = name;

                            foreach (var field in BiomeGroup_fields)
                            {
                                try
                                {
                                    if (biomeGroupJSON[field.Name] != null)
                                    {
                                        field.SetValue(biomeGroup, biomeGroupJSON[field.Name].ToObject(field.FieldType, serializer));
                                    }
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine(field.Name);
                                    Console.WriteLine(e);
                                }
                            }

                            AddObjectToUserResources(biomeGroup, name);
                        }
                        else
                        {
                            LogError(string.Format("BiomeGroup \"{0}\" already exists!", name), BiomeGroupsTag);
                        }
                    }
                    else
                    {
                        LogError(string.Format("BiomeGroup in file \"{0}\" has no name!", fileName), BiomeGroupsTag);
                    }
                }
                catch (Exception e)
                {
                    LogFileError(file, e.ToString(), BiomeGroupsTag);
                }

                yield return new WaitForEndOfFrame();
            }
            ReadyBiomeGroups = true;
            LogAsset("BiomesGroups loaded", BiomeGroupsTag);
            InvokeHelper.InvokeCoroutine(EnsureLoadComplete());
        }


        private static IEnumerator EnsureLoadComplete()
        {
            LogAsset("EnsureLoadComplete");
            while (true)
            {
                if (ReadyTextures && ReadyTerrainLayers && ReadyMapGenerators &&
                    ReadyBiomes && ReadyBiomeGroups)
                {   // Good to go, START!!!
                    if (DoingReboot)
                    {
                        DoingReboot = false;
                        ManWorld.inst.CurrentBiomeMap?.InvalidateBiomeDB();
                        ManWorldTileExt.HostOnly_ReloadENTIREScene(true);
                        ManBiomeSave.inst.ForceReloadBiomesImmedeate();
                        yield return new WaitForEndOfFrame();
                        LogAsset("RELOADED BIOMES");
                    }
                    yield break;
                }
                yield return new WaitForEndOfFrame();
            }
        }

        internal static void FORCE_THIS_BIOME_LOADED_IMMEDEATE(Biome biome)
        {
            ToFORCELOAD2 = null;
            ToFORCELOAD = biome;
            ManWorld.inst.CurrentBiomeMap?.InvalidateBiomeDB();
            ManWorldTileExt.HostOnly_ReloadENTIREScene(true);
        }
        internal static void FORCE_THIS_BIOMEGROUP_LOADED_IMMEDEATE(BiomeGroup biomeGroup)
        {
            ToFORCELOAD = null;
            ToFORCELOAD2 = biomeGroup;
            ManWorld.inst.CurrentBiomeMap?.InvalidateBiomeDB();
            ManWorldTileExt.HostOnly_ReloadENTIREScene(true);
        }
        public static Biome ToFORCELOAD = null;
        public static BiomeGroup ToFORCELOAD2 = null;

    }
}
