using System;
using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using hamburbur.Managers;
using hamburbur.Tools;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

internal enum SlapBattlesObject
{
    Wall,
    Bus,
    SilverWall,
    GoldWall,
    Pumpkin,
    SludgeBall,
    Bee,
    Blackhole,
    EmpShock,
    Nuke,
    Moai,
    Orb,
    BigOrb,
    BoosterWall,
    FrankWall,
    Planet,
}

internal static class SlapBattlesNetwork
{
    internal const float MinimumEventDelay = 0.07f;

    private const int MaxQueuedEvents = 220;
    internal static readonly string[] ObjectNames =
    [
            "Wall", "Bus", "Silver Wall", "Gold Wall", "Pumpkin", "Green Ball", "Bee", "Blackhole", "EMP Shock",
            "Nuke", "Moai", "Orb", "Big Orb", "Booster Wall", "Frank Wall", "Planet",
    ];
    private static readonly Queue<PendingEvent> PriorityEvents = new();
    private static readonly Queue<PendingEvent> VisualEvents   = new();
    private static readonly Vector3             WallMeshOffset = new(0.444f, 0.349f, 0.463f);
    private static          Coroutine           sendRoutine;
    private static          float               nextEventTime;
    internal static bool IsInMap => PhotonNetwork.InRoom &&
                                    CustomGameMode.LuaScript?.Contains("SlapBattles") == true;

    internal static bool SupportsNewSlaps => IsInMap &&
                                             CustomGameMode.LuaScript.Contains("elseif tag == \"nukeDropped\"");

    internal static bool SupportsBoosterWall => IsInMap &&
                                                CustomGameMode.LuaScript.Contains("boosterwall = { sourceNames = { \"BoosterWall\" }");

    internal static bool SupportsAbilityChecks => IsInMap &&
                                                  CustomGameMode.LuaScript.Contains("function allowAbility(sender, ability, spot, key)");

    internal static Player PlayerFor(int actorNumber) =>
            PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber) : null;

    internal static bool HasRelaySource(int targetActor, float maxDistance) =>
            SlapBattlesNearbySourceRelay.IsEnabled && FindRelaySource(targetActor, maxDistance) != null;

    private static Player SourceFor(int targetActor, float maxDistance)
    {
        if (!SupportsBoosterWall || !SlapBattlesNearbySourceRelay.IsEnabled ||
            SlapBattlesRigRangeProxy.IsReadyFor(targetActor))
            return PhotonNetwork.LocalPlayer;

        return FindRelaySource(targetActor, maxDistance) ?? PhotonNetwork.LocalPlayer;
    }

    private static Player FindRelaySource(int targetActor, float maxDistance)
    {
        VRRig target = targetActor.Rig();

        if (target == null || NetworkSystem.Instance == null) return null;
        if (Vector3.Distance(SlapBattlesAim.Body, target.transform.position) < maxDistance - 3f)
            return null;

        VRRig closest         = null;
        float closestDistance = (maxDistance - 3f) * (maxDistance - 3f);
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
        {
            if (rig?.Creator == null || rig.Creator.ActorNumber == targetActor) continue;
            float distance = (rig.transform.position - target.transform.position).sqrMagnitude;

            if (distance >= closestDistance) continue;
            closest         = rig;
            closestDistance = distance;
        }

        return closest == null ? null : PlayerFor(closest.Creator.ActorNumber);
    }

    internal static void Fling(int actorNumber, Vector3 velocity)
    {
        if (Finite(velocity))
        {
            if (SupportsBoosterWall)
                TargetEvent("slapped", actorNumber, velocity, SourceFor(actorNumber, 120f), 1d);
            else
                TargetEvent("slapped", actorNumber, velocity);
        }
    }

    internal static void Move(int actorNumber, Vector3 position)
    {
        if (Finite(position))
        {
            if (SupportsBoosterWall)
            {
                Player source = SourceFor(actorNumber, 30f);
                if (PrepareTargetAbility(source, "GridSlap"))
                    TargetEvent("gridSwapped", actorNumber, position, source);
            }
            else
                TargetEvent("gridSwapped", actorNumber, position);
        }
    }

    internal static void TargetEffect(string eventName, int actorNumber)
    {
        if (eventName is not ("trapped" or "slowed" or "empd" or "glitched" or "redacted")) return;
        if (SupportsBoosterWall)
        {
            Player source = SourceFor(actorNumber, 25f);

            if (eventName == "trapped" && !PrepareTargetAbility(source, "TrapSlap") ||
                eventName == "empd"    && !PrepareTargetAbility(source, "AdminSlap")) return;

            TargetEvent(eventName, actorNumber, source);
        }
        else
            TargetEvent(eventName, actorNumber);
    }

    internal static void Shield(int actorNumber) => TargetEvent("shielded", actorNumber, 8d);
    internal static void Bomb(int actorNumber)
    {
        if (SupportsBoosterWall)
            TargetEvent("bombGiven", actorNumber, 20d, SourceFor(actorNumber, 25f));
        else
            TargetEvent("bombGiven", actorNumber, 20d);
    }

    internal static void Stopwatch(int actorNumber)
    {
        if (!SupportsNewSlaps) return;
        Player source = SourceFor(actorNumber, 25f);
        if (PrepareTargetAbility(source, "StopwatchSlap"))
            TargetEvent("stopwatched", actorNumber, source);
    }

    internal static void StopwatchAll()
    {
        if (!SupportsNewSlaps || NetworkSystem.Instance == null) return;
        HashSet<int> prepared = new();
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
        {
            if (rig?.Creator == null) continue;
            int    actor  = rig.Creator.ActorNumber;
            Player source = SourceFor(actor, 25f);

            if (source == null) continue;
            if (!prepared.Contains(source.ActorNumber))
            {
                if (!PrepareTargetAbility(source, "StopwatchSlap")) continue;
                prepared.Add(source.ActorNumber);
            }

            TargetEvent("stopwatched", actor, source);
        }
    }

    internal static void EmpPulse(Vector3 center)
    {
        if (!IsInMap || NetworkSystem.Instance == null) return;
        EmpShock(center);
        Player source = PhotonNetwork.LocalPlayer;

        if (!PrepareTargetAbility(source, "AdminSlap")) return;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null && Vector3.Distance(center, rig.transform.position) <= 14f)
                TargetEvent("empd", rig.Creator.ActorNumber, source);
    }

    internal static void ErrorScreen(int actorNumber)
    {
        if (SupportsNewSlaps)
        {
            if (SupportsBoosterWall)
                TargetEvent("errored", actorNumber, SourceFor(actorNumber, 25f));
            else
                TargetEvent("errored", actorNumber);
        }
    }

    internal static void Boomerang(int actorNumber, Vector3 direction)
    {
        Player target = PlayerFor(actorNumber);

        if (target == null || !Finite(direction)) return;
        if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
        Send("handArmed", ReceiverGroup.All, 1d,                   target);
        Send("boomerang", ReceiverGroup.All, direction.normalized, target);
    }

    internal static void MultiMode(int actorNumber, int mode)
    {
        Player target = PlayerFor(actorNumber);
        if (target != null && mode is >= 1 and <= 4)
            Send("multiMode", ReceiverGroup.All, (double)mode, target);
    }

    internal static bool GiveSlapAppearance(int actorNumber, string name)
    {
        if (!IsInMap) return false;
        Player target = PlayerFor(actorNumber);
        int    index  = SlapBattlesLocalProgress.PlayableSlapIndex(name);

        if (target == null || target == PhotonNetwork.LocalPlayer || index == 0) return false;
        Send("slapEquipped", ReceiverGroup.All, (double)index, target);
        Send("handArmed",    ReceiverGroup.All, 1d,            target);

        return true;
    }

    internal static bool DisguiseAsSlap(string name)
    {
        int index = SlapBattlesLocalProgress.PlayableSlapIndex(name);

        if (!IsInMap || index == 0 || PhotonNetwork.LocalPlayer == null) return false;
        Send("slapEquipped", ReceiverGroup.All, (double)index, PhotonNetwork.LocalPlayer);
        Send("handArmed",    ReceiverGroup.All, 1d,            PhotonNetwork.LocalPlayer);

        return true;
    }

    internal static void BossBall(Vector3 origin, Vector3 velocity)
    {
        if (Finite(origin) && Finite(velocity))
            Send("bossThrow", ReceiverGroup.All, origin, velocity);
    }

    internal static void Rock(int actorNumber, bool on, bool mega = false)
    {
        if (!SupportsNewSlaps) return;
        Player target = PlayerFor(actorNumber);

        if (target == null) return;
        if (on) Send("rocked", ReceiverGroup.All, 0d, target, 0d);
        Send("rocked", ReceiverGroup.All, on ? 1d : 0d, target, mega ? 1d : 0d);
    }

    internal static void Bee(Vector3 position, int actorNumber)
    {
        Player target = PlayerFor(actorNumber);

        if (target == null || !Finite(position)) return;
        Photon.Realtime.Room room = PhotonNetwork.CurrentRoom;
        AbilitySpawn("beesSpawned",                                         "BeeSlap", position,
                () => StartTracking(TrackBee(position, actorNumber, room)), position,  target);
    }

    internal static void FlingAll(Vector3 center, float force, bool inward = false)
    {
        if (!IsInMap || !Finite(center)) return;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
        {
            if (rig?.Creator == null) continue;
            Vector3 direction = inward ? center - rig.transform.position : rig.transform.position - center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
            Fling(rig.Creator.ActorNumber, direction.normalized * force + Vector3.up * (inward ? 5f : 12f));
        }
    }

    internal static void Spawn(SlapBattlesObject kind, Vector3 point, Vector3 forward, int targetActor = 0)
    {
        if (!Finite(point) || !Finite(forward)) return;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        forward.Normalize();

        switch (kind)
        {
            case SlapBattlesObject.Wall:
                Wall(point + Vector3.up * 0.85f, Quaternion.LookRotation(forward) * Quaternion.Euler(0f, -90f, 0f));

                break;

            case SlapBattlesObject.FrankWall:
                FrankWall(point + Vector3.up * 1.25f,
                        Quaternion.LookRotation(forward) * Quaternion.Euler(0f, -90f, 0f));

                break;

            case SlapBattlesObject.Bus:
                Bus(point, forward, 1);

                break;

            case SlapBattlesObject.SilverWall:
                Bus(point, forward, 2);

                break;

            case SlapBattlesObject.GoldWall:
                Bus(point, forward, 3);

                break;

            case SlapBattlesObject.BoosterWall:
                Bus(point, forward, 4);

                break;

            case SlapBattlesObject.Pumpkin:
                Throw(false, point + Vector3.up * 1.2f, forward * 16f + Vector3.up * 3f);

                break;

            case SlapBattlesObject.SludgeBall:
                Throw(true, point + Vector3.up * 1.2f, forward * 16f + Vector3.up * 3f);

                break;

            case SlapBattlesObject.Bee:
                if (targetActor == 0 && NetworkSystem.Instance != null)
                {
                    float closest = float.MaxValue;
                    foreach (VRRig rig in NetworkSystem.Instance.Rigs())
                    {
                        if (rig?.Creator == null) continue;
                        float distance = (rig.transform.position - point).sqrMagnitude;

                        if (distance >= closest) continue;
                        closest     = distance;
                        targetActor = rig.Creator.ActorNumber;
                    }
                }

                if (targetActor != 0)
                    Bee(SlapBattlesAim.Body + SlapBattlesAim.Forward * 2f + Vector3.up, targetActor);

                break;

            case SlapBattlesObject.Blackhole:
                Blackhole(point + Vector3.up);

                break;

            case SlapBattlesObject.EmpShock:
                EmpShock(point + Vector3.up);

                break;

            case SlapBattlesObject.Nuke:
                Nuke(point);

                break;

            case SlapBattlesObject.Moai:
                Moai(point, forward);

                break;

            case SlapBattlesObject.Planet:
                Planet(point + Vector3.up * 2f);

                break;

            case SlapBattlesObject.Orb:
            case SlapBattlesObject.BigOrb:
                Orb(kind == SlapBattlesObject.BigOrb, point + Vector3.up, forward);

                break;
        }
    }

    internal static void Nuke(Vector3 ground)
    {
        if (!SupportsNewSlaps || !Finite(ground)) return;
        Photon.Realtime.Room room = PhotonNetwork.CurrentRoom;
        AbilitySpawn("nukeDropped",                           "NukeSlap", ground,
                () => StartTracking(TrackNuke(ground, room)), ground);
    }

    internal static void Moai(Vector3 point, Vector3 forward)
    {
        if (!SupportsNewSlaps || !Finite(point) || !Finite(forward)) return;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
        Quaternion           rotation             = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 180f, 0f);
        Photon.Realtime.Room room                 = PhotonNetwork.CurrentRoom;
        AbilitySpawn("moaiSpawned",                          "MoaiSlap", point,
                () => StartTracking(TrackMoai(point, room)), point,      rotation);
    }

    internal static void Orb(bool big, Vector3 origin, Vector3 direction)
    {
        if (!SupportsNewSlaps || !Finite(origin) || !Finite(direction)) return;
        if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
        Vector3              velocity                 = direction.normalized * (big ? 20f : 14f);
        Photon.Realtime.Room room                     = PhotonNetwork.CurrentRoom;
        AbilitySpawn("orbThrown",                                           "KillStreakSlap", origin,
                () => StartTracking(TrackOrb(origin, velocity, big, room)), origin,           velocity, big ? 1d : 0d);
    }

    internal static void Wall(Vector3 center, Quaternion rotation)
    {
        if (!Finite(center) || !Finite(rotation)) return;
        Vector3 pivot = center - rotation * WallMeshOffset;
        AbilitySpawn("wallRaised", "AdminSlap", center, null, pivot, rotation);
    }

    internal static void FrankWall(Vector3 center, Quaternion rotation)
    {
        if (!SupportsAbilityChecks || !Finite(center) || !Finite(rotation)) return;
        Vector3 pivot = center - rotation * (WallMeshOffset * 1.52f);
        AbilitySpawn("wallRaised", "FrankSlap", center, null, pivot, rotation);
    }

    internal static void Bus(Vector3 point, Vector3 direction, int kind)
    {
        if (kind is < 1 or > 4 || kind == 4 && !SupportsBoosterWall || !Finite(point) || !Finite(direction)) return;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) direction = Vector3.forward;
        direction.Normalize();

        Vector3 strikeCenter = point + Vector3.up * (kind == 1 ? 0.1f : 0.5f);
        Vector3 origin       = strikeCenter;
        if (kind != 1)
        {
            Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.Euler(0f, -90f, 0f);
            origin -= rotation * new Vector3(-0.153f, 0f, -1.78f);
        }

        Photon.Realtime.Room room = PhotonNetwork.CurrentRoom;
        string slap = kind switch
                      {
                              2 => "10KSlap", 3 => "100KSlap", 4 => "BoosterSlap", var _ => "BusSlap",
                      };

        AbilitySpawn("busSpawned",                                            slap,   strikeCenter,
                () => StartTracking(TrackBus(strikeCenter, direction, room)), origin, direction, (double)kind);
    }

    internal static void Throw(bool green, Vector3 origin, Vector3 velocity)
    {
        if (Finite(origin) && Finite(velocity))
        {
            Photon.Realtime.Room room = PhotonNetwork.CurrentRoom;
            AbilitySpawn(green ? "sludgeThrown" : "pumpkinThrown",
                    green ? "SludgeSlap" : "SpookySlap",                          origin,
                    () => StartTracking(TrackProjectile(origin, velocity, room)), origin, velocity);
        }
    }

    internal static void Blackhole(Vector3 position)
    {
        if (!Finite(position)) return;
        Photon.Realtime.Room room = PhotonNetwork.CurrentRoom;
        AbilitySpawn("blackholeSpawned",                             "BlackholeSlap", position,
                () => StartTracking(TrackBlackhole(position, room)), position);
    }

    internal static void EmpShock(Vector3 position)
    {
        if (Finite(position)) AbilitySpawn("empShock", "AdminSlap", position, null, position);
    }

    internal static void Planet(Vector3 position)
    {
        if (SupportsAbilityChecks && Finite(position))
            AbilitySpawn("planetSpawned", "MoonSlap", position, null, position);
    }

    internal static void Lucy(Vector3 position, int targetActor, bool red)
    {
        if (!SupportsAbilityChecks || !Finite(position)) return;
        Player target = PlayerFor(targetActor);
        Player source = PhotonNetwork.LocalPlayer;
        int    index  = SlapBattlesLocalProgress.PlayableSlapIndex("LucySlap");

        if (target                                    == null || source == null || index == 0 ||
            PriorityEvents.Count + VisualEvents.Count > MaxQueuedEvents - 2) return;

        Send("slapEquipped",   source == PhotonNetwork.LocalPlayer ? ReceiverGroup.Others : ReceiverGroup.All,
                (double)index, source, 0d);

        double kind = red ? 2d : 1d;
        Enqueue("lucySpawned", ReceiverGroup.Others,
                () => SlapBattlesLocalVisual.Spawn("lucySpawned", [position, target, source, kind,]),
                position, target, source, kind);
    }

    internal static void SpawnGoldenSlapples()
    {
        if (!IsInMap || !SupportsAbilityChecks) return;
        Player host = null;
        foreach (Player player in PhotonNetwork.PlayerList)
            if (player != null && (host == null || player.ActorNumber < host.ActorNumber))
                host = player;

        if (host == null) return;
        for (int i = 1; i <= 5; i++)
            Send("slappleSpawn", ReceiverGroup.All, (double)i, 2d, host);
    }

    private static void AbilitySpawn(string tag,    string          slap, Vector3 spot,
                                     Action onSent, params object[] arguments)
    {
        if (!IsInMap || !Finite(spot)) return;
        if (!SupportsAbilityChecks)
        {
            Enqueue(tag, ReceiverGroup.All, onSent, arguments);

            return;
        }

        Player source = ClosestSource(spot);

        if (source == null) return;

        int index = SlapBattlesLocalProgress.PlayableSlapIndex(slap);

        if (index                                     == 0 ||
            PriorityEvents.Count + VisualEvents.Count > MaxQueuedEvents - 2) return;

        Send("slapEquipped",   source == PhotonNetwork.LocalPlayer ? ReceiverGroup.Others : ReceiverGroup.All,
                (double)index, source, 0d);

        bool     frankWall = tag == "wallRaised" && slap == "FrankSlap";
        object[] payload   = new object[arguments.Length + (frankWall ? 2 : 1)];
        Array.Copy(arguments, payload, arguments.Length);
        payload[arguments.Length] = source;
        if (frankWall) payload[arguments.Length + 1] = 1d;
        Enqueue(tag, ReceiverGroup.Others, () =>
                                           {
                                               SlapBattlesLocalVisual.Spawn(tag, payload);
                                               onSent?.Invoke();
                                           }, payload);
    }

    private static Player ClosestSource(Vector3 spot)
    {
        Player nearest = PhotonNetwork.LocalPlayer;
        float bestDistance = nearest == null ? float.PositiveInfinity :
                                     (SlapBattlesAim.Body - spot).sqrMagnitude;

        if (NetworkSystem.Instance == null) return nearest;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
        {
            if (rig?.Creator == null || rig.Creator.ActorNumber == PhotonNetwork.LocalPlayer?.ActorNumber) continue;
            float distance = (rig.transform.position - spot).sqrMagnitude;

            if (distance >= bestDistance) continue;
            nearest      = PlayerFor(rig.Creator.ActorNumber);
            bestDistance = distance;
        }

        return nearest;
    }

    private static bool PrepareTargetAbility(Player source, string slap)
    {
        if (source == null || !SupportsAbilityChecks) return source != null;
        int index = SlapBattlesLocalProgress.PlayableSlapIndex(slap);

        if (index == 0 || PriorityEvents.Count + VisualEvents.Count > MaxQueuedEvents - 2) return false;
        Send("slapEquipped",   source == PhotonNetwork.LocalPlayer ? ReceiverGroup.Others : ReceiverGroup.All,
                (double)index, source, 0d);

        return true;
    }

    internal static void Spell(string text, Vector3 point, Quaternion layout)
    {
        if (string.IsNullOrWhiteSpace(text) || !Finite(point) || !Finite(layout)) return;

        Quaternion wallRotation = layout                                                     * Quaternion.Euler(0f, 90f, 0f);
        float      width        = (text.Length                                      * 4 - 1) * 3.15f;
        Vector3    origin       = point + Vector3.up * 3f - layout * (Vector3.right * (width * 0.5f));
        int        count        = 0;
        foreach (Vector3 center in text.TextToPositions(origin, layout, 3.15f, 1.85f))
        {
            if (++count > 90) break;
            Wall(center, wallRotation);
        }
    }

    internal static void StartTournament() => Send("tournStart", ReceiverGroup.All);
    internal static void StartBoss()       => Send("bossStart",  ReceiverGroup.All, 0d);

    private static void TargetEvent(string eventName, int actorNumber, params object[] arguments)
    {
        Player target = PlayerFor(actorNumber);

        if (target == null || target == PhotonNetwork.LocalPlayer) return;
        object[] payload = new object[arguments.Length + 1];
        payload[0] = target;
        Array.Copy(arguments, 0, payload, 1, arguments.Length);
        Send(eventName, ReceiverGroup.All, payload);
    }

    private static void Send(string eventName, ReceiverGroup receivers, params object[] arguments)
        => Enqueue(eventName, receivers, null, arguments);

    private static void Enqueue(string eventName, ReceiverGroup receivers, Action onSent, params object[] arguments)
    {
        if (!IsInMap || PriorityEvents.Count + VisualEvents.Count >= MaxQueuedEvents) return;
        object[] payload = new object[arguments.Length + 1];
        payload[0] = eventName;
        Array.Copy(arguments, 0, payload, 1, arguments.Length);
        bool priority = eventName is "slapped" or "gridSwapped" or "trapped" or "bombGiven" or
                                     "shielded" or "empd" or "slowed" or "glitched" or "redacted" or
                                     "stopwatched" or "errored" or "slapEquipped" or "wallRaised" or
                                     "busSpawned" or "beesSpawned" or "blackholeSpawned" or "empShock" or
                                     "pumpkinThrown" or "sludgeThrown" or "moaiSpawned" or "nukeDropped" or
                                     "orbThrown" or "planetSpawned" or "lucySpawned";

        (priority ? PriorityEvents : VisualEvents).Enqueue(new PendingEvent
        {
                Room      = PhotonNetwork.CurrentRoom,
                Payload   = payload,
                Receivers = receivers,
                OnSent    = onSent,
        });

        if (sendRoutine != null) return;
        MonoBehaviour runner            = CoroutineManager.Instance != null ? CoroutineManager.Instance : Plugin.Instance;
        if (runner != null) sendRoutine = runner.StartCoroutine(Drain());
    }

    private static IEnumerator Drain()
    {
        while (PriorityEvents.Count + VisualEvents.Count > 0)
        {
            if (Time.realtimeSinceStartup < nextEventTime)
            {
                yield return null;

                continue;
            }

            PendingEvent pending = PriorityEvents.Count > 0 ? PriorityEvents.Dequeue() : VisualEvents.Dequeue();

            if (!IsInMap || !ReferenceEquals(pending.Room, PhotonNetwork.CurrentRoom)) continue;

            bool sent = PhotonNetwork.RaiseEvent(CustomMapUtils.EventCode, pending.Payload,
                    new RaiseEventOptions { Receivers = pending.Receivers, }, SendOptions.SendReliable);

            nextEventTime = Time.realtimeSinceStartup + MinimumEventDelay;
            if (sent) pending.OnSent?.Invoke();

            yield return null;
        }

        sendRoutine = null;
    }

    private static void StartTracking(IEnumerator routine)
    {
        MonoBehaviour runner = CoroutineManager.Instance != null ? CoroutineManager.Instance : Plugin.Instance;
        if (runner != null && IsInMap) runner.StartCoroutine(routine);
    }

    private static void PropFling(int actorNumber, Vector3 velocity, Photon.Realtime.Room room)
    {
        if (!IsInMap || !ReferenceEquals(room, PhotonNetwork.CurrentRoom)) return;
        Fling(actorNumber, velocity);
    }

    private static IEnumerator TrackProjectile(Vector3 origin, Vector3 velocity, Photon.Realtime.Room room)
    {
        float start = Time.time;
        while (Time.time - start < 3f && IsInMap && ReferenceEquals(room, PhotonNetwork.CurrentRoom))
        {
            float   t     = Time.time - start;
            Vector3 point = origin    + velocity * t + Vector3.down * (4.9f * t * t);
            foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            {
                if (rig?.Creator == null || Vector3.Distance(point, rig.transform.position) > 1f) continue;
                Vector3 away = rig.transform.position - point;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = Vector3.forward;
                PropFling(rig.Creator.ActorNumber, away.normalized * 10f + Vector3.up * 4f, room);

                yield break;
            }

            yield return null;
        }
    }

    private static IEnumerator TrackBus(Vector3 origin, Vector3 direction, Photon.Realtime.Room room)
    {
        float        start    = Time.time;
        HashSet<int> hit      = [];
        Vector3      previous = origin;
        while (Time.time - start < 6f && IsInMap && ReferenceEquals(room, PhotonNetwork.CurrentRoom))
        {
            Vector3 point = origin + direction * (16f * (Time.time - start));
            foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            {
                if (rig?.Creator                                               == null || hit.Contains(rig.Creator.ActorNumber) ||
                    DistanceToSegment(rig.transform.position, previous, point) > 3.5f) continue;

                hit.Add(rig.Creator.ActorNumber);
                Vector3 away = rig.transform.position - ClosestPointOnSegment(rig.transform.position, previous, point);
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = direction;
                PropFling(rig.Creator.ActorNumber, away.normalized * 32f + Vector3.up * 16f, room);
            }

            previous = point;

            yield return null;
        }
    }

    private static Vector3 ClosestPointOnSegment(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment       = end - start;
        float   lengthSquared = segment.sqrMagnitude;

        if (lengthSquared < 0.0001f) return start;

        return start + segment * Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
    }

    private static float DistanceToSegment(Vector3 point, Vector3 start, Vector3 end) =>
            Vector3.Distance(point, ClosestPointOnSegment(point, start, end));

    private static IEnumerator TrackBee(Vector3 origin, int actorNumber, Photon.Realtime.Room room)
    {
        float   start     = Time.time;
        float   nextSting = 0f;
        Vector3 point     = origin;
        while (Time.time - start < 14f && IsInMap && ReferenceEquals(room, PhotonNetwork.CurrentRoom))
        {
            VRRig rig = actorNumber.Rig();

            if (rig == null) yield break;
            point = Vector3.MoveTowards(point, rig.transform.position, 8f * Time.deltaTime);
            if (Time.time >= nextSting && Vector3.Distance(point, rig.transform.position) < 1.3f)
            {
                Vector3 away                        = rig.transform.position - point;
                if (away.sqrMagnitude < 0.01f) away = Vector3.up;
                PropFling(actorNumber, away.normalized * 7.5f + Vector3.up * 3f, room);
                nextSting = Time.time + 2f;
            }

            yield return null;
        }
    }

    private static IEnumerator TrackBlackhole(Vector3 center, Photon.Realtime.Room room)
    {
        float end = Time.time + 4f;
        while (Time.time < end && IsInMap && ReferenceEquals(room, PhotonNetwork.CurrentRoom))
        {
            foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            {
                if (rig?.Creator == null || Vector3.Distance(center, rig.transform.position) > 14f) continue;
                Vector3 direction = center - rig.transform.position;
                if (direction.sqrMagnitude > 0.01f)
                    PropFling(rig.Creator.ActorNumber, direction.normalized * 16f, room);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private static IEnumerator TrackNuke(Vector3 ground, Photon.Realtime.Room room)
    {
        yield return new WaitForSeconds(5f);

        if (!SupportsNewSlaps || !ReferenceEquals(room, PhotonNetwork.CurrentRoom)) yield break;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
        {
            if (rig?.Creator == null) continue;
            Vector3 offset = rig.transform.position - ground;
            Vector3 flat   = new(offset.x, 0f, offset.z);

            if (flat.magnitude > 5f || offset.y is < -1f or > 5f) continue;
            if (flat.sqrMagnitude < 0.01f) flat = Vector3.forward;
            PropFling(rig.Creator.ActorNumber, flat.normalized * 32f + Vector3.up * 16f, room);
        }
    }

    private static IEnumerator TrackMoai(Vector3 center, Photon.Realtime.Room room)
    {
        float end = Time.time + 10f;
        while (Time.time < end && SupportsNewSlaps && ReferenceEquals(room, PhotonNetwork.CurrentRoom))
        {
            foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            {
                if (rig?.Creator == null) continue;
                Vector3 offset = rig.transform.position - (center + Vector3.up);

                if (offset.magnitude > 3f) continue;
                offset.y = 0f;
                if (offset.sqrMagnitude < 0.01f) offset = Vector3.forward;
                PropFling(rig.Creator.ActorNumber, offset.normalized * 7f + Vector3.up * 3.5f, room);
            }

            yield return new WaitForSeconds(1f);
        }
    }

    private static IEnumerator TrackOrb(Vector3 origin, Vector3 velocity, bool big, Photon.Realtime.Room room)
    {
        float start  = Time.time;
        float radius = big ? 1.8f : 1.2f;
        float force  = big ? 16f : 13f;
        while (Time.time - start < 2.5f && SupportsNewSlaps && ReferenceEquals(room, PhotonNetwork.CurrentRoom))
        {
            Vector3 point = origin + velocity * (Time.time - start);
            foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            {
                if (rig?.Creator == null || Vector3.Distance(point, rig.transform.position) > radius) continue;
                Vector3 away = rig.transform.position - point;
                away.y = 0f;
                if (away.sqrMagnitude < 0.01f) away = velocity;
                PropFling(rig.Creator.ActorNumber, away.normalized * force + Vector3.up * (force * 0.5f), room);

                yield break;
            }

            yield return null;
        }
    }

    private static bool Finite(Vector3 value) =>
            Finite(value.x) && Finite(value.y) && Finite(value.z) && value.sqrMagnitude < 100000000f;

    private static bool Finite(Quaternion value) =>
            Finite(value.x) && Finite(value.y) && Finite(value.z) && Finite(value.w) &&
            Quaternion.Dot(value, value) > 0.5f;

    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private sealed class PendingEvent
    {
        internal Action               OnSent;
        internal object[]             Payload;
        internal ReceiverGroup        Receivers;
        internal Photon.Realtime.Room Room;
    }
}