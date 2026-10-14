using System.Globalization;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using hamburbur.GUI;
using hamburbur.Managers;
using hamburbur.Mod_Backend;
using hamburbur.Tools;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

internal enum SlapBattlesProgressCommand
{
    GrantAdmin      = 1,
    UnlockAll       = 2,
    SetSlaps        = 3,
    AddSlaps        = 4,
    UnlockAdminSlap = 5,
    UnlockNamedSlap = 6,
    EquipNamedSlap = 7,
    VisitAdminPanel = 9,
    SetCorn = 10,
    AddCorn = 11,
}

internal static class SlapBattlesLocalProgress
{
    private static string cachedSlapScript;
    private static string installedBridgeScript;
    private static (string Name, int Index)[] cachedPlayableSlaps = [];
    private const string EventFunction = "function onEvent(tag, data)";
    private const string EventTag      = "hamburburSlapCmdV1";
    private const string LocalEventFunction = """
                                              function onEvent(tag, data, sender)
                                                  -- hamburbur local progress bridge: network events always have a sender.
                                                  if tag == "hamburburSlapCmdV1" then
                                                      if sender ~= nil then return end
                                                      local ok, err = pcall(function()
                                                          local action = data[1]
                                                          local value = data[2]
                                                          if action == 1 then
                                                              grantAdmin()
                                                          elseif action == 2 then
                                                              local changed = false
                                                              for i = 1, #slaps do
                                                                  if not slaps[i].retired and not unlockedSlaps[i] then
                                                                      unlockedSlaps[i] = true
                                                                      changed = true
                                                                  end
                                                              end
                                                              if changed then
                                                                  if sceneReady then applyEquipButtonVisibility() end
                                                                  saveSaveData()
                                                              end
                                                          elseif action == 3 and isSaveNumber(value) then
                                                              slapCount = value
                                                              local changed = refreshUnlocks()
                                                              if changed and sceneReady then applyEquipButtonVisibility() end
                                                              updateSlapsText()
                                                              saveSaveData()
                                                          elseif action == 4 and isSaveNumber(value) then
                                                              slapCount = math.min(SAVE_MOD - 1, slapCount + value)
                                                              local changed = refreshUnlocks()
                                                              if changed and sceneReady then applyEquipButtonVisibility() end
                                                              updateSlapsText()
                                                              saveSaveData()
                                                          elseif action == 5 then
                                                              for i = 1, #slaps do
                                                                  if slaps[i].unlock == "admin" and not slaps[i].retired and not unlockedSlaps[i] then
                                                                      unlockedSlaps[i] = true
                                                                      if sceneReady then applyEquipButtonVisibility() end
                                                                      saveSaveData()
                                                                  end
                                                              end
                                                          elseif (action == 6 or action == 7) and type(value) == "string" then
                                                              local wanted = string.lower(value)
                                                              for i = 1, #slaps do
                                                                  local slap = slaps[i]
                                                                  if not slap.retired and string.lower(slap.name) == wanted then
                                                                      if action == 6 then
                                                                          if not unlockedSlaps[i] then
                                                                              unlockedSlaps[i] = true
                                                                              if sceneReady then applyEquipButtonVisibility() end
                                                                              saveSaveData()
                                                                          end
                                                                      elseif unlockedSlaps[i] and sceneReady then
                                                                          equipSlap(i)
                                                                      end
                                                                      break
                                                                  end
                                                              end
                                                          elseif action == 9 and sceneReady then
                                                              grantAdmin()
                                                              if adminPanel then
                                                                  teleportSafely(adminPanel.position + Vec3.new(0, 1, 0), false, false, "lift")
                                                              end
                                                          elseif action == 10 and isSaveNumber(value) and corn then
                                                              corn.count = math.min(value, 16777215)
                                                              local changed = refreshUnlocks()
                                                              if changed and sceneReady then applyEquipButtonVisibility() end
                                                              updateCornText()
                                                              saveSaveData()
                                                          elseif action == 11 and isSaveNumber(value) and corn then
                                                              addCorn(value)
                                                          end
                                                      end)
                                                      if not ok then print("SlapBattles local command error: " .. tostring(err)) end
                                                      return
                                                  end
                                              """;
    private static readonly string LocalEventFunctionV11 = LocalEventFunction
            .Replace("grantAdmin()", "grantAdm()", StringComparison.Ordinal)
            .Replace("applyEquipButtonVisibility()", "applyEqBtnVis()", StringComparison.Ordinal)
            .Replace("isSaveNumber(value)", "isSaveNum(value)", StringComparison.Ordinal)
            .Replace("equipSlap(i)", "eqSlap(i)", StringComparison.Ordinal)
            .Replace("adminPanel", "admPanel", StringComparison.Ordinal);

    internal static bool TrySend(SlapBattlesProgressCommand command, int value = 0)
        => TrySend(command, (object)(double)value);

    internal static bool TrySend(SlapBattlesProgressCommand command, string value)
        => TrySend(command, (object)value);

    private static bool EnsureBridge()
    {
        string script = CustomGameMode.LuaScript;
        if (!SlapBattlesNetwork.IsInMap || CustomGameMode.gameScriptRunner?.ShouldTick != true)
            return false;
        if (ReferenceEquals(script, installedBridgeScript)) return true;

        string bridge = script.Contains("function grantAdm()", StringComparison.Ordinal)
                                ? LocalEventFunctionV11 : LocalEventFunction;
        if (!CustomMapUtils.TryReplaceCustomScriptFragment(EventFunction, bridge) ||
            CustomGameMode.gameScriptRunner?.ShouldTick != true)
            return false;

        installedBridgeScript = CustomGameMode.LuaScript;
        return true;
    }

    private static bool TrySend(SlapBattlesProgressCommand command, object value)
    {
        if (!EnsureBridge()) return false;

        // This is an in-memory Lua event, NOT a raise event
        LuauVm.eventQueue.Enqueue([EventTag, (double)command, value,]);

        return true;
    }

    internal static bool HasPlayableSlap(string name) => PlayableSlapIndex(name) > 0;

    internal static int PlayableSlapIndex(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return 0;
        foreach ((string slapName, int index) in PlayableSlaps())
            if (string.Equals(slapName, name, StringComparison.OrdinalIgnoreCase))
                return index;
        return 0;
    }

    internal static (string Name, int Index)[] PlayableSlaps()
    {
        string script = CustomGameMode.LuaScript;
        if (ReferenceEquals(script, cachedSlapScript)) return cachedPlayableSlaps;
        cachedSlapScript = script;
        cachedPlayableSlaps = [];
        if (string.IsNullOrEmpty(script)) return cachedPlayableSlaps;
        int start = script.IndexOf("local slaps = {", StringComparison.Ordinal);
        if (start < 0) return cachedPlayableSlaps;
        Match end = Regex.Match(script[start..], @"(?m)^\}");
        if (!end.Success) return cachedPlayableSlaps;
        List<(string Name, int Index)> playable = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);
        int index = 0;
        foreach (Match match in Regex.Matches(script.Substring(start, end.Index),
                         @"\{\s*name\s*=\s*""([^""]+)""[^\r\n]*"))
        {
            index++;
            string name = match.Groups[1].Value;
            if (!Regex.IsMatch(match.Value, @"\bretired\s*=\s*true\b") && seen.Add(name))
                playable.Add((name, index));
        }
        cachedPlayableSlaps = playable.ToArray();
        return cachedPlayableSlaps;
    }

    internal static void Notify(string message) =>
            NotificationManager.SendNotification("Slap Battles", message, 4f, false, false);
}

[hamburburmod(            "Open Admin Panel",   "Grant local admin, show the map's AdminPanel, and save the status",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesLocalAdmin : hamburburmod
{
    protected override void Pressed() => SlapBattlesLocalProgress.Notify(
            SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.GrantAdmin)
                    ? "Local admin granted and saved." : "Enter Slap Battles or wait for its script to start.");
}

[hamburburmod(            "Unlock All Slaps",   "Unlock every playable slap for your client and save it",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesUnlockAllSlaps : hamburburmod
{
    protected override void Pressed() => SlapBattlesLocalProgress.Notify(
            SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.UnlockAll)
                    ? "All playable slaps unlocked and saved." : "Enter Slap Battles or wait for its script to start.");
}

[hamburburmod(            "Unlock Admin Slap",  "Unlock the Admin Slap without using the map's giver",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesUnlockAdminSlap : hamburburmod
{
    protected override void Pressed() => SlapBattlesLocalProgress.Notify(
            SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.UnlockAdminSlap)
                    ? "Admin Slap unlocked and saved." : "Enter Slap Battles or wait for its script to start.");
}

[hamburburmod("Unlock Booster Slap", "Unlock the playable Booster Slap in your local map save",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesUnlockBoosterSlap : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesLocalProgress.HasPlayableSlap("BoosterSlap"))
        {
            SlapBattlesLocalProgress.Notify("This map version has no playable Booster Slap.");
            return;
        }
        SlapBattlesLocalProgress.Notify(
                SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.UnlockNamedSlap, "BoosterSlap")
                        ? "Booster Slap unlocked locally and saved." : "Wait for the map script to start.");
    }
}

[hamburburmod(            "Set Slaps",          "Type the exact slap count to display and save",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesSetSlaps : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance?.Prompt(new PromptData(
            PromptType.Keyboard, "Set Slaps (0 to 2147483647)", Set, null));

    private static void Set(string input)
    {
        if (!int.TryParse(input?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount < 0)
        {
            SlapBattlesLocalProgress.Notify("Enter a whole number from 0 to 2147483647.");

            return;
        }

        SlapBattlesLocalProgress.Notify(SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.SetSlaps, amount)
                                                ? $"Slap count set to {amount} and saved." : "Enter Slap Battles or wait for its script to start.");
    }
}

[hamburburmod(            "Add Slaps",          "Type how many slaps to add to your counter",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesAddSlaps : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance?.Prompt(new PromptData(
            PromptType.Keyboard, "Add Slaps (0 to 2147483647)", Add, null));

    private static void Add(string input)
    {
        if (!int.TryParse(input?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) || amount < 0)
        {
            SlapBattlesLocalProgress.Notify("Enter a whole number from 0 to 2147483647.");

            return;
        }

        SlapBattlesLocalProgress.Notify(SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.AddSlaps, amount)
                                                ? $"Added {amount} slaps and saved." : "Enter Slap Battles or wait for its script to start.");
    }
}

[hamburburmod("Set Candy Corn", "Set and save your Halloween candy corn count, including the new Broom price",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesSetCorn : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance?.Prompt(new PromptData(
            PromptType.Keyboard, "Candy corn (0 to 16777215)", Set, null));

    private static void Set(string input)
    {
        if (!int.TryParse(input?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) ||
            amount is < 0 or > 16777215)
        {
            SlapBattlesLocalProgress.Notify("Enter a whole number from 0 to 16777215.");
            return;
        }
        SlapBattlesLocalProgress.Notify(
                SlapBattlesNetwork.SupportsAbilityChecks &&
                SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.SetCorn, amount)
                        ? $"Candy corn set to {amount} and saved." : "Enter the updated Slap Battles map first.");
    }
}

[hamburburmod("Add Candy Corn", "Add candy corn and save it for Halloween slap unlocks",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesAddCorn : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance?.Prompt(new PromptData(
            PromptType.Keyboard, "Add candy corn (0 to 16777215)", Add, null));

    private static void Add(string input)
    {
        if (!int.TryParse(input?.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int amount) ||
            amount is < 0 or > 16777215)
        {
            SlapBattlesLocalProgress.Notify("Enter a whole number from 0 to 16777215.");
            return;
        }
        SlapBattlesLocalProgress.Notify(
                SlapBattlesNetwork.SupportsAbilityChecks &&
                SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.AddCorn, amount)
                        ? $"Added {amount} candy corn and saved." : "Enter the updated Slap Battles map first.");
    }
}

[hamburburmod("Unlock Slap By Name", "Type a slap name from the current map script to unlock it locally",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesUnlockNamedSlap : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance?.Prompt(new PromptData(
            PromptType.Keyboard, "Slap name", Unlock, null));

    private static void Unlock(string input)
    {
        string name = input?.Trim();
        if (string.IsNullOrWhiteSpace(name) || !SlapBattlesLocalProgress.HasPlayableSlap(name))
        {
            SlapBattlesLocalProgress.Notify("That slap is not in this map's playable slap list.");
            return;
        }
        SlapBattlesLocalProgress.Notify(SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.UnlockNamedSlap, name)
                                                ? $"{name} unlocked locally and saved." : "Wait for the map script to start.");
    }
}

[hamburburmod("Equip Slap By Name", "Equip an unlocked slap by typing its name",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesEquipNamedSlap : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance?.Prompt(new PromptData(
            PromptType.Keyboard, "Slap name", Equip, null));

    private static void Equip(string input)
    {
        string name = input?.Trim();
        if (string.IsNullOrWhiteSpace(name) || !SlapBattlesLocalProgress.HasPlayableSlap(name))
        {
            SlapBattlesLocalProgress.Notify("That slap is not in this map's playable slap list.");
            return;
        }
        SlapBattlesLocalProgress.Notify(SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.EquipNamedSlap, name)
                                                ? $"Equip request sent for {name}; it must be unlocked." : "Wait for the map script to start.");
    }
}

[hamburburmod("Go To Admin Panel", "Show your local AdminPanel and travel to it",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesVisitAdminPanel : hamburburmod
{
    protected override void Pressed() => SlapBattlesLocalProgress.Notify(
            SlapBattlesLocalProgress.TrySend(SlapBattlesProgressCommand.VisitAdminPanel)
                    ? "Opening the admin panel." : "Wait for the map script to start.");
}

[hamburburmod("Golden Slapples", "Fill the five Slapple spots with 100 slap gold fruit",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesGoldenSlapples : hamburburmod
{
    protected override void Pressed() => SlapBattlesNetwork.SpawnGoldenSlapples();
}

[hamburburmod("Golden Slapple Restock", "Keep the five gold Slapple spots stocked while enabled",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesGoldenSlappleRestock : hamburburmod
{
    private float nextRestock;

    protected override void OnEnable() => nextRestock = 0f;

    protected override void Update()
    {
        if (!SlapBattlesNetwork.SupportsAbilityChecks ||
            CustomGameMode.gameScriptRunner?.ShouldTick != true || Time.realtimeSinceStartup < nextRestock) return;
        nextRestock = Time.realtimeSinceStartup + 2.5f;
        SlapBattlesNetwork.SpawnGoldenSlapples();
    }
}
