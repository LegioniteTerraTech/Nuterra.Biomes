using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nuterra.World.Biomes;
using TerraTechETCUtil;
using UnityEngine;

namespace Nuterra.World
{
    internal class NuterraRes
    {
        public const string Tag = ".Resources";
        public const string ModResourcesName = ManModWorld.ModLogName + Tag;
        private static bool DevMode => ManModWorld.DevMode;
        internal static bool InitialLoad = true;
        internal static string LogAsset(string value, string tag = "")
        {
            string outS = string.Format("[" + ModResourcesName + "{0}] {1}", tag, value);
            Console.WriteLine(outS);
            return outS;
        }

        internal static void LogError(string value, string tag = "")
        {
            string error = LogAsset("Error: " + value, tag);
            if (ManModWorld.DisplayErrorsToUser)
                ManModGUI.ShowErrorPopup(error, false);
        }
        internal static void LogErrorMandatory(string value, string tag = "")
        {
            string error = LogAsset("Error: " + value, tag);
            ManModGUI.ShowErrorPopup(error, false);
        }

        internal static void  LogFileError(FileInfo file, string e, string tag = "")
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


        public static readonly string RapidLoadableExtension = ".gzip";
        public class ResEntry
        {
            public Dictionary<string, UnityEngine.Object> obj = new Dictionary<string, UnityEngine.Object>();
            public Dictionary<string, ResEntryDate> dates = new Dictionary<string, ResEntryDate>();
        }
        public class ResEntryDate
        {
            public string name = null;
            public DateTime time = default;
        }
        internal static Dictionary<Type, ResEntry> userResources = new Dictionary<Type, ResEntry>();
        internal static Dictionary<Type, Dictionary<string, UnityEngine.Object>> gameResources =
            new Dictionary<Type, Dictionary<string, UnityEngine.Object>>();
        internal static HashSet<UnityEngine.Object> markedToDestroy = new HashSet<UnityEngine.Object>();


        internal static int EstTime<T>(bool doesTurbo, string ABPre, DirectoryInfo targetFolder, string fileExt, out FileInfo[] files, out FileInfo[] filesRL)
            where T : UnityEngine.Object
        {
            int otherCount = 0;
            if (InitialLoad)
            {
                otherCount = ResourcesHelper.IterateAllModAssetsBundle<T>().Count((text) =>
                    text.Value != null && text.Value.name.StartsWith(ABPre));
            }
            if (doesTurbo)
            {
                // Load whatever variant is most recent
                var fileList = new List<FileInfo>();
                var fileListRL = new List<FileInfo>();
                files = targetFolder.GetFiles(fileExt, SearchOption.AllDirectories);
                foreach (var fileRL2 in targetFolder.GetFiles(fileExt + RapidLoadableExtension, SearchOption.AllDirectories))
                {
                    string shortName = fileRL2.Name.Remove(fileRL2.Name.Length - RapidLoadableExtension.Length, RapidLoadableExtension.Length);
                    var file = files.FirstOrDefault(x => shortName == fileRL2.Name);
                    if (file != null && fileRL2.LastWriteTime >= file.LastWriteTime)
                        fileListRL.Add(fileRL2);
                    else
                        fileList.Add(file);
                }
                files = fileList.ToArray();
                filesRL = fileListRL.ToArray();
                return otherCount + files.Count(x => UserResourcesCanUpdate<T>(x)) +
                    filesRL.Count(x => UserResourcesCanUpdate<T>(x));
            }
            else
            {
                filesRL = Array.Empty<FileInfo>();
                files = targetFolder.GetFiles(fileExt, SearchOption.AllDirectories);
                return otherCount + files.Count(x => UserResourcesCanUpdate<T>(x));
            }
        }
        
        
        /// <inheritdoc cref="AddObjectToUserResources(Type, UnityEngine.Object, string, FileInfo)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="fileInfo"></param>
        public static bool AddObjectToUserResources<T>(T obj, string name, FileInfo fileInfo = null) 
            where T : UnityEngine.Object => AddObjectToUserResources(typeof(T), obj, name, fileInfo);
        /// <summary>
        /// Adds an object to <see cref="NuterraRes"/>
        /// <para>Takes no action if the object already exists</para>
        /// </summary>
        /// <param name="type">Type to add</param>
        /// <param name="obj">Instance to add</param>
        /// <param name="name">Name/ID to register for lookups under</param>
        /// <param name="fileInfo">The file data for automatic reload management while <see cref="ManModWorld.DevMode"/> is true. 
        /// Leave null for non-file system assets</param>
        /// <returns>True if we just added it</returns>
        public static bool AddObjectToUserResources(Type type, UnityEngine.Object obj, string name, FileInfo fileInfo = null)
        {
            markedToDestroy.Remove(obj);
            if (!userResources.TryGetValue(type, out var entry))
            {
                entry = new ResEntry();
                userResources.Add(type, entry);
            }
            if (!entry.obj.ContainsKey(name))
            {
                entry.obj.Add(name, obj);
                if (DevMode)
                {
                    if (fileInfo != null)
                    {
                        if (!entry.dates.ContainsKey(fileInfo.Name.Replace(RapidLoadableExtension, string.Empty)))
                        {
                            entry.dates.Add(fileInfo.Name.Replace(RapidLoadableExtension, string.Empty), new ResEntryDate()
                            {
                                name = name,
                                time = fileInfo.LastWriteTime
                            });
                        }
                        else
                            LogAsset(string.Format("Conflict for file update tracking for {0} {1}, will not be able to track updates!", type.Name, name), ManModBiomes.MetaTag);
                    }
                }
                LogAsset(string.Format("Added {0} {1}", type.Name, name), ManModBiomes.MetaTag);
                return true;
            }
            return false;
        }


        /// <inheritdoc cref="AlterObjectInUserResources(Type, UnityEngine.Object, string, FileInfo)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="fileInfo"></param>
        public static bool AlterObjectInUserResources<T>(T obj, string name, FileInfo fileInfo = null) 
            where T : UnityEngine.Object => AlterObjectInUserResources(typeof(T), obj, name, fileInfo);
        /// <summary>
        /// Adds or replaces an object in <see cref="NuterraRes"/>
        /// </summary>
        /// <param name="type">Type to add</param>
        /// <param name="obj">Instance to add</param>
        /// <param name="name">Name/ID to register for lookups under</param>
        /// <param name="fileInfo">The file data for automatic reload management while <see cref="ManModWorld.DevMode"/> is true. 
        /// Leave null for non-file system assets</param>
        /// <returns>True if we just altered it</returns>
        public static bool AlterObjectInUserResources(Type type, UnityEngine.Object obj, string name, FileInfo fileInfo = null)
        {
            if (userResources.TryGetValue(type, out var next) && next.obj.Remove(name))
            {
                markedToDestroy.Remove(obj);
                next.obj.Add(name, obj);
                BuildRequestReloads(type, name);
                if (fileInfo != null && !next.dates.TryGetValue(fileInfo.Name, out ResEntryDate RED))
                {
                    RED.name = name;
                    RED.time = fileInfo.LastWriteTime;
                }
                LogAsset(string.Format("Altered {0} {1}", type.Name, name), ManModBiomes.MetaTag);
                return true;
            }
            else
                return AddObjectToUserResources(type, obj, name, fileInfo);
        }

        /// <inheritdoc cref="RemoveObjectFromUserResources(Type, UnityEngine.Object, string, FileInfo)"/>
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="name"></param>
        /// <param name="fileInfo"></param>
        /// <returns></returns>
        public static bool RemoveObjectFromUserResources<T>(T obj, string name, FileInfo fileInfo = null) 
            where T : UnityEngine.Object => RemoveObjectFromUserResources(typeof(T), obj, name, fileInfo);
        /// <summary>
        /// Removes an object from <see cref="NuterraRes"/>
        /// <para><b>NOT ADVISED, ALSO DOES NOT DESTROY, you will need to call <see cref="DestroyThisUserResourceWhenSafeTo{T}(T)"/> for that</b></para>
        /// </summary>
        /// <param name="type">Type to remove</param>
        /// <param name="obj">Instance to remove</param>
        /// <param name="name">Name/ID to unregister for lookups under</param>
        /// <param name="fileInfo">The file data for automatic reload management while <see cref="ManModWorld.DevMode"/> is true. 
        /// Leave null for non-file system assets</param>
        /// <returns>True if we just removed it</returns>
        public static bool RemoveObjectFromUserResources(Type type, UnityEngine.Object obj, string name, FileInfo fileInfo = null)
        {
            if (userResources.TryGetValue(type, out var next) && next.obj.Remove(name))
            {
                ClearTrackingHierachy(type, name);
                if (fileInfo != null && !next.dates.TryGetValue(fileInfo.Name, out ResEntryDate RED))
                {
                    RED.name = name;
                    RED.time = fileInfo.LastWriteTime;
                }
                LogAsset(string.Format("Removed {0} {1}", type.Name, name), ManModBiomes.MetaTag);
                return true;
            }
            return false;
        }


        internal static void SuperCompress(FileInfo fileInfo)
        {
            if (fileInfo.Name.EndsWith(RapidLoadableExtension))
                return;
            using (FileStream fs = new FileStream(fileInfo.FullName + RapidLoadableExtension, FileMode.OpenOrCreate))
            {
                using (GZipStream sr = new GZipStream(fs, CompressionMode.Compress))
                {
                    using (FileStream fs2 = new FileStream(fileInfo.FullName, FileMode.Open))
                    {
                        sr.CopyTo(fs2);
                    }
                }
            }
        }

        public static bool UserResourcesContainsKey(Type type, string name)
        {
            return userResources.TryGetValue(type, out var next) && next.obj.ContainsKey(name);
        }
        public static bool UserResourcesContainsKey<T>(string name) =>
            UserResourcesContainsKey(typeof(T), name);
        public static bool UserResourcesCanUpdate(Type type, FileInfo fileInfo)
        {
            return !userResources.TryGetValue(type, out var next) ||
                !next.dates.TryGetValue(fileInfo.Name.Replace(RapidLoadableExtension, string.Empty), out var next2) ||
                (next2.time != default && next2.time < fileInfo.LastWriteTime) || (DevMode && 
                Dependants.TryGetValue(type, out var deltaRequested) && deltaRequested.Contains(fileInfo.Name));
        }
        public static bool UserResourcesCanUpdate<T>(FileInfo fileInfo) =>
            UserResourcesCanUpdate(typeof(T), fileInfo);


        public static T GetObjectFromUserResources<T>(string name)
            where T : UnityEngine.Object => (T)GetObjectFromUserResources(typeof(T), name);
        public static UnityEngine.Object GetObjectFromUserResources(Type type, string name)
        {
            if (userResources.TryGetValue(type, out var bucket) && bucket.obj.TryGetValue(name, out var item))
            {
                CurrentTracking?.Add(new KeyValuePair<Type, string>(type, name));
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


        private static Dictionary<KeyValuePair<Type, string>, HashSet<KeyValuePair<Type, string>>> Dependancies = 
            new Dictionary<KeyValuePair<Type, string>, HashSet<KeyValuePair<Type, string>>>();
        public static Dictionary<Type, HashSet<string>> Dependants = new Dictionary<Type, HashSet<string>>();
        private static HashSet<KeyValuePair<Type, string>> CurrentTracking;
        public static void BeginOrResetTrackingHierachy(Type type, string name)
        {
            if (!DevMode)
                return;
            var searcher = new KeyValuePair<Type, string>(type, name);
            if (!Dependancies.TryGetValue(searcher, out var hashS))
            {
                hashS = new HashSet<KeyValuePair<Type, string>>();
                Dependancies.Add(searcher, hashS);
            }
            hashS.Clear();
            CurrentTracking = hashS;
        }
        public static void ClearTrackingHierachy(Type type, string name)
        {
            if (!DevMode)
                return;
            var searcher = new KeyValuePair<Type, string>(type, name);
            if (Dependancies.TryGetValue(searcher, out var hashS))
            {
                hashS.Clear();
                if (CurrentTracking == hashS)
                    CurrentTracking = null;
            }
            Dependancies.Remove(searcher);
        }
        public static void StopTrackingHierachy()
        {
            if (!DevMode)
                return;
            CurrentTracking = null;
        }
        public static void BuildRequestReloads(Type type, string name)
        {
            if (DevMode)
            {
                var searcher = new KeyValuePair<Type, string>(type, name);
                foreach (var item in Dependancies)
                {
                    if (item.Value.Contains(searcher))
                    {
                        if (!Dependants.TryGetValue(item.Key.Key, out var hashS))
                        {
                            hashS = new HashSet<string>();
                            Dependants.Add(item.Key.Key, hashS);
                        }
                        hashS.Add(item.Key.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Destroy an Object during the game reload tranzition phase, hopefully not leaving <see cref="NullReferenceException"/> anywhere.
        /// <para>This will not destroy any instance that is still registered in <see cref="NuterraRes"/></para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        public static void DestroyThisUserResourceWhenSafeTo<T>(T instance) where T : UnityEngine.Object
        {
            if (instance != null)
                markedToDestroy.Add(instance);
        }

        internal static void DestroyAllPending(Mode _)
        {
            foreach (var item in new List<UnityEngine.Object>(markedToDestroy))
            {
                try
                {
                    if (item?.name != null)
                    {
                        var type = item.GetType();
                        var fromRes = GetObjectFromUserResources(type, item.name);
                        if (fromRes != null)
                        {   // It is registered!!! 
                            if (item != fromRes)// Make sure we aren't obliterating something that is STILL REGISTERED
                                UnityEngine.Object.Destroy(item);
                            // else we ignore since it's still registered.
                        }
                        else
                            UnityEngine.Object.Destroy(item);
                    }
                }
                catch { }
            }
            markedToDestroy.Clear();
        }
    }
}
