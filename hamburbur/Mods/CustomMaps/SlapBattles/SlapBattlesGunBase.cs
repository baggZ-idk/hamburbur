using hamburbur.Libs;
using hamburbur.Mod_Backend;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

public abstract class SlapBattlesGun : hamburburmod
{
    protected readonly GunLib Gun = new() { ShouldFollow = true, };
    private            float  nextShot;
    private            bool   firedThisHold;
    protected virtual  float  RepeatDelay => SlapBattlesNetwork.MinimumEventDelay;

    protected override void Start() => Gun.Start();

    protected override void LateUpdate()
    {
        Gun.LateUpdate();
        bool shooting = Gun.IsShooting;
        if (!shooting || !SlapBattlesNetwork.IsInMap)
        {
            StopShooting();
            firedThisHold = false;
            return;
        }

        if (PrepareShot() && (!firedThisHold || RepeatDelay > 0f && Time.time >= nextShot))
        {
            Shoot();
            nextShot = Time.time + RepeatDelay;
            firedThisHold = true;
        }
    }

    protected virtual bool PrepareShot() => true;
    protected virtual void StopShooting() { }
    protected abstract void Shoot();

    protected override void OnDisable()
    {
        StopShooting();
        firedThisHold = false;
        nextShot    = 0f;
        Gun.OnDisable();
    }
}

internal static class SlapBattlesAim
{
    internal static Vector3 Body => GorillaTagger.Instance?.bodyCollider != null
                                            ? GorillaTagger.Instance.bodyCollider.transform.position : Vector3.zero;

    internal static Vector3 Forward
    {
        get
        {
            Vector3 direction = Camera.main != null ? Camera.main.transform.forward : Vector3.forward;
            direction.y = 0f;

            return direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward;
        }
    }

    internal static Vector3 ThrowOrigin => GunLib.GetGunHand() != null
                                                   ? GunLib.GetGunHand().position : Body + Vector3.up;
}

public abstract class SlapBattlesTargetGun : SlapBattlesGun
{
    protected override float RepeatDelay => SlapBattlesNetwork.SupportsBoosterWall
                                                    ? 0.18f : SlapBattlesNetwork.MinimumEventDelay;
    protected virtual bool UsesSourceRange => false;
    protected virtual float SourceRange => 25f;

    protected override bool PrepareShot()
    {
        VRRig target = Gun.ChosenRig;
        if (target?.Creator == null)
        {
            SlapBattlesRigRangeProxy.Release(this);
            return false;
        }

        if (!UsesSourceRange || !SlapBattlesRigRangeAssist.IsEnabled ||
            !SlapBattlesNetwork.SupportsBoosterWall)
        {
            SlapBattlesRigRangeProxy.Release(this);
            return true;
        }

        if (Vector3.Distance(SlapBattlesAim.Body, target.transform.position) <= SourceRange - 3f ||
            SlapBattlesNetwork.HasRelaySource(target.Creator.ActorNumber, SourceRange))
        {
            SlapBattlesRigRangeProxy.Release(this);
            return true;
        }

        return SlapBattlesRigRangeProxy.Prepare(this, target);
    }

    protected override void StopShooting() => SlapBattlesRigRangeProxy.Release(this);

    protected override void Shoot()
    {
        if (Gun.ChosenRig?.Creator != null) Use(Gun.ChosenRig);
    }

    protected abstract void Use(VRRig rig);
}
