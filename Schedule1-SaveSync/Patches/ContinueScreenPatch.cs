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

namespace Schedule1_SaveSync.Patches
{
    internal class ContinueScreenPatch
    {
        [HarmonyPatch(typeof(SaveDisplay), "Refresh")]
        public class ContinueScreenUpdateOnce
        {
            private static bool injected = false;

            [HarmonyPrefix]
            public static void Postfix(SaveDisplay __instance)
            {
                SyncSaveManager.latestSaveDisplay = __instance;

                MelonLogger.Msg("Injecting synced save buttons from Update()");
                InjectExtraButtons(__instance);

            }
        }
        public static void InjectExtraButtons(SaveDisplay saveDisplay)
        {

            // ensure that there are ample buttons to add, if you have SaveGames.Count larger than the actual ui slots you will encounter errors
            if (saveDisplay.Slots.Count < LoadManager.SaveGames.Count)
            {
                MelonLogger.Msg("UPDATING SLOTS COUNT AND ARRAY NOW");
                saveDisplay.Slots = createNewSlots(saveDisplay);
            }


            foreach (SaveInfo save in LoadManager.SaveGames)
            {
                if (save is null)
                {

                    continue;
                }

                try
                {
                    MelonLogger.Msg("Trying to set save: " + (save.SaveSlotNumber - 1).ToString());
                    saveDisplay.SetDisplayedSave(save.SaveSlotNumber - 1, save);
                    MelonLogger.Msg("Set successfully?");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

            }
        }

        static private Il2CppReferenceArray<RectTransform> createNewSlots(SaveDisplay saveDisplay)
        {
            Il2CppReferenceArray<RectTransform> newSlots = new Il2CppReferenceArray<RectTransform>(8);
            for (int slotIndex = 0; slotIndex < saveDisplay.Slots.Count; slotIndex++)
            {
                newSlots[slotIndex] = saveDisplay.Slots[slotIndex];
            }
            for (int i = saveDisplay.Slots.Count; i < newSlots.Count; i++)
            {
                // construct new slots that werent there prior

                //copy last slot (most likely to be empty)
                newSlots[i] = saveDisplay.Slots.Last().DuplicateGameObject<RectTransform>(saveDisplay.gameObject.transform);
                //change index text
                var indexText = newSlots[i].transform.Find("Index")?.GetComponent<TextMeshProUGUI>();
                indexText.SetText((i + 1).ToString());

                Button b = newSlots[i].Find("Container")?.Find("Button")?.GetComponent<Button>();

                //this is the proper way to clear out the preexisting events on the copied button
                b.m_OnClick = new Button.ButtonClickedEvent();
                //add new listener for loading the save 
                b.onClick.AddListener(new Action(() =>
                {
                    //why it must be subtracted by two, i havent a clue
                    LoadManager.Instance.StartGame(LoadManager.SaveGames[i - 2]);
                }));
            }
            return newSlots;
        }
    }
}
