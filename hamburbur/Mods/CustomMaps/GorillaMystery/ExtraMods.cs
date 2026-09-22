using System.Linq;
using hamburbur.Libs;
using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.GorillaMystery;

[hamburburmod(                "Kill Self",          "Triggers Gorilla Mystery's PlayerDeath event on yourself", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class MysteryKillSelf : hamburburmod
{
    protected override void Pressed() => MysteryTagNetwork.KillSelf();
}

[hamburburmod(                "Drop Sheriff Pistol Here", "Drops and frees the sheriff pistol at your position", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled,       0)]
public class DropMysteryPistolHere : hamburburmod
{
    protected override void Pressed()
    {
        if (GorillaTagger.Instance?.bodyCollider != null)
            MysteryTagNetwork.DropGun(GorillaTagger.Instance.bodyCollider.transform.position);
    }
}

public abstract class MysteryUtilityGun : hamburburmod
{
    private readonly  GunLib gun = new() { ShouldFollow = true, };
    private           float  nextUse;
    protected virtual float  Delay => 0.25f;

    protected override void Start() => gun.Start();

    protected override void LateUpdate()
    {
        gun.LateUpdate();

        if (!gun.IsShooting || gun.ChosenRig?.Creator == null || Time.time < nextUse)
            return;

        nextUse = Time.time + Delay;
        Use(gun.ChosenRig);
    }

    protected abstract void Use(VRRig rig);

    protected override void OnDisable()
    {
        nextUse = 0f;
        gun.OnDisable();
    }
}

[hamburburmod(                "Drop Pistol Gun",    "Drops the sheriff pistol at the targeted player", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class DropMysteryPistolGun : MysteryUtilityGun
{
    protected override void Use(VRRig rig) => MysteryTagNetwork.DropGun(rig.transform.position);
}

[hamburburmod(                "Gunshot Sound Gun",  "Plays the map's sheriff shot sound at the targeted player", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class MysteryGunshotSoundGun : MysteryUtilityGun
{
    protected override void Use(VRRig rig) => MysteryTagNetwork.PlaySheriffShot(rig.transform.position);
}

[hamburburmod(                "Force Right Hand Gun", "Makes the target's map weapon follow their right hand", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled,   0)]
public class MysteryRightHandGun : MysteryUtilityGun
{
    protected override void Use(VRRig rig) => MysteryTagNetwork.SetHand(rig.Creator.ActorNumber, false);
}

[hamburburmod(                "Force Left Hand Gun", "Makes the target's map weapon follow their left hand", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled,  0)]
public class MysteryLeftHandGun : MysteryUtilityGun
{
    protected override void Use(VRRig rig) => MysteryTagNetwork.SetHand(rig.Creator.ActorNumber, true);
}

[hamburburmod(                "Gunshot Sound Spam", "Repeatedly plays the sheriff shot sound at your position", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class MysteryGunshotSoundSpam : hamburburmod
{
    private float nextShot;

    protected override void Update()
    {
        if (Time.time < nextShot || GorillaTagger.Instance?.bodyCollider == null)
            return;

        nextShot = Time.time + 0.3f;
        MysteryTagNetwork.PlaySheriffShot(GorillaTagger.Instance.bodyCollider.transform.position);
    }
}

public abstract class MysteryTeleport : hamburburmod
{
    protected abstract string ParentName { get; }
    protected virtual  string ChildName  => null;

    protected override void Pressed()
    {
        GameObject parent = FindObject(ParentName);

        if (parent == null)
            return;

        Transform destination = ChildName == null ? parent.transform : parent.transform.Find(ChildName);
        if (destination != null)
            Tools.Utils.TeleportPlayer(destination.position);
    }

    private static GameObject FindObject(string objectName) =>
            GameObject.Find(objectName) ??
            Resources.FindObjectsOfTypeAll<GameObject>().FirstOrDefault(item => item.name == objectName);
}

[hamburburmod(                "Teleport Mystery Lobby", "Teleports to the Gorilla Mystery lobby point", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled,     0)]
public class TeleportMysteryLobby : MysteryTeleport
{
    protected override string ParentName => "TeleportPoint";
}

[hamburburmod(                "Teleport Dropped Pistol", "Teleports to the sheriff pistol's dropped position", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled,      0)]
public class TeleportMysteryPistol : MysteryTeleport
{
    protected override string ParentName => "DroppedPistol";
}

public abstract class TeleportMysteryMap : MysteryTeleport
{
    protected override string ChildName => "MapTeleporter1";
}

[hamburburmod(                "Teleport Mystery Map 1", "Teleports to the first spawn point on map one", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled,     0)]
public class TeleportMysteryMap1 : TeleportMysteryMap
{
    protected override string ParentName => "Map1Teleporters";
}

[hamburburmod(                "Teleport Mystery Map 2", "Teleports to the first spawn point on map two", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled,     0)]
public class TeleportMysteryMap2 : TeleportMysteryMap
{
    protected override string ParentName => "Map2Teleporters";
}

[hamburburmod(                "Teleport Mystery Map 3", "Teleports to the first spawn point on map three", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled,     0)]
public class TeleportMysteryMap3 : TeleportMysteryMap
{
    protected override string ParentName => "Map3Teleporters";
}