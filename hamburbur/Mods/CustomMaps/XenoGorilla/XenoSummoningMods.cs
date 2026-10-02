using hamburbur.Libs;
using hamburbur.Mod_Backend;
using Photon.Pun;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.XenoGorilla;

public abstract class XenoSummonGhost : hamburburmod
{
    protected abstract string GhostName { get; }

    protected override void Pressed() => XenoNetwork.Summon(GhostName, XenoNetwork.LocalActor);
}

[hamburburmod("Summon Daisy09", "Master: summon Daisy09 on yourself; normal ghost cooldown applies", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonDaisy09 : XenoSummonGhost { protected override string GhostName => "Daisy09"; }

[hamburburmod("Summon Blue Lucy", "Master: summon Blue Lucy on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonBlueLucy : XenoSummonGhost { protected override string GhostName => "BlueLucy"; }

[hamburburmod("Summon Lucyfer", "Master: summon Lucyfer on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonLucyfer : XenoSummonGhost { protected override string GhostName => "Lucyfer"; }

[hamburburmod("Summon Error Lucy", "Master: summon Error Lucy on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonErrorLucy : XenoSummonGhost { protected override string GhostName => "ErrorLucy"; }

[hamburburmod("Summon White Lucy", "Master: summon White Lucy on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonWhiteLucy : XenoSummonGhost { protected override string GhostName => "WhiteLucy"; }

[hamburburmod("Summon Tiptoe", "Master: summon Tiptoe on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonTiptoe : XenoSummonGhost { protected override string GhostName => "Tiptoe"; }

[hamburburmod("Summon Echo", "Master: summon Echo on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonEcho : XenoSummonGhost { protected override string GhostName => "Echo"; }

[hamburburmod("Summon Mirror Man", "Master: summon Mirror Man on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonMirrorMan : XenoSummonGhost { protected override string GhostName => "MirrorMan"; }

[hamburburmod("Summon RUN", "Master: start the RUN encounter", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonRun : XenoSummonGhost { protected override string GhostName => "RUN"; }

[hamburburmod("Summon Angel", "Master: summon Angel on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonAngel : XenoSummonGhost { protected override string GhostName => "Angel"; }

[hamburburmod("Summon Dimwi", "Master: summon Dimwi on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonDimwi : XenoSummonGhost { protected override string GhostName => "Dimwi"; }

[hamburburmod("Summon Clone", "Master: summon Clone on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonClone : XenoSummonGhost { protected override string GhostName => "Clone"; }

[hamburburmod("Summon Boiled", "Master: summon Boiled on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonBoiled : XenoSummonGhost { protected override string GhostName => "Boiled"; }

[hamburburmod("Summon Black Lucy", "Master: summon Black Lucy on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonBlackLucy : XenoSummonGhost { protected override string GhostName => "BlackLucy"; }

[hamburburmod("Summon Morty", "Master: summon Morty on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonMorty : XenoSummonGhost { protected override string GhostName => "Morty"; }

[hamburburmod("Summon Flowery", "Master: summon Flowery on yourself", ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoSummonFlowery : XenoSummonGhost { protected override string GhostName => "Flowery"; }

public abstract class XenoGhostTargetGun : hamburburmod
{
    private readonly GunLib gun = new() { ShouldFollow = true, };
    private bool wasShooting;
    protected abstract string GhostName { get; }

    protected override void Start() => gun.Start();

    protected override void LateUpdate()
    {
        gun.LateUpdate();
        bool shooting = gun.IsShooting;
        if (shooting && !wasShooting && gun.ChosenRig?.Creator != null)
            XenoNetwork.Summon(GhostName, gun.ChosenRig.Creator.ActorNumber);
        wasShooting = shooting;
    }

    protected override void OnDisable()
    {
        wasShooting = false;
        gun.OnDisable();
    }
}

[hamburburmod("Blue Lucy Summon Gun", "Master: summon Blue Lucy targeting the selected player", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoBlueLucySummonGun : XenoGhostTargetGun { protected override string GhostName => "BlueLucy"; }

[hamburburmod("Tiptoe Summon Gun", "Master: summon Tiptoe targeting the selected player", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoTiptoeSummonGun : XenoGhostTargetGun { protected override string GhostName => "Tiptoe"; }

[hamburburmod("Mirror Man Summon Gun", "Master: summon Mirror Man targeting the selected player", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoMirrorManSummonGun : XenoGhostTargetGun { protected override string GhostName => "MirrorMan"; }

[hamburburmod("Boiled Summon Gun", "Master: summon Boiled targeting the selected player", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoBoiledSummonGun : XenoGhostTargetGun { protected override string GhostName => "Boiled"; }
