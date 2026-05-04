using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Nuterra.World.Biomes;
using TerraTechETCUtil;
using UnityEngine;

namespace Nuterra.World
{
    public static class DebugWorld
    {
        public const string ModName = ManModWorld.ModName;

        internal static bool ShouldLog = true;
        internal static bool ShouldLogRails = true;
        internal static bool LogAll = false;
        private const bool LogDev = false;

        internal static void Info(string tag, string message)
        {
            if (!ShouldLog || !LogAll)
                return;
            Debug.Log(string.Format("[" + ModName + "{0}] {1}", tag, message));
        }
        internal static void Log(string tag, string message)
        {
            if (!ShouldLog)
                return;
            Debug.Log(string.Format("[" + ModName + "{0}] {1}", tag, message));
        }
        internal static void Log(string tag, Exception e)
        {
            if (!ShouldLog)
                return;
            Debug.Log(string.Format("[" + ModName + "{0}] {1}", tag, e));
        }
        internal static void Assert(string tag, string message)
        {
            if (!ShouldLog)
                return;
            Debug.Log(string.Format("[" + ModName + "{0}] {1}\n{2}", tag, StackTraceUtility.ExtractStackTrace().ToString(), message));
        }
        internal static void Assert(string tag, bool shouldAssert, string message)
        {
            if (!ShouldLog || !shouldAssert)
                return;
            Debug.Log(string.Format("[" + ModName + "{0}] {1}\n{2}", tag, StackTraceUtility.ExtractStackTrace().ToString(), message));
        }
        internal static void LogError(string tag, string message)
        {
            if (!ShouldLog)
                return;
            Debug.Log(string.Format("[" + ModName + "{0}] {1}\n{2}", tag, StackTraceUtility.ExtractStackTrace().ToString(), message));
        }
        internal static void LogDevOnly(string tag, string message)
        {
            if (!LogDev)
                return;
            Debug.Log(string.Format("[" + ModName + "{0}] {1}\n{2}", tag, StackTraceUtility.ExtractStackTrace().ToString(), message));
        }
        internal static void FatalError(string e)
        {
            try
            {
                ManModGUI.ShowErrorPopup("[" + ModName + "] ENCOUNTERED CRITICAL ERROR: " + e + StackTraceUtility.ExtractStackTrace());
            }
            catch { }
            Debug.Log("[" + ModName + "] ENCOUNTERED CRITICAL ERROR: " + e);
            Debug.Log("[" + ModName + "] MAY NOT WORK PROPERLY AFTER THIS ERROR, PLEASE REPORT!");
            Debug.Log("[" + ModName + "] STACKTRACE: " + StackTraceUtility.ExtractStackTrace());
        }
        internal static void Exception(bool shouldAssert, string e)
        {
            if (shouldAssert)
                throw new Exception(e);
        }
        internal static void LogPopupToPlayer(string Warning, bool IsSeriousError = false, Action OnFixRequested = null)
        {
            ManModGUI.ShowErrorPopup(Warning, IsSeriousError, OnFixRequested);
        }

        internal static void DrawDirIndicator(Vector3 posScene, Vector3 vectorWorld, Color color, float duration = 2) =>
            DebugExtUtilities.DrawDirIndicator(posScene, vectorWorld, color, duration);
    }
}
