using System;
using System.Linq;
using System.Reflection;
using SafeSaves;
using TerraTechETCUtil;
using UnityEngine.SceneManagement;

namespace Nuterra.World.Biomes
{
    /// <summary>
    /// Handles biome saving information
    /// </summary>
    [AutoSaveManager]
    public class ManBiomeSave
    {
        [SSManagerInst]
        public static ManBiomeSave inst = new ManBiomeSave();
        /// <summary> Saved configuration </summary>
        [SSaveField]
        public string[] biomes = new string[0];
        /// <summary> Saved configuration </summary>
        [SSaveField]
        public string[] groups = new string[0];


        static FieldInfo m_Biomes = null;
        static FieldInfo m_BiomeWeights = null;

        static Type BiomeMap_T = default;

        static FieldInfo m_BiomeGroups = null;
        static FieldInfo m_BiomeGroupDatabase = null;


        //static ManSaveGame.SaveDataJSONType saveType = (ManSaveGame.SaveDataJSONType)7000;

        private void ResetData()
        {
            biomes = new string[0];
            groups = new string[0];
        }
        /// <summary> ACTIVE ON-SCENE </summary>
        static BiomeGroup[] injectedGroups = new BiomeGroup[0];
        /// <summary> ACTIVE ON-SCENE </summary>
        static BiomeWrapper[] injectedBiomes = new BiomeWrapper[0];

        bool addSaveData = false;

        /// <summary>
        /// Sanity Check!
        /// </summary>
        static void SanityCheck()
        {
            if (m_BiomeGroupDatabase != null)
                return;
            var fields = ManModBiomes.GetBiomeGroupFields();
            if (fields == null)
                throw new NullReferenceException(nameof(ManModBiomes.GetBiomeGroupFields));
            m_Biomes = fields.FirstOrDefault(f => f?.Name != null && f.Name == "m_Biomes");
            if (m_Biomes == null)
                throw new NullReferenceException(nameof(m_Biomes));
            m_BiomeWeights = fields.FirstOrDefault(f => f?.Name != null && f.Name == "m_BiomeWeights");
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
            CleanupAll();
            ManModBiomes.selector.Reset();
            ResetData();
            addSaveData = false;
        }

        public void ModeStart(Mode mode)
        {
            /*
            if (!loaded)
            {
                loaded = true;
                SanityCheck();
                if (ManModBiomes.selector == null)
                {
                    ManModBiomes.InsureSelector();
                    if (ManModBiomes.selector == null)
                        throw new NullReferenceException(nameof(ManModBiomes.selector));
                }
                ManModBiomes.selector.Resync();
                InitiateBiomes(true);
                DebugWorld.Log(ManModBiomes.Tag, "ModeStart()");
            }*/
        }
        internal void InitiateBiomes(bool WeAreLoadingFromSave)
        {
            ManModBiomes.selector.useGUILayout = false;
            if (WeAreLoadingFromSave)
            {   // We are LOADING, move saved to on-scene
                LoadBiomesFromSave();
            }
            else
            {   // We are SAVING, move on-scene to saved
                SaveBiomesToSave();
            }


            if (addSaveData)
            {
                ManModBiomes.BeforeWeInject();
                InjectBiomes();
                InjectGroups();
                ManModBiomes.AfterWeInjected();
            }
        }
        private void LoadBiomesFromSave()
        {
            injectedBiomes = biomes.Select(i => ManModBiomes.biomeWrappers.Find(bw => bw.biome.name == i)).ToArray();

            injectedGroups = groups.Select(i => NuterraRes.GetObjectFromUserResources<BiomeGroup>(i)).ToArray();

            addSaveData = true;
        }
        private void SaveBiomesToSave()
        {
            injectedBiomes = BiomeSelector.biomes.Where(i => i.enabled).Select(i => i.biome).ToArray();
            biomes = injectedBiomes.Select(i => i.biome.name).ToArray();

            injectedGroups = BiomeSelector.groups.Where(i => i.enabled).Select(i => i.group).ToArray();
            groups = injectedGroups.Select(i => i.name).ToArray();
            addSaveData = true;
        }

        public void ForceReloadBiomesImmedeate()
        {
            CleanupAll();
            InitiateBiomes(true);
        }
        internal void CleanupAll()
        {
            CleanupGroups();
            CleanupBiomes();
            loaded = false;
        }

        /// <summary>
        /// Call this when the game has started saving it's SafeSaves serialization for this system
        /// </summary>
        public void PrepareForSaving()
        {
            injectedBiomes = BiomeSelector.biomes.Where(i => i.enabled).Select(i => i.biome).ToArray();
            biomes = injectedBiomes.Select(i => i.biome.name).ToArray();

            injectedGroups = BiomeSelector.groups.Where(i => i.enabled).Select(i => i.group).ToArray();
            groups = injectedGroups.Select(i => i.name).ToArray();
        }
        /// <summary>
        /// Call this when the game has finished saving it's SafeSaves serialization for this system
        /// </summary>
        public void FinishedSaving()
        {
        }
        bool loaded = false;
        /// <summary>
        /// Call this when the game has started loading it's SafeSaves serialization for this system
        /// </summary>
        public void PrepareForLoading()
        {
        }
        /// <summary>
        /// Call this when the game has finished loading it's SafeSaves serialization for this system
        /// </summary>
        public void FinishedLoading()
        {
            if (!loaded)
            {
                loaded = true;
                SanityCheck();
                if (ManModBiomes.selector == null)
                {
                    ManModBiomes.InsureSelector();
                    if (ManModBiomes.selector == null)
                        throw new NullReferenceException(nameof(ManModBiomes.selector));
                }
                ManModBiomes.selector.Resync();
                InitiateBiomes(true);
                DebugWorld.Log(ManModBiomes.Tag, "FinishedLoading()");
            }
        }

        void InjectBiomes()
        {
            if (biomes.Any(x => x == null))
            {
                NuterraRes.LogErrorMandatory("Biome savedata is corrupted!!! Removing NULL entries", ManModBiomes.BiomesTag);
                var list = biomes.ToList();
                list.RemoveAll(x => x == null);
                biomes = list.ToArray();
            }
            if (injectedBiomes.Any(x => x.biome == null))
            {   // SANITY CHECK
                NuterraRes.LogErrorMandatory("Some biomes are " + AltUI.EnemyString("completely absent") + "! Missing:", ManModBiomes.BiomesTag);
                foreach (var item in biomes.Where(x => injectedBiomes.FirstOrDefault(
                    y => y.biome?.name == x).biome == null))
                    NuterraRes.LogErrorMandatory(" - " + item, ManModBiomes.BiomesTag);
                NuterraRes.LogErrorMandatory(" <i>Are you missing mods?</i>", ManModBiomes.BiomesTag);
                var list = injectedBiomes.ToList();
                list.RemoveAll(x => x.biome == null);
                injectedBiomes = list.ToArray();
            }
            foreach (var item in injectedBiomes)
            {
                if (item.biome != null)
                {
                    for (int i = 0; i < item.biomeGroupNames.Length; i++)
                    {
                        var groupName = item.biomeGroupNames[i];
                        if (groupName == null)
                            continue; // CORRUPTED
                        try
                        {
                            var group = NuterraRes.GetObjectFromResources<BiomeGroup>(groupName);
                            if (group)
                            {
                                var biomes = ((Biome[])m_Biomes.GetValue(group))?.ToList();
                                if (biomes == null)
                                    throw new NullReferenceException($"{m_Biomes} does not exist");
                                biomes.Add(item.biome);
                                m_Biomes.SetValue(group, biomes.ToArray());

                                var weights = ((float[])m_BiomeWeights.GetValue(group))?.ToList();
                                if (weights == null)
                                    throw new NullReferenceException($"{m_BiomeWeights} does not exist");
                                weights.Add(item.biomeWeights[i]);
                                m_BiomeWeights.SetValue(group, weights.ToArray());

                                NuterraRes.LogAsset(string.Format("Biome \"{0}\" added to BiomeGroup \"{1}\"", item.biome.name, groupName), ManModBiomes.BiomesTag);
                            }
                            else
                            {
                                NuterraRes.LogError(string.Format("BiomeGroup \"{0}\" doesn't exist for Biome \"{1}\"", groupName, item.biome.name), ManModBiomes.BiomesTag);
                            }
                        }
                        catch (Exception e)
                        {
                            NuterraRes.LogErrorMandatory("Biomes loading INTERNAL ERROR!!! - " + e, ManModBiomes.BiomesTag);
                        }
                    }
                }
            }
        }

        void InjectGroups()
        {
            var MainBiomeMap = NuterraRes.GetObjectFromGameResources<BiomeMap>("MainBiomeMap");

            var mainGroups = ((BiomeGroup[])m_BiomeGroups.GetValue(MainBiomeMap)).ToList();

            if (groups.Any(x => x == null))
            {
                NuterraRes.LogErrorMandatory("Biome groups savedata is corrupted!!! Removing NULL entries", ManModBiomes.BiomesTag);
                var list = groups.ToList();
                list.RemoveAll(x => x == null);
                groups = list.ToArray();
            }
            if (injectedGroups.Any(x => x == null))
            {
                NuterraRes.LogErrorMandatory("Some biome groups are " + AltUI.EnemyString("completely absent") + "! Missing:", ManModBiomes.BiomesTag);
                foreach (var item in groups.Where(x => injectedGroups.FirstOrDefault(
                    y => y.name == x) == null))
                    NuterraRes.LogErrorMandatory(" - " + item, ManModBiomes.BiomesTag);
                NuterraRes.LogErrorMandatory(" <i>Are you missing mods?</i>", ManModBiomes.BiomesTag);
                var list = injectedGroups.ToList();
                list.RemoveAll(x => x == null);
                injectedGroups = list.ToArray();
            }

            foreach (var group in injectedGroups)
            {
                mainGroups.Add(group);
            }

            m_BiomeGroups.SetValue(MainBiomeMap, mainGroups.ToArray());
            NuterraRes.LogAsset("Custom BiomeGroups injected in MainBiomeMap", ManModBiomes.MetaTag);

            ResetBiomeDB(MainBiomeMap);
        }

        void CleanupBiomes()
        {
            SanityCheck();
            var MainBiomeMap = NuterraRes.GetObjectFromGameResources<BiomeMap>("MainBiomeMap");

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
                NuterraRes.LogAsset("Custom BiomeGroups removed from MainBiomeMap", ManModBiomes.MetaTag);
            }
            catch { }

            ResetBiomeDB(MainBiomeMap);
        }

        void CleanupGroups()
        {
            foreach (var item in injectedBiomes)
            {
                if (item.biome != null)
                {
                    foreach (var groupName in item.biomeGroupNames)
                    {
                        if (groupName == null)
                            continue;
                        var group = NuterraRes.GetObjectFromResources<BiomeGroup>(groupName);
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

                                NuterraRes.LogAsset(string.Format("Biome \"{0}\" removed from BiomeGroup \"{1}\"", item.biome.name, groupName), ManModBiomes.BiomesTag);
                            }
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
