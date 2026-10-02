using System;
using System.Collections;
using System.Linq;
using hamburbur.Managers;
using hamburbur.Mod_Backend;
using hamburbur.Tools;
using Photon.Pun;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

[hamburburmod(                "Rig Range Assist",   "For distant targeted effects, hold your network rig near the target while shooting",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesRigRangeAssist : hamburburmod
{
    internal static bool IsEnabled { get; private set; }

    protected override void OnEnable() => IsEnabled = true;

    protected override void Update() => SlapBattlesRigRangeProxy.Tick();

    protected override void OnDisable()
    {
        IsEnabled = false;
        SlapBattlesRigRangeProxy.Restore();
    }
}

[hamburburmod(                "Nearby Source Relay", "Use a player near the target as the reported source of distant map effects",
        ButtonType.Togglable, AccessSetting.Public,  EnabledType.Disabled, 0)]
public class SlapBattlesNearbySourceRelay : hamburburmod
{
    internal static bool IsEnabled { get; private set; }

    protected override void OnEnable()  => IsEnabled = true;
    protected override void OnDisable() => IsEnabled = false;
}

[hamburburmod(            "Water Flight Mask",  "Broadcast Water Slap so the map's client-side flight check exempts your rig",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesWaterFlightMask : hamburburmod
{
    protected override void Pressed() => SlapBattlesLocalProgress.Notify(
            SlapBattlesNetwork.DisguiseAsSlap("WaterSlap")
                    ? "Water Slap appearance broadcast. Your actual slap ability is unchanged."
                    : "Enter Slap Battles and wait for the map script.");
}

[hamburburmod(                  "Distant Effect: ",   "Choose the effect for the staged range assist sweep",
        ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesDistantEffect : hamburburmod
{
    internal static readonly string[] Names =
            ["Fling", "Pull", "Sky", "Trap", "Slow", "EMP", "Glitch", "Redacted", "Bomb", "Stopwatch", "Error",];
    internal static SlapBattlesDistantEffect Instance { get; private set; }
    internal static int                      Selected => Instance?.IncrementalValue ?? 0;

    public override string ModName => AssociatedAttribute.Name + Names[Math.Clamp(IncrementalValue, 0, Names.Length - 1)];

    protected override void Start()                    => Instance = this;
    protected override void Increment()                => IncrementalValue = (IncrementalValue                                + 1)            % Names.Length;
    protected override void Decrement()                => IncrementalValue = (IncrementalValue                            - 1 + Names.Length) % Names.Length;
    protected override void OnIncrementalStateLoaded() => IncrementalValue = Math.Clamp(IncrementalValue, 0, Names.Length - 1);
}

[hamburburmod(            "Distant Effect All", "Apply the selected effect one player at a time using range assist",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesDistantEffectAll : hamburburmod
{
    private static bool running;

    protected override void Pressed()
    {
        if (running)
        {
            SlapBattlesLocalProgress.Notify("Distant effect sweep is already running.");

            return;
        }

        if (!SlapBattlesNetwork.SupportsBoosterWall || !SlapBattlesRigRangeAssist.IsEnabled ||
            CoroutineManager.Instance == null       || NetworkSystem.Instance == null)
        {
            SlapBattlesLocalProgress.Notify("Enable Rig Range Assist in Slap Battles first.");

            return;
        }

        running = true;
        CoroutineManager.Instance.StartCoroutine(Sweep(SlapBattlesDistantEffect.Selected));
    }

    private static IEnumerator Sweep(int effect)
    {
        object               owner = new();
        Photon.Realtime.Room room  = PhotonNetwork.CurrentRoom;
        try
        {
            foreach (VRRig initial in NetworkSystem.Instance.Rigs().Where(rig => rig?.Creator != null).ToArray())
            {
                if (!SlapBattlesRigRangeAssist.IsEnabled || !ReferenceEquals(room, PhotonNetwork.CurrentRoom))
                    yield break;

                VRRig target = initial.Creator.ActorNumber.Rig();

                if (target?.Creator == null) continue;
                int   actor = target.Creator.ActorNumber;
                float range = effect <= 2 ? 120f : 25f;
                bool far = Vector3.Distance(SlapBattlesAim.Body, target.transform.position) > range - 3f &&
                           !SlapBattlesNetwork.HasRelaySource(actor, range);

                if (far)
                {
                    float deadline = Time.realtimeSinceStartup + 1.5f;
                    bool  ready    = false;
                    while (Time.realtimeSinceStartup < deadline &&
                           SlapBattlesRigRangeAssist.IsEnabled  &&
                           ReferenceEquals(room, PhotonNetwork.CurrentRoom))
                    {
                        ready = SlapBattlesRigRangeProxy.Prepare(owner, target);

                        if (ready) break;

                        yield return null;
                    }

                    if (!ready || !SlapBattlesRigRangeProxy.IsActiveFor(actor))
                    {
                        SlapBattlesRigRangeProxy.Release(owner);

                        yield return new WaitForSeconds(1.1f);

                        continue;
                    }
                }

                switch (effect)
                {
                    case 0:
                    {
                        Vector3 away                        = target.transform.position - SlapBattlesAim.Body;
                        if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                        SlapBattlesNetwork.Fling(actor, away.normalized * 30f + Vector3.up * 14f);

                        break;
                    }

                    case 1:
                    {
                        Vector3 toward                          = SlapBattlesAim.Body - target.transform.position;
                        if (toward.sqrMagnitude < 0.01f) toward = Vector3.forward;
                        SlapBattlesNetwork.Fling(actor, toward.normalized * 24f + Vector3.up * 5f);

                        break;
                    }

                    case 2:  SlapBattlesNetwork.Fling(actor, Vector3.up * 32f); break;
                    case 3:  SlapBattlesNetwork.TargetEffect("trapped",  actor); break;
                    case 4:  SlapBattlesNetwork.TargetEffect("slowed",   actor); break;
                    case 5:  SlapBattlesNetwork.TargetEffect("empd",     actor); break;
                    case 6:  SlapBattlesNetwork.TargetEffect("glitched", actor); break;
                    case 7:  SlapBattlesNetwork.TargetEffect("redacted", actor); break;
                    case 8:  SlapBattlesNetwork.Bomb(actor); break;
                    case 9:  SlapBattlesNetwork.Stopwatch(actor); break;
                    case 10: SlapBattlesNetwork.ErrorScreen(actor); break;
                }

                yield return new WaitForSeconds(0.25f);
                if (far)
                {
                    SlapBattlesRigRangeProxy.Release(owner);

                    yield return new WaitForSeconds(1.1f);
                }
                else
                {
                    yield return new WaitForSeconds(0.18f);
                }
            }
        }
        finally
        {
            SlapBattlesRigRangeProxy.Release(owner);
            running = false;
        }
    }
}

internal static class SlapBattlesRigRangeProxy
{
    private const  float                SettleTime   = 0.5f;
    private const  float                ReleaseDelay = 1f;
    private static object               owner;
    private static int                  actorNumber;
    private static Photon.Realtime.Room room;
    private static Vector3              lastPosition;
    private static float                readyAt;
    private static float                releaseAt;
    private static float                nextConflictNotice;

    internal static bool IsBusy => owner != null;

    internal static bool IsActiveFor(int actor) => owner != null && actorNumber == actor &&
                                                   ReferenceEquals(room, PhotonNetwork.CurrentRoom);
    internal static bool IsReadyFor(int actor) => IsActiveFor(actor) && Time.realtimeSinceStartup >= readyAt;

    internal static bool Prepare(object requester, VRRig target)
    {
        if (!SlapBattlesRigRangeAssist.IsEnabled || !SlapBattlesNetwork.IsInMap ||
            requester == null                    || target?.Creator == null     || !VRRig.LocalRig)
            return false;

        if (owner != null && owner != requester) return false;
        if (owner == null && !RigUtils.IsRigEnabled)
        {
            if (Time.realtimeSinceStartup >= nextConflictNotice)
            {
                SlapBattlesLocalProgress.Notify("Range Assist needs control of your rig; turn off the other rig mod.");
                nextConflictNotice = Time.realtimeSinceStartup + 4f;
            }

            return false;
        }

        if (owner != null && !ReferenceEquals(room, PhotonNetwork.CurrentRoom))
        {
            Restore();

            return false;
        }

        if (owner != null && Vector3.Distance(RigUtils.RigPosition, lastPosition) > 3f)
        {
            owner       = null;
            actorNumber = 0;
            room        = null;

            return false;
        }

        Vector3 toward = SlapBattlesAim.Body - target.transform.position;
        toward.y = 0f;
        if (toward.sqrMagnitude < 0.01f) toward = Vector3.forward;
        Vector3 position                        = target.transform.position + toward.normalized * 3f;

        if (owner == null)
        {
            RigUtils.ToggleRig(false, position);
            owner   = requester;
            room    = PhotonNetwork.CurrentRoom;
            readyAt = Time.realtimeSinceStartup + SettleTime;
        }
        else if (actorNumber != target.Creator.ActorNumber)
        {
            readyAt = Time.realtimeSinceStartup + SettleTime;
        }

        actorNumber                       = target.Creator.ActorNumber;
        releaseAt                         = float.PositiveInfinity;
        lastPosition                      = position;
        RigUtils.RigPosition              = position;
        VRRig.LocalRig.transform.position = position;

        return Time.realtimeSinceStartup >= readyAt;
    }

    internal static void Release(object requester)
    {
        if (owner == requester && float.IsPositiveInfinity(releaseAt))
            releaseAt = Time.realtimeSinceStartup + ReleaseDelay;
    }

    internal static void Tick()
    {
        if (owner != null && (Time.realtimeSinceStartup >= releaseAt ||
                              !SlapBattlesNetwork.IsInMap            ||
                              !ReferenceEquals(room, PhotonNetwork.CurrentRoom)))
            Restore();
    }

    internal static void Restore()
    {
        if (owner                                                != null && !RigUtils.IsRigEnabled &&
            Vector3.Distance(RigUtils.RigPosition, lastPosition) <= 3f)
            RigUtils.ToggleRig(true);

        owner       = null;
        actorNumber = 0;
        room        = null;
        releaseAt   = 0f;
    }
}