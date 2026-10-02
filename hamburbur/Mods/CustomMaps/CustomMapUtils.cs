using System;
using System.Collections.Generic;
using GorillaTagScripts.VirtualStumpCustomMaps;

namespace hamburbur.Mods.CustomMaps;

public static class CustomMapUtils
{
    public const byte EventCode = 180;

    public static bool TryReplaceCustomScriptFragment(string expected, string replacement)
    {
        string input = CustomGameMode.LuaScript;
        if (string.IsNullOrEmpty(input) || string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(replacement) ||
            NetworkSystem.Instance?.InRoom != true || !CustomGameMode.GameModeInitialized)
            return false;

        if (input.Contains(replacement, StringComparison.Ordinal))
            return true;

        int index = input.IndexOf(expected, StringComparison.Ordinal);
        if (index < 0 || input.IndexOf(expected, index + expected.Length, StringComparison.Ordinal) >= 0)
            return false;

        CustomGameMode.LuaScript = input[..index] + replacement + input[(index + expected.Length)..];
        try
        {
            if (CustomGameMode.gameScriptRunner?.ShouldTick == true)
                CustomGameMode.StopScript();
            CustomGameMode.LuaStart();
            if (CustomGameMode.gameScriptRunner?.ShouldTick == true)
                return true;
        }
        catch (Exception error)
        {
            UnityEngine.Debug.LogWarning($"Custom map script update failed: {error}");
        }

        CustomGameMode.LuaScript = input;
        try
        {
            if (CustomGameMode.gameScriptRunner?.ShouldTick == true)
                CustomGameMode.StopScript();
            CustomGameMode.LuaStart();
        }
        catch (Exception error)
        {
            UnityEngine.Debug.LogWarning($"Custom map script restore failed: {error}");
        }
        return false;
    }

    public static void ModifyCustomScript(Dictionary<int, string> replacements)
    {
        string   input = CustomGameMode.LuaScript;
        string[] lines = input.Split(["\r\n", "\n",], StringSplitOptions.None);

        foreach ((int lineIndex, string value) in replacements)
        {
            if (lineIndex >= 0 && lineIndex < lines.Length)
                lines[lineIndex] = value;
        }

        CustomGameMode.LuaScript = string.Join(Environment.NewLine, lines);

        if (NetworkSystem.Instance.InRoom)
            LuauHud.Instance.RestartLuauScript();

        CustomMapManager.ReturnToVirtualStump();
    }
}
