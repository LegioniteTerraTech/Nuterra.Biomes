using System;
using System.Collections.Generic;
using System.Linq;
using Nuterra.World.Chunks;
using Nuterra.World.Scenery;
using TerraTechETCUtil;
using UnityEngine;

namespace Nuterra.World.Biomes
{
    internal class BiomeSelector : MonoBehaviour
    {
        static readonly int ID = 7971;
        static Rect rect = new Rect(Screen.width - 200, Screen.height - 300, 200, 300);
        static Vector2 groupsScroll = Vector2.zero;

        internal static List<BiomeGroupItem> groups = new List<BiomeGroupItem>();
        internal static List<BiomeItem> biomes = new List<BiomeItem>();

        internal void Clear()
        {
            groups.Clear();
            biomes.Clear();
        }
        internal void Resync()
        {
            if (NuterraRes.userResources.TryGetValue(typeof(BiomeGroup), out var customGroups))
                groups = customGroups.obj.Select(i =>
                new BiomeGroupItem() {
                    enabled = ManBiomeSave.inst.groups.Contains(i.Key), 
                    group = (BiomeGroup)i.Value,
                    IsValid = BiomeGeneration.IsGroupValid(i.Value as BiomeGroup)}).ToList();

            if (ManModBiomes.biomeWrappers.Count > 0)
                biomes = ManModBiomes.biomeWrappers.Select(i =>
                new BiomeItem() { enabled = ManBiomeSave.inst.biomes.Contains(i.biome.name), biome = i }).ToList();
        }


        internal void Update()
        {
            if (ManModWorld.DevMode)
            {
                if (Input.GetKeyDown(KeyCode.F6))
                    useGUILayout = !useGUILayout;
                if (Input.GetKeyDown(KeyCode.F7))
                    ManModScenery.inst.OpenToggleManagerMenu();
                if (Input.GetKeyDown(KeyCode.F8))
                    ManModChunks.inst.OpenToggleManagerMenu();
            }
        }
        internal void OnGUI()
        {
            if (!useGUILayout)
                return;

            rect = GUI.Window(ID, rect, DoWindow, "Biome selector");
            if (UIHelpersExt.MouseIsOverGUIMenu(rect))
                ManModGUI.IsMouseOverAnyModGUI = 4;
        }

        private void DoWindow(int id)
        {
            bool DANGER = ManGameMode.inst.GetCurrentGameType() == ManGameMode.GameType.MainGame;
            if (DANGER)
                GUILayout.Label("WARNING: CHANGE AT\nYOUR OWN RISK");
            groupsScroll = GUILayout.BeginScrollView(groupsScroll);
            if (groups.Count > 0)
            {
                GUILayout.Label("Biome groups");
                foreach (var item in groups)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(item.IsValid ? item.group.name : AltUI.EnemyString(item.group.name)) && item.IsValid)
                        ManModBiomes.FORCE_THIS_BIOMEGROUP_LOADED_IMMEDEATE(item.group);
                    if (!item.IsValid)
                        AltUI.Tooltip.GUITooltip("This biome group failed to load: no assigned biomes?");
                    else
                        AltUI.Tooltip.GUITooltip("Has " + item.group.Biomes.Length + " assigned biomes.");
                    item.enabled = GUILayout.Toggle(item.enabled, string.Empty);
                    GUILayout.EndHorizontal();
                }
            }
            else if (ManModBiomes.DoingReboot)
                GUILayout.Label("Loading...");
            else
                GUILayout.Label("No Biome groups!");

            if (biomes.Count > 0)
            {
                GUILayout.Label("Biomes");
                foreach (var item in biomes)
                {
                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button(item.biome.biome.name))
                        ManModBiomes.FORCE_THIS_BIOME_LOADED_IMMEDEATE(item.biome.biome);
                    item.enabled = GUILayout.Toggle(item.enabled, string.Empty);
                    GUILayout.EndHorizontal();
                }
            }
            else if (ManModBiomes.DoingReboot)
                GUILayout.Label("Loading...");
            else
                GUILayout.Label("No Biomes!");
            GUILayout.EndScrollView();

            if (GUILayout.Button(DANGER ? AltUI.EnemyString("Apply Selected") : "Apply Selected"))
                ManModBiomes.LoadBiomeNext();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Default"))
                ManModBiomes.LoadBiomeDefaults();
            if (GUILayout.Button("Close"))
                useGUILayout = false;
            GUILayout.EndHorizontal();
        }

        public void Reset()
        {
            bool defaultState = true;

            foreach (var biome in biomes)
                biome.enabled = defaultState;

            foreach (var group in groups)
                group.enabled = defaultState;
        }

        internal class BiomeGroupItem
        {
            public BiomeGroup group;
            public bool IsValid = false;
            public bool enabled = true;
        }

        internal class BiomeItem
        {
            public BiomeWrapper biome;
            public bool enabled = true;
        }
    }
}
