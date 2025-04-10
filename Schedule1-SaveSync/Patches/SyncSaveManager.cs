using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using UnityEngine;
using Il2CppNewtonsoft;
using Il2CppNewtonsoft.Json;
using Il2CppNewtonsoft.Json.Linq;
using MelonLoader;
using Harmony;
using Il2CppFluffyUnderware.DevTools.Extensions;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using System.Runtime.CompilerServices;
using Il2CppScheduleOne.UI.MainMenu;

namespace Schedule1_SaveSync.Patches
{
    /// <summary>
    /// SyncSaveManager handles injecting SyncSave_1, SyncSave_2, etc. into the LoadManager.
    /// </summary>
    public static class SyncSaveManager
    {
        public static SaveDisplay latestSaveDisplay;

        private const string SyncPrefix = "SyncSave_";
        private static readonly string GameSavesPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "TVGS", "Schedule I", "Saves");

        /// <summary>
        /// Finds all synced save folders (SyncSave_1, SyncSave_2, etc.)
        /// </summary>
        public static List<string> GetSyncedSaveFolders()
        {
            if (!Directory.Exists(GameSavesPath))
            {
                MelonLogger.Msg($"Save folder not found: {GameSavesPath}");
                return new List<string>();
            }

            var allSyncFolders = new List<string>();

            foreach (var steamIdFolder in Directory.GetDirectories(GameSavesPath)
                                                   .Where(f => ulong.TryParse(Path.GetFileName(f), out _)))
            {
                var syncSaves = Directory.GetDirectories(steamIdFolder, SyncPrefix + "*");
                allSyncFolders.AddRange(syncSaves);
            }

            return allSyncFolders.OrderBy(f => f).ToList();
        }

        /// <summary>
        /// Injects synced save folders into LoadManager.SaveGames.
        /// </summary>
        public static void InjectIntoLoadManager()
        {
            MelonLogger.Msg(LoadManager.SaveGames.Count);
            var original = LoadManager.SaveGames ?? Array.Empty<SaveInfo>();
            var allSaves = new List<SaveInfo>(original);

            //foreach (var save in allSaves)
            //{
            //    MelonLogger.Msg(Il2CppNewtonsoft.Json.JsonConvert.SerializeObject(save));
            //}

            Il2CppReferenceArray<SaveInfo> newSi = new Il2CppReferenceArray<SaveInfo>(8);
            for (int i = 0; i < LoadManager.SaveGames.Count; i++)
            {
                newSi[i] = LoadManager.SaveGames[i];
            }

            LoadManager.SaveGames = newSi;

            int injectedCount = 0;
            foreach (var folder in GetSyncedSaveFolders())
            {
                // Check if this save path is already in the list
                if (allSaves.Any(save => save != null && save.SavePath == folder))
                    continue;

                var info = CreateSaveInfoFromFolder(folder);
                if (info != null)
                {
                    LoadManager.SaveGames[info.SaveSlotNumber] = info;
                    MelonLogger.Msg($"Injected {folder}, now: {LoadManager.SaveGames.Count}");
                    injectedCount++;
                }
            }

            if (latestSaveDisplay)
            {
                latestSaveDisplay.Refresh();

            }

            MelonLogger.Msg($"Injected {injectedCount} synced saves.");

        }

        /// <summary>
        /// Creates a SaveInfo object that mimics the format of native saves.
        /// </summary>
        private static SaveInfo CreateSaveInfoFromFolder(string folderPath)
        {
            string folderName = Path.GetFileName(folderPath);
            if (!folderName.StartsWith(SyncPrefix))
                return null;

            int slotNumber = 5; // Default fallback
            if (int.TryParse(folderName.Substring(SyncPrefix.Length), out int parsed))
                slotNumber = parsed; // Keep synced slots separate from native slots

            // Default values in case we can't read the files
            string organisationName = "[SyncSave] Unknown";
            float networth = 69f;
            string saveVersion = "1.0";
            DateTime createdDateTime = DateTime.UtcNow;
            DateTime lastPlayedDateTime = DateTime.UtcNow;
            MetaData metaData = null;

            try
            {
                // Try to read metadata.json
                string metadataPath = Path.Combine(folderPath, "Metadata.json");
                if (File.Exists(metadataPath))
                {
                    Il2CppSystem.DateTime? il2created = null;
                    Il2CppSystem.DateTime? il2lastPlayed = null;
                    string metadataJson = File.ReadAllText(metadataPath);
                    metaData = JsonConvert.DeserializeObject<MetaData>(metadataJson);

                    // Extract save version
                    saveVersion = metaData?.LastSaveVersion ?? "0.3.3f15";

                    // Extract creation date
                    if (metaData?.CreationDate != null)
                    {
                        int year = metaData.CreationDate.Year;
                        int month = metaData.CreationDate.Month;
                        int day = metaData.CreationDate.Day;
                        int hour = metaData.CreationDate.Hour;
                        int minute = metaData.CreationDate.Minute;
                        int second = metaData.CreationDate.Second;

                        createdDateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                        il2created = new Il2CppSystem.DateTime(year, month, day, hour, minute, second, Il2CppSystem.DateTimeKind.Utc);
                    }

                    // Extract last played date
                    if (metaData?.LastPlayedDate != null)
                    {
                        int year = metaData.LastPlayedDate.Year;
                        int month = metaData.LastPlayedDate.Month;
                        int day = metaData.LastPlayedDate.Day;
                        int hour = metaData.LastPlayedDate.Hour;
                        int minute = metaData.LastPlayedDate.Minute;
                        int second = metaData.LastPlayedDate.Second;

                        lastPlayedDateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                        il2lastPlayed = new Il2CppSystem.DateTime(year, month, day, hour, minute, second, Il2CppSystem.DateTimeKind.Utc);

                    }

                }
                else
                {
                    // Fallback to file timestamps if metadata.json doesn't exist
                    string[] files = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
                    if (files.Length > 0)
                    {
                        lastPlayedDateTime = files
                            .Select(f => File.GetLastWriteTimeUtc(f))
                            .OrderByDescending(d => d)
                            .FirstOrDefault();

                        createdDateTime = files
                            .Select(f => File.GetCreationTimeUtc(f))
                            .OrderBy(d => d)
                            .FirstOrDefault();
                    }
                }

                // Try to read Game.json for organization name
                string gamePath = Path.Combine(folderPath, "Game.json");
                if (File.Exists(gamePath))
                {
                    string gameJson = File.ReadAllText(gamePath);
                    JObject gameData = JsonConvert.DeserializeObject<JObject>(gameJson);

                    // Extract organization name and prepend [SyncSave]
                    string orgName = (string)gameData?.SelectToken("OrganisationName") ?? "Unknown";
                    organisationName = $"[SyncSave] {orgName}";
                }

                // Convert DateTime to Il2CppSystem.DateTime
                Il2CppSystem.DateTime created = new Il2CppSystem.DateTime(createdDateTime.Ticks);
                Il2CppSystem.DateTime lastPlayed = new Il2CppSystem.DateTime(lastPlayedDateTime.Ticks);

                return new SaveInfo(
                    folderPath,
                    slotNumber,
                    organisationName,
                    created,
                    lastPlayed,
                    networth,
                    saveVersion,
                    metaData
                );
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to create SaveInfo for {folderPath}: {ex.Message}");

                // Fallback to basic info if there's an error
                Il2CppSystem.DateTime created = new Il2CppSystem.DateTime(createdDateTime.Ticks);
                Il2CppSystem.DateTime lastPlayed = new Il2CppSystem.DateTime(lastPlayedDateTime.Ticks);

                return new SaveInfo(
                    folderPath,
                    slotNumber,
                    organisationName,
                    created,
                    lastPlayed,
                    networth,
                    saveVersion,
                    metaData
                );
            }
        }

        public static bool IsSyncSlot(string folderPath)
        {
            return Path.GetFileName(folderPath).StartsWith(SyncPrefix, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetSyncSlotPath(string slotName)
        {
            return Path.Combine(GameSavesPath, slotName);
        }

        public static string GetNextAvailableSyncSlotName()
        {
            int i = 1;
            while (Directory.Exists(Path.Combine(GameSavesPath, $"{SyncPrefix}{i}")))
                i++;

            return $"{SyncPrefix}{i}";
        }
    }
}
