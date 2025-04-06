using MelonLoader;
using HarmonyLib;
using System.Reflection;
using Il2CppScheduleOne.UI.MainMenu;
using UnityEngine;
using System.Collections.Generic;
using Il2CppScheduleOne.Persistence;

[assembly: MelonInfo(typeof(ScheduleOne_SaveSync.Core), "ScheduleI-SaveSync", "1.0.0", "Andrew Houser", null)]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ScheduleOne_SaveSync
{
    public class Core : MelonMod
    {
        private static readonly HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("com.andrew.schedule1savesync");

        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Schedule1-SaveSync initialized.");
            ApplyPatches();
        }

        private void ApplyPatches()
        {
            LoggerInstance.Msg("Applying patches...");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            LoggerInstance.Msg("Patches applied successfully.");
        }
    }

    [HarmonyPatch(typeof(LoadManager), "RefreshSaveInfo")]
    public class InjectSyncSavesPatch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            SyncSaveManager.InjectIntoLoadManager();
        }
    }

}