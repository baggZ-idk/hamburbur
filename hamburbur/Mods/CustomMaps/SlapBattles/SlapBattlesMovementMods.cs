using System;
using System.Linq;
using hamburbur.Mod_Backend;
using hamburbur.Tools;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

[hamburburmod("Anti Fling", "Block incoming Slap Battles launch velocity in your local map script",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesAntiFling : hamburburmod
{
    private const string NormalLaunch = "function applyInLaunch(launch)";
    private const string BlockedLaunch = "function applyInLaunch(launch, unused)\n\treturn false -- hamburbur Anti Fling";
    private float nextAttempt;
    private bool reportedFailure;

    protected override void OnEnable()
    {
        nextAttempt = 0f;
        reportedFailure = false;
    }

    protected override void Update()
    {
        if (!SlapBattlesNetwork.IsInMap || Time.realtimeSinceStartup < nextAttempt) return;
        nextAttempt = Time.realtimeSinceStartup + 1.5f;

        string script = CustomGameMode.LuaScript;
        if (script?.Contains(BlockedLaunch, StringComparison.Ordinal) == true) return;
        if (script?.Contains(NormalLaunch, StringComparison.Ordinal) == true &&
            CustomMapUtils.TryReplaceCustomScriptFragment(NormalLaunch, BlockedLaunch))
        {
            SlapBattlesLocalProgress.Notify("Anti Fling is active for incoming map launches.");
            reportedFailure = false;
            return;
        }

        if (reportedFailure) return;
        SlapBattlesLocalProgress.Notify("Anti Fling is waiting for the current Slap Battles script.");
        reportedFailure = true;
    }

    protected override void OnDisable()
    {
        if (CustomGameMode.LuaScript?.Contains(BlockedLaunch, StringComparison.Ordinal) == true)
            if (!CustomMapUtils.TryReplaceCustomScriptFragment(BlockedLaunch, NormalLaunch) &&
                SlapBattlesNetwork.IsInMap)
                SlapBattlesLocalProgress.Notify("Anti Fling could not restore the current script. Rejoin the map.");
    }
}

[hamburburmod(                "Fling Gun",          "Launch a targeted player with the map's slap event", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesFlingGun : SlapBattlesTargetGun
{
    protected override bool UsesSourceRange => true;
    protected override float SourceRange => 120f;

    protected override void Use(VRRig rig)
    {
        Vector3 from = GorillaTagger.Instance?.bodyCollider != null
                               ? GorillaTagger.Instance.bodyCollider.transform.position
                               : rig.transform.position - Vector3.forward;

        Vector3 direction                             = rig.transform.position - from;
        if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
        SlapBattlesNetwork.Fling(rig.Creator.ActorNumber,
                direction.normalized * 30f + Vector3.up * 14f);
    }
}

[hamburburmod(                "Move Player Gun",    "Shoot a player, then a spot in the same slap arena to bring them there",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesMoveGun : SlapBattlesGun
{
    private int selectedActor;
    protected override float RepeatDelay => 0f;

    protected override bool PrepareShot()
    {
        if (selectedActor == 0) return Gun.ChosenRig?.Creator != null;
        if (Gun.Hit.collider == null) return false;
        if (!SlapBattlesRigRangeAssist.IsEnabled || !SlapBattlesNetwork.SupportsBoosterWall)
            return true;
        VRRig target = selectedActor.Rig();
        if (target == null) return false;
        if (Vector3.Distance(SlapBattlesAim.Body, target.transform.position) <= 27f ||
            SlapBattlesNetwork.HasRelaySource(selectedActor, 30f))
        {
            SlapBattlesRigRangeProxy.Release(this);
            return true;
        }
        return SlapBattlesRigRangeProxy.Prepare(this, target);
    }

    protected override void StopShooting() => SlapBattlesRigRangeProxy.Release(this);

    protected override void Shoot()
    {
        if (selectedActor == 0)
        {
            if (Gun.ChosenRig?.Creator == null)
                return;

            selectedActor = Gun.ChosenRig.Creator.ActorNumber;

            return;
        }

        if (Gun.Hit.collider == null)
            return;

        SlapBattlesNetwork.Move(selectedActor, Gun.Hit.point);
        selectedActor = 0;
    }

    protected override void OnDisable()
    {
        selectedActor = 0;
        base.OnDisable();
    }
}

[hamburburmod(                "Pull Gun",           "Pull the targeted player toward you", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesPullGun : SlapBattlesTargetGun
{
    protected override bool UsesSourceRange => true;
    protected override float SourceRange => 120f;

    protected override void Use(VRRig rig)
    {
        Vector3 direction = SlapBattlesAim.Body - rig.transform.position;

        if (direction.sqrMagnitude < 0.01f) return;
        SlapBattlesNetwork.Fling(rig.Creator.ActorNumber, direction.normalized * 24f + Vector3.up * 5f);
    }
}

[hamburburmod(                "Sky Gun",            "Launch the targeted player straight upward", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesSkyGun : SlapBattlesTargetGun
{
    protected override bool UsesSourceRange => true;
    protected override float SourceRange => 120f;

    protected override void Use(VRRig rig) =>
            SlapBattlesNetwork.Fling(rig.Creator.ActorNumber, Vector3.up * 32f);
}

[hamburburmod(                "Fling All",          "Fling every other player outward from you", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesFlingAll : hamburburmod
{
    protected override void Pressed() => SlapBattlesNetwork.FlingAll(SlapBattlesAim.Body, 30f);
}

[hamburburmod(                "Pull All",           "Pull every other player toward you", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesPullAll : hamburburmod
{
    protected override void Pressed() => SlapBattlesNetwork.FlingAll(SlapBattlesAim.Body, 24f, true);
}

[hamburburmod(                "Sky All",            "Launch every other player upward", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesSkyAll : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null)
                SlapBattlesNetwork.Fling(rig.Creator.ActorNumber, Vector3.up * 30f);
    }
}

[hamburburmod(                "Tornado Gun",        "Shoot a point to start a brief tornado of flying props and player motion",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesTornadoGun : SlapBattlesGun
{
    protected override float RepeatDelay => 0f;

    private Vector3 center;
    private float   endTime;
    private float   nextProp;
    private float   nextPull;
    private int     step;

    protected override void Shoot()
    {
        if (Gun.Hit.collider == null) return;
        center   = Gun.Hit.point + Vector3.up * 2f;
        endTime  = Time.time     + 7f;
        nextPull = 0f;
        nextProp = 0f;
        step     = 0;
        SlapBattlesNetwork.Blackhole(center);
    }

    protected override void LateUpdate()
    {
        base.LateUpdate();

        if (Time.time >= endTime || !SlapBattlesNetwork.IsInMap) return;

        if (Time.time >= nextPull)
        {
            nextPull = Time.time + 0.12f;
            VRRig[] rigs = NetworkSystem.Instance.Rigs().Where(r => r?.Creator != null).ToArray();
            if (rigs.Length > 0)
            {
                VRRig   rig    = rigs[step % rigs.Length];
                Vector3 offset = rig.transform.position - center;
                if (offset.sqrMagnitude is > 1f and < 225f)
                {
                    Vector3 radial                          = new Vector3(offset.x, 0f, offset.z).normalized;
                    if (radial.sqrMagnitude < 0.01f) radial = Vector3.forward;
                    Vector3 tangent                         = Vector3.Cross(Vector3.up, radial);
                    SlapBattlesNetwork.Fling(rig.Creator.ActorNumber,
                            tangent * 20f - radial * 6f + Vector3.up * 11f);
                }
            }

            step++;
        }

        if (Time.time >= nextProp)
        {
            nextProp = Time.time + 0.35f;
            float   angle   = step * 0.85f;
            Vector3 radial  = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            Vector3 tangent = Vector3.Cross(Vector3.up, radial);
            SlapBattlesNetwork.Throw(step % 2 == 0, center + radial     * 3f + Vector3.up * 3f,
                    tangent               * 10f            + Vector3.up * 3f);
        }
    }

    protected override void OnDisable()
    {
        endTime = 0f;
        base.OnDisable();
    }
}
