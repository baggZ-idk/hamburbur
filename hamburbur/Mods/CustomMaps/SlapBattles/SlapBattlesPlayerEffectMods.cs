using System;
using hamburbur.GUI;
using hamburbur.Mod_Backend;
using hamburbur.Tools;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

public abstract class SlapBattlesEffectGun : SlapBattlesTargetGun
{
    protected override bool UsesSourceRange => true;
    protected abstract string EventName { get; }
    protected override void Use(VRRig rig) =>
            SlapBattlesNetwork.TargetEffect(EventName, rig.Creator.ActorNumber);
}

[hamburburmod(                "Trap Gun",           "Trap the targeted player in the map's cage", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesTrapGun : SlapBattlesEffectGun
{
    protected override string EventName => "trapped";
}

[hamburburmod(                "Slow Gun",           "Apply the map's Void slow to the targeted player", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesSlowGun : SlapBattlesEffectGun
{
    protected override string EventName => "slowed";
}

[hamburburmod(                "EMP Gun",            "Disable the targeted player's slap ability briefly", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesEmpGun : SlapBattlesEffectGun
{
    protected override string EventName => "empd";
}

[hamburburmod(                "Glitch Gun",         "Apply the map's delayed glitch effect to the target", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesGlitchGun : SlapBattlesEffectGun
{
    protected override string EventName => "glitched";
}

[hamburburmod(                "Redacted Gun",       "Apply the map's changing movement effect to the target", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesRedactedGun : SlapBattlesEffectGun
{
    protected override string EventName => "redacted";
}

[hamburburmod(                "Shield Gun",         "Give the targeted player the map's shield", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesShieldGun : SlapBattlesTargetGun
{
    protected override void Use(VRRig rig) => SlapBattlesNetwork.Shield(rig.Creator.ActorNumber);
}

[hamburburmod(                "Bomb Gun",           "Give the targeted player a 20 second map bomb", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBombGun : SlapBattlesTargetGun
{
    protected override bool UsesSourceRange => true;
    protected override void Use(VRRig rig) => SlapBattlesNetwork.Bomb(rig.Creator.ActorNumber);
}

[hamburburmod(            "EMP Pulse",          "Send an EMP shockwave and disable every other player's ability",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesEmpPulse : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        Vector3 center = SlapBattlesAim.Body;
        SlapBattlesNetwork.EmpShock(center);
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null && Vector3.Distance(center, rig.transform.position) <= 14f)
                SlapBattlesNetwork.TargetEffect("empd", rig.Creator.ActorNumber);
    }
}

[hamburburmod(                "Trap All",           "Put every other player in the map's temporary cage", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesTrapAll : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null)
                SlapBattlesNetwork.TargetEffect("trapped", rig.Creator.ActorNumber);
    }
}

[hamburburmod(                "Shield All",         "Give every other player the map's shield", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesShieldAll : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null)
                SlapBattlesNetwork.Shield(rig.Creator.ActorNumber);
    }
}

[hamburburmod(            "Glitch All",         "Apply the map's delayed glitch effect to every other player",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesGlitchAll : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null)
                SlapBattlesNetwork.TargetEffect("glitched", rig.Creator.ActorNumber);
    }
}

[hamburburmod("Rock Gun", "Turn a targeted player into the new Rock Slap's rock",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesRockGun : SlapBattlesTargetGun
{
    protected override float RepeatDelay => 0f;
    protected override void Use(VRRig rig) => SlapBattlesNetwork.Rock(rig.Creator.ActorNumber, true);
}

[hamburburmod("Mega Rock Gun", "Turn a targeted player into a solid MegaRock",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesMegaRockGun : SlapBattlesTargetGun
{
    protected override float RepeatDelay => 0f;
    protected override void Use(VRRig rig) => SlapBattlesNetwork.Rock(rig.Creator.ActorNumber, true, true);
}

[hamburburmod("Unrock Gun", "Release a targeted player from Rock or MegaRock",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesUnrockGun : SlapBattlesTargetGun
{
    protected override float RepeatDelay => 0f;
    protected override void Use(VRRig rig) => SlapBattlesNetwork.Rock(rig.Creator.ActorNumber, false);
}

[hamburburmod("Rock All", "Turn every other player into a rock",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesRockAll : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.SupportsNewSlaps) return;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null)
                SlapBattlesNetwork.Rock(rig.Creator.ActorNumber, true);
    }
}

[hamburburmod("Unrock All", "Release every other player from Rock or MegaRock",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesUnrockAll : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.SupportsNewSlaps) return;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null)
                SlapBattlesNetwork.Rock(rig.Creator.ActorNumber, false);
    }
}

[hamburburmod("Stopwatch Gun", "Freeze a targeted player with the new Stopwatch effect",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesStopwatchGun : SlapBattlesTargetGun
{
    protected override bool UsesSourceRange => true;
    protected override void Use(VRRig rig) => SlapBattlesNetwork.Stopwatch(rig.Creator.ActorNumber);
}

[hamburburmod("Stopwatch All", "Freeze every other player with the Stopwatch effect",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesStopwatchAll : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.SupportsNewSlaps) return;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null)
                SlapBattlesNetwork.Stopwatch(rig.Creator.ActorNumber);
    }
}

[hamburburmod("Error Screen Gun", "Show the map's Error Slap message to a targeted player",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesErrorGun : SlapBattlesTargetGun
{
    protected override bool UsesSourceRange => true;
    protected override void Use(VRRig rig) => SlapBattlesNetwork.ErrorScreen(rig.Creator.ActorNumber);
}

[hamburburmod("Boomerang Slap Gun", "Send a targeted player's held slap through the map's boomerang arc",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBoomerangGun : SlapBattlesTargetGun
{
    protected override float RepeatDelay => 2f;

    protected override void Use(VRRig rig)
    {
        Vector3 direction = rig.transform.position - SlapBattlesAim.Body;
        direction.y = 0f;
        SlapBattlesNetwork.Boomerang(rig.Creator.ActorNumber, direction);
    }
}

[hamburburmod("Multi Slap Color Gun", "Cycle the target's networked Multi Slap color; requires them to hold Multi Slap",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesMultiColorGun : SlapBattlesTargetGun
{
    private int mode;
    protected override float RepeatDelay => 0.4f;

    protected override void Use(VRRig rig)
    {
        mode = mode % 4 + 1;
        SlapBattlesNetwork.MultiMode(rig.Creator.ActorNumber, mode);
    }
}

[hamburburmod("Slap to Give: ", "Choose a playable slap appearance for Give Slap Gun and Give Slap All",
        ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesSlapToGive : hamburburmod
{
    internal static SlapBattlesSlapToGive Instance { get; private set; }

    internal static string SelectedName
    {
        get
        {
            (string Name, int Index)[] slaps = SlapBattlesLocalProgress.PlayableSlaps();
            return slaps.Length == 0 ? null : slaps[Math.Clamp(Instance?.IncrementalValue ?? 0, 0, slaps.Length - 1)].Name;
        }
    }

    public override string ModName
    {
        get
        {
            (string Name, int Index)[] slaps = SlapBattlesLocalProgress.PlayableSlaps();
            return AssociatedAttribute.Name + (slaps.Length == 0
                            ? "Load Slap Battles" : slaps[Math.Clamp(IncrementalValue, 0, slaps.Length - 1)].Name);
        }
    }

    protected override void Start() => Instance = this;

    protected override void Increment()
    {
        int count = SlapBattlesLocalProgress.PlayableSlaps().Length;
        if (count > 0) IncrementalValue = (IncrementalValue + 1) % count;
    }

    protected override void Decrement()
    {
        int count = SlapBattlesLocalProgress.PlayableSlaps().Length;
        if (count > 0) IncrementalValue = (IncrementalValue - 1 + count) % count;
    }

    protected override void OnIncrementalStateLoaded()
    {
        int count = SlapBattlesLocalProgress.PlayableSlaps().Length;
        if (count > 0) IncrementalValue = Math.Clamp(IncrementalValue, 0, count - 1);
    }
}

[hamburburmod("Give Slap Gun", "Show the selected slap on a targeted player's hand; does not unlock its ability",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesGiveSlapGun : SlapBattlesTargetGun
{
    protected override float RepeatDelay => 0f;
    protected override void Use(VRRig rig) =>
            SlapBattlesNetwork.GiveSlapAppearance(rig.Creator.ActorNumber, SlapBattlesSlapToGive.SelectedName);
}

[hamburburmod("Give Slap All", "Show the selected slap on every other player's hand; does not unlock its ability",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesGiveSlapAll : hamburburmod
{
    protected override void Pressed()
    {
        string name = SlapBattlesSlapToGive.SelectedName;
        if (name == null || !SlapBattlesNetwork.IsInMap || NetworkSystem.Instance == null)
        {
            SlapBattlesLocalProgress.Notify("Enter Slap Battles and choose a playable slap first.");
            return;
        }

        int count = 0;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null && SlapBattlesNetwork.GiveSlapAppearance(rig.Creator.ActorNumber, name))
                count++;
        SlapBattlesLocalProgress.Notify($"Showing {name} on {count} players. Their unlocks and abilities are unchanged.");
    }
}

[hamburburmod("Slap Appearance", "Show any playable slap on your hand to the room without changing its ability",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesSlapAppearance : hamburburmod
{
    protected override void Pressed() => ButtonHandler.Instance?.Prompt(new PromptData(
            PromptType.Keyboard, "Slap name", Set, null));

    private static void Set(string input)
    {
        string name = input?.Trim();
        SlapBattlesLocalProgress.Notify(SlapBattlesNetwork.DisguiseAsSlap(name)
                                                ? $"Showing {name} to the room." : "Enter a playable slap name in Slap Battles.");
    }
}
