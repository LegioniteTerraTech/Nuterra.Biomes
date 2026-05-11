using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Nuterra.World;
using SafeSaves;
using TerraTechETCUtil;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.SceneManagement;

namespace Nuterra.ModLoading
{
    public class JSONConverterUniversal : JsonConverter
    {
        public static bool CreateNew;
        public static GameObject Foundation;
        public override bool CanWrite => false;
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
        }

        private static void GenerateAdvanced(JToken token)
        {
            //Foundation = CustomModules.NuterraDeserializer.DeserializeIntoGameObject((JObject)token, Foundation);
        }
        private static HashSet<string> JSONBLOCKNames = new HashSet<string>()
            {
                "JSONData",
                "JSONBLOCK",
                "Deserializer",
            };
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            if (CreateNew)
            {
                if (existingValue == null)
                    existingValue = Activator.CreateInstance(objectType);//json.ToObject(objectType);
                try
                {
                    foreach (var item in json)
                    {
                        JToken token = item.Value;
                        if (token is JObject obj)
                        {
                            if (!JSONBLOCKNames.Contains(item.Key))
                                obj.Merge(existingValue);
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidCastException("The whole file was in an unexpected format", e);
                }
            }
            else
            {
                try
                {
                    foreach (var item in json)
                    {
                        JToken token = item.Value;
                        if (JSONBLOCKNames.Contains(item.Key))
                        {
                            try
                            {
                                GenerateAdvanced(token);
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception e)
                {
                    throw new InvalidCastException("The whole file was in an unexpected format", e);
                }
            }
            return existingValue;
        }

        public override bool CanConvert(Type objectType) => true;
    }

    /// <summary>
    /// Anything that is loaded by any <see cref="ModLoaderSystem{T, V, A}"/>
    /// </summary>
    public abstract class ModLoadable
    {
        [JsonIgnore]
        public string modID;
        [JsonIgnore]
        public string fileName;
        [Doc("The advanced fields for fine alterations to this item")]
        public Dictionary<string, object> JSONData = new Dictionary<string, object>();

        /// <summary>
        /// <b>DO NOT USE THIS, USE <see cref="ModLoadable(string)"/></b>
        /// <para><u>MOST BE INCLUDED IN EVERY LOADABLE FOR <see cref="Newtonsoft.Json"/>!!!</u></para>
        /// </summary>
        [JsonConstructor]
        [Obsolete("ONLY FOR JSON SERIALIZATION")]
        public ModLoadable() { }
        /// <summary>
        /// Create a new instance of this in script.
        /// <para>Register this in it's respective manager</para>
        /// </summary>
        /// <param name="ID">ID to register as.  Make sure it doesn't overlap. 
        /// <br/><b>Avoid changing this as doing so may break peoples' saves!</b></param>
        public ModLoadable(string ID) 
        { 
            fileName = ID;
        }
    }

    /// <summary>
    /// Anything that is loaded by any <see cref="T"/>
    /// </summary>
    public abstract class ModLoadable<T, V, A> : ModLoadable where T : ModLoaderSystem<T, V, A>, IModPreloadable, new()
        where V : struct where A : ModLoadable<T, V, A>, new()
    {
        /// <inheritdoc/>
        [JsonConstructor]
        [Obsolete("ONLY FOR JSON SERIALIZATION")]
        public ModLoadable() : base() { }
        /// <summary>
        /// Create a new instance of this in script.
        /// <para>Register this in <see cref="T"/></para>
        /// </summary>
        /// <param name="ID">ID to register as.  Make sure it doesn't overlap. 
        /// <br/><b>Avoid changing this as doing so may break peoples' saves!</b></param>
        /// <inheritdoc/>
        public ModLoadable(string ID) : base(ID)
        {
        }
    }

    internal class AutoSpawner : MonoBehaviour
    {
        private const int IDBasis = 7972;
        private static AutoSpawner master;
        private static Rect windowPos = new Rect(0, 20, 200, 300);
        private static Vector2 scroll = new Vector2(0,0);

        private static string title = "unknown";
        private static Dictionary<string, ModLoadable> spawnables = new Dictionary<string, ModLoadable>();
        private static EventNoParams TempHoldSubDetach;
        private static Action TempHoldSub;
        private static Action<ModLoadable> TempSpawn;

        public static void InitFor(string titleIn, Action<Dictionary<string, ModLoadable>> addToThisList,
             Action<ModLoadable> spawnRequested, EventNoParams toSubTo) 
        {
            if (addToThisList != null)
            {
                if (master == null)
                    master = new GameObject().AddComponent<AutoSpawner>();
                else
                {
                    if (TempSpawn == spawnRequested)
                    {
                        ResetAndDisableThis();
                        return;
                    }
                    else
                        ResetAndDisableThis();
                }
                title = titleIn;
                TempHoldSubDetach = toSubTo;
                TempHoldSub = () => {
                    spawnables.Clear();
                    addToThisList(spawnables); 
                };
                TempSpawn = spawnRequested;
                toSubTo.Subscribe(TempHoldSub);
                TempHoldSub();
                ManModGUI.AddEscapeableCallback(ResetAndDisableThis, true);
                master.enabled = true;
            }
            else if (master != null)
                ResetAndDisableThis();
        }
        public static void ResetAndDisableThis()
        {
            ManModGUI.RemoveEscapeableCallback(ResetAndDisableThis, true);
            if (master == null)
                return;
            if (TempSpawn == null)
                return;
            spawnables.Clear();
            TempHoldSubDetach.Unsubscribe(TempHoldSub);
            TempHoldSub = null;
            TempHoldSubDetach = default;
            TempSpawn = null;
            master.enabled = false;
        }
        private void OnGUI()
        {
            //AltUI.StartUI();
            try
            {
                windowPos = GUILayout.Window(IDBasis, windowPos, DisplayWindow, title);
                if (UIHelpersExt.MouseIsOverGUIMenu(windowPos))
                    ManModGUI.IsMouseOverAnyModGUI = 4;
            }
            finally
            {
                //AltUI.EndUI();
            }
        }
        private void DisplayWindow(int id)
        {
            scroll = GUILayout.BeginScrollView(scroll);
            if (spawnables.Any())
            {
                foreach (var spawnable in spawnables)
                {
                    if (GUILayout.Button(spawnable.Key))
                        TempSpawn.Invoke(spawnable.Value);
                }
            }
            else
                GUILayout.Button("None!");
            GUILayout.EndScrollView();
            GUI.DragWindow();
        }
    }
    /// <summary>
    /// This works in the following way:
    /// <para><b>Creation</b>(Before game starts) -> <b>Injection</b>(When the game starts) -> <b>Activation</b>(During the game)</para>
    /// </summary>
    /// <typeparam name="T">The Manager's type for loading content</typeparam>
    /// <typeparam name="V">The enum that is used to point to the targeted type</typeparam>
    /// <typeparam name="A">The content type to load</typeparam>
    public abstract class ModLoaderSystem<T, V, A> where T : ModLoaderSystem<T, V, A>, IModPreloadable, new()
        where V : struct where A : ModLoadable<T, V, A>, new()
    {
        protected abstract string ourTag { get; }
        protected abstract string leadingFileName { get; }
        protected const BindingFlags spamFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        protected static readonly FieldInfo healthMain = typeof(Damageable).GetField("m_OrigMaxHealth", spamFlags);
        //protected static readonly MethodInfo poolStart = typeof(Visible).GetMethod("OnPool", spamFlags);// Already done by "Instance.CreatePool(4)"
        public static bool enabled = false;

        protected static bool DevMode => ManModWorld.DevMode;

        public abstract string LogDirectoryName { get; }
        public void Log(string logData)
        {
            DebugWorld.Log(ourTag, logData);
        }
        public void Error(string logData)
        {
            DebugWorld.Log(ourTag, logData);
            if (ManModWorld.DisplayErrorsToUser)
                ManModGUI.ShowErrorPopup(logData);
        }
        public void ErrorMandatory(string logData)
        {
            DebugWorld.Log(ourTag, logData);
            ManModGUI.ShowErrorPopup(logData);
        }



        // ---------------------------------  CREATION  ---------------------------------
        /// <summary>
        /// Sent when this is about to create the script-managed <see cref="A"/> instances.
        /// <para>Call <see cref="CreateRequest.RequestToCreate(ModContainer, A)"/> to register within the subscribing method.</para>
        /// <para><see cref="CreateRequest.IsReloading"/> returns <b>true</b> if this is a reload request</para>
        /// </summary>
        public static Event<CreateRequest> ScriptCreateEvent = new Event<CreateRequest>();
        /// <summary> <see cref="A"/> instances created for <see cref="T"/>, but <b>not</b> insured to be registered </summary>
        public Dictionary<string, A> Active = new Dictionary<string, A>();
        public Dictionary<string, DateTime> Tracked = new Dictionary<string, DateTime>();

        public static string InProgress = null;
        public static float EstPercentDone = 1f;
        public static int EstNumSteps = 1;
        public static int EstNumStepsIterator = 0;
        public int GetTimeEstimate(bool reload, string path)
        {
            int count = 0;
            foreach (var item in Directory.GetFiles(path))
            {
                string filename = Path.GetFileNameWithoutExtension(item);
                if (File.Exists(item))
                    count++;
                foreach (var item2 in Directory.GetDirectories(path))
                    count += GetTimeEstimate(reload, item2);
            }
            if (!reload)
            {
                string searchCache = leadingFileName;
                foreach (var item in ResourcesHelper.IterateAllMods())
                    count += item.Value.Contents.m_AdditionalAssets.Count(x => x.name.StartsWith(searchCache) && x is TextAsset);
            }
            return count;
        }
        /// <summary>
        /// Creates all managed types "A" in all storage mediums.
        /// </summary>
        /// <param name="reload">Reload the objects from file</param>
        /// <param name="path">The file path of the type</param>
        public IEnumerable CreateAll(bool reload, string path)
        {
            foreach (var step in CreateWorkshop(reload))
                yield return step;
            foreach (var step in CreateLocal(reload, path))
                yield return step;
            foreach (var step in CreateScript(reload))
                yield return step;
        }
        /// <summary>
        /// Creates all managed types "A" using data stored in an AssetBundle for Workshop/Local Mods entries
        ///  Call CreateAll() instead for first init.
        /// </summary>
        public IEnumerable CreateWorkshop(bool reload)
        {
            Log(GetType().Name + ": Beginning to CreateWorkshop");
            string searchCache = leadingFileName;
            int stepp = 0;
            int opCount = 0;
            var iterator = ResourcesHelper.IterateAllMods();
            EstNumSteps = iterator.Count();
            foreach (var item in iterator)
            {
                var contain = item.Value;
                foreach (var item2 in contain.Contents.m_AdditionalAssets.FindAll(x => x.name.StartsWith(searchCache) && x is TextAsset))
                {
                    InProgress = item2.name;
                    if (stepp > ManModWorld.IterateExtraRate)
                    {
                        stepp = 0;
                        yield return new WaitForEndOfFrame();
                    }
                    try
                    {
                        if (!reload || !Active.ContainsKey(item2.name))
                        {
                            opCount++;
                            CreatePrefabFromAsset(item.Key, item2 as TextAsset, reload);
                        }
                    }
                    catch (Exception e)
                    {
                        Log(GetType().Name + ": Failed to load " + (item2.name.NullOrEmpty() ? "<NULL>" : item2.name) + " - " + e);
                    }
                    EstNumStepsIterator++;
                }
            }
            InProgress = null;
            Log(GetType().Name + ": Ended CreateWorkshop with " + opCount + " operations");
        }
        /// <summary>
        /// Creates all managed types "A" using data stored in a respective JSON file for that type.
        ///  Call CreateAll() instead for first init.  CreateLocal() is better for JSON editing and testing.
        /// </summary>
        /// <param name="reload">Reload the objects from file</param>
        /// <param name="path">The file path of the type</param>
        public IEnumerable CreateLocal(bool reload, string path)
        {
            Log(GetType().Name + ": Beginning to CreateLocal");
            int stepp = 0;
            int opCount = 0;
            var iterator = Directory.GetFiles(path);
            EstNumSteps = iterator.Count();
            foreach (var item in iterator)
            {
                string filename = Path.GetFileNameWithoutExtension(item);
                if (File.Exists(item))
                {
                    InProgress = filename;
                    if (stepp > ManModWorld.IterateExtraRate)
                    {
                        stepp = 0;
                        yield return new WaitForEndOfFrame();
                    }
                    try
                    {
                        CreatePrefabFromFile(ManModWorld.ModID, item, DevMode && Tracked.TryGetValue(filename, out var DT) && DT < File.GetLastWriteTime(item));
                        Tracked.Remove(filename);
                        Tracked.Add(filename, File.GetLastWriteTime(item));
                        opCount++;
                    }
                    catch (Exception e)
                    {
                        Log(GetType().Name + ": Failed to load " + (filename.NullOrEmpty() ? "<NULL>" : filename) + " - " + e);
                    }
                    EstNumStepsIterator++;
                }
                foreach (var item2 in Directory.GetDirectories(path))
                {
                    foreach (var step in CreateLocal(reload, item2))
                        yield return step;
                }
            }
            InProgress = null;
            Log(GetType().Name + ": Ended CreateLocal with " + opCount + " operations");
        }
        public IEnumerable CreateScript(bool reload)
        {
            Log(GetType().Name + ": Beginning to CreateScript");
            ScriptCreateEvent.Send(new CreateRequest(this, reload));
            int stepp = 0;
            int opCount = 0;
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
                    opCount++;
                }
                catch (Exception e)
                {
                    DebugWorld.Log(ourTag, "Unhandled exception whilist creating scripts: " + e);
                }
                EstNumStepsIterator++;
            }
            CreationPending.Clear();
            InProgress = null;
            Log(GetType().Name + ": Ended CreateScript with " + opCount + " operations");
        }
        private List<Action> CreationPending = new List<Action>();

        /// <summary>
        /// The callback to register for the managed <see cref="T"/> via script.
        /// </summary>
        public class CreateRequest
        {
            private ModLoaderSystem<T, V, A> inst;
            /// <summary>
            /// Only returns true AFTER the first, initial load
            /// </summary>
            public readonly bool IsReloading;
            internal CreateRequest(ModLoaderSystem<T, V, A> instSet, bool reloading)
            {
                inst = instSet;
                IsReloading = reloading;
            }
            /// <summary>
            /// <para>Loads <typeparamref name="A"/> instance from given class to the game.</para>
            /// <para>Will not replace already active instances of it.  You will need to call <see cref="InitiateMassLoading"/> to rebuild it all.</para>
            /// <inheritdoc cref="NuterraRes.AddObjectToUserResources(Type, UnityEngine.Object, string, FileInfo)"/>
            /// </summary>
            /// <param name="Mod">Mod adding this</param>
            /// <param name="Data">To add</param>
            /// <returns>True if we just added or altered it</returns>
            public void RequestToCreate(string ModID, A Data) => inst.RequestToCreate_Internal(ModID, Data);
            /// <summary>
            /// <para>Removes <typeparamref name="A"/> instance from given class from the game.</para>
            /// <para>Will not replace already active instances of it.  You will need to call <see cref="InitiateMassLoading"/> to rebuild it all.</para>
            /// <inheritdoc cref="NuterraRes.RemoveObjectFromUserResources(Type, UnityEngine.Object, string, FileInfo)"/>
            /// </summary>
            /// <param name="Mod">Mod removing this</param>
            /// <param name="Data">To remove</param>
            /// <returns>True if we just removed it</returns>
            public void CancelCreateRequest(string ModID, A Data) => inst.CancelCreateRequest_Internal(ModID, Data);
        }

        private void RequestToCreate_Internal(string ModID, A Data)
        {
            CreationPending.Add(() => CreatePrefabFromScript(ModID, Data));
        }
        private void CancelCreateRequest_Internal(string ModID, A Data)
        {
            CreationPending.Add(() => CreatePrefabFromScript(ModID, Data));
        }


        /// <summary>
        /// Extract data from an existing instance from the base game this is supposed to target
        /// </summary>
        protected abstract A ExtractFromExisting(object target);
        /// <summary>
        /// Loads <typeparamref name="A"/> instance from the file path to the game
        /// </summary>
        /// <param name="ModID"></param>
        /// <param name="path"></param>
        /// <param name="Reload">Only returns true AFTER the initial load</param>
        protected abstract void CreatePrefabFromFile(string ModID, string path, bool Reload = false);
        /// <summary>
        /// Loads <typeparamref name="A"/> instance from the AssetBundle to the game
        /// </summary>
        /// <param name="ModID"></param>
        /// <param name="path"></param>
        /// <param name="Reload">Only returns true AFTER the initial load</param>
        protected abstract void CreatePrefabFromAsset(string ModID, TextAsset asset, bool Reload = false);
        /// <summary>
        /// Loads <typeparamref name="A"/> instance from given class to the game
        /// </summary>
        protected void CreatePrefabFromScript(string ModID, A Data)
        {
            string fileName = Data?.fileName;
            if (fileName == null)
                throw new ArgumentNullException(nameof(Data) + " or it's  fileName");
            if (Active.Remove(fileName))
                DestroyPrefab(Data);
            CreatePrefab(ModID, fileName, Data);
        }
        /// <summary>
        /// Loads <typeparamref name="A"/> instance from given class to the game
        /// </summary>
        protected abstract void CreatePrefab(string ModID, string ID, A Data);
        /// <summary>
        /// Removes <typeparamref name="A"/> instance from given class to the game
        /// </summary>
        protected abstract void DestroyPrefab(A Data);



        // ---------------------------------  INJECTION  ---------------------------------
        /// <summary>
        /// Called before registering all <see cref="T"/> into the active play game 
        /// </summary>
        public static EventNoParams BeforeInjectionEvent = new EventNoParams();
        /// <summary>
        /// Called after registering all <see cref="T"/> into the active play game 
        /// </summary>
        public static EventNoParams AfterInjectionEvent = new EventNoParams();
        /// <summary> <see cref="A"/> instances created for <see cref="T"/> that are <b>registered and obtainable</b> in the game </summary>
        [SSaveField]
        public Dictionary<string, V> Registered = new Dictionary<string, V>();
        [SSaveField]
        public int RegisteredIDIterator = 420;

        /// <summary>
        /// This is automatically called before the save system loads anything
        /// </summary>
        protected abstract void Init_Internal();


        /// <summary>
        /// Use this to clear lists and whatnot
        /// </summary>
        protected abstract void InjectionStarting();

        /// <summary>
        /// This is automatically called for each Active type "A" to insert them into the world loading.
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="type"></param>
        /// <returns>True if it added successfully</returns>
        protected abstract void InjectOne(A instance, V type);
        /// <summary>
        /// Use this to rebuild lists and whatnot
        /// </summary>
        protected abstract void InjectionFinished();


        /// <summary>
        /// Call this when the game has started saving it's SafeSaves serialization for this system
        /// </summary>
        public void PrepareForSaving()
        {
            if (!enabled)
                return;
            try
            {
                Init_Internal();
            }
            catch (Exception e)
            {
                Log("Cascade crash of " + typeof(T) + ".PrepareForSaving(): " + e);
            }
        }
        /// <summary>
        /// Call this when the game has finished saving it's SafeSaves serialization for this system
        /// </summary>
        public void FinishedSaving()
        {
        }
        /// <summary>
        /// Call this when the game has started loading it's SafeSaves serialization for this system
        /// </summary>
        public void PrepareForLoading()
        {
            if (!enabled)
                return;
            try
            {
                Init_Internal();
            }
            catch (Exception e)
            {
                Log("Cascade crash of " + typeof(T) + ".Init_Internal(): " + e);
            }
        }
        /// <summary>
        /// Call this when the game has finished loading it's SafeSaves serialization for this system
        /// </summary>
        public void FinishedLoading()
        {
            if (!enabled)
                return;
            try
            {
                BeforeInjectionEvent.Send();
                InjectionStarting();
                // Check for all active instance prefabs. If we have any, reassign then.
                foreach (var item in Registered.ToList())// dupe it so we can do changes
                {
                    if (Active.TryGetValue(item.Key, out var val))
                    {
                        Log(typeof(A).Name + " \"" + item.Key + ", (" + item.Value + ")\" Re-registering.");
                        InjectOne(val, item.Value);
                    }
                    else
                    {
                        ErrorMandatory(typeof(A).Name + " \"" + item.Key + ", (" + item.Value + ")\" is not available!  " +
                           AltUI.EnemyString("Will not be able to load it into game world!"));
                        Registered.Remove(item.Key);
                    }
                }
                // Then add in all of the new Active ones.
                foreach (var item in Active)
                {
                    if (!Registered.ContainsKey(item.Key))
                    {   // Add in all of the new ones
                        V value = (V)(object)RegisteredIDIterator++;
                        Log(typeof(A).Name + " \"" + item.Key + ", (" + value + ")\" is being added!");
                        InjectOne(item.Value, value);
                    }
                }
                InjectionFinished();
                Log("Active [" + Active.Count + "], Registered [" + Registered.Count + "] for type \"" + typeof(A).ToString() + "\"");
                AfterInjectionEvent.Send();
            }
            catch (Exception e)
            {
                ErrorMandatory("Cascade crash of " + typeof(T) + ".FinishedLoading(): " + e);
            }
        }



        // ---------------------------------  ACTIVATION  ---------------------------------
        /// <summary>
        /// Call this to spawn our <see cref="A"/> thingy now, assuming it is  
        /// <b><see cref="Active"/></b> and 
        ///  <b><see cref="Registered"/></b>.
        /// </summary>
        /// <param name="Mod">Data to attempt to load</param>
        /// <param name="scenePos">Where in the scene it should be placed</param>
        public abstract void SpawnNow(A Mod, Vector3 scenePos);


        /// <summary>
        /// Open the mini spawner menu
        /// </summary>
        public void OpenToggleManagerMenu()
        {
            AutoSpawner.InitFor(typeof(T).Name, (inlist) => {
                foreach (var item in Registered)
                {
                    if (Active.TryGetValue(item.Key, out var result))
                        inlist.Add(item.Key, result);
                    else
                    {   // Mismatch????
                        var SB = new StringBuilder();
                        SB.AppendLine("Registered");
                        foreach (var item2 in Registered)
                            SB.AppendLine(" - " + item2.Key + ": " + item2.Value.ToString());
                        SB.AppendLine("Active");
                        foreach (var item2 in Active)
                            SB.AppendLine(" - " + item2.Key + ": " + item2.Value.ToString());
                        throw new Exception("Mismatch in data lookup whilist using " + nameof(AutoSpawner) + ":\n" +  SB.ToString());
                    }
                }
            }, (toSpawn) => { 
                SpawnNow((A)toSpawn, Singleton.cameraTrans.position + (16 * Singleton.cameraTrans.forward)); }, 
                AfterInjectionEvent);
        }
        public void CloseManagerMenu() => AutoSpawner.ResetAndDisableThis();

        public static bool EnumTryGetTypeFlexable<I>(string name, out I output) where I : struct
        {
            output = default;
            if (name.NullOrEmpty())
                return false;

            if (int.TryParse(name, out int result))
            {
                output = (I)(object)result;
                return true;
            }
            if (Enum.TryParse(name, out I result2))
            {
                output = result2;
                return true;
            }
            return false;
        }

        public static A GenerateGOFromJSON(string json)
        {
            return JsonConvert.DeserializeObject<A>(json, serializerSettings);
        }

        public static JSONConverterUniversal serializerSuper = new JSONConverterUniversal();
        public static JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
        };
        /*
        public static Stack<StringBuilder> seqencer = new Stack<StringBuilder>();
        public static void RecurseGenerateGOFromJSON(string data, GameObject hierachyCurrent)
        {
            CustomModules.NuterraDeserializer.DeserializeIntoGameObject();
            foreach (var item in data)
            {
                if (item.Key.NullOrEmpty()) 
                    continue;
                if (item.Value is MonoBehaviour Mono)
                {
                }
                else if (item.Value is Dictionary<string, object> nested)
                {
                    GameObject GO2 = new GameObject(item.Key);
                    GO2.transform.SetParent(hierachyCurrent.transform);
                    RecurseGenerateGOFromJSON(nested, GO2);
                }
            }
        }


        public static void ReadUntilEndLine(int position, string data)
        {
            seqencer.Append(data.Skip(position).TakeWhile(x => x == '\n'));
        }
        private static void FinalizeAndBuild(StringBuilder SB)
        { 

        }
        private static void IterateAndBuildBrackets(string data)
        {
            int depth = 0;
            StringBuilder context = new StringBuilder();
            foreach (var item in data)
            {
                switch (item)
                {
                    case '{':
                        seqencer.Push(context);
                        context = new StringBuilder();
                        depth++;
                        break;
                    case '}':
                        FinalizeAndBuild(context);
                        context = seqencer.Pop();
                        depth--;
                        break;
                    default:
                        break;
                }
                context.Append(item);
            }
        }
        public static void GetBrackets(int position, string data)
        {
            seqencer.Append(data.Skip(position).TakeWhile(x => x == '\n'));
        }
        */
    }

    // OBSOLETE
    /*
    /// <summary>
    /// For NON integer ID based systems
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="A"></typeparam>
    public abstract class ModLoaderSystem<T, A> where T : ModLoaderSystem<T, A>, new()
        where A : ModLoadable
    {
        protected abstract string ourTag { get; }
        protected abstract string leadingFileName { get; }
        protected const BindingFlags spamFlags = BindingFlags.NonPublic | BindingFlags.Instance;
        protected static readonly FieldInfo healthMain = typeof(Damageable).GetField("m_OrigMaxHealth", spamFlags);
        protected static readonly MethodInfo poolStart = typeof(Visible).GetMethod("OnPool", spamFlags);
        public static bool enabled = false;

        protected static bool DevMode => ManModWorld.DevMode;

        public abstract string LogDirectoryName { get; }
        public void Log(string logData)
        {
            DebugWorld.Log(ourTag, logData);
        }
        public void Error(string logData)
        {
            if (ManModWorld.DisplayErrorsToUser)
                ManModGUI.ShowErrorPopup(logData);
            DebugWorld.Log(ourTag, logData);
        }

        /// <summary>
        /// Sent when this is creating the managed instances.
        /// <para>bool return is if this is a reload request</para>
        /// </summary>
        public Event<bool> OnCreation = new Event<bool>();
        public EventNoParams AfterRegistation = new EventNoParams();
        public Dictionary<string, A> Active = new Dictionary<string, A>();
        [SSaveField]
        public Dictionary<string, string> Registered = new Dictionary<string, string>();
        [SSaveField]
        public int RegisteredIDIterator = 420;

        /// <summary>
        /// This is automatically called before the save system loads anything
        /// </summary>
        protected abstract void Init_Internal();
        /// <summary>
        /// This is automatically called for each Active type "A" to insert them into the world loading.
        /// </summary>
        protected abstract void FinalAssignment(A instance, string ID);
        /// <summary>
        /// Use this to rebuild lists and whatnot
        /// </summary>
        protected abstract void FinalAssignmentFinished();

        /// <summary>
        /// Call this when the game has started saving it's SafeSaves serialization for this system
        /// </summary>
        public void PrepareForSaving()
        {
            if (!enabled)
                return;
            try
            {
                Init_Internal();
            }
            catch (Exception e)
            {
                Log("Cascade crash of " + typeof(T) + ".PrepareForSaving(): " + e);
            }
        }
        /// <summary>
        /// Call this when the game has finished saving it's SafeSaves serialization for this system
        /// </summary>
        public void FinishedSaving()
        {
        }
        /// <summary>
        /// Call this when the game has started loading it's SafeSaves serialization for this system
        /// </summary>
        public void PrepareForLoading()
        {
            if (!enabled)
                return;
            try
            {
                Init_Internal();
            }
            catch (Exception e)
            {
                Log("Cascade crash of " + typeof(T) + ".PrepareForLoading(): " + e);
            }
        }
        /// <summary>
        /// Call this when the game has finished loading it's SafeSaves serialization for this system
        /// </summary>
        public void FinishedLoading()
        {
            if (!enabled)
                return;
            try
            {
                // Check for all active instance prefabs. If we have any, reassign then.
                foreach (var item in Registered)
                {
                    if (Active.TryGetValue(item.Key, out var val))
                    {
                        Log(typeof(A).Name + " \"" + item.Key + ", (" + item.Value + ")\" Re-registering.");
                        FinalAssignment(val, item.Value);
                    }
                    else
                        Log(typeof(A).Name + " \"" + item.Key + ", (" + item.Value + ")\" is not available!  " +
                            "Will not be able to load it into game world!");
                }
                // Then add in all of the new Active ones.
                foreach (var item in Active)
                {
                    if (!Registered.ContainsKey(item.Key))
                    {   // Add in all of the new ones
                        string value = item.Value.fileName;
                        Log(typeof(A).Name + " \"" + item.Key + ", (" + value + ")\" is being added!");
                        FinalAssignment(item.Value, value);
                    }
                }
                FinalAssignmentFinished();
                Log("Active [" + Active.Count + "], Registered [" + Registered.Count + "] for type \"" + typeof(A).ToString() + "\"");
                AfterRegistation.Send();
            }
            catch (Exception e)
            {
                Log("Cascade crash of " + typeof(T) + ".FinishedLoading(): " + e);
            }
        }

        public static string InProgress = null;
        public static float EstPercentDone = 1f;
        public static int EstNumSteps = 1;
        public static int EstNumStepsIterator = 0;
        /// <summary>
        /// Creates all managed types "A" in all storage mediums.
        /// </summary>
        /// <param name="reload">Reload the objects from file</param>
        /// <param name="path">The file path of the type</param>
        public void CreateAll(bool reload, string path)
        {
            CreateLocal(reload, path);
            CreateWorkshop(reload);
            CreateScript(reload);
        }
        /// <summary>
        /// Creates all managed types "A" using data stored in a respective JSON file for that type.
        ///  Call CreateAll() instead for first init.  CreateLocal() is better for JSON editing and testing.
        /// </summary>
        /// <param name="reload">Reload the objects from file</param>
        /// <param name="path">The file path of the type</param>
        public void CreateLocal(bool reload, string path)
        {
            var MC = ResourcesHelper.GetModContainer(ManModWorld.ModID);
            foreach (var item in Directory.GetFiles(path))
            {
                string filename = Path.GetFileNameWithoutExtension(item);
                if (File.Exists(item))
                {
                    try
                    {
                        InProgress = filename;
                        CreateInstanceFile(MC, item, reload);
                    }
                    catch (Exception e)
                    {
                        Log(GetType().Name + ": Failed to load " + (filename.NullOrEmpty() ? "<NULL>" : filename) + " - " + e);
                    }
                }
                foreach (var item2 in Directory.GetDirectories(path))
                {
                    CreateLocal(reload, item2);
                }
            }
            InProgress = null;
        }
        /// <summary>
        /// Creates all managed types "A" using data stored in an AssetBundle for Workshop/Local Mods entries
        ///  Call CreateAll() instead for first init.
        /// </summary>
        public void CreateWorkshop(bool reload)
        {
            Init_Internal();
            string searchCache = leadingFileName;
            foreach (var item in ResourcesHelper.IterateAllMods())
            {
                var contain = item.Value;
                foreach (var item2 in contain.Contents.m_AdditionalAssets.FindAll(x => x.name.StartsWith(searchCache) && x is TextAsset))
                {
                    try
                    {
                        InProgress = item2.name;
                        CreateInstanceAsset(contain, item2 as TextAsset, reload);
                    }
                    catch (Exception e)
                    {
                        Log(GetType().Name + ": Failed to load " + (item2.name.NullOrEmpty() ? "<NULL>" : item2.name) + " - " + e);
                    }
                }
            }
            InProgress = null;
        }
        public void CreateScript(bool reload) => OnCreation.Send(reload);

        /// <summary>
        /// Extract data from an existing instance from the base game this is supposed to target
        /// </summary>
        protected abstract A ExtractFromExisting(object target);
        /// <summary>
        /// Saves a new instance file to the disk as JSON
        /// </summary>
        protected abstract void CreateInstanceFile(ModContainer Mod, string path, bool Reload = false);
        /// <summary>
        /// Saves a new instance file to the disk as an AssetBundle
        /// </summary>
        protected abstract void CreateInstanceAsset(ModContainer Mod, TextAsset asset, bool Reload = false);

        public abstract void SpawnNow(A Mod, Vector3 scenePos);

        public void OpenManagerMenu()
        {
            AutoSpawner.InitFor(typeof(T).Name, (inlist) => {
                foreach (var item in Registered)
                    inlist.Add(item.Key, Active[item.Key]);
            }, (toSpawn) => {
                SpawnNow((A)toSpawn, Singleton.cameraTrans.position + (16 * Singleton.cameraTrans.forward));
            }, AfterRegistation);
        }
        public void CloseManagerMenu() => AutoSpawner.ResetAndDisableThis();

        public static bool EnumTryGetTypeFlexable<T>(string name, out T output) where T : struct
        {
            output = default;
            if (name.NullOrEmpty())
                return false;

            if (int.TryParse(name, out int result))
            {
                output = (T)(object)result;
                return true;
            }
            if (Enum.TryParse(name, out T result2))
            {
                output = result2;
                return true;
            }
            return false;
        }

        public static A GenerateGOFromJSON(string json)
        {
            return JsonConvert.DeserializeObject<A>(json, serializerSettings);
        }

        public static JSONConverterUniversal serializerSuper = new JSONConverterUniversal();
        public static JsonSerializerSettings serializerSettings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
        };
    }
    */
}