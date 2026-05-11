using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuterra.ModLoading;
using Nuterra.World.PatchBatch;
using RandomAdditions;
using TerraTechETCUtil;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Circuits;
using static MapGenerator;

namespace Nuterra.World.Biomes
{
    /// <summary>
    /// <para>This is the merged <b>Nuterra.Biomes.Bootstrapper</b> with <b>Nuterra.Biomes.Resources</b></para>
    /// Will be converting some of <see cref="ManModBiomes"/> to use 
    /// <see cref="TerraTechETCUtil.ResourcesHelper"/> which has support for 
    /// <see cref="AssetBundle"/>s from <see cref="ModBase"/>s
    /// </summary>
    public class ManModBiomes : IModPreloadable
    {
        public const string Tag = ".Biomes";
        public const string ModBiomesName = ManModWorld.ModLogName + Tag;
        internal static BiomeSelector selector;
        internal static BiomeExtractor extractor;
        private static bool Init = false;

        private static bool DevMode => ManModWorld.DevMode;

        /// <summary>
        /// Only exists for the sole purpose of preloading here.
        /// </summary>
        public ManModBiomes() { }


        ModDataHandle IModPreloadable.ModHandle => KickStartWorld.oInst;
        bool IModPreloadable.ChainFail => false;
        void IModPreloadable.OnFail() { }
        public string Subject => "Injecting modded biomes - ";
        string IModPreloadable.InProgress => InProgress;
        float IModPreloadable.EstPercentDone => EstPercentDone;
        int IModPreloadable.EstNumSteps => EstNumSteps;
        int IModPreloadable.EstNumStepsIterator => EstNumStepsIterator;
        public IEnumerator GetEnumerator() => Load(false);


        public static bool TrySuperCompress = false;
        public static Stopwatch profiling = new Stopwatch();

        public static readonly string BiomesFolderPath = Path.Combine(Application.dataPath, "../Custom Biomes");
        public static readonly string BiomesExtension = ".biome.json";
        public static readonly string ABPrefixBiomes = "Bio_";
        public static readonly string TerrainLayerExtension = ".layer.json";
        public static readonly string ABPrefixLayer = "Lay_";
        public static readonly string MapGeneratorExtension = ".generator.json";
        public static readonly string ABPrefixGenerator = "Gen_";
        public static readonly string BiomeGroupsExtension = ".group.json";
        public static readonly string ABPrefixGroup = "Gro_";

        public static readonly DirectoryInfo BiomesFolder = Directory.CreateDirectory(BiomesFolderPath);

        internal static HashSet<Biome> DarkInDayBiomes = new HashSet<Biome>();
        internal static Dictionary<Biome, string> BiomesToMods = new Dictionary<Biome, string>();

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
        private static FieldInfo[] BiomeGroup_fields = null;
        public static FieldInfo[] GetBiomeGroupFields()
        {
            if (BiomeGroup_fields != null)
                return BiomeGroup_fields;
            BiomeGroup_T = typeof(BiomeGroup);
            if (BiomeGroup_T == null)
                throw new NullReferenceException(nameof(BiomeGroup_T));
            BiomeGroup_fields = BiomeGroup_T?.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (BiomeGroup_fields == null)
                throw new NullReferenceException(nameof(BiomeGroup_fields));
            return BiomeGroup_fields;
        }


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

        private static void FirstLoad()
        {
            WikiPageBiome.GetBiomeData = GetBiomeJSON;
            WikiPageBiome.GetBiomeModName = GetBiomeMod;
            WikiPageBiome.GetBiomeName = GetBiomeDisplayName;
            WikiPageBiome.GetBiomeDescription = GetBiomeDesc;
        }
        private static string GetBiomeJSON(Biome biomeInst)
        {
            return BiomeExtractor.ExtractBiome(biomeInst);
        }
        private static string GetBiomeMod(Biome biomeInst)
        {
            if (BiomesToMods.TryGetValue(biomeInst, out var modID))
                return modID;
            return WikiPageBiome.GetBiomeModNameDefault(biomeInst);
        }
        private static string GetBiomeDisplayName(Biome biomeInst)
        {
            if (BiomesToMods.ContainsKey(biomeInst))
            {
                if (biomeInst is SuperBiome SB)
                    return SB.DisplayName;
                return biomeInst.name;
            }
            return WikiPageBiome.GetBiomeNameDefault(biomeInst);
        }
        private static string GetBiomeDesc(Biome biomeInst)
        {
            if (biomeInst is SuperBiome SB)
                return SB.Description;
            return WikiPageBiome.GetBiomeDescriptionDefault(biomeInst);
        }
        public static IEnumerator Load(bool reloading)
        {
            NuterraRes.LogAsset("Nuterra Biome Injector Library started");
            GetBiomeGroupFields();

            if (Init || MassPatcher.MassPatchAllWithin(ManModWorld.harmonyInst, typeof(BiomePatches), ManModWorld.ModLogName, true))
            {
                InsureSelector();

                if (!Init)
                {
                    FirstLoad();
                    Init = true;
                    ManGameMode.inst.ModeFinishedEvent.Subscribe(OnModeSwitch);
                }

                NuterraRes.LogAsset("Begin loading...");
                foreach (var item in InitiateMassLoadingAsync(reloading))
                    yield return item;
                NuterraRes.LogAsset("Finished loading.");
            }
            else
                DebugWorld.Log(Tag, "Rebuilding lookup FAILED! " + nameof(BiomePatches) + " didn't load!");
        }

        private static void OnModeSwitch(Mode _)
        {
            defaultMap = null;
            LastOverrideBiome = null;
            LastOverrideGroup = null;
            nextMap = null;
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



        // ---------------------------------  CREATION  ---------------------------------
        public static string InProgress = null;
        public static float EstPercentDone = 1f;
        public static int EstNumSteps = 1;
        public static int EstNumStepsIterator = 0;
        internal static bool DoingReboot = false;
        internal static Biome FallbackBiome = null;
        public static void InitiateMassLoading()
        {
            InvokeHelper.InvokeCoroutine(InitiateMassLoadingAsync(true).GetEnumerator());
        }
        internal static IEnumerable InitiateMassLoadingAsync(bool reloading)
        {
            if (DoingReboot)
            {
                NuterraRes.LogError("InitiateMassLoadingAsync() was called whilist an operation was already in progress!?! IGNORING.");
                yield break;
            }
            NuterraRes.Dependants.Clear();
            EstNumSteps = 1;
            EstPercentDone = 0f;
            EstNumStepsIterator = 0;
            DoingReboot = true;
            selector.Clear();
            ReadyTextures = false;
            ReadyTerrainLayers = false;
            ReadyMapGenerators = false;
            ReadyBiomes = false;
            ReadyBiomeGroups = false;
            if (FallbackBiome == null)
                FallbackBiome = ManWorld.inst.CurrentBiomeMap.LookupBiome(0);
            foreach (var item in LoadAllTextures(reloading))
            {
                EstPercentDone = EstNumStepsIterator / (float)EstNumSteps;
                yield return item;
            }
            EstPercentDone = 0f;
            EstNumStepsIterator = 0;
            foreach (var item in LoadAllTerrainLayers(reloading))
            {
                ManWorld.inst.TileManager.PauseGenerationOneFrame();
                EstPercentDone = EstNumStepsIterator / (float)EstNumSteps;
                yield return item;
            }
            EstPercentDone = 0f;
            EstNumStepsIterator = 0;
            foreach (var item in LoadAllMapGenerators(reloading))
            {
                ManWorld.inst.TileManager.PauseGenerationOneFrame();
                EstPercentDone = EstNumStepsIterator / (float)EstNumSteps;
                yield return item;
            }
            EstPercentDone = 0f;
            EstNumStepsIterator = 0;
            foreach (var item in LoadAllBiomes(reloading))
            {
                ManWorld.inst.TileManager.PauseGenerationOneFrame();
                EstPercentDone = EstNumStepsIterator / (float)EstNumSteps;
                yield return item;
            }
            EstPercentDone = 0f;
            EstNumStepsIterator = 0;
            foreach (var item in LoadAllBiomeGroups(reloading))
            {
                ManWorld.inst.TileManager.PauseGenerationOneFrame();
                EstPercentDone = EstNumStepsIterator / (float)EstNumSteps;
                yield return item;
            }
            EstPercentDone = 0f;
            EstNumStepsIterator = 0;
            foreach (var item in EnsureLoadComplete(reloading))
                yield return item;
        }


        /// <summary>
        /// The callback to register for the managed <see cref="T"/> via script.
        /// </summary>
        public abstract class CreateRequest<T> where T : UnityEngine.Object
        {
            /// <summary>
            /// Only returns true AFTER the first, initial load
            /// </summary>
            public readonly bool IsReloading;
            internal CreateRequest(bool reloading)
            {
                IsReloading = reloading;
            }

            /// <summary>
            /// Create a <see cref="T"/> from <see cref="JObject"/> JSON
            /// <para>This does not register it. To do that call <see cref="RequestToCreate(ModContainer, T)"/></para>
            /// </summary>
            /// <param name="getJSON"><see cref="JObject"/> to create it from</param>
            /// <param name="name">name/ID this should register under. <b>AVOID CHANGING THIS</b></param>
            /// <returns></returns>
            public abstract T CreateFromJSON(string ModID, JObject getJSON, string name);
            /// <summary>
            /// <para>Loads <typeparamref name="T"/> instance from given class to the game.</para>
            /// <para>Will not replace already active instances of it.  You will need to call <see cref="InitiateMassLoading"/> to rebuild it all.</para>
            /// <inheritdoc cref="NuterraRes.AddObjectToUserResources(Type, UnityEngine.Object, string, FileInfo)"/>
            /// </summary>
            /// <param name="Mod">Mod adding this</param>
            /// <param name="Data">To add</param>
            /// <returns>True if we just added or altered it</returns>
            public void RequestToCreate(string ModID, T Data) => RequestToCreateExt(ModID, Data);
            /// <summary>
            /// <para>Removes <typeparamref name="T"/> instance from given class from the game.</para>
            /// <para>Will not replace already active instances of it.  You will need to call <see cref="InitiateMassLoading"/> to rebuild it all.</para>
            /// <inheritdoc cref="NuterraRes.RemoveObjectFromUserResources(Type, UnityEngine.Object, string, FileInfo)"/>
            /// </summary>
            /// <param name="Mod">Mod removing this</param>
            /// <param name="Data">To remove</param>
            /// <returns>True if we just removed it</returns>
            public void CancelCreateRequest(string ModID, T Data) => CancelCreateRequestExt(ModID, Data);
        }
        private static bool RequestToCreateExt<T>(string ModID, T inst) where T : UnityEngine.Object
        {
            if (inst == null)
                throw new ArgumentNullException(nameof(inst));
            if (inst.name == null)
                throw new NullReferenceException(nameof(inst.name));
            return NuterraRes.AlterObjectInUserResources(inst, inst.name, null);
        }
        private static bool CancelCreateRequestExt<T>(string ModID, T inst) where T : UnityEngine.Object
        {
            if (inst == null)
                throw new ArgumentNullException(nameof(inst));
            if (inst.name == null)
                throw new NullReferenceException(nameof(inst.name));
            return NuterraRes.RemoveObjectFromUserResources(inst, inst.name, null);
        }
        private static IEnumerable CreateFromScripts<T, V>(bool reload, Event<V> eventSender,
            V callback) where T : UnityEngine.Object where V : CreateRequest<T>
        {
            eventSender.Send(callback);
            int stepp = 0;
            EstNumSteps = CreationPending.Count;
            foreach (var item in CreationPending)
            {
                if (item?.Method?.Name == null)
                    continue;
                InProgress = item.Method.Name;
                if (stepp > ManModWorld.IterateExtraRate)
                {
                    stepp = 0;
                    yield return new WaitForEndOfFrame();
                }
                try
                {
                    item();
                }
                catch (Exception e)
                {
                    NuterraRes.LogError("Unhandled exception whilist creating scripts for " +
                        typeof(T).Name + ": " + e, Tag);
                }
                EstNumStepsIterator++;
            }
            CreationPending.Clear();
            InProgress = null;
        }
        private static List<Action> CreationPending = new List<Action>();
        /// <summary>
        /// Loads <typeparamref name="A"/> instance from given class to the game
        /// </summary>
        /// <param name="Mod"></param>
        /// <param name="ID"></param>
        /// <param name="Data"></param>
        public static void RegisterToCreate(Action Data)
        {
            CreationPending.Add(Data);
        }



        private static bool ReadyTextures = false;
        internal static IEnumerable LoadAllTextures(bool reloading)
        {
            profiling.Start();
            NuterraRes.LogAsset("Loading PNGs from assetBundles...", TexturesTag);
            // assetBundle is FAST
            EstNumSteps = NuterraRes.EstTime<Texture2D>(false, string.Empty, BiomesFolder, "*.png", out var PNGs, out var fst);
            InProgress = nameof(Texture2D) + " AssetBundles";
            foreach (var item in ResourcesHelper.IterateAllModAssetsBundle<Texture2D>())
            {
                var name = item.Value?.name;
                if (name != null)
                {
                    if (!NuterraRes.UserResourcesContainsKey<Texture>(name))
                    {
                        try
                        {
                            var tex = item.Value;
                            NuterraRes.AddObjectToUserResources<Texture2D>(tex, name, null);
                            NuterraRes.AddObjectToUserResources<Texture>(tex, name, null);
                            EstNumStepsIterator++;
                        }
                        catch (Exception e)
                        {
                            NuterraRes.LogAssetBundleError(item.Key, e.ToString(), TexturesTag);
                        }
                    }
                    else if (NuterraRes.InitialLoad)
                    {
                        NuterraRes.LogError(string.Format("Texture(assetBundle) \"{0}\" already exists!", name), TexturesTag);
                    }
                }
                else
                {
                    NuterraRes.LogError(string.Format("Texture(assetBundle) name is NULL"), TexturesTag);
                }
            }
            yield return new WaitForEndOfFrame();

            NuterraRes.LogAsset("Loading PNGs from disk...", TexturesTag);
            foreach (var file in PNGs)
            {
                var name = file.Name;
                if (NuterraRes.UserResourcesCanUpdate<Texture>(file))
                {
                    InProgress = nameof(Texture2D) + " " + name;
                    yield return new WaitForEndOfFrame();
                    bool did = false;
                    try
                    {
                        if (!NuterraRes.InitialLoad || !NuterraRes.UserResourcesContainsKey<Texture>(name))
                        {
                            try
                            {
                                var destroyTarg = NuterraRes.GetObjectFromUserResources<Texture>(name);
                                var tex = FileUtils.LoadTexture(file.FullName);
                                if (tex != null && destroyTarg != null)
                                    UnityEngine.Object.Destroy(destroyTarg);

                                NuterraRes.AlterObjectInUserResources<Texture2D>(tex, name, file);
                                NuterraRes.AlterObjectInUserResources<Texture>(tex, name, file);
                                did = true;
                            }
                            catch (Exception e)
                            {
                                NuterraRes.LogFileError(file, e.ToString(), TexturesTag);
                            }
                        }
                        else if (NuterraRes.InitialLoad)
                        {
                            NuterraRes.LogError(string.Format("Texture(file) \"{0}\" already exists!", name), TexturesTag);
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogFileError(file, e.ToString(), TexturesTag);
                    }
                    if (did)
                    {
                        EstNumStepsIterator++;
                        yield return null;
                    }
                }
            }
            ReadyTextures = true;
            profiling.Stop();
            NuterraRes.LogAsset("Time for " + nameof(Texture2D) + " took " + profiling.ElapsedMilliseconds.ToString() + "ms");
            profiling.Reset();
            yield break;
        }

        // TerraTechETCUtil does this now with FMOD.Sounds, and AudioClips no longer work due to how FMOD intrusively disables all AudioClips and forces crashes with them
        //  Should be relatively close, but it is now restricted to AssetBundled audio...
        /*
        public static IEnumerator LoadAllAudioClips()
        {
            var MP3s = BiomesFolder.GetFiles("*.mp3", SearchOption.AllDirectories);
            NuterraRes.LogAsset("Loading MP3s", AudioTag);
            yield return LoadAudioClipsFromFiles(MP3s, AudioType.MPEG);

            var WAVs = BiomesFolder.GetFiles("*.wav", SearchOption.AllDirectories);
            NuterraRes.LogAsset("Loading WAVs", AudioTag);
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
                                 NuterraRes.LogFileError(file, www.error, AudioTag);
                            }
                            else
                            {
                                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                                AddObjectToUserResources(clip, name);
                            }
                        }
                        catch (Exception e)
                        {
                             NuterraRes.LogFileError(file, e.ToString(), AudioTag);
                        }
                    }
                }
                else
                {
                    NuterraRes.LogError(string.Format("Audio \"{0}\" already exists!", name), AudioTag);
                }
            }

            yield break;
        }
        */


        private static bool ReadyTerrainLayers = false;
        /// <summary>
        /// Sent when this is about to create the script-managed <see cref="TerrainLayer"/> instances.
        /// <para>Call <see cref="CreateRequest{TerrainLayer}.RequestToCreate(ModContainer, TerrainLayer)"/> to register within the subscribing method.</para>
        /// <para><see cref="CreateRequest{TerrainLayer}.IsReloading"/> returns <b>true</b> if this is a reload request</para>
        /// </summary>
        public static Event<CreateRequestTerrainLayer> ScriptCreateTerrainLayerEvent = new Event<CreateRequestTerrainLayer>();
        public class CreateRequestTerrainLayer : CreateRequest<TerrainLayer>
        {
            internal CreateRequestTerrainLayer(bool reloading) :
                base(reloading)
            { }
            /// <summary>
            /// Create a <see cref="TerrainLayer"/> from <see cref="JObject"/> JSON
            /// </summary>
            /// <param name="layerJSON"></param>
            /// <param name="name"></param>
            /// <returns></returns>
            public override TerrainLayer CreateFromJSON(string ModID, JObject layerJSON, string name) => 
                BuildTerrainLayer(layerJSON, name, IsReloading);
        }
        private static TerrainLayer BuildTerrainLayer(JObject layerJSON, string name, bool reloading)
        {
            TerrainLayer terrainLayer = NuterraRes.GetObjectFromUserResources<TerrainLayer>(name);
            if (terrainLayer == null)
                terrainLayer = new TerrainLayer();
            if (!reloading)
                NuterraRes.BeginOrResetTrackingHierachy(typeof(TerrainLayer), name);
            else
                NuterraRes.BuildRequestReloads(typeof(TerrainLayer), name);
            JsonConvert.PopulateObject(layerJSON.ToString(), terrainLayer, new JsonSerializerSettings()
            {
                Converters = {
                                new JsonConverters.UnityObjectConverter<Texture>(),
                            },
                MissingMemberHandling = MissingMemberHandling.Ignore
            });
            if (!reloading)
                NuterraRes.StopTrackingHierachy();
            return terrainLayer;
        }
        internal static IEnumerable LoadAllTerrainLayers(bool reloading)
        {
            profiling.Start();
            NuterraRes.LogAsset("Loading TerrainLayers from assetBundles...", TerrainLayersTag);
            // assetBundle is FAST
            EstNumSteps = NuterraRes.EstTime<TextAsset>(TrySuperCompress, ABPrefixLayer, BiomesFolder, "*" + TerrainLayerExtension, out var layers, out var fst);
            InProgress = nameof(TerrainLayer) + " AssetBundles";
            foreach (var item in ResourcesHelper.IterateAllModAssetsBundle<TextAsset>((text) => {
                return text?.name != null && text.name.StartsWith(ABPrefixLayer);
            }))
            {
                var assetName = item.Value?.name;
                if (assetName != null)
                {
                    try
                    {
                        var layerJSON = JObject.Parse(item.Value.text);

                        if (layerJSON["name"] != null)
                        {
                            var name = layerJSON["name"].ToString();
                            if (!NuterraRes.UserResourcesContainsKey<TerrainLayer>(name))
                            {
                                var layer = BuildTerrainLayer(layerJSON, name, false);
                                if (layer != null)
                                    NuterraRes.AddObjectToUserResources(layer, name, null);
                                EstNumStepsIterator++;
                            }
                            else
                                NuterraRes.LogError(string.Format("TerrainLayer(assetBundle) \"{0}\" already exists!", assetName), TerrainLayersTag);
                        }
                        else
                        {
                            NuterraRes.LogError(string.Format("TerrainLayer(assetBundle) in file \"{0}\" has no name!", assetName), TerrainLayersTag);
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogAssetBundleError(item.Key, e.ToString(), TerrainLayersTag);
                    }
                }
                else
                {
                    NuterraRes.LogError(string.Format("TerrainLayer(assetBundle) name is NULL"), TerrainLayersTag);
                }
            }
            yield return new WaitForEndOfFrame();

            NuterraRes.LogAsset("Loading TerrainLayers", TerrainLayersTag);

            foreach (var file in layers)
            {
                var fileName = file.Name;
                if (NuterraRes.UserResourcesCanUpdate<TerrainLayer>(file))
                {
                    InProgress = nameof(TerrainLayer) + " " + fileName;
                    yield return new WaitForEndOfFrame();
                    bool did = false;
                    try
                    {
                        using (FileStream FS = new FileStream(file.FullName, FileMode.Open))
                        {
                            using (StreamReader sr = new StreamReader(FS))
                            {
                                using (JsonReader reader = new JsonTextReader(sr))
                                {
                                    var layerJSON = JObject.Load(reader);

                                    if (layerJSON["name"] != null)
                                    {
                                        var name = layerJSON["name"].ToString();
                                        if (!NuterraRes.InitialLoad || !NuterraRes.UserResourcesContainsKey<TerrainLayer>(name))
                                        {
                                            var layer = BuildTerrainLayer(layerJSON, name, reloading);
                                            if (layer != null)
                                                NuterraRes.AlterObjectInUserResources(layer, name, file);
                                            did = true;
                                        }
                                        else if (NuterraRes.InitialLoad)
                                        {
                                            NuterraRes.LogError(string.Format("TerrainLayer \"{0}\" already exists!", name), TerrainLayersTag);
                                        }
                                    }
                                    else
                                    {
                                        NuterraRes.LogError(string.Format("TerrainLayer in file \"{0}\" has no name!", fileName), TerrainLayersTag);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogFileError(file, e.ToString(), TerrainLayersTag);
                    }
                    if (did)
                    {
                        EstNumStepsIterator++;
                        yield return null;
                    }
                }
            }
            foreach (var file in fst)
            {
                var fileName = file.Name;
                if (NuterraRes.UserResourcesCanUpdate<TerrainLayer>(file))
                {
                    InProgress = nameof(TerrainLayer) + " " + fileName;
                    yield return new WaitForEndOfFrame();
                    bool did = false;
                    try
                    {
                        using (FileStream FS = new FileStream(file.FullName, FileMode.Open))
                        {
                            using (GZipStream zs = new GZipStream(FS, CompressionMode.Decompress))
                            {
                                using (StreamReader sr = new StreamReader(zs))
                                {
                                    using (JsonReader reader = new JsonTextReader(sr))
                                    {
                                        var layerJSON = JObject.Load(reader);

                                        if (layerJSON["name"] != null)
                                        {
                                            var name = layerJSON["name"].ToString();
                                            if (!NuterraRes.InitialLoad || !NuterraRes.UserResourcesContainsKey<TerrainLayer>(name))
                                            {
                                                var layer = BuildTerrainLayer(layerJSON, name, reloading);
                                                if (layer != null)
                                                    NuterraRes.AlterObjectInUserResources(layer, name, file);
                                                did = true;
                                            }
                                            else if (NuterraRes.InitialLoad)
                                            {
                                                NuterraRes.LogError(string.Format("TerrainLayer \"{0}\" already exists!", name), TerrainLayersTag);
                                            }
                                        }
                                        else
                                        {
                                            NuterraRes.LogError(string.Format("TerrainLayer in file \"{0}\" has no name!", fileName), TerrainLayersTag);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogFileError(file, e.ToString(), TerrainLayersTag);
                    }
                    if (did)
                    {
                        EstNumStepsIterator++;
                        yield return null;
                    }
                }
            }
            foreach (var item in CreateFromScripts<TerrainLayer, CreateRequestTerrainLayer>(
                DoingReboot, ScriptCreateTerrainLayerEvent, new CreateRequestTerrainLayer(reloading)))
                yield return item;

            ReadyTerrainLayers = true;
            NuterraRes.LogAsset("TerrainLayers loaded", TerrainLayersTag);
            InProgress = null;
            profiling.Stop();
            NuterraRes.LogAsset("Time for " + nameof(TerrainLayer) + " took " + profiling.ElapsedMilliseconds.ToString() + "ms");
            profiling.Reset();
            yield break;
        }


        private static bool ReadyMapGenerators = false;
        private static GameObject MasterGeneratorGO = null;
        /// <summary>
        /// Sent when this is about to create the script-managed <see cref="MapGenerator"/> instances.
        /// <para>Call <see cref="CreateRequest{MapGenerator}.RequestToCreate(ModContainer, MapGenerator)"/> to register within the subscribing method.</para>
        /// <para><see cref="CreateRequest{MapGenerator}.IsReloading"/> returns <b>true</b> if this is a reload request</para>
        /// </summary>
        public static Event<CreateRequestMapGenerator> ScriptCreateMapGeneratorEvent = new Event<CreateRequestMapGenerator>();
        public class CreateRequestMapGenerator : CreateRequest<MapGenerator>
        {
            private FieldInfo layers;
            internal CreateRequestMapGenerator(bool reloading, FieldInfo m_Layers) :
                base(reloading)
            {
                layers = m_Layers;
            }
            /// <summary>
            /// Create a <see cref="MapGenerator"/> from <see cref="JObject"/> JSON
            /// </summary>
            /// <param name="layerJSON"></param>
            /// <param name="name"></param>
            /// <returns></returns>
            public override MapGenerator CreateFromJSON(string ModID, JObject layerJSON, string name) => 
                BuildMapGenerator(layers, layerJSON, name, IsReloading);
        }
        private static MapGenerator BuildMapGenerator(FieldInfo m_Layers, JObject generatorJSON, string name, bool reloading)
        {
            MapGenerator generator_base = NuterraRes.GetObjectFromUserResources<MapGenerator>(name);
            if (generator_base == null)
            {
                if (MasterGeneratorGO == null)
                    MasterGeneratorGO = new GameObject("MasterModGenerator");
                var generator_go = new GameObject(name);
                generator_go.transform.SetParent(MasterGeneratorGO.transform);
                generator_base = generator_go.AddComponent<MapGenerator>();
            }
            generator_base.name = name;
            if (!reloading)
                NuterraRes.BeginOrResetTrackingHierachy(typeof(MapGenerator), name);
            else
                NuterraRes.BuildRequestReloads(typeof(MapGenerator), name);
            JsonConvert.PopulateObject(generatorJSON.ToString(), generator_base, new JsonSerializerSettings()
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                Converters = {
                        new Newtonsoft.Json.Converters.StringEnumConverter()
                        {
                            AllowIntegerValues = true
                        }
                    }
            }
            );

            var layers = ((JArray)generatorJSON["m_Layers"]).ToObject<MapGenerator.Layer[]>();

            m_Layers.SetValue(generator_base, layers);

            if (!reloading)
                NuterraRes.StopTrackingHierachy();
            return generator_base;
        }
        internal static IEnumerable LoadAllMapGenerators(bool reloading)
        {
            profiling.Start();
            var m_Layers = typeof(MapGenerator).GetField("m_Layers", BindingFlags.Instance | BindingFlags.NonPublic);
            EstNumSteps = NuterraRes.EstTime<TextAsset>(TrySuperCompress, ABPrefixLayer, BiomesFolder, "*" + MapGeneratorExtension, out var generators, out var fst);

            NuterraRes.LogAsset("Loading MapGenerators from assetBundles...", MapGeneratorsTag);
            // assetBundle is FAST
            InProgress = nameof(MapGenerator) + " AssetBundles";
            foreach (var item in ResourcesHelper.IterateAllModAssetsBundle<TextAsset>((text) => {
                return text?.name != null && text.name.StartsWith(ABPrefixLayer);
            }))
            {
                var assetName = item.Value?.name;
                if (assetName != null)
                {
                    try
                    {
                        var generatorJSON = JObject.Parse(item.Value.text);

                        if (generatorJSON["name"] != null)
                        {
                            var name = generatorJSON["name"].ToString();
                            if (!NuterraRes.UserResourcesContainsKey<MapGenerator>(name))
                            {
                                var generator_base = BuildMapGenerator(m_Layers, generatorJSON, name, false);
                                if (generator_base != null)
                                    NuterraRes.AddObjectToUserResources(generator_base, name, null);
                                EstNumStepsIterator++;
                            }
                            else
                                NuterraRes.LogError(string.Format("MapGenerator \"{0}\" already exists!", name), MapGeneratorsTag);
                        }
                        else
                        {
                            NuterraRes.LogError(string.Format("MapGenerator in file \"{0}\" has no name!", assetName), MapGeneratorsTag);
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogAssetBundleError(item.Key, e.ToString(), MapGeneratorsTag);
                    }
                }
                else
                {
                    NuterraRes.LogError(string.Format("MapGenerators(assetBundle) name is NULL"), MapGeneratorsTag);
                }
            }
            yield return new WaitForEndOfFrame();

            NuterraRes.LogAsset("Loading MapGenerators", MapGeneratorsTag);

            foreach (var file in generators)
            {
                var fileName = file.Name;
                if (NuterraRes.UserResourcesCanUpdate<MapGenerator>(file))
                {
                    InProgress = nameof(MapGenerator) + " " + fileName;
                    yield return new WaitForEndOfFrame();
                    bool did = false;
                    try
                    {
                        using (FileStream FS = new FileStream(file.FullName, FileMode.Open))
                        {
                            using (StreamReader sr = new StreamReader(FS))
                            {
                                using (JsonReader reader = new JsonTextReader(sr))
                                {
                                    var generatorJSON = JObject.Load(reader);

                                    if (generatorJSON["name"] != null)
                                    {
                                        var name = generatorJSON["name"].ToString();
                                        if (!NuterraRes.InitialLoad || !NuterraRes.UserResourcesContainsKey<MapGenerator>(name))
                                        {
                                            var generator_base = BuildMapGenerator(m_Layers, generatorJSON, name, reloading);
                                            if (generator_base != null)
                                                NuterraRes.AlterObjectInUserResources(generator_base, name, file);
                                            did = true;
                                        }
                                        else if (NuterraRes.InitialLoad)
                                        {
                                            NuterraRes.LogError(string.Format("MapGenerator \"{0}\" already exists!", name), MapGeneratorsTag);
                                        }
                                    }
                                    else
                                    {
                                        NuterraRes.LogError(string.Format("MapGenerator in file \"{0}\" has no name!", fileName), MapGeneratorsTag);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogFileError(file, e.ToString(), MapGeneratorsTag);
                    }
                    if (did)
                    {
                        EstNumStepsIterator++;
                        yield return null;
                    }
                }
            }
            foreach (var file in fst)
            {
                var fileName = file.Name;
                if (NuterraRes.UserResourcesCanUpdate<MapGenerator>(file))
                {
                    InProgress = nameof(MapGenerator) + " " + fileName;
                    yield return new WaitForEndOfFrame();
                    bool did = false;
                    try
                    {
                        using (FileStream FS = new FileStream(file.FullName, FileMode.Open))
                        {
                            using (GZipStream zs = new GZipStream(FS, CompressionMode.Decompress))
                            {
                                using (StreamReader sr = new StreamReader(zs))
                                {
                                    using (JsonReader reader = new JsonTextReader(sr))
                                    {
                                        var generatorJSON = JObject.Load(reader);

                                        if (generatorJSON["name"] != null)
                                        {
                                            var name = generatorJSON["name"].ToString();
                                            if (!NuterraRes.InitialLoad || !NuterraRes.UserResourcesContainsKey<MapGenerator>(name))
                                            {
                                                var generator_base = BuildMapGenerator(m_Layers, generatorJSON, name, reloading);
                                                if (generator_base != null)
                                                    NuterraRes.AlterObjectInUserResources(generator_base, name, file);
                                                did = true;
                                            }
                                            else if (NuterraRes.InitialLoad)
                                            {
                                                NuterraRes.LogError(string.Format("MapGenerator \"{0}\" already exists!", name), MapGeneratorsTag);
                                            }
                                        }
                                        else
                                        {
                                            NuterraRes.LogError(string.Format("MapGenerator in file \"{0}\" has no name!", fileName), MapGeneratorsTag);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogFileError(file, e.ToString(), MapGeneratorsTag);
                    }
                    if (did)
                    {
                        EstNumStepsIterator++;
                        yield return null;
                    }
                }
            }
            foreach (var item in CreateFromScripts<MapGenerator, CreateRequestMapGenerator>(
                DoingReboot, ScriptCreateMapGeneratorEvent, new CreateRequestMapGenerator(reloading, m_Layers)))
                yield return item;

            ReadyMapGenerators = true;
            NuterraRes.LogAsset("MapGenerators loaded", MapGeneratorsTag);
            InProgress = null;
            profiling.Stop();
            NuterraRes.LogAsset("Time for " + nameof(MapGenerator) + " took " + profiling.ElapsedMilliseconds.ToString() + "ms");
            profiling.Reset();
            yield break;
        }


        private static bool ReadyBiomes = false;
        /// <summary>
        /// Sent when this is about to create the script-managed <see cref="Biome"/> instances.
        /// <para>Call <see cref="CreateRequest{Biome}.RequestToCreate(ModContainer, Biome)"/> to register within the subscribing method.</para>
        /// <para><see cref="CreateRequest{Biome}.IsReloading"/> returns <b>true</b> if this is a reload request</para>
        /// </summary>
        public static Event<CreateRequestBiome> ScriptCreateBiomeEvent = new Event<CreateRequestBiome>();
        public class CreateRequestBiome : CreateRequest<Biome>
        {
            private FieldInfo[] fields;
            private JsonSerializer serializer;
            internal CreateRequestBiome(bool reloading, FieldInfo[] fields, JsonSerializer serializer) :
                base(reloading)
            {
                this.serializer = serializer;
                this.fields = fields;
            }
            /// <summary>
            /// Create a <see cref="Biome"/> from <see cref="JObject"/> JSON
            /// </summary>
            /// <param name="layerJSON"></param>
            /// <param name="name"></param>
            /// <returns></returns>
            public override Biome CreateFromJSON(string ModID, JObject layerJSON, string name) => 
                BuildBiome(fields, serializer, layerJSON, name, ModID, IsReloading);
        }
        private static Biome BuildBiome(FieldInfo[] fields, JsonSerializer serializer, 
            JObject biomeJSON, string name, string ModID, bool reloading)
        {
            Biome biome;
            if (biomeJSON["Reference"] != null)
            {
                biome = NuterraRes.GetObjectFromUserResources<Biome>(name);
                if (biome != null)
                    NuterraRes.DestroyThisUserResourceWhenSafeTo(biome);
                var refName = biomeJSON["Reference"].ToString();
                var reference = NuterraRes.GetObjectFromGameResources<Biome>(refName);
                if (!reference)
                {
                    NuterraRes.LogError(string.Format("Biome reference \"{0}\" for Biome \"{1}\" doesn't exists!", refName, name), BiomesTag);
                    return null;
                }

                biome = UnityEngine.Object.Instantiate(reference);
            }
            else
            {
                biome = NuterraRes.GetObjectFromUserResources<Biome>(name);
                if (biome == null)
                    biome = ScriptableObject.CreateInstance<Biome>();
            }
            if (!reloading)
                NuterraRes.BeginOrResetTrackingHierachy(typeof(Biome), name);
            else
                NuterraRes.BuildRequestReloads(typeof(Biome), name);

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
                if (biomeJSON["BiomeWeights"] == null)
                {
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
            BiomeGeneration.FixupBiome(biome, FallbackBiome, true);
            var colorAtMidday = biome.DayLighting.AmbientColour.Evaluate(1);// FULL day
            if (0.35f > colorAtMidday.a || (0.75f > (colorAtMidday.r + colorAtMidday.g + colorAtMidday.b)))
                DarkInDayBiomes.Add(biome);
            else
                DarkInDayBiomes.Remove(biome);
            if (ManIngameWiki.GetBiomePage(name) == null)
                new WikiPageBiome(biome, ManIngameWiki.InsureBiomesWikiGroup(ModID));
            if (!reloading)
                NuterraRes.StopTrackingHierachy();
            BiomesToMods.Remove(biome);
            BiomesToMods.Add(biome, ModID);
            return biome;
        }
        internal static IEnumerable LoadAllBiomes(bool reloading)
        {
            profiling.Start();

            var Biome_T = typeof(Biome);
            var fields = Biome_T.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

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

            NuterraRes.LogAsset("Loading Biomes from assetBundles...", BiomesTag);
            // assetBundle is FAST
            EstNumSteps = NuterraRes.EstTime<TextAsset>(TrySuperCompress, ABPrefixBiomes, BiomesFolder, "*" + BiomesExtension, out var biomes, out var fst);
            InProgress = nameof(Biome) + " AssetBundles";
            foreach (var item in ResourcesHelper.IterateAllModAssetsBundle<TextAsset>((text) => {
                return text?.name != null && text.name.StartsWith(ABPrefixBiomes);
            }))
            {
                var assetName = item.Value?.name;
                if (assetName != null)
                {
                    try
                    {
                        var biomeJSON = JObject.Parse(item.Value.text);

                        if (biomeJSON["name"] != null)
                        {
                            var name = biomeJSON["name"].ToString();
                            if (!NuterraRes.UserResourcesContainsKey<Biome>(name))
                            {
                                var biome = BuildBiome(fields, serializer, biomeJSON, name, item.Key.ModID, false);
                                if (biome != null)
                                    NuterraRes.AddObjectToUserResources(biome, name, null);
                                EstNumStepsIterator++;
                            }
                            else
                                NuterraRes.LogError(string.Format("Biome \"{0}\" already exists!", name), BiomesTag);
                        }
                        else
                        {
                            NuterraRes.LogError(string.Format("Biome in file \"{0}\" has no name!", assetName), BiomesTag);
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogAssetBundleError(item.Key, e.ToString(), BiomesTag);
                    }
                }
                else
                {
                    NuterraRes.LogError(string.Format("Biomes(assetBundle) name is NULL"), BiomesTag);
                }
            }
            yield return new WaitForEndOfFrame();

            NuterraRes.LogAsset("Loading Biomes", BiomesTag);

            foreach (var file in biomes)
            {
                var fileName = file.Name;
                if (NuterraRes.UserResourcesCanUpdate<Biome>(file))
                {
                    InProgress = nameof(Biome) + " " + fileName;
                    yield return new WaitForEndOfFrame();
                    bool did = false;
                    try
                    {
                        using (FileStream FS = new FileStream(file.FullName, FileMode.Open))
                        {
                            using (StreamReader sr = new StreamReader(FS))
                            {
                                using (JsonReader reader = new JsonTextReader(sr))
                                {
                                    var biomeJSON = JObject.Load(reader);

                                    if (biomeJSON["name"] != null)
                                    {
                                        var name = biomeJSON["name"].ToString();
                                        if (!NuterraRes.InitialLoad || !NuterraRes.UserResourcesContainsKey<Biome>(name))
                                        {
                                            var biome = BuildBiome(fields, serializer, biomeJSON, name, ManModWorld.ModID, reloading);
                                            if (biome != null)
                                                NuterraRes.AlterObjectInUserResources(biome, name, file);
                                            did = true;
                                        }
                                        else if (NuterraRes.InitialLoad)
                                        {
                                            NuterraRes.LogError(string.Format("Biome \"{0}\" already exists!", name), BiomesTag);
                                        }
                                    }
                                    else
                                    {
                                        NuterraRes.LogError(string.Format("Biome in file \"{0}\" has no name!", fileName), BiomesTag);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogFileError(file, e.ToString(), BiomesTag);
                    }

                    if (did)
                    {
                        EstNumStepsIterator++;
                        yield return null;
                    }
                }
            }
            foreach (var file in fst)
            {
                var fileName = file.Name;
                if (NuterraRes.UserResourcesCanUpdate<Biome>(file))
                {
                    InProgress = nameof(Biome) + " " + fileName;
                    yield return new WaitForEndOfFrame();
                    bool did = false;
                    try
                    {
                        using (FileStream FS = new FileStream(file.FullName, FileMode.Open))
                        {
                            using (GZipStream zs = new GZipStream(FS, CompressionMode.Decompress))
                            {
                                using (StreamReader sr = new StreamReader(zs))
                                {
                                    using (JsonReader reader = new JsonTextReader(sr))
                                    {
                                        var biomeJSON = JObject.Load(reader);

                                        if (biomeJSON["name"] != null)
                                        {
                                            var name = biomeJSON["name"].ToString();
                                            if (!NuterraRes.InitialLoad || !NuterraRes.UserResourcesContainsKey<Biome>(name))
                                            {
                                                var biome = BuildBiome(fields, serializer, biomeJSON, name, ManModWorld.ModID, reloading);
                                                if (biome != null)
                                                    NuterraRes.AlterObjectInUserResources(biome, name, file);
                                                did = true;
                                            }
                                            else if (NuterraRes.InitialLoad)
                                            {
                                                NuterraRes.LogError(string.Format("Biome \"{0}\" already exists!", name), BiomesTag);
                                            }
                                        }
                                        else
                                        {
                                            NuterraRes.LogError(string.Format("Biome in file \"{0}\" has no name!", fileName), BiomesTag);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogFileError(file, e.ToString(), BiomesTag);
                    }

                    if (did)
                    {
                        EstNumStepsIterator++;
                        yield return null;
                    }
                }
            }
            foreach (var item in CreateFromScripts<Biome, CreateRequestBiome>(
                DoingReboot, ScriptCreateBiomeEvent, new CreateRequestBiome(reloading, fields, serializer)))
                yield return item;
            ReadyBiomes = true;
            NuterraRes.LogAsset("Biomes loaded", BiomesTag);
            InProgress = null;
            profiling.Stop();
            NuterraRes.LogAsset("Time for " + nameof(Biome) + " took " + profiling.ElapsedMilliseconds.ToString() + "ms");
            profiling.Reset();
        }


        private static bool ReadyBiomeGroups = false;
        /// <summary>
        /// Sent when this is about to create the script-managed <see cref="BiomeGroup"/> instances.
        /// <para>Call <see cref="CreateRequest{BiomeGroup}.RequestToCreate(ModContainer, BiomeGroup)"/> to register within the subscribing method.</para>
        /// <para><see cref="CreateRequest{T}.IsReloading"/> returns <b>true</b> if this is a reload request</para>
        /// </summary>
        public static Event<CreateRequestBiomeGroup> ScriptCreateBiomeGroupEvent = new Event<CreateRequestBiomeGroup>();
        public class CreateRequestBiomeGroup : CreateRequest<BiomeGroup>
        {
            private JsonSerializer serializer;
            internal CreateRequestBiomeGroup(bool reloading, JsonSerializer serializer) :
                base(reloading)
            {
                this.serializer = serializer;
            }
            /// <summary>
            /// Create a <see cref="BiomeGroup"/> from <see cref="JObject"/> JSON
            /// </summary>
            /// <param name="layerJSON"></param>
            /// <param name="name"></param>
            /// <returns></returns>
            public override BiomeGroup CreateFromJSON(string ModID, JObject layerJSON, string name) => 
                BuildBiomeGroup(serializer, layerJSON, name, IsReloading);
        }
        private static BiomeGroup BuildBiomeGroup(JsonSerializer serializer, JObject biomeGroupJSON, string name, bool reloading)
        {
            BiomeGroup biomeGroup = NuterraRes.GetObjectFromUserResources<BiomeGroup>(name);
            NuterraRes.BeginOrResetTrackingHierachy(typeof(BiomeGroup), name);
            if (biomeGroup == null)
                biomeGroup = ScriptableObject.CreateInstance<BiomeGroup>();
            biomeGroup.name = name;

            foreach (var field in BiomeGroup_fields)
            {
                try
                {
                    if (biomeGroupJSON[field.Name] != null)
                        field.SetValue(biomeGroup, biomeGroupJSON[field.Name].ToObject(field.FieldType, serializer));
                }
                catch (Exception e)
                {
                    Console.WriteLine(field.Name);
                    Console.WriteLine(e);
                }
            }
            BiomeGeneration.FixupGroup(biomeGroup, true);
            NuterraRes.StopTrackingHierachy();
            return biomeGroup;
        }
        internal static IEnumerable LoadAllBiomeGroups(bool reloading)
        {
            profiling.Start();
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

            NuterraRes.LogAsset("Loading BiomeGroups from assetBundles...", BiomeGroupsTag);
            // assetBundle is FAST
            EstNumSteps = NuterraRes.EstTime<TextAsset>(TrySuperCompress, ABPrefixGroup, BiomesFolder, "*" + BiomeGroupsExtension, out var biomeGroups, out var fst);
            InProgress = nameof(BiomeGroup) + " AssetBundles";
            foreach (var item in ResourcesHelper.IterateAllModAssetsBundle<TextAsset>((text) => {
                return text?.name != null && text.name.StartsWith(ABPrefixGroup);
            }))
            {
                var assetName = item.Value?.name;
                if (assetName != null)
                {
                    try
                    {
                        var biomeGroupJSON = JObject.Parse(item.Value.text);

                        if (biomeGroupJSON["name"] != null)
                        {
                            var name = biomeGroupJSON["name"].ToString();
                            if (!NuterraRes.UserResourcesContainsKey<BiomeGroup>(name))
                            {
                                var biomeGroup = BuildBiomeGroup(serializer, biomeGroupJSON, name, false);
                                if (biomeGroup != null)
                                    NuterraRes.AddObjectToUserResources(biomeGroup, name, null);
                                EstNumStepsIterator++;
                            }
                            else
                                NuterraRes.LogError(string.Format("BiomesGroups \"{0}\" already exists!", name), BiomeGroupsTag);
                        }
                        else
                        {
                            NuterraRes.LogError(string.Format("BiomesGroups in file \"{0}\" has no name!", assetName), BiomeGroupsTag);
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogAssetBundleError(item.Key, e.ToString(), BiomeGroupsTag);
                    }
                }
                else
                {
                    NuterraRes.LogError(string.Format("BiomesGroups(assetBundle) name is NULL"), BiomeGroupsTag);
                }
            }
            yield return new WaitForEndOfFrame();

            NuterraRes.LogAsset("Loading BiomeGroups", BiomeGroupsTag);

            foreach (var file in biomeGroups)
            {
                var fileName = file.Name;
                bool did = false;
                if (NuterraRes.UserResourcesCanUpdate<BiomeGroup>(file))
                {
                    InProgress = nameof(BiomeGroup) + " " + fileName;
                    yield return new WaitForEndOfFrame();
                    try
                    {
                        using (FileStream FS = new FileStream(file.FullName, FileMode.Open))
                        {
                            using (StreamReader sr = new StreamReader(FS))
                            {
                                using (JsonReader reader = new JsonTextReader(sr))
                                {
                                    var biomeGroupJSON = JObject.Load(reader);

                                    if (biomeGroupJSON["name"] != null)
                                    {
                                        var name = biomeGroupJSON["name"].ToString();
                                        if (!NuterraRes.InitialLoad || !NuterraRes.UserResourcesContainsKey<BiomeGroup>(name))
                                        {
                                            var biomeGroup = BuildBiomeGroup(serializer, biomeGroupJSON, name, reloading);
                                            if (biomeGroup != null)
                                                NuterraRes.AlterObjectInUserResources(biomeGroup, name, file);
                                            did = true;
                                        }
                                        else if (NuterraRes.InitialLoad)
                                        {
                                            NuterraRes.LogError(string.Format("BiomeGroup \"{0}\" already exists!", name), BiomeGroupsTag);
                                        }
                                    }
                                    else
                                    {
                                        NuterraRes.LogError(string.Format("BiomeGroup in file \"{0}\" has no name!", fileName), BiomeGroupsTag);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogFileError(file, e.ToString(), BiomeGroupsTag);
                    }
                }
                if (did)
                {
                    EstNumStepsIterator++;
                    yield return new WaitForEndOfFrame();
                }
            }

            foreach (var file in fst)
            {
                var fileName = file.Name;
                bool did = false;
                if (NuterraRes.UserResourcesCanUpdate<BiomeGroup>(file))
                {
                    InProgress = nameof(BiomeGroup) + " " + fileName;
                    yield return new WaitForEndOfFrame();
                    try
                    {
                        using (FileStream FS = new FileStream(file.FullName, FileMode.Open))
                        {
                            using (GZipStream zs = new GZipStream(FS, CompressionMode.Decompress))
                            {
                                using (StreamReader sr = new StreamReader(zs))
                                {
                                    using (JsonReader reader = new JsonTextReader(sr))
                                    {
                                        var biomeGroupJSON = JObject.Load(reader);

                                        if (biomeGroupJSON["name"] != null)
                                        {
                                            var name = biomeGroupJSON["name"].ToString();
                                            if (!NuterraRes.InitialLoad || !NuterraRes.UserResourcesContainsKey<BiomeGroup>(name))
                                            {
                                                var biomeGroup = BuildBiomeGroup(serializer, biomeGroupJSON, name, reloading);
                                                if (biomeGroup != null)
                                                    NuterraRes.AlterObjectInUserResources(biomeGroup, name, file);
                                                did = true;
                                            }
                                            else if (NuterraRes.InitialLoad)
                                            {
                                                NuterraRes.LogError(string.Format("BiomeGroup \"{0}\" already exists!", name), BiomeGroupsTag);
                                            }
                                        }
                                        else
                                        {
                                            NuterraRes.LogError(string.Format("BiomeGroup in file \"{0}\" has no name!", fileName), BiomeGroupsTag);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        NuterraRes.LogFileError(file, e.ToString(), BiomeGroupsTag);
                    }
                }
                if (did)
                {
                    EstNumStepsIterator++;
                    yield return new WaitForEndOfFrame();
                }
            }
            foreach (var item in CreateFromScripts<BiomeGroup, CreateRequestBiomeGroup>(
                DoingReboot, ScriptCreateBiomeGroupEvent, new CreateRequestBiomeGroup(reloading, serializer)))
                yield return item;
            ReadyBiomeGroups = true;
            NuterraRes.LogAsset("BiomeGroups loaded", BiomeGroupsTag);
            InProgress = null;
            profiling.Stop();
            NuterraRes.LogAsset("Time for " + nameof(BiomeGroup) + " took " + profiling.ElapsedMilliseconds.ToString() + "ms");
            profiling.Reset();
        }


        private static IEnumerable EnsureLoadComplete(bool reloading)
        {
            NuterraRes.LogAsset("EnsureLoadComplete");
            EstNumSteps = 3;
            EstPercentDone = 0f;
            EstNumStepsIterator = 0;
            if (NuterraRes.Dependants.Any())
            {
                int count = 0;
                foreach (var item in NuterraRes.Dependants)
                    count += item.Value.Count;
                NuterraRes.LogAsset("Loaded " + count + " dependants in the process");
                NuterraRes.Dependants.Clear();
            }
            InProgress = "ManModBiomes Final Init";
            while (true)
            {
                if (ReadyTextures && ReadyTerrainLayers && ReadyMapGenerators &&
                    ReadyBiomes && ReadyBiomeGroups)
                {   // Good to go, START!!!
                    if (DoingReboot)
                    {
                        //ManWorld.inst.CurrentBiomeMap?.InvalidateBiomeDB();
                        //ManWorldTileExt.HostOnly_ReloadENTIREScene(true);
                        EstPercentDone = 1f / 3f;
                        EstNumStepsIterator = 1;
                        yield return new WaitForEndOfFrame();
                        SpawnHelper.RefetchBiomes();
                        if (LastOverrideGroup != null)
                            FORCE_THIS_BIOMEGROUP_LOADED_IMMEDEATE(LastOverrideGroup);
                        else if (LastOverrideBiome != null)
                            FORCE_THIS_BIOME_LOADED_IMMEDEATE(LastOverrideBiome);
                        else
                            ManBiomeSave.inst.ForceReloadBiomesImmedeate();
                        EstPercentDone = 2f / 3f;
                        EstNumStepsIterator = 2;
                        yield return new WaitForEndOfFrame();
                        DoingReboot = false;
                        selector.Resync();
                        NuterraRes.LogAsset("RELOADED BIOMES");
                        if (biomeWrappers.Count > 0 || NuterraRes.userResources.ContainsKey(typeof(BiomeGroup)) &&
                           NuterraRes.userResources[typeof(BiomeGroup)].obj.Count > 0)
                        {
                            var m_ModePlayPrefab = typeof(UIGameMode).GetField("m_ModePlayPrefab", BindingFlags.NonPublic | BindingFlags.Instance);
                            var m_ModePlayPrefabNoTwitter = typeof(UIGameMode).GetField("m_ModePlayPrefabNoTwitter", BindingFlags.NonPublic | BindingFlags.Instance);

                            var UIGameModes = UnityEngine.Resources.FindObjectsOfTypeAll<UIGameMode>().ToList();

                            var CampaignMode = UIGameModes.Find(gm => gm.name == "Campaign Mode");
                            var CreativeMode = UIGameModes.Find(gm => gm.name == "Creative Mode");

                            var buttonPrefab = DefaultControls.CreateButton(default(DefaultControls.Resources));
                            var button = buttonPrefab.GetComponent<Button>();
                            button.onClick.AddListener(() =>
                            {
                                selector.useGUILayout = true;
                            });
                            var text = buttonPrefab.GetComponentInChildren<Text>();
                            text.text = "Select biomes";
                            buttonPrefab.AddComponent<UIModeInitButton>();
                            buttonPrefab.transform.SetParent(null, false);

                            var containers = new object[]
                            {
                                m_ModePlayPrefab.GetValue(CampaignMode),
                                m_ModePlayPrefabNoTwitter.GetValue(CampaignMode),
                                m_ModePlayPrefab.GetValue(CreativeMode),
                                m_ModePlayPrefabNoTwitter.GetValue(CreativeMode)
                            };

                            foreach (var item in containers)
                            {
                                if (item != null && item is GameObject go)
                                {
                                    var parent = go.transform.Find("Options");
                                    var btn = GameObject.Instantiate(buttonPrefab);
                                    btn.transform.SetParent(parent, false);

                                    parent.GetComponent<GridLayoutGroup>().constraintCount += 1;
                                }
                            }

                            ManUI.inst.GetScreen(ManUI.ScreenType.NewGame).ScreenInitialize(ManUI.ScreenType.NewGame);
                            selector.enabled = true;
                        }
                        EstPercentDone = 1f;
                        EstNumStepsIterator = 3;
                        yield return new WaitForEndOfFrame();
                        InProgress = null;
                        NuterraRes.InitialLoad = false;
                        yield return new WaitForEndOfFrame();
                        AfterWeInjected();
                    }
                    yield break;
                }
                yield return new WaitForEndOfFrame();
            }
        }



        // ---------------------------------  INJECTION  ---------------------------------
        /// <summary>
        /// Sent before this starts assigning the values IDs.
        /// </summary>
        public static EventNoParams BeforeInjectionEvent = new EventNoParams();
        /// <summary>
        /// Sent when this is finished assigning the values IDs and now they are in play.
        /// </summary>
        public static EventNoParams AfterInjectionEvent = new EventNoParams();
        /// <summary>
        /// Most of the registration is handled in <see cref="ManBiomeSave"/>
        /// </summary>
        internal static void BeforeWeInject()
        {
            BeforeInjectionEvent.Send();
        }
        internal static void AfterWeInjected()
        {
            AfterInjectionEvent.Send();
            SpawnHelper.RefetchBiomes();
        }



        // ---------------------------------  ACTIVATION  ---------------------------------
        class UIModeInitButton : MonoBehaviour, UIGameModeSettings.ModeInitSettingProvider
        {
            public void AddSettings(ManGameMode.ModeSettings modeSettings) { }

            public void InitComponent()
            {
                GetComponent<Button>().onClick.AddListener(() =>
                {
                    ManModBiomes.selector.useGUILayout = true;
                });
            }
        }

        internal static BiomeMap defaultMap;
        private static BiomeMap nextMap;
        private static int tileAction = 0;
        internal static Biome LastOverrideBiome = null;
        internal static BiomeGroup LastOverrideGroup = null;
        private static bool LoadNextMap(BiomeMap toLoad)
        {
            if (nextMap != null)
                return false;
            nextMap = toLoad;
            ManWorld.inst.TileManager.TileCreatedEvent.Subscribe(TileUpset);
            ManWorld.inst.TileManager.TileDepopulatedEvent.Subscribe(TileUpset);
            ManWorld.inst.TileManager.TileDestroyedEvent.Subscribe(TileUpset);
            ManWorld.inst.TileManager.TileLoadedEvent.Subscribe(TileUpset);
            ManWorld.inst.TileManager.TilePopulatedEvent.Subscribe(TileUpset);
            ManWorld.inst.TileManager.TileStartPopulatingEvent.Subscribe(TileUpsetBIG);
            ManWorld.inst.TileManager.TileUnloadedEvent.Subscribe(TileUpsetBIG);
            InvokeHelper.InvokeCoroutine(LoadNextMapAsync());
            return true;
        }
        private const int BusyFramesDelayInstantLoad = 8;
        private const int BusyFramesDelayInstantLoadMajor = 32;
        private static void TileUpset(WorldTile _)
        {
            if (tileAction < BusyFramesDelayInstantLoad)
                tileAction = BusyFramesDelayInstantLoad;
        }
        private static void TileUpsetBIG(WorldTile _) => tileAction = BusyFramesDelayInstantLoadMajor;
        private static IEnumerator LoadNextMapAsync()
        {
            var tileman = ManWorld.inst.TileManager;
            ManWorld.inst.Reset(null, true);
            tileAction = BusyFramesDelayInstantLoad;
            while (tileAction > 0)
            {
                if (tileman.IsPopulating)
                    tileAction = BusyFramesDelayInstantLoadMajor;
                else if (tileman.IsClearing || tileman.IsGenerating)
                    tileAction = BusyFramesDelayInstantLoad;
                tileAction--;
                yield return new WaitForEndOfFrame();
            }
            if (nextMap == null)
                yield break;
            try
            {
                LastOverrideBiome = null;
                LastOverrideGroup = null;
                ManWorld.inst.Reset(nextMap);
            }
            catch { }
            tileAction = BusyFramesDelayInstantLoad;
            while (tileAction > 0)
            {
                if (tileman.IsPopulating)
                    tileAction = BusyFramesDelayInstantLoadMajor;
                else if (tileman.IsClearing || tileman.IsGenerating)
                    tileAction = BusyFramesDelayInstantLoad;
                tileAction--;
                yield return new WaitForEndOfFrame();
            }
            nextMap = null;
            tileAction = 0;
            ManWorld.inst.TileManager.TileCreatedEvent.Unsubscribe(TileUpset);
            ManWorld.inst.TileManager.TileDepopulatedEvent.Unsubscribe(TileUpset);
            ManWorld.inst.TileManager.TileDestroyedEvent.Unsubscribe(TileUpset);
            ManWorld.inst.TileManager.TileLoadedEvent.Unsubscribe(TileUpset);
            ManWorld.inst.TileManager.TilePopulatedEvent.Unsubscribe(TileUpset);
            ManWorld.inst.TileManager.TileStartPopulatingEvent.Unsubscribe(TileUpset);
            ManWorld.inst.TileManager.TileUnloadedEvent.Unsubscribe(TileUpset);
        }
        internal static void LoadBiomeDefaults()
        {
            if (defaultMap == null)
                defaultMap = ManWorld.inst.CurrentBiomeMap;
            LastOverrideBiome = null;
            LastOverrideGroup = null;
            if (nextMap == null)
            {
                ManBiomeSave.inst.CleanupAll();
                if (LoadNextMap(defaultMap))
                    NuterraRes.LogAsset("FORCE - loaded Biome \"default\"");
            }
        }
        internal static void LoadBiomeNext()
        {
            if (defaultMap == null)
                defaultMap = ManWorld.inst.CurrentBiomeMap;
            LastOverrideBiome = null;
            LastOverrideGroup = null;
            if (nextMap == null)
            {
                ManBiomeSave.inst.InitiateBiomes(false);
                if (LoadNextMap(ManWorld.inst.CurrentBiomeMap))
                    NuterraRes.LogAsset("FORCE - loaded Biome \"Modified\"");
            }
        }

        internal static void FORCE_THIS_BIOME_LOADED_IMMEDEATE(Biome biome)
        {
            if (defaultMap == null)
                defaultMap = ManWorld.inst.CurrentBiomeMap;
            LastOverrideBiome = biome;
            LastOverrideGroup = null;
            try
            {
                if (nextMap == null)
                {
                    var fakeMAp = BiomeGeneration.GetFakeBiomeMap(defaultMap, biome);
                    if (LoadNextMap(fakeMAp))
                        NuterraRes.LogAsset("FORCE - loaded Biome \"" + biome.name + "\"");
                }
            }
            catch { }
        }
        internal static void FORCE_THIS_BIOMEGROUP_LOADED_IMMEDEATE(BiomeGroup biomeGroup)
        {
            if (defaultMap == null)
                defaultMap = ManWorld.inst.CurrentBiomeMap;
            LastOverrideBiome = null;
            LastOverrideGroup = biomeGroup;
            try
            {
                if (nextMap == null)
                {
                    var fakeMAp = BiomeGeneration.GetFakeBiomeMap(defaultMap, biomeGroup);
                    if (LoadNextMap(fakeMAp))
                        NuterraRes.LogAsset("FORCE - loaded BiomeGroup \"" + biomeGroup.name + "\"");
                }
            }
            catch { }
        }

    }
}
