using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Il2CppScheduleOne.Persistence;
using Il2CppScheduleOne.Persistence.Datas;
using UnityEngine;

namespace Schedule1_SaveSync
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

            MelonLoader.MelonLogger.Msg($"[SaveSync] Injected {allSaves.Count - original.Length} synced saves.");
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

            string[] files = Directory.GetFiles(folderPath, "*.json", SearchOption.TopDirectoryOnly);
            if (files.Length == 0)
                return null;

            DateTime lastModifiedSystemDateTime = files
                .Select(f => File.GetLastWriteTimeUtc(f))
                .OrderByDescending(d => d)
                .FirstOrDefault();

            Il2CppSystem.DateTime lastPlayed = new Il2CppSystem.DateTime(lastModifiedSystemDateTime.Ticks);
            Il2CppSystem.DateTime created = new Il2CppSystem.DateTime(lastModifiedSystemDateTime.AddMinutes(-5).Ticks);

            string organisationName = "[Synced Save]";
            float networth = 0f;
            string saveVersion = "1.0";
            MetaData metaData = null; // optionally load Meta.json later

            try
            {
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
                MelonLoader.MelonLogger.Error($"[SaveSync] Failed to create SaveInfo for {folderPath}: {ex.Message}");
                return null;
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
