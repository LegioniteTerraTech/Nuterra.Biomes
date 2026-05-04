using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Nuterra.World.Biomes
{
    internal class BiomeSelector : MonoBehaviour
    {
        static readonly int ID = 7971;
        static Rect rect = new Rect(Screen.width - 200, Screen.height - 300, 200, 300);
        static Vector2 groupsScroll = Vector2.zero;
        static Vector2 biomesScroll = Vector2.zero;

        internal static List<BiomeGroupItem> groups = new List<BiomeGroupItem>();
        internal static List<BiomeItem> biomes = new List<BiomeItem>();

        internal void Resync()
        {
            bool defaultState = true;
            if (ManModBiomes.userResources.TryGetValue(typeof(BiomeGroup), out var customGroups)) {
                groups = customGroups.Select(i => new BiomeGroupItem() { enabled = defaultState, group = (BiomeGroup)i.Value }).ToList();
            }

            if (ManModBiomes.biomeWrappers.Count > 0)
            {
                biomes = ManModBiomes.biomeWrappers.Select(i => new BiomeItem() { enabled = defaultState, biome = i }).ToList();
            }
        }


        internal void Update()
        {
            if (Input.GetKeyDown(KeyCode.F6))
                useGUILayout = !useGUILayout;
        }
        internal void OnGUI()
        {
            if (!useGUILayout)
                return;

            GUI.Window(ID, rect, DoWindow, "Biome selector");
        }

        private void DoWindow(int id)
        {
            if (groups.Count > 0)
            {
                GUILayout.Label("Biome groups");
                groupsScroll = GUILayout.BeginScrollView(groupsScroll);
                {
                    foreach (var item in groups)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(item.group.name))
                            ManModBiomes.FORCE_THIS_BIOMEGROUP_LOADED_IMMEDEATE(item.group);
                        item.enabled = GUILayout.Toggle(item.enabled, string.Empty);
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();
            }

            if (biomes.Count > 0)
            {
                GUILayout.Label("Biomes");
                biomesScroll = GUILayout.BeginScrollView(biomesScroll);
                {
                    foreach (var item in biomes)
                    {
                        GUILayout.BeginHorizontal();
                        if (GUILayout.Button(item.biome.biome.name))
                            ManModBiomes.FORCE_THIS_BIOME_LOADED_IMMEDEATE(item.biome.biome);
                        item.enabled = GUILayout.Toggle(item.enabled, string.Empty);
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();
            }

            if (GUILayout.Button("Close"))
                useGUILayout = false;
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
            public bool enabled = true;
        }

        internal class BiomeItem
        {
            public BiomeWrapper biome;
            public bool enabled = true;
        }
    }
}
