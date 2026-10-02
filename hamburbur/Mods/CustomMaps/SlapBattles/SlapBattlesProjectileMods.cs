using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

public abstract class SlapBattlesThrowGun : SlapBattlesGun
{
    protected abstract bool Green { get; }

    protected override void Shoot()
    {
        Vector3 origin                                = SlapBattlesAim.ThrowOrigin;
        Vector3 direction                             = Gun.Hit.collider != null ? Gun.Hit.point - origin : SlapBattlesAim.Forward;
        if (direction.sqrMagnitude < 0.01f) direction = SlapBattlesAim.Forward;
        SlapBattlesNetwork.Throw(Green, origin + direction.normalized * 0.5f,
                direction.normalized                                  * 16f + Vector3.up * 3f);
    }
}

[hamburburmod(                "Pumpkin Throw Gun",  "Throw synchronized map pumpkins", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesPumpkinGun : SlapBattlesThrowGun
{
    protected override bool Green => false;
}

[hamburburmod(                "Green Ball Throw Gun", "Throw synchronized green sludge balls", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled,   0)]
public class SlapBattlesGreenBallGun : SlapBattlesThrowGun
{
    protected override bool Green => true;
}

public abstract class SlapBattlesRain : hamburburmod
{
    protected abstract bool Green { get; }

    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        Vector3 center = SlapBattlesAim.Body;
        for (int i = 0; i < 8; i++)
        {
            float   angle  = i * Mathf.PI * 0.25f;
            Vector3 offset = new(Mathf.Cos(angle) * 5f, 8f, Mathf.Sin(angle) * 5f);
            SlapBattlesNetwork.Throw(Green, center + offset, Vector3.down * 9f);
        }
    }
}

[hamburburmod(                "Pumpkin Rain",       "Rain eight map pumpkins around you", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesPumpkinRain : SlapBattlesRain
{
    protected override bool Green => false;
}

[hamburburmod(                "Green Ball Rain",    "Rain eight green sludge balls around you", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesGreenBallRain : SlapBattlesRain
{
    protected override bool Green => true;
}

[hamburburmod(            "Pumpkin Crossfire",  "Fire eight synchronized pumpkins inward from a ring",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesPumpkinCrossfire : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        Vector3 center = SlapBattlesAim.Body;
        for (int i = 0; i < 8; i++)
        {
            float   angle  = i * Mathf.PI * 0.25f;
            Vector3 radial = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            SlapBattlesNetwork.Throw(false, center + radial * 7f  + Vector3.up * 2f,
                    -radial                                 * 14f + Vector3.up * 3f);
        }
    }
}

[hamburburmod("Green Ball Crossfire", "Fire eight synchronized green balls inward from a ring",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesGreenBallCrossfire : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        Vector3 center = SlapBattlesAim.Body;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 0.25f;
            Vector3 radial = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            SlapBattlesNetwork.Throw(true, center + radial * 7f + Vector3.up * 2f,
                    -radial * 14f + Vector3.up * 3f);
        }
    }
}

[hamburburmod("Boss Ball Gun", "Fire the boss fight's green ball while its fight is active",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBossBallGun : SlapBattlesGun
{
    protected override float RepeatDelay => 0.3f;

    protected override void Shoot()
    {
        Vector3 origin = SlapBattlesAim.ThrowOrigin;
        Vector3 direction = Gun.Hit.collider != null ? Gun.Hit.point - origin : SlapBattlesAim.Forward;
        if (direction.sqrMagnitude < 0.01f) direction = SlapBattlesAim.Forward;
        SlapBattlesNetwork.BossBall(origin + direction.normalized * 0.5f,
                direction.normalized * 16f + Vector3.up * 3f);
    }
}

public abstract class SlapBattlesOrbGunBase : SlapBattlesGun
{
    protected abstract bool Big { get; }
    protected override float RepeatDelay => SlapBattlesNetwork.MinimumEventDelay;

    protected override void Shoot()
    {
        Vector3 origin = SlapBattlesAim.ThrowOrigin;
        Vector3 direction = Gun.Hit.collider != null ? Gun.Hit.point - origin : SlapBattlesAim.Forward;
        if (direction.sqrMagnitude < 0.01f) direction = SlapBattlesAim.Forward;
        direction.Normalize();
        SlapBattlesNetwork.Orb(Big, origin + direction * 0.5f, direction);
    }
}

[hamburburmod("Orb Throw Gun", "Fire a networked Kill Streak orb",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesOrbGun : SlapBattlesOrbGunBase
{
    protected override bool Big => false;
}

[hamburburmod("Big Orb Throw Gun", "Fire a larger, stronger networked Kill Streak orb",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBigOrbGun : SlapBattlesOrbGunBase
{
    protected override bool Big => true;
}
