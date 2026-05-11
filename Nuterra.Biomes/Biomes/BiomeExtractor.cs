using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using static Circuits;


namespace Nuterra.World.Biomes
{
    internal class BiomeExtractor : MonoBehaviour
    {
        static readonly JsonConverters.ContractResolver resolver = new JsonConverters.ContractResolver()
        {
            Members = {
                {
                    "BiomeGroup",
                    new List<string> {
                        "name",
                        "m_Biomes",
                        "m_BiomeWeights",
                        "m_WeightingByDistance"
                    }
                },
                {
                    "Biome",
                    new List<string> {
                        "name",
                        "editorRenderColour",
                        "heightMapGenerator",
                        "multiTextureGenerator",
                        "layers",
                        "m_MainMaterialLayer",
                        "m_AltMaterialLayer",
                        "textureBlendSteepnessRange",
                        "textureBlendSteepnessWeighting",
                        "m_DayLighting",
                        "m_NightLighting",
                        "m_CloudParams",
                        "m_DustVFXColor",
                        "impactEffects",
                        "m_AllowLandmarks",
                        "m_AllowVendors",
                        "m_AllowStuntRamps",
                        "surfaceFriction",
                        "m_BiomeType",
                    }
                },
                {
                    "MapGenerator",
                    new List<string> {
                        "name",
                        "m_Layers",
                        "m_UseLegacy",
                        "m_ScaleAll",
                        "m_CutoffThreshold",
                        "m_RenderGradient",
                        "m_RenderColThreshold",
                    }
                }
            }
        };

        static readonly JsonConverter biomeConverter = new JsonConverters.BiomeConverter();
        static readonly JsonConverter colorConverter = new JsonConverters.ColorConverter();
        static readonly JsonConverter terrainLayerConverter = new JsonConverters.UnityObjectConverter<TerrainLayer>();
        static readonly JsonConverter mapGeneratorConverter = new JsonConverters.UnityObjectConverter<MapGenerator>();
        static readonly JsonConverter terrainObjectConverter = new JsonConverters.UnityObjectConverter<TerrainObject>();
        static readonly JsonConverter enumConverter = new Newtonsoft.Json.Converters.StringEnumConverter();
        static readonly JsonConverter textureConverter = new JsonConverters.UnityObjectConverter<Texture>();
        static readonly JsonConverter prefabGroupConverter = new JsonConverters.PrefabGroupConverter();

        static readonly JsonSerializer groupSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings()
        {
            ContractResolver = resolver,
            Converters = {
                biomeConverter,
                enumConverter
            }
        });

        static readonly JsonSerializer biomeSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings()
        {
            ContractResolver = resolver,
            Converters = {
                colorConverter,
                terrainLayerConverter,
                mapGeneratorConverter,
                terrainObjectConverter,
                enumConverter,
                prefabGroupConverter
            }
        });

        static readonly JsonSerializer mapGeneratorSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings()
        {
            ContractResolver = resolver,
            Converters = {
                colorConverter,
                enumConverter
            }
        });

        static readonly JsonSerializer terrainLayerSerializer = JsonSerializer.CreateDefault(new JsonSerializerSettings()
        {
            ContractResolver = resolver,
            Converters = {
                textureConverter,
                colorConverter
            }
        });

        static readonly string ExportFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "Export");
        static readonly string GroupsFolder = Path.Combine(ExportFolder, "BiomeGroups");
        static readonly string BiomesFolder = Path.Combine(ExportFolder, "Biomes");
        static readonly string MapGeneratorsFolder = Path.Combine(ExportFolder, "MapGenerators");
        static readonly string TerrainLayersFolder = Path.Combine(ExportFolder, "TerrainLayers");

        static readonly BiomeGroup[] groups;
        static readonly Biome[] biomes;
        static readonly MapGenerator[] generators;
        static readonly TerrainLayer[] layers;


        static readonly int ID = 7970;
        static float width = 400;
        static float height = Screen.height;

        static Rect rect = new Rect(Screen.width - width, 0, width, height);
        static Vector2 groupScroll = Vector2.zero;
        static Vector2 biomeScroll = Vector2.zero;
        static Vector2 generatorScroll = Vector2.zero;
        static Vector2 layerScroll = Vector2.zero;
        static string[] choices = new string[] { "Biome groups", "Biomes", "Map generators", "Terrain layers" };
        static int selected = -1;

        static BiomeExtractor()
        {
            Directory.CreateDirectory(ExportFolder);
            Directory.CreateDirectory(GroupsFolder);
            Directory.CreateDirectory(BiomesFolder);
            Directory.CreateDirectory(MapGeneratorsFolder);
            Directory.CreateDirectory(TerrainLayersFolder);

            groups = UnityEngine.Resources.FindObjectsOfTypeAll<BiomeGroup>().OrderBy(i => i.name).ToArray();
            biomes = UnityEngine.Resources.FindObjectsOfTypeAll<Biome>().OrderBy(i => i.name).ToArray();
            generators = UnityEngine.Resources.FindObjectsOfTypeAll<MapGenerator>().OrderBy(i => i.name).ToArray();
            layers = UnityEngine.Resources.FindObjectsOfTypeAll<TerrainLayer>().OrderBy(i => i.name).ToArray();
        }

        void Start()
        {
            useGUILayout = false;
        }

        void Update()
        {
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.L))
                useGUILayout = true;
        }

        void OnGUI()
        {
            if (!useGUILayout)
                return;

            if(selected == -1)
            {
                rect.height = GUI.skin.button.CalcHeight(new GUIContent("A"), 1) * (choices.Length + 4);
            }
            else
            {
                rect.height = Screen.height;
            }

            GUI.Window(ID, rect, DoWindow, "Biomes Extractor");
        }
        internal static string ExtractBiome(Biome biome) =>
             JObject.FromObject(biome, biomeSerializer).ToString(Formatting.Indented);

        void DoWindow(int id)
        {
            var maxWidth = GUILayout.MaxWidth(rect.width - GUI.skin.verticalScrollbar.fixedWidth);

            if (ManGameMode.inst.GetIsInPlayableMode())
            {
                var tile = ManWorld.inst.TileManager.LookupTile(Singleton.playerPos, false);
                var cell = ManWorld.inst.TileManager.GetMapCell(tile, Singleton.playerPos);
                GUILayout.Label("Current Biome: ");
                GUILayout.Label(ManWorld.inst.CurrentBiomeMap.LookupBiome(cell.Index(0)).name);
            }

            if (selected == 0)
            {
                if (GUILayout.Button("Export all BiomeGroups"))
                {
                    foreach (var group in groups)
                    {
                        var filename = group.name + ManModBiomes.BiomeGroupsExtension;
                        var json = JObject.FromObject(group, groupSerializer).ToString(Formatting.Indented);
                        File.WriteAllText(Path.Combine(GroupsFolder, filename), json);
                    }
                }
                groupScroll = GUILayout.BeginScrollView(groupScroll);
                {
                    foreach (var group in groups)
                    {
                        if (GUILayout.Button(group.name, maxWidth))
                        {
                            var filename = group.name + ManModBiomes.BiomeGroupsExtension;
                            var json =  JObject.FromObject(group, groupSerializer).ToString(Formatting.Indented);
                            File.WriteAllText(Path.Combine(GroupsFolder, filename), json);
                        }
                    }
                }
                GUILayout.EndScrollView();
            }
            else if (selected == 1)
            {
                if (GUILayout.Button("Export all Biomes"))
                {
                    foreach (var biome in biomes)
                    {
                        var filename = biome.name + ManModBiomes.BiomesExtension;
                        var json = ExtractBiome(biome);
                        File.WriteAllText(Path.Combine(BiomesFolder, filename), json);
                    }
                }
                biomeScroll = GUILayout.BeginScrollView(biomeScroll);
                foreach (var biome in biomes)
                {
                    if (GUILayout.Button(biome.name, maxWidth))
                    {
                        var filename = biome.name + ManModBiomes.BiomesExtension;
                        var json = JObject.FromObject(biome, biomeSerializer).ToString(Formatting.Indented);
                        File.WriteAllText(Path.Combine(BiomesFolder, filename), json);
                    }
                }
                GUILayout.EndScrollView();
            }
            else if (selected == 2)
            {
                if (GUILayout.Button("Export all MapGenerators"))
                {
                    foreach (var generator in generators)
                    {
                        var filename = generator.name + ManModBiomes.MapGeneratorExtension;
                        var json = JObject.FromObject(generator, mapGeneratorSerializer).ToString(Formatting.Indented);
                        File.WriteAllText(Path.Combine(MapGeneratorsFolder, filename), json);
                    }
                }
                generatorScroll = GUILayout.BeginScrollView(generatorScroll);
                foreach (var generator in generators)
                {
                    if (GUILayout.Button(generator.name, maxWidth))
                    {
                        var filename = generator.name + ManModBiomes.MapGeneratorExtension;
                        var json = JObject.FromObject(generator, mapGeneratorSerializer).ToString(Formatting.Indented);
                        File.WriteAllText(Path.Combine(MapGeneratorsFolder, filename), json);
                    }
                }
                GUILayout.EndScrollView();
            }
            else if (selected == 3)
            {
                if (GUILayout.Button("Export all TerrainLayers"))
                {
                    foreach (var layer in layers)
                    {
                        var filename = layer.name + ManModBiomes.TerrainLayerExtension;
                        var json = JObject.FromObject(layer, terrainLayerSerializer).ToString(Formatting.Indented);
                        File.WriteAllText(Path.Combine(TerrainLayersFolder, filename), json);
                    }
                }
                layerScroll = GUILayout.BeginScrollView(layerScroll);
                foreach (var layer in layers)
                {
                    if (GUILayout.Button(layer.name, maxWidth))
                    {
                        var filename = layer.name + ManModBiomes.TerrainLayerExtension;
                        var json = JObject.FromObject(layer, terrainLayerSerializer).ToString(Formatting.Indented);
                        File.WriteAllText(Path.Combine(TerrainLayersFolder, filename), json);
                    }
                }
                GUILayout.EndScrollView();
            }

            if (selected == -1)
                selected = GUILayout.SelectionGrid(-1, choices, 1);
            else if (GUILayout.Button("Back"))
                selected = -1;


            if (GUILayout.Button("Close"))
            {
                selected = -1;
                useGUILayout = false;
            }
        }
    }
}
