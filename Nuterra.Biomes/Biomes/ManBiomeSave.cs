using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nuterra.World.Biomes
{
    class ManBiomeSave : Mode.IManagerModeEvents
    {
        static FieldInfo m_Biomes = ManModBiomes.BiomeGroup_fields.First(f => f.Name == "m_Biomes");
        static FieldInfo m_BiomeWeights = ManModBiomes.BiomeGroup_fields.First(f => f.Name == "m_BiomeWeights");

        static Type BiomeMap_T = typeof(BiomeMap);

        static FieldInfo m_BiomeGroups = BiomeMap_T.GetField("m_BiomeGroups", BindingFlags.NonPublic | BindingFlags.Instance);
        static FieldInfo m_BiomeGroupDatabase = BiomeMap_T.GetField("m_BiomeGroupDatabase", BindingFlags.NonPublic | BindingFlags.Instance);


        static ManSaveGame.SaveDataJSONType saveType = (ManSaveGame.SaveDataJSONType)7000;

        SaveData data = new SaveData();
        BiomeGroup[] injectedGroups = new BiomeGroup[0];
        BiomeWrapper[] injectedBiomes = new BiomeWrapper[0];

        bool addSaveData = false;

        public void ModeExit()
        {
            CleanupGroups();
            CleanupBiomes();
            ManModBiomes.selector.Reset();
            data = new SaveData();
            addSaveData = false;
        }

        public void ModeStart(ManSaveGame.State optionalLoadState)
        {
            ManModBiomes.selector.useGUILayout = false;
            if(optionalLoadState != null)
            {
                if (optionalLoadState.GetSaveData<SaveData>(saveType, out data))
                {
                    injectedBiomes = data.biomes.Select(i => ManModBiomes.biomeWrappers.Find(bw => bw.biome.name == i)).ToArray();

                    injectedGroups = data.groups.Select(i => ManModBiomes.GetObjectFromUserResources<BiomeGroup>(i)).ToArray();

                    addSaveData = true;
                }
            }
            else
            {
                injectedBiomes = BiomeSelector.biomes.Where(i => i.enabled).Select(i => i.biome).ToArray();
                data.biomes = injectedBiomes.Select(i => i.biome.name).ToArray();

                injectedGroups = BiomeSelector.groups.Where(i => i.enabled).Select(i => i.group).ToArray();
                data.groups = injectedGroups.Select(i => i.name).ToArray();
                addSaveData = true;
            }


            if (addSaveData)
            {
                InjectBiomes();
                InjectGroups();
            }
        }

        public void Save(ManSaveGame.State saveState)
        {
            if (addSaveData)
                saveState.AddSaveData<SaveData>(saveType, data);
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
            var MainBiomeMap = ManModBiomes.GetObjectFromGameResources<BiomeMap>("MainBiomeMap");

            var groups = ((BiomeGroup[])m_BiomeGroups.GetValue(MainBiomeMap)).ToList();

            foreach (var group in injectedGroups)
            {
                groups.Remove(group);
            }

            m_BiomeGroups.SetValue(MainBiomeMap, groups.ToArray());
            ManModBiomes.LogAsset("Custom BiomeGroups removed from MainBiomeMap", ManModBiomes.MetaTag);

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
