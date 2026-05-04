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

        internal void Start()
        {
            if(ManModBiomes.userResources.TryGetValue(typeof(BiomeGroup), out var customGroups)) {
                groups = customGroups.Select(i => new BiomeGroupItem() { enabled = false, group = (BiomeGroup)i.Value }).ToList();
            }

            if (ManModBiomes.biomeWrappers.Count > 0)
            {
                biomes = ManModBiomes.biomeWrappers.Select(i => new BiomeItem() { enabled = false, biome = i }).ToList();
            }
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
                        item.enabled = GUILayout.Toggle(item.enabled, item.group.name);
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
                        item.enabled = GUILayout.Toggle(item.enabled, item.biome.biome.name);
                    }
                }
                GUILayout.EndScrollView();
            }

            if (GUILayout.Button("Close"))
                useGUILayout = false;
        }

        public void Reset()
        {
            foreach (var biome in biomes)
            {
                biome.enabled = false;
            }

            foreach (var group in groups)
            {
                group.enabled = false;
            }
        }

        internal class BiomeGroupItem
        {
            public BiomeGroup group;
            public bool enabled;
        }

        internal class BiomeItem
        {
            public BiomeWrapper biome;
            public bool enabled;
        }
    }
}
