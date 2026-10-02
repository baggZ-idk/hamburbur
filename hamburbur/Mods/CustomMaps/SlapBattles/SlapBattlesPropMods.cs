using System;
using GorillaNetworking;
using hamburbur.Mod_Backend;
using hamburbur.Tools;
using Photon.Pun;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

[hamburburmod(                  "Spawn Object: ",     "Choose a map object with the left and right menu buttons",
        ButtonType.Incremental, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesNextProp : hamburburmod
{
    public static SlapBattlesNextProp Instance { get; private set; }
    internal static SlapBattlesObject Selected =>
            (SlapBattlesObject)(Instance?.IncrementalValue ?? 0);

    public override string ModName => AssociatedAttribute.Name +
                                      SlapBattlesNetwork.ObjectNames[Math.Clamp(IncrementalValue, 0,
                                              SlapBattlesNetwork.ObjectNames.Length - 1)];

    protected override void Start()     => Instance = this;
    protected override void Increment() => IncrementalValue = (IncrementalValue + 1) % SlapBattlesNetwork.ObjectNames.Length;
    protected override void Decrement() => IncrementalValue =
                                                   (IncrementalValue - 1 + SlapBattlesNetwork.ObjectNames.Length) % SlapBattlesNetwork.ObjectNames.Length;

    protected override void OnIncrementalStateLoaded() =>
            IncrementalValue = Math.Clamp(IncrementalValue, 0, SlapBattlesNetwork.ObjectNames.Length - 1);
}

[hamburburmod(                "Spawn Object Gun",   "Hold to spawn the selected map object for the room",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesSpawnGun : SlapBattlesGun
{
    protected override float RepeatDelay => SlapBattlesNextProp.Selected switch
    {
            SlapBattlesObject.Nuke or SlapBattlesObject.Blackhole => 1f,
            SlapBattlesObject.Moai => 0.8f,
            SlapBattlesObject.Bus or SlapBattlesObject.SilverWall or SlapBattlesObject.GoldWall or
                    SlapBattlesObject.BoosterWall => 0.35f,
            SlapBattlesObject.Bee => 0.3f,
            _ => SlapBattlesNetwork.MinimumEventDelay,
    };

    protected override void Shoot()
    {
        if (Gun.Hit.collider != null)
        {
            Vector3 point = Gun.Hit.point + Gun.Hit.normal * 0.2f;
            Vector3 from = GorillaTagger.Instance?.bodyCollider != null
                                   ? GorillaTagger.Instance.bodyCollider.transform.position : point - Vector3.forward;

            SlapBattlesNetwork.Spawn(SlapBattlesNextProp.Selected, point, point - from,
                    Gun.ChosenRig?.Creator?.ActorNumber ?? 0);
        }
    }
}

[hamburburmod(                "Bus Convoy",         "Spawn three synchronized buses ahead of you", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBusConvoy : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        Vector3 forward = SlapBattlesAim.Forward;
        Vector3 right   = Vector3.Cross(Vector3.up, forward);
        Vector3 center  = SlapBattlesAim.Body + forward * 4f - Vector3.up;
        for (int i = -1; i <= 1; i++)
            SlapBattlesNetwork.Bus(center + right * (i * 4f), forward, 1);
    }
}

[hamburburmod("Booster Wall Gun", "Fire the new synchronized Booster Wall prop",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBoosterWallGun : SlapBattlesGun
{
    protected override float RepeatDelay => 0.35f;

    protected override void Shoot()
    {
        if (Gun.Hit.collider == null || !SlapBattlesNetwork.SupportsBoosterWall) return;
        Vector3 point = Gun.Hit.point;
        Vector3 direction = point - SlapBattlesAim.Body;
        SlapBattlesNetwork.Bus(point, direction, 4);
    }
}

[hamburburmod("Booster Wall Burst", "Launch eight Booster Walls outward around you",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBoosterWallBurst : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.SupportsBoosterWall) return;
        Vector3 center = SlapBattlesAim.Body - Vector3.up;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 0.25f;
            Vector3 direction = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            SlapBattlesNetwork.Bus(center + direction * 2f, direction, 4);
        }
    }
}

[hamburburmod(                "Wall Ring",          "Build a short lived ring of eight synchronized walls", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesWallRing : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        Vector3 center = SlapBattlesAim.Body;
        for (int i = 0; i < 8; i++)
        {
            float   angle  = i * Mathf.PI * 0.25f;
            Vector3 radial = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            SlapBattlesNetwork.Wall(center + radial * 5f,
                    Quaternion.LookRotation(radial) * Quaternion.Euler(0f, -90f, 0f));
        }
    }
}

[hamburburmod("Wall Anti Report", "Keep a map wall in front of your VStump leaderboard report button",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Enabled, 0)]
public class SlapBattlesWallAntiReport : hamburburmod
{
    private const float RefreshInterval = 12f; // The map retires wall clones after 15 seconds.
    private Photon.Realtime.Room room;
    private Vector3 wallCenter;
    private Quaternion wallRotation;
    private bool hasWallPosition;
    private float nextRefresh;
    private bool reportedMissingBoard;

    protected override void OnEnable()
    {
        nextRefresh = 0f;
        room = null;
        hasWallPosition = false;
        reportedMissingBoard = false;
    }

    protected override void Update()
    {
        if (!SlapBattlesNetwork.IsInMap || CustomGameMode.gameScriptRunner?.ShouldTick != true)
        {
            nextRefresh = 0f;
            return;
        }
        if (!ReferenceEquals(room, PhotonNetwork.CurrentRoom))
        {
            room = PhotonNetwork.CurrentRoom;
            hasWallPosition = false;
            nextRefresh = 0f;
        }
        if (Time.realtimeSinceStartup < nextRefresh) return;

        Transform reportButton = FindVStumpReportButton();
        if (reportButton != null)
        {
            Vector3 outward = reportButton.forward;
            outward.y = 0f;
            Vector3 towardRoom = SlapBattlesAim.Body - reportButton.position;
            towardRoom.y = 0f;
            if (outward.sqrMagnitude < 0.01f) outward = towardRoom;
            if (outward.sqrMagnitude < 0.01f) outward = Vector3.forward;
            if (towardRoom.sqrMagnitude > 0.25f && Vector3.Dot(outward, towardRoom) < 0f)
                outward = -outward;
            outward.Normalize();

            Vector3 across = Vector3.Cross(outward, Vector3.up);
            wallCenter = reportButton.position + outward * 0.45f;
            wallRotation = Quaternion.LookRotation(across, Vector3.up);
            hasWallPosition = true;
        }
        if (!hasWallPosition)
        {
            nextRefresh = Time.realtimeSinceStartup + 1f;
            if (!reportedMissingBoard)
            {
                SlapBattlesLocalProgress.Notify("Wall Anti Report is waiting for the VStump leaderboard.");
                reportedMissingBoard = true;
            }
            return;
        }

        SlapBattlesNetwork.Wall(wallCenter, wallRotation);
        nextRefresh = Time.realtimeSinceStartup + RefreshInterval;
    }

    private static Transform FindVStumpReportButton()
    {
        if (NetworkSystem.Instance?.LocalPlayer == null ||
            GorillaScoreboardTotalUpdater.allScoreboardLines == null)
            return null;

        bool inVStump = GorillaComputer.hasInstance && GorillaComputer.instance.IsPlayerInVirtualStump();
        Transform vStumpRoot = GameObject.Find("VStump_Lobby")?.transform;
        if (!inVStump && vStumpRoot == null) return null;

        Transform ownButton = null;
        float ownDistance = float.PositiveInfinity;
        Transform nearestButton = null;
        float nearestDistance = float.PositiveInfinity;
        foreach (GorillaPlayerScoreboardLine line in GorillaScoreboardTotalUpdater.allScoreboardLines)
        {
            if (line == null || line.reportButton == null) continue;
            Transform button = line.reportButton.transform;
            if (!inVStump && !button.IsChildOf(vStumpRoot)) continue;
            float distance = (button.position - SlapBattlesAim.Body).sqrMagnitude;
            if (distance < nearestDistance)
            {
                nearestButton = button;
                nearestDistance = distance;
            }
            if (line.playerActorNumber != NetworkSystem.Instance.LocalPlayer.ActorNumber &&
                line.linePlayer        != NetworkSystem.Instance.LocalPlayer || distance >= ownDistance)
                continue;
            ownButton = button;
            ownDistance = distance;
        }
        return ownButton ?? nearestButton;
    }
}

[hamburburmod(                "Blackhole Gun",      "Spawn a blackhole and pull nearby players toward it", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBlackholeGun : SlapBattlesGun
{
    protected override float RepeatDelay => 0.7f;

    protected override void Shoot()
    {
        if (Gun.Hit.collider == null) return;
        Vector3 center = Gun.Hit.point + Vector3.up * 2f;
        SlapBattlesNetwork.Blackhole(center);
    }
}

[hamburburmod(                "Bee Swarm Gun",      "Send a synchronized bee after the targeted player", ButtonType.Togglable,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBeeGun : SlapBattlesTargetGun
{
    protected override void Use(VRRig rig) =>
            SlapBattlesNetwork.Bee(SlapBattlesAim.Body + SlapBattlesAim.Forward * 2f,
                    rig.Creator.ActorNumber);
}

[hamburburmod(                "Bee Storm",          "Send a synchronized bee after every other player", ButtonType.Fixed,
        AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBeeStorm : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        Vector3 origin = SlapBattlesAim.Body + Vector3.up;
        foreach (VRRig rig in NetworkSystem.Instance.Rigs())
            if (rig?.Creator != null)
                SlapBattlesNetwork.Bee(origin, rig.Creator.ActorNumber);
    }
}

[hamburburmod(            "Bus Burst",          "Send eight synchronized buses out from a circle around you",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesBusBurst : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.IsInMap) return;
        Vector3 center = SlapBattlesAim.Body - Vector3.up;
        for (int i = 0; i < 8; i++)
        {
            float   angle     = i * Mathf.PI * 0.25f;
            Vector3 direction = new(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            SlapBattlesNetwork.Bus(center + direction * 2f, direction, 1);
        }
    }
}

[hamburburmod("Nuke Gun", "Drop a networked nuke at the pointed spot",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesNukeGun : SlapBattlesGun
{
    protected override float RepeatDelay => 1f;

    protected override void Shoot()
    {
        if (Gun.Hit.collider != null) SlapBattlesNetwork.Nuke(Gun.Hit.point);
    }
}

[hamburburmod("Moai Gun", "Place a networked Moai that repeatedly pushes nearby players",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesMoaiGun : SlapBattlesGun
{
    protected override float RepeatDelay => 0.8f;

    protected override void Shoot()
    {
        if (Gun.Hit.collider != null)
            SlapBattlesNetwork.Moai(Gun.Hit.point, SlapBattlesAim.Forward);
    }
}

[hamburburmod("Nuke Crossfire", "Drop four networked nukes in a ring around you",
        ButtonType.Fixed, AccessSetting.Public, EnabledType.Disabled, 0)]
public class SlapBattlesNukeCrossfire : hamburburmod
{
    protected override void Pressed()
    {
        if (!SlapBattlesNetwork.SupportsNewSlaps) return;
        Vector3 center = SlapBattlesAim.Body - Vector3.up;
        for (int i = 0; i < 4; i++)
        {
            float angle = i * Mathf.PI * 0.5f;
            Vector3 offset = new(Mathf.Cos(angle) * 6f, 0f, Mathf.Sin(angle) * 6f);
            SlapBattlesNetwork.Nuke(center + offset);
        }
    }
}
