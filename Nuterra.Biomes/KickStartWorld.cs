using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public override void DeInit()
        {
        }

        public override void Init()
        {
            DebugWorld.Log(ManModWorld.Tag, "CALLED INIT");
            if (isInit)
                return;
            try
            {
                TerraTechETCUtil.ModStatusChecker.EncapsulateSafeInit(ManModWorld.ModName,
                    ManModWorld.Initiate, ManModWorld.DeInitiate);
                oInst = new ModDataHandle(ManModWorld.ModName);
            }
            catch { }
            //ManRails.LateInit();
            isInit = true;
        }

    }
}
