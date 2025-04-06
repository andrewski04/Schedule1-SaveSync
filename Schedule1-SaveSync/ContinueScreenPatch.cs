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

namespace ScheduleOne_SaveSync
{
    internal class ContinueScreenPatch
    {


        [HarmonyPatch(typeof(Il2CppScheduleOne.UI.MainMenu.ContinueScreen), "Update")]
        public class ContinueScreenUpdateOnce
        {
            private static bool injected = false;

            [HarmonyPostfix]
            public static void Postfix(Il2CppScheduleOne.UI.MainMenu.ContinueScreen __instance)
            {

                if (injected) return;

                if (__instance.transform.Find("SyncedSlot_1001") != null)
                {
                    injected = true;
                    return;
                }

                //Utils.DumpUIHierarchy(__instance.transform);

                MelonLogger.Msg("Injecting synced save buttons from Update()");
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
                MelonLogger.Error("Could not find 'Container' under Continue screen.");
                return;
            }

            var template = container.Find("Slot") ?? container.Find("Slot (1)");
            if (template == null)
            {
                MelonLogger.Error("Could not find save slot template.");
                return;
            }

            MelonLogger.Msg("Found container and template slot.");

            int injected = 0;

            foreach (SaveInfo save in LoadManager.SaveGames)
            {
                //MelonLogger.Msg(Il2CppNewtonsoft.Json.JsonConvert.SerializeObject(save));

                if (save is null || save.SaveSlotNumber < 100)
                    continue;

                try
                {
                    var newSlot = UnityEngine.Object.Instantiate(template.gameObject, container);
                    newSlot.name = $"SyncedSlot_{save.SaveSlotNumber}";

                    // fix save slot number styling for sync save slots
                    var indexText = newSlot.transform.Find("Index")?.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                    if (indexText != null)
                    {
                        indexText.SetText(save.SaveSlotNumber.ToString());

                        var rectTransform = indexText.rectTransform;
                        rectTransform.sizeDelta = new Vector2(60f, rectTransform.sizeDelta.y);

                        indexText.fontSize = 18f; 

                        indexText.horizontalAlignment = HorizontalAlignmentOptions.Center;
                        indexText.verticalAlignment = VerticalAlignmentOptions.Middle;
                    }


                    var slotContainer = newSlot.transform.Find("Container");
                    if (slotContainer == null)
                    {
                        MelonLogger.Error("Slot missing 'Container' child.");
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
                            MelonLogger.Msg($"Launching synced save: {save.SavePath}");
                            LoadManager.Instance.StartGame(save, false);
                        }));
                    }

                    injected++;
                }
                catch (Exception ex)
                {
                    MelonLogger.Error($"Failed to inject a save slot: {ex.Message}");
                }
            }

            MelonLogger.Msg($"Injected {injected} synced save slots.");
        }




    }
}
