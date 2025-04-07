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
using UnityEngine.Rendering;
using Unity.Services.Analytics.Internal;
using UnityEngine.Device;
using System.Transactions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppFluffyUnderware.DevTools.Extensions;

namespace ScheduleOne_SaveSync
{
    internal class ContinueScreenPatch
    {

        [HarmonyPatch(typeof(Il2CppScheduleOne.UI.MainMenu.MainMenuScreen),"Awake")]
        public class MainMenuAwake
        {
            private static bool injected = false;
            [HarmonyPostfix] 
            private static void Postfix(Il2CppScheduleOne.UI.MainMenu.MainMenuScreen __instance)
            {
                if (!injected)
                {
                    Utils.DumpUIHierarchy(__instance.transform);
                    injected = true;
                }
            }
        }
        [HarmonyPatch(typeof(Il2CppScheduleOne.UI.MainMenu.SaveDisplay), "Refresh")]
        public class ContinueScreenUpdateOnce
        {
            private static bool injected = false;

            [HarmonyPrefix]
            public static void Postfix(Il2CppScheduleOne.UI.MainMenu.SaveDisplay __instance)
            {
                SyncSaveManager.latestSD = __instance;
                if (!injected)
                {
                    MelonLogger.Msg("Injecting synced save buttons from Update()");
                    InjectExtraButtons(__instance);

                    //ils.DumpUIHierarchy(__instance.transform);
                } 
            }
        }
        public static void InjectExtraButtons(Il2CppScheduleOne.UI.MainMenu.SaveDisplay saveDisplay)
        {
            //var root = saveDisplay.transform;
   
            /*var container = root.Find("Container");
            if (container == null)
            {
                MelonLogger.Error("Could not find 'Container' under Continue screen.");
               // return;
            }

            //var template = container.Find("Slot") ?? container.Find("Slot (1)");
            if (template == null)
            {
                MelonLogger.Error("Could not find save slot template.");
                //return;
            }*/
            
            if(saveDisplay.Slots.Count < LoadManager.SaveGames.Count)
            {
                MelonLogger.Msg("UPDATING SLOTS COUNT AND ARRAY NOW");
                saveDisplay.Slots = createNewSlots(saveDisplay);
            }


            foreach (SaveInfo save in LoadManager.SaveGames)
            {
                if(save is null)
                {
                    
                    continue;
                }
                
                    try
                    {
                        MelonLogger.Msg("Trying to set save: " + (save.SaveSlotNumber-1).ToString());
                        saveDisplay.SetDisplayedSave(save.SaveSlotNumber-1, save);
                        MelonLogger.Msg("Set successfully?");
                    } catch(Exception e) {
                        Console.WriteLine(e.ToString());
                    }
                
            }
        }

        static private Il2CppReferenceArray<RectTransform> createNewSlots(SaveDisplay saveDisplay)
        {
            Il2CppReferenceArray<RectTransform> newSlots = new Il2CppReferenceArray<RectTransform>(8);
            for (int i = 0; i < saveDisplay.Slots.Count; i++)
            {
                newSlots[i] = saveDisplay.Slots[i];
            }
            for (int i = saveDisplay.Slots.Count; i < newSlots.Count; i++)
            {
                // construct new slots that werent there prior

                //copy last slot
                newSlots[i] = saveDisplay.Slots.Last().DuplicateGameObject<RectTransform>(saveDisplay.gameObject.transform);
                //change index text
                var indexText = newSlots[i].transform.Find("Index")?.GetComponent<Il2CppTMPro.TextMeshProUGUI>();
                indexText.SetText((i + 1).ToString());

                UnityEngine.UI.Button b = newSlots[i].Find("Container")?.Find("Button")?.GetComponent<UnityEngine.UI.Button>();
                b.m_OnClick = new UnityEngine.UI.Button.ButtonClickedEvent();
                b.onClick.AddListener(new System.Action(() =>
                {
                    //why it must be subtracted by two, i havent a clue
                    LoadManager.Instance.StartGame(LoadManager.SaveGames[i - 2]);
                }));
            }
            return newSlots;
        }
        /*public static void InjectSyncedButtons(Il2CppScheduleOne.UI.MainMenu.ContinueScreen screen)
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

                if (save is null || save.SaveSlotNumber < -1)
                    continue;
                Il2CppScheduleOne.UI.MainMenu.SaveDisplay.
                /*try
                {
                    var newSlot = UnityEngine.Object.Instantiate(template.gameObject, container);
                    newSlot.name = $"Slot ({save.SaveSlotNumber})";

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
                        Button ogButton = template?.Find("Container")?.Find("Button")?.GetComponent<UnityEngine.UI.Button>();
                        button.m_OnClick = new Button.ButtonClickedEvent();
                        
                        button.onClick.AddListener(new System.Action(() =>
                        {
                            MelonLogger.Msg($"Launching synced save: {save.SavePath}");
                            //ogButton.onClick.Invoke();

                            //screen.LoadGame(1);
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
            LoadManager.instance.RefreshSaveInfo();
            LoadManager.Instance.RefreshSaveInfo();
            MelonLogger.Msg($"Injected  synced save slots.");
        }*/




    }
}
