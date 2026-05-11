using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using Nuterra.World.Biomes;
using TerraTechETCUtil;
using UnityEngine;

namespace Nuterra.World
{
    public class KickStartWorld : ModBase
    {

        internal static ModDataHandle oInst;

        bool isInit = false;

        public override bool HasEarlyInit()
        {
            DebugWorld.Log(ManModWorld.Tag, "CALLED");
            return true;
        }
        public override void EarlyInit()
        {
            DebugWorld.Log(ManModWorld.Tag, "CALLED EARLYINIT");
            if (isInit)
                return;
            try
            {
                ModStatusChecker.EncapsulateSafeInit(ManModWorld.ModID,
                    ManModWorld.Initiate, ManModWorld.DeInitiate);
#if DEBUG
                ((ModStatusChecker)AccessTools.Field(typeof(ModStatusChecker), "inst").GetValue(null)).CheckOnlineStatus = false;
#endif
                oInst = new ModDataHandle(ManModWorld.ModID);
            }
            catch { }
            isInit = true;
        }
        public override void Init()
        {
            DebugWorld.Log(ManModWorld.Tag, "CALLED INIT");
            if (isInit)
                return;
            try
            {
                ModStatusChecker.EncapsulateSafeInit(ManModWorld.ModID,
                    ManModWorld.Initiate, ManModWorld.DeInitiate);
                oInst = new ModDataHandle(ManModWorld.ModID);
            }
            catch { }
            isInit = true;
        }
        public override void DeInit()
        {
        }
    }
}
