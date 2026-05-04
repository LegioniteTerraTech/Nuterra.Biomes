using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SafeSaves;

namespace Nuterra.World.Biomes
{
    [AutoSaveManager]
    public class ManBiomeSave
    {
        [SSManagerInst]
        public static ManBiomeSave inst = new ManBiomeSave();

        static FieldInfo m_Biomes = null;
        static FieldInfo m_BiomeWeights = null;

        static Type BiomeMap_T = default;

        static FieldInfo m_BiomeGroups = null;
        static FieldInfo m_BiomeGroupDatabase = null;


        static ManSaveGame.SaveDataJSONType saveType = (ManSaveGame.SaveDataJSONType)7000;

        public string[] biomes = new string[0];
        public string[] groups = new string[0];
        private void ResetData()
        {
            biomes = new string[0];
            groups = new string[0];
        }
        static BiomeGroup[] injectedGroups = new BiomeGroup[0];
        static BiomeWrapper[] injectedBiomes = new BiomeWrapper[0];

        bool addSaveData = false;

        /// <summary>
        /// Sanity Check!
        /// </summary>
        static void SanityCheck()
        {
            if (m_BiomeGroupDatabase != null)
                return;
            if (ManModBiomes.BiomeGroup_fields == null)
                throw new NullReferenceException(nameof(ManModBiomes.BiomeGroup_fields));
            m_Biomes = ManModBiomes.BiomeGroup_fields?.FirstOrDefault(f => f?.Name != null && f.Name == "m_Biomes");
            if (m_Biomes == null)
                throw new NullReferenceException(nameof(m_Biomes));
            m_BiomeWeights = ManModBiomes.BiomeGroup_fields?.FirstOrDefault(f => f?.Name != null && f.Name == "m_BiomeWeights");
            if (m_BiomeWeights == null)
                throw new NullReferenceException(nameof(m_BiomeWeights));
            BiomeMap_T = typeof(BiomeMap);
            if (BiomeMap_T == null)
                throw new NullReferenceException(nameof(BiomeMap_T));
            m_BiomeGroups = BiomeMap_T?.GetField("m_BiomeGroups", BindingFlags.NonPublic | BindingFlags.Instance);
            if (m_BiomeGroups == null)
                throw new NullReferenceException(nameof(m_BiomeGroups));
            m_BiomeGroupDatabase = BiomeMap_T?.GetField("m_BiomeGroupDatabase", BindingFlags.NonPublic | BindingFlags.Instance);
            if (m_BiomeGroupDatabase == null)
                throw new NullReferenceException(nameof(m_BiomeGroupDatabase));
        }

        public void ModeExit(Mode mode)
        {
            CleanupGroups();
            CleanupBiomes();
            ManModBiomes.selector.Reset();
            ResetData();
            addSaveData = false;
        }

        public void ModeStart(Mode mode)
        {
            SanityCheck();
            if (ManModBiomes.selector == null)
            {
                ManModBiomes.InsureSelector();
                if (ManModBiomes.selector == null)
                    throw new NullReferenceException(nameof(ManModBiomes.selector));
            }
            ManModBiomes.selector.Resync();
            InitiateBiomes(true);
        }
        internal void InitiateBiomes(bool WeAreLoading)
        {
            ManModBiomes.selector.useGUILayout = false;
            if (WeAreLoading)
            {
                injectedBiomes = biomes.Select(i => ManModBiomes.biomeWrappers.Find(bw => bw.biome.name == i)).ToArray();

                injectedGroups = groups.Select(i => ManModBiomes.GetObjectFromUserResources<BiomeGroup>(i)).ToArray();

                addSaveData = true;
            }
            else
            {
                injectedBiomes = BiomeSelector.biomes.Where(i => i.enabled).Select(i => i.biome).ToArray();
                biomes = injectedBiomes.Select(i => i.biome.name).ToArray();

                injectedGroups = BiomeSelector.groups.Where(i => i.enabled).Select(i => i.group).ToArray();
                groups = injectedGroups.Select(i => i.name).ToArray();
                addSaveData = true;
            }


            if (addSaveData)
            {
                InjectBiomes();
                InjectGroups();
            }
        }
        public void ForceReloadBiomesImmedeate()
        {
            CleanupGroups();
            CleanupBiomes();
            InitiateBiomes(true);
        }

        public void Save(ManSaveGame.State saveState)
        {
            SanityCheck();
            if (addSaveData) 
            { 
            }
            addSaveData = false;
        }

        void InjectBiomes()
        {
            foreach (var item in injectedBiomes)
            {
                for (int i = 0; i < item.biomeGroupNames.Length; i++)
                {
                    var groupName = item.biomeGroupNames[i];
                    var group = ManModBiomes.GetObjectFromResources<BiomeGroup>(groupName);
                    if (group)
                    {
                        var biomes = ((Biome[])m_Biomes.GetValue(group)).ToList();
                        biomes.Add(item.biome);
                        m_Biomes.SetValue(group, biomes.ToArray());

                        var weights = ((float[])m_BiomeWeights.GetValue(group)).ToList();
                        weights.Add(item.biomeWeights[i]);
                        m_BiomeWeights.SetValue(group, weights.ToArray());

                        ManModBiomes.LogAsset(string.Format("Biome \"{0}\" added to BiomeGroup \"{1}\"", item.biome.name, groupName), ManModBiomes.BiomesTag);
                    }
                    else
                    {
                        ManModBiomes.LogError(string.Format("BiomeGroup \"{0}\" doesn't exist for Biome \"{1}\"", groupName, item.biome.name), ManModBiomes.BiomesTag);
                    }
                }
            }
        }

        void InjectGroups()
        {
            var MainBiomeMap = ManModBiomes.GetObjectFromGameResources<BiomeMap>("MainBiomeMap");

            var groups = ((BiomeGroup[])m_BiomeGroups.GetValue(MainBiomeMap)).ToList();

            foreach (var group in injectedGroups)
            {
                groups.Add(group);
            }

            m_BiomeGroups.SetValue(MainBiomeMap, groups.ToArray());
            ManModBiomes.LogAsset("Custom BiomeGroups injected in MainBiomeMap", ManModBiomes.MetaTag);

            ResetBiomeDB(MainBiomeMap);
        }

        void CleanupBiomes()
        {
            SanityCheck();
            var MainBiomeMap = ManModBiomes.GetObjectFromGameResources<BiomeMap>("MainBiomeMap");

            if (MainBiomeMap == null)
                return;
            try
            {
                var groups = ((BiomeGroup[])m_BiomeGroups.GetValue(MainBiomeMap))?.ToList();
                if (groups == null)
                    return;

                foreach (var group in injectedGroups)
                    groups.Remove(group);

                m_BiomeGroups.SetValue(MainBiomeMap, groups.ToArray());
                ManModBiomes.LogAsset("Custom BiomeGroups removed from MainBiomeMap", ManModBiomes.MetaTag);
            }
            catch { }

            ResetBiomeDB(MainBiomeMap);
        }

        void CleanupGroups()
        {
            foreach (var item in injectedBiomes)
            {
                foreach (var groupName in item.biomeGroupNames)
                {
                    var group = ManModBiomes.GetObjectFromResources<BiomeGroup>(groupName);
                    if (group)
                    {
                        var biomes = ((Biome[])m_Biomes.GetValue(group)).ToList();
                        var index = biomes.IndexOf(item.biome);

                        if (index != -1)
                        {
                            biomes.RemoveAt(index);
                            m_Biomes.SetValue(group, biomes.ToArray());

                            var weights = ((float[])m_BiomeWeights.GetValue(group)).ToList();
                            weights.RemoveAt(index);
                            m_BiomeWeights.SetValue(group, weights.ToArray());

                            ManModBiomes.LogAsset(string.Format("Biome \"{0}\" removed from BiomeGroup \"{1}\"", item.biome.name, groupName), ManModBiomes.BiomesTag);
                        }
                    }
                }
            }
        }

        void ResetBiomeDB(BiomeMap map)
        {
            try
            {
                m_BiomeGroupDatabase.SetValue(map, null);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        class SaveData
        {
            public string[] biomes = new string[0];
            public string[] groups = new string[0];
        }
    }
}
