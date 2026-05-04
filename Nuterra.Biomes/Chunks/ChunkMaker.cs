using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using TerraTechETCUtil;
using static LocalisationEnums;

namespace Nuterra.World.Chunks
{
    internal class ChunkMaker
    {
        private static readonly FieldInfo recData = typeof(RecipeManager).GetField("m_ModdedRecipes", BindingFlags.NonPublic | BindingFlags.Instance);
        private static Dictionary<string, RecipeTable.RecipeList> modRecipies;

        public static HashSet<ChunkTypes> Resurrected = new HashSet<ChunkTypes>();
        public static int ChunkPrice(ChunkTypes CT) => ResourceManager.inst.GetResourceDef(CT).saleValue;
        private static RecipeTable.RecipeList foundryRecipes;
        private static List<RecipeTable.Recipe> foundryDirect => foundryRecipes.m_Recipes;
        public void FindOldChunks()
        {
            DebugWorld.Log(ManModChunks.Tag, "FindOldChunks - Getting unused Chunks...");
            var defaultS = ManUI.inst.m_SpriteFetcher.GetSprite(ObjectTypes.Block, -1);
            for (int i = 0; i < Enum.GetValues(typeof(ChunkTypes)).Length; i++)
            {
                ChunkTypes CT = (ChunkTypes)i;
                if (CT.ToString().StartsWith("_deprecated"))
                {
                    var nameLoc = StringLookup.GetItemName(ObjectTypes.Chunk, i);
                    var sprite = ManUI.inst.m_SpriteFetcher.GetSprite(ObjectTypes.Chunk, i);
                    ResourceTable.Definition def = ResourceManager.inst.GetResourceDef(CT);
                    DebugWorld.Log(ManModChunks.Tag, CT.ToString() + " - Name: " + def.name + ",  Prefab: " + (def.basePrefab ? "True" : " False") +
                        ",  Sprite: " + (sprite != defaultS ? "True" : " False") + ",  LOC Name: " +
                        ("ERROR: String Not Found" != nameLoc ? nameLoc : "No_Name") +
                        "\n  Value: " + def.saleValue + ",  Mass: " + def.mass);
                    ManUI.inst.m_SpriteFetcher.GetSprite(ObjectTypes.Chunk, i);
                }
            }
        }

        public static void RenewOldChunks()
        {
            InsureFoundryRecipes();
            DebugWorld.Log(ManModChunks.Tag, "FindOldChunks - Renewing unused Chunks...");
            int stepper = 420;
            Dictionary<int, int> vars = (Dictionary<int, int>)ManModChunks.ResLook.GetValue(null);
            Dictionary<int, int> vars2 = (Dictionary<int, int>)ManModChunks.ResLook2.GetValue(null);
            var defaultS = ManUI.inst.m_SpriteFetcher.GetSprite(ObjectTypes.Block, -1);
            for (int i = 0; i < Enum.GetValues(typeof(ChunkTypes)).Length; i++)
            {
                ChunkTypes CT = (ChunkTypes)i;
                if (CT.ToString().StartsWith("_deprecated"))
                {
                    Resurrected.Add(CT);
                    vars.Add(i, stepper);
                    vars2.Add(i, stepper);
                    var nameLoc = StringLookup.GetItemName(ObjectTypes.Chunk, i);
                    var sprite = ManUI.inst.m_SpriteFetcher.GetSprite(ObjectTypes.Chunk, i);
                    ResourceTable.Definition def = ResourceManager.inst.GetResourceDef(CT);
                    /*
                    DebugWorld.Log(CT.ToString() + " - Name: " + def.name + ",  Prefab: " + (def.basePrefab ? "True" : " False") +
                        ",  Sprite: " + (sprite != defaultS ? "True" : " False") + ",  LOC Name: " +
                        ("ERROR: String Not Found" != nameLoc ? nameLoc : "No_Name") +
                        "\n  Value: " + def.saleValue + ",  Mass: " + def.mass);
                    */
                    int hash = ItemTypeInfo.GetHashCode(ObjectTypes.Chunk, i);
                    switch (CT)
                    {
                        case ChunkTypes._deprecated_TerreriaIngot:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Terreria Ingot");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Makes up tough, explosive-absorbing armor." +
                                "\nA very sturdy alloy fused from the best-bonding matchup out there: Plumbite and Titania!");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            def.saleValue = 3 * ChunkPrice(ChunkTypes.PlumbiaIngot) +
                                3 * ChunkPrice(ChunkTypes.TitaniaIngot);
                            if (!RecipeExistsInFoundry((ChunkTypes)stepper))
                                AddRecipeFoundryFast(new RecipeTable.Recipe.ItemSpec[]
                                {
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.PlumbiaIngot), 3),
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.TitaniaIngot), 3),
                                }, new ItemTypeInfo(ObjectTypes.Chunk, stepper));
                            break;
                        case ChunkTypes._deprecated_ThermiaIngot:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Thermia Ingot");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Heat-resistant materials used for orbital re-entry." +
                                "\nAn alloy with extreme heat resistance: Ignite, Titania, and Oleite make this possible.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            def.saleValue = ChunkPrice(ChunkTypes.PlumbiaIngot) +
                                5 * ChunkPrice(ChunkTypes.OlasticBrick);
                            if (!RecipeExistsInFoundry((ChunkTypes)stepper))
                                AddRecipeFoundryFast(new RecipeTable.Recipe.ItemSpec[]
                                {
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.PlumbiaIngot), 1),
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.OlasticBrick), 5),
                                }, new ItemTypeInfo(ObjectTypes.Chunk, stepper));
                            break;
                        case ChunkTypes._deprecated_FulmeniaIngot:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Fulmenia Ingot");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "A highly-potent energy conductor and storage.\n" +
                                "It is highly radioactive!");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            def.saleValue = ChunkPrice(ChunkTypes.PlumbiaIngot) +
                                5 * ChunkPrice(ChunkTypes.RodiusCapsule);
                            if (!RecipeExistsInFoundry((ChunkTypes)stepper))
                                AddRecipeFoundryFast(new RecipeTable.Recipe.ItemSpec[]
                                {
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.PlumbiaIngot), 1),
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.RodiusCapsule), 5),
                                }, new ItemTypeInfo(ObjectTypes.Chunk, stepper));
                            break;
                        case ChunkTypes._deprecated_FunderiaIngot:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Funderia Ingot");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Can unleash extreme amounts of power safely." +
                                "\nAn alloy with powerful energy bending properties.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            def.saleValue = ChunkPrice(ChunkTypes.PlumbiaIngot) +
                                5 * ChunkPrice(ChunkTypes.IgnianCrystal);
                            if (!RecipeExistsInFoundry((ChunkTypes)stepper))
                                AddRecipeFoundryFast(new RecipeTable.Recipe.ItemSpec[]
                                {
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.PlumbiaIngot), 1),
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.IgnianCrystal), 5),
                                }, new ItemTypeInfo(ObjectTypes.Chunk, stepper));
                            break;
                        case ChunkTypes._deprecated_PenniaIngot:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Pennia Ingot");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Used to manipulate gravity at a finely grained level." +
                                "\nAn alloy with gravity-bending properties.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            def.saleValue = ChunkPrice(ChunkTypes.PlumbiaIngot) +
                                5 * ChunkPrice(ChunkTypes.CelestianCrystal);
                            if (!RecipeExistsInFoundry((ChunkTypes)stepper))
                                AddRecipeFoundryFast(new RecipeTable.Recipe.ItemSpec[]
                                {
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.PlumbiaIngot), 1),
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.CelestianCrystal), 5),
                                }, new ItemTypeInfo(ObjectTypes.Chunk, stepper));
                            break;
                        case ChunkTypes._deprecated_BosoniaIngot:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Bosonia Ingot");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Used in the creation of highly intelligent blocks." +
                                "\nAn alloy capable of building advanced neural pathways.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            def.saleValue = ChunkPrice(ChunkTypes.PlumbiaIngot) +
                                5 * ChunkPrice(ChunkTypes.ErudianCrystal);
                            if (!RecipeExistsInFoundry((ChunkTypes)stepper))
                                AddRecipeFoundryFast(new RecipeTable.Recipe.ItemSpec[]
                                {
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.PlumbiaIngot), 1),
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.ErudianCrystal), 5),
                                }, new ItemTypeInfo(ObjectTypes.Chunk, stepper));
                            break;
                        case ChunkTypes._deprecated_ChristmasPresent1:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Present");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "What's this? A present for me?  You shouldn't have!");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            if (def.saleValue == 0)
                                def.saleValue = 500;
                            break;
                        case ChunkTypes._deprecated_ChristmasPresent2:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Gift");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Whatever is inside is very soft.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            if (def.saleValue == 0)
                                def.saleValue = 500;
                            break;
                        case ChunkTypes._deprecated_ChristmasPresent3:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Bonus");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "It's GOTTA be worth a LOT.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            if (def.saleValue == 0)
                                def.saleValue = 500;
                            break;
                        case ChunkTypes._deprecated_ChristmasPresent4:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Surprise");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Hmm, what lies within?");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            if (def.saleValue == 0)
                                def.saleValue = 500;
                            break;
                        case ChunkTypes._deprecated_Stone:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Stone");
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Absolutely useless.  This has practically no use. Not even the trading stations want it...");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            if (def.saleValue == 0)
                                def.saleValue = -1;
                            break;
                        case ChunkTypes._deprecated_HeartOre:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Heart Ore");//"Cardiacite"
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "An extremely rare ore, \"Cardiacite\" is used in the creation of even greater intelligences.\n" +
                                "Why it has self-repairing properties like Luxite, but unlike it's yellow fellow, it can " +
                                "self-replicate!  \nIf given the right substances and conditions that is.\n\n" +
                                "(WIP) Can be grown with the Lab block.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Raw);
                            if (def.saleValue == 0)
                                def.saleValue = 327;
                            break;
                        case ChunkTypes._deprecated_HeartCrystal:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Heart Crystal");//"Cardiac Prism"
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Used in the creation of reality-bending, self-replicating machines.\nBeware of the singularity!\n" +
                                "Some believe it was the aftermath of a mighty widespread nano-machine race.  That's bogus!");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Refined);
                            def.saleValue = 863;
                            break;
                        case ChunkTypes._deprecated_CommOre:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Comm Ore");//"Magellus Fragment"
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "A mysterious material bursting with tiny explosive electrical sparks.  Looks alien in origin." +
                                "\nAffectionately known as \"Plasmite\" amongst many nations out there, this ore posesses " +
                                "impressive quantum energy funneling properties and is very volatile in nature.\n" +
                                "Rumors say of a planet made almost entirely of it lie somewhere in the cosmos, waiting to be plundered " +
                                "(or 'poloded).\n\n" +
                                "(WIP) Needs to be collected in a Pillars Biome with a Pillar Cracker");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Raw);
                            if (def.saleValue == 0)
                                def.saleValue = 291;
                            break;
                        case ChunkTypes._deprecated_CommCrystal:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Comm Crystal");//Magellus Compound
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "The basis for excessively powerful energy manipulation weapons like E.P.M.C." +
                                "\nComm Crystals have the most fine-grained command over the wide spectrum of energies." +
                                "\nThe reach of energy types this can control appears seemingly limitless.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Refined);
                            if (def.saleValue == 0)
                                def.saleValue = 442;
                            break;
                        case ChunkTypes._deprecated_SenseOre:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Sense Ore");//"Adaranthium"
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "It is prized for it's powerful 3D detection and space-time bending properties." +
                                "\nThis originates from vibrant seas of <b>Adaranth</b>, a distant planet several parsecs away from the Off-World." +
                                "\nMany Kingdoms reside upon the oceanic planet and prosper greatly.\n\n" +
                                "(WIP) Can be found deep in the ocean with Ocean Mode enabled with Water Mod + Lava.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Raw);
                            if (def.saleValue == 0)
                                def.saleValue = 4321;
                            break;
                        case ChunkTypes._deprecated_SenseCrystal:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Sense Crystal");//"Adaranth Ingot"
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Used in the creation of devices which detect in the 3rd dimension as well as bend space-time. " +
                                "\nA beautiful pearl-like crystal worthy for a king or queen." +
                                "\nI have no idea how this works. Stuff this into a block" +
                                " and return to me with the results!");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Refined);
                            if (def.saleValue == 0)
                                def.saleValue = 5263;
                            break;
                        case ChunkTypes._deprecated_SmallMetalOre:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Small Metal Ore");//"Tessellium"
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Extremely tough and durable, this stellar metal ore is no ordinary Earth metal.\n" +
                                "It binds effortlessly to other metals, and becomes even stronger in the process.\n" +
                                "It goes by a great many names.  Some nations call it \"Tessellium\", others call it Bulk Compound.\n\n" +
                                "(WIP) Can be found high in the sky where Spaceships can spawn.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Raw);
                            if (def.saleValue == 0)
                                def.saleValue = 103636;
                            break;
                        case ChunkTypes._deprecated_SmallMetalIngot:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Small Metal Ingot");//Tesseract
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "The ultimate building material.  \nA critical component for any bulky, tough armors " +
                                "that could take an onslaught from an entire armada.");
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Refined);
                            if (def.saleValue == 0)
                                def.saleValue = 243262;
                            break;
                        case ChunkTypes._deprecated_AlloyExpRes:
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkName, stepper, "Alloy Exp Res");//Abstractum
                            LocalisationExt.RegisterRawEng(StringBanks.ChunkDescription, stepper,
                                "Better known as Bulkhead Alloy, this alloy is famous for having \"more hitpoints than god\"," +
                                " seemingly able to take on anything in the galaxy without a dent." +
                                "\nProcuring this however is nearly impossible as it's a deeply guarded trade secret by the highest of " +
                                "empires, and the most mighty of space pirateers."
                /*
            "It's... in a strange state of being...\n\"Abstractus\" is better known for jumping random places when it is restrained." +
            "\n...It's probably best if you don't put this in any tractor beam..."*/);
                            ManSpawn.inst.VisibleTypeInfo.SetDescriptor(hash, ChunkCategory.Component);
                            if (def.saleValue == 0)
                                def.saleValue = 4209001;
                            if (!RecipeExistsInFoundry((ChunkTypes)stepper))
                                AddRecipeFoundryFast(new RecipeTable.Recipe.ItemSpec[]
                                {
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.PlumbiaIngot), 32),
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes.TitaniaIngot), 8),
                                new RecipeTable.Recipe.ItemSpec( new ItemTypeInfo(ObjectTypes.Chunk,
                                (int)ChunkTypes._deprecated_SmallMetalIngot), 4),
                                }, new ItemTypeInfo(ObjectTypes.Chunk, stepper));
                            break;
                    }
                }
                stepper++;
            }
            AddRecipesForOldChunks();
        }

        private static void InsureFoundryRecipes()
        {
            if (foundryRecipes == null)
            {
                foundryRecipes = new RecipeTable.RecipeList()
                {
                    m_Name = "foundry",
                    m_Recipes = new List<RecipeTable.Recipe>(),
                    m_Root = false,
                    m_UseForChunkCategoryCalculation = false,
                    m_UseForMoneyRecipeCalculation = false,
                    m_ValueAddFactor = 3,
                };
            }
        }

        private static void AddRecipesForOldChunks()
        {
            if (modRecipies == null)
            {
                modRecipies = (Dictionary<string, RecipeTable.RecipeList>)recData.GetValue(RecipeManager.inst);
            }
            if (modRecipies != null)
            {
                AddRecipeListDemo(foundryRecipes);
            }
        }
        private static bool RecipeExistsInFoundry(ChunkTypes outputChunk)
        {
            int search = (int)outputChunk;
            foreach (var item in foundryDirect)
            {
                if (item?.m_OutputItems?.FirstOrDefault() != null &&
                    item.m_OutputItems.First().m_Item.ItemType == search)
                    return true;
            }
            return false;
        }
        private static RecipeTable.Recipe AddRecipeFoundryFast(RecipeTable.Recipe.ItemSpec[] inputs, ItemTypeInfo outputChunk)
        {
            return new RecipeTable.Recipe()
            {
                m_EnergyOutput = 0f,
                m_BuildTimeSeconds = 3f,
                m_CalcState = RecipeTable.Recipe.CalcState.NeedUpdate,
                m_EnergyType = TechEnergy.EnergyType.Electric,
                m_MoneyOutput = 0,
                m_OutputType = RecipeTable.Recipe.OutputType.Items,
                m_InputItems = inputs,
                m_OutputItems = new RecipeTable.Recipe.ItemSpec[]
                {
                new RecipeTable.Recipe.ItemSpec(outputChunk, 3),
                },
            };
        }
        private static void AddRecipeListDemo(RecipeTable.RecipeList list)
        {
            if (modRecipies == null)
                modRecipies = (Dictionary<string, RecipeTable.RecipeList>)recData.GetValue(ResourceManager.inst);
            if (modRecipies != null && !modRecipies.ContainsKey(list.m_Name))
                modRecipies.Add(list.m_Name, list);
        }

    }
}
