using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.UI.MainMenu;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;
using Il2CppTMPro;

namespace Schedule1_SaveSync
{

    internal class ContinueScreenPatch
    {
        // util method to dump the UI hierarchy for debugging
        public static void DumpUIHierarchy(Transform root, int depth = 0)
        {
            string indent = new string(' ', depth * 2);
            MelonLogger.Msg($"{indent}- {root.name}");

            for (int i = 0; i < root.childCount; i++)
                DumpUIHierarchy(root.GetChild(i), depth + 1);
        }


        [HarmonyPatch(typeof(Il2CppScheduleOne.UI.MainMenu.ContinueScreen), "Update")]
        public class ContinueScreenUpdateOnce
        {
            private static bool injected = false;

            [HarmonyPostfix]
            public static void Postfix(Il2CppScheduleOne.UI.MainMenu.ContinueScreen __instance)
            {

                if (injected) return;

                //if (__instance.transform.Find("SyncedSlot_1001") != null)
                //{
                //    injected = true;
                //    return;
                //}

                //DumpUIHierarchy(__instance.transform);

                MelonLogger.Msg("[SaveSync] Injecting synced save buttons from Update()");
                InjectSyncedButtons(__instance);
                injected = true;
            }
        }

        public static void InjectSyncedButtons(Il2CppScheduleOne.UI.MainMenu.ContinueScreen screen)
        {
            var root = screen.transform;
            var container = root.Find("Container");
            if (container == null)
            {
                MelonLogger.Error("[SaveSync] Could not find 'Container' under Continue screen.");
                return;
            }

            var template = container.Find("Slot") ?? container.Find("Slot (1)");
            if (template == null)
            {
                MelonLogger.Error("[SaveSync] Could not find save slot template.");
                return;
            }

            MelonLogger.Msg("[SaveSync] Found container and template slot.");

            int injected = 0;

            foreach (var save in LoadManager.SaveGames)
            {
                if (save.SaveSlotNumber < 1000)
                    continue;

                try
                {
                    var newSlot = UnityEngine.Object.Instantiate(template.gameObject, container);
                    newSlot.name = $"SyncedSlot_{save.SaveSlotNumber}";

                    var slotContainer = newSlot.transform.Find("Container");
                    if (slotContainer == null)
                    {
                        MelonLogger.Error("[SaveSync] Slot missing 'Container' child.");
                        continue;
                    }

                    var orgText = slotContainer.Find("Organisation")?.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                    var createdText = slotContainer.Find("Created/Text")?.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                    var lastPlayedText = slotContainer.Find("LastPlayed/Text")?.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                    var networthText = slotContainer.Find("NetWorth/Text")?.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                    var versionText = slotContainer.Find("Version")?.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                    var button = slotContainer.Find("Button")?.GetComponent<UnityEngine.UI.Button>();

                    if (orgText != null) orgText.text = save.OrganisationName;
                    if (createdText != null) createdText.text = save.DateCreated.ToLocalTime().ToString("g");
                    if (lastPlayedText != null) lastPlayedText.text = save.DateLastPlayed.ToLocalTime().ToString("g");
                    if (networthText != null) networthText.text = $"{save.Networth:N0}";
                    if (versionText != null) versionText.text = save.SaveVersion;

                    if (button != null)
                    {
                        button.onClick.RemoveAllListeners();
                        button.onClick.AddListener(new System.Action(() =>
                        {
                            MelonLogger.Msg($"[SaveSync] Launching synced save: {save.SavePath}");
                            LoadManager.Instance.StartGame(save, false);
                        }));
                    }

                    injected++;
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"[SaveSync] Failed to inject a save slot: {ex.Message}");
                }
            }

            MelonLogger.Msg($"[SaveSync] Injected {injected} synced save slots.");
        }




    }
}
