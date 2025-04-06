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

namespace ScheduleOne_SaveSync
{
    /// <summary>
    /// SyncSaveManager handles injecting SyncSave_1, SyncSave_2, etc. into the LoadManager.
    /// </summary>
    public static class SyncSaveManager
    {
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
                MelonLoader.MelonLogger.Msg($"Save folder not found: {GameSavesPath}");
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
            var original = LoadManager.SaveGames ?? Array.Empty<SaveInfo>();
            var allSaves = new List<SaveInfo>(original);

            foreach (var folder in GetSyncedSaveFolders())
            {
                var info = CreateSaveInfoFromFolder(folder);
                if (info != null)
                    allSaves.Add(info);
            }

            LoadManager.SaveGames = allSaves.ToArray();

            //foreach (var save in allSaves) {
            //    MelonLoader.MelonLogger.Msg(Il2CppNewtonsoft.Json.JsonConvert.SerializeObject(save));
            //}

            MelonLoader.MelonLogger.Msg($"Injected {allSaves.Count - original.Length} synced saves.");
        }

        /// <summary>
        /// Creates a SaveInfo object that mimics the format of native saves.
        /// </summary>
        private static SaveInfo CreateSaveInfoFromFolder(string folderPath)
        {
            string folderName = Path.GetFileName(folderPath);
            if (!folderName.StartsWith(SyncPrefix))
                return null;

            // Attempt to extract the slot number
            int slotNumber = 1000; // Default fallback
            if (int.TryParse(folderName.Substring(SyncPrefix.Length), out int parsed))
                slotNumber = parsed + 1000; // Keep synced slots separate from native slots

            // Default values in case we can't read the files
            string organisationName = "[SyncSave] Unknown";
            float networth = 0f;
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
                    string metadataJson = File.ReadAllText(metadataPath);
                    metaData = Il2CppNewtonsoft.Json.JsonConvert.DeserializeObject<MetaData>(metadataJson);

                    // Extract save version
                    saveVersion = metaData?.LastSaveVersion ?? "0.3.3f15";

                    // Extract creation date
                    if (metaData?.CreationDate != null)
                    {
                        int year = (int)metaData.CreationDate.Year;
                        int month = (int)metaData.CreationDate.Month;
                        int day = (int)metaData.CreationDate.Day;
                        int hour = (int)metaData.CreationDate.Hour;
                        int minute = (int)metaData.CreationDate.Minute;
                        int second = (int)metaData.CreationDate.Second;

                        createdDateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
                    }

                    // Extract last played date
                    if (metaData?.LastPlayedDate != null)
                    {
                        int year = (int)metaData.LastPlayedDate.Year;
                        int month = (int)metaData.LastPlayedDate.Month;
                        int day = (int)metaData.LastPlayedDate.Day;
                        int hour = (int)metaData.LastPlayedDate.Hour;
                        int minute = (int)metaData.LastPlayedDate.Minute;
                        int second = (int)metaData.LastPlayedDate.Second;

                        lastPlayedDateTime = new DateTime(year, month, day, hour, minute, second, DateTimeKind.Utc);
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
                    JObject gameData = Il2CppNewtonsoft.Json.JsonConvert.DeserializeObject<JObject>(gameJson);

                    // Extract organization name and prepend [SyncSave]
                    string orgName = (string)gameData?.SelectToken("OrganisationName")  ?? "Unknown";
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
                MelonLoader.MelonLogger.Error($"Failed to create SaveInfo for {folderPath}: {ex.Message}");

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
