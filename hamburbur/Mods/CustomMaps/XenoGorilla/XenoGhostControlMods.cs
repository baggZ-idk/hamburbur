using hamburbur.Libs;
using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.XenoGorilla;

public abstract class XenoGhostWarpGun : hamburburmod
{
    private readonly GunLib gun = new() { ShouldFollow = true, };
    private float nextWarp;
    protected abstract string GhostName { get; }

    protected override void Start() => gun.Start();

    protected override void LateUpdate()
    {
        gun.LateUpdate();
        if (!XenoNetwork.IsMaster || !gun.IsShooting || gun.Hit.collider == null || Time.time < nextWarp) return;
        nextWarp = Time.time + 0.2f;
        XenoNetwork.Send("MC_F" + GhostName, gun.Hit.point + Vector3.up);
    }

    protected override void OnDisable() => gun.OnDisable();
}

[hamburburmod("Warp Dimwi Gun", "Master: move active Dimwi to the aimed point", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoWarpDimwiGun : XenoGhostWarpGun { protected override string GhostName => "Dimwi"; }

[hamburburmod("Warp Morty Gun", "Master: move active Morty to the aimed point", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoWarpMortyGun : XenoGhostWarpGun { protected override string GhostName => "Morty"; }

[hamburburmod("Warp Angel Gun", "Master: move active Angel to the aimed point", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoWarpAngelGun : XenoGhostWarpGun { protected override string GhostName => "Angel"; }

[hamburburmod("Warp Flowery Gun", "Master: move active Flowery to the aimed point", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoWarpFloweryGun : XenoGhostWarpGun { protected override string GhostName => "Flowery"; }

[hamburburmod("Daisy09 Chase Gun", "Master: direct standing Daisy09 toward the selected player", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoDaisy09ChaseGun : hamburburmod
{
    private readonly GunLib gun = new() { ShouldFollow = true, };
    private bool wasShooting;

    protected override void Start() => gun.Start();

    protected override void LateUpdate()
    {
        gun.LateUpdate();
        bool shooting = gun.IsShooting;
        if (shooting && !wasShooting && XenoNetwork.IsMaster && gun.ChosenRig?.Creator != null)
            XenoNetwork.Send("MC_Daisy09_Chase", (double)gun.ChosenRig.Creator.ActorNumber);
        wasShooting = shooting;
    }

    protected override void OnDisable()
    {
        wasShooting = false;
        gun.OnDisable();
    }
}
