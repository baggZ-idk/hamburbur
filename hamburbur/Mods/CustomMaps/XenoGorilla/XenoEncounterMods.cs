using hamburbur.Libs;
using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.XenoGorilla;

[hamburburmod("Knock Xeno Door", "Triggers the map's door knock; the master summons Door when available", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoKnockDoor : hamburburmod
{
    protected override void Pressed() => XenoNetwork.Send("DoorKnocked", 1d);
}

[hamburburmod("Pop Xeno Balloon", "Pops the active balloon and starts its delayed encounter", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoPopBalloon : hamburburmod
{
    protected override void Pressed() => XenoNetwork.Send("BalloonPop", 1d);
}

[hamburburmod("Spawn Xeno Balloon", "Master: starts the balloon encounter", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSpawnBalloon : hamburburmod
{
    protected override void Pressed()
    {
        if (XenoNetwork.IsMaster) XenoNetwork.Send("MC_Balloon", 1d);
    }
}

[hamburburmod("Clear Xeno Balloon", "Master: removes the active balloon", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoClearBalloon : hamburburmod
{
    protected override void Pressed()
    {
        if (XenoNetwork.IsMaster) XenoNetwork.Send("MC_Balloon", 0d);
    }
}

[hamburburmod("Provoke Daisy09", "Asks the master to start Daisy09's chase while she is standing", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoProvokeDaisy09 : hamburburmod
{
    protected override void Pressed() => XenoNetwork.Send("Daisy09_ChaseMe", 1d);
}

[hamburburmod("Trigger Mirror Man Grab", "Triggers the active Mirror Man grab; it kills the sender after four seconds", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoTriggerMirrorManGrab : hamburburmod
{
    protected override void Pressed() => XenoNetwork.Send("MirrorManGrab", 1d);
}

[hamburburmod("Play Xeno Ban Sound", "Plays the map's ban sound at your position", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoPlayBanSound : hamburburmod
{
    protected override void Pressed() => XenoNetwork.Send("BanSFX", 1d);
}

[hamburburmod("Spawn Xeno Corpse", "Plays a death effect and spawns a map corpse at your position", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSpawnCorpse : hamburburmod
{
    protected override void Pressed() => XenoNetwork.Send("IDied", XenoNetwork.BodyPosition, Vector3.up * 4f);
}

[hamburburmod("Corpse Effect Gun", "Plays a death effect and spawns a map corpse at the aimed point", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoCorpseEffectGun : hamburburmod
{
    private readonly GunLib gun = new() { ShouldFollow = true, };
    private float nextEffect;

    protected override void Start() => gun.Start();

    protected override void LateUpdate()
    {
        gun.LateUpdate();
        if (!XenoNetwork.IsInMap || !gun.IsShooting || gun.Hit.collider == null || Time.time < nextEffect) return;
        nextEffect = Time.time + 2.2f;
        XenoNetwork.Send("IDied", gun.Hit.point, Vector3.up * 5f);
    }

    protected override void OnDisable() => gun.OnDisable();
}
