A simple mod to sync your Schedule I save files with friends, similar to the Ungrounded shared save system.


## Continue Menu UI Dump 

 - Continue
   - Background
     - Image
   - Title
   - Beta
   - Container
     - Slot
       - Index
       - Container
         - Button
         - Organisation
         - NetWorth
           - Text
         - Created
           - Text
         - LastPlayed
           - Text
         - Version
     - Slot (1)
       - Index
       - Container
         - Button
         - Organisation
         - NetWorth
           - Text
         - Created
           - Text
         - LastPlayed
           - Text
         - Version
     - Slot (2)
       - Index
       - Container
         - Button
         - Organisation
         - NetWorth
           - Text
         - Created
           - Text
         - LastPlayed
           - Text
         - Version
     - Slot (3)
       - Index
       - Container
         - Button
         - Organisation
         - NetWorth
           - Text
         - Created
           - Text
         - LastPlayed
           - Text
         - Version
     - Slot (4)
       - Index
       - Container
         - Button
         - Organisation
         - NetWorth
           - Text
         - Created
           - Text
         - LastPlayed
           - Text
         - Version

[13:08:59.925] Injecting synced save buttons from Update()
[13:08:59.927] [ScheduleI-SaveSync] Found container and template slot.
[13:08:59.927] [Il2CppInterop] During invoking native->managed trampoline
System.NullReferenceException: Object reference not set to an instance of an object.
   at ScheduleOne_SaveSync.ContinueScreenPatch.InjectSyncedButtons(ContinueScreen screen)
   at ScheduleOne_SaveSync.ContinueScreenPatch.ContinueScreenUpdateOnce.Postfix(ContinueScreen __instance)
   at DMD<Il2CppScheduleOne.UI.MainMenu.ContinueScreen::Update>(ContinueScreen this)
   at (il2cpp -> managed) Update(IntPtr , Il2CppMethodInfo* )


