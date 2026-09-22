using System.Collections.Generic;
using System.Linq;
using hamburbur.Managers;
using hamburbur.Mod_Backend;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace hamburbur.Mods.OP;

[hamburburmod("Force Add Friend", "Tries to forcefully add people with rare cosmetics when in range", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class ForceFriend : hamburburmod
{
    private const float SendDelay = 0.2f;

    private static ForceFriend activeInstance;

    private readonly Dictionary<FriendingStation, FriendingManager.FriendStationData> stationData = new();
    private          bool                                                             friendshipResolved;
    private          float                                                            nextSendTime;

    private FriendingStation pendingStation;
    private int              pendingTargetActor = -1;
    private bool             sentFriendRequest;

    protected override void OnEnable()
    {
        activeInstance = this;
        ResetPendingSend();
    }

    protected override void OnDisable()
    {
        if (activeInstance == this)
            activeInstance = null;

        stationData.Clear();
        ResetPendingSend();
    }

    protected override void Update()
    {
        if (!TryGetMatchingStation(out FriendingStation station, out NetPlayer targetPlayer,
                    out FriendingManager.FriendStationData data))
        {
            ResetPendingSend();

            return;
        }

        if (pendingStation != station || pendingTargetActor != targetPlayer.ActorNumber)
        {
            pendingStation     = station;
            pendingTargetActor = targetPlayer.ActorNumber;
            nextSendTime       = Time.time + SendDelay;
            sentFriendRequest  = false;
            friendshipResolved = false;

            return;
        }

        if (friendshipResolved)
            return;

        if (IsFriend(data, targetPlayer.ActorNumber))
        {
            if (sentFriendRequest)
            {
                NotificationManager.SendNotification(
                        "<color=green>Force Friend</color>",
                        $"Successfully added {targetPlayer.SanitizedNickName} as a friend",
                        3f,
                        false,
                        false
                );
            }

            friendshipResolved = true;

            return;
        }

        if (!CanSendRequest(data.state))
            return;

        if (Time.time < nextSendTime)
            return;

        FriendingManager manager = FriendingManager.Instance;

        if (manager == null)
            return;

        manager.photonView.RPC(
                "NotifyClientsFriendRequestReadyRPC",
                RpcTarget.All,
                station.Zone
        );

        NotificationManager.SendNotification(
                "<color=green>Force Friend</color>",
                $"Tried to friend {targetPlayer.SanitizedNickName}",
                3f,
                true,
                false
        );

        sentFriendRequest = true;
        nextSendTime      = Time.time + SendDelay;
    }

    private bool TryGetMatchingStation(out FriendingStation                   station, out NetPlayer targetPlayer,
                                       out FriendingManager.FriendStationData matchingData)
    {
        station      = null;
        targetPlayer = null;
        matchingData = default(FriendingManager.FriendStationData);

        if (!PhotonNetwork.InRoom || NetworkSystem.Instance?.LocalPlayer == null || VRRigCache.Instance == null)
            return false;

        int localActor = NetworkSystem.Instance.LocalPlayer.ActorNumber;

        foreach (KeyValuePair<FriendingStation, FriendingManager.FriendStationData> entry in stationData)
        {
            if (entry.Key == null)
                continue;

            FriendingManager.FriendStationData data = entry.Value;

            if (data.actorNumberA <= 0 || data.actorNumberB <= 0 || data.actorNumberA == data.actorNumberB)
                continue;

            int targetActor;

            if (data.actorNumberA == localActor && data.actorNumberB != localActor)
                targetActor = data.actorNumberB;
            else if (data.actorNumberB == localActor && data.actorNumberA != localActor)
                targetActor = data.actorNumberA;
            else
                continue;

            NetPlayer candidate = NetworkSystem.Instance.GetNetPlayerByID(targetActor);

            if (candidate == null                                                                         ||
                !VRRigCache.Instance.TryGetVrrig(candidate.GetPlayerRef(), out RigContainer rigContainer) ||
                !IsTargetType(rigContainer.Rig))
                continue;

            station      = entry.Key;
            targetPlayer = candidate;
            matchingData = data;

            return true;
        }

        return false;
    }

    private static bool IsFriend(FriendingManager.FriendStationData data, int targetActor)
    {
        if (data.state is FriendingManager.FriendStationState.Friends or
                          FriendingManager.FriendStationState.AlreadyFriends)
            return true;

        return FriendSystem.Instance                         != null &&
               FriendBackendController.Instance?.FriendsList != null &&
               FriendSystem.Instance.CheckFriendshipWithPlayer(targetActor);
    }

    private static bool CanSendRequest(FriendingManager.FriendStationState state) =>
            state >= FriendingManager.FriendStationState.WaitingOnButtonBoth &&
            state <= FriendingManager.FriendStationState.RequestFailed;

    private static bool IsTargetType(VRRig rig) =>
            rig                       != null &&
            rig._playerOwnedCosmetics != null &&
            rig._playerOwnedCosmetics.Any(cosmetic =>
                                                  Plugin.Instance.SpecialCosmetics.Keys.Contains(cosmetic));

    private void ResetPendingSend()
    {
        pendingStation     = null;
        pendingTargetActor = -1;
        nextSendTime       = 0f;
        sentFriendRequest  = false;
        friendshipResolved = false;
    }

    private void StoreStationData(FriendingStation station, FriendingManager.FriendStationData data)
    {
        if (station != null)
            stationData[station] = data;
    }

    private void RemoveStationData(FriendingStation station)
    {
        if (station != null)
            stationData.Remove(station);

        if (pendingStation == station)
            ResetPendingSend();
    }

    [HarmonyPatch(typeof(FriendingStation), nameof(FriendingStation.UpdateState))]
    private static class FriendingStationUpdateStatePatch
    {
        private static void Postfix(FriendingStation __instance, FriendingManager.FriendStationData data) =>
                activeInstance?.StoreStationData(__instance, data);
    }

    [HarmonyPatch(typeof(FriendingStation), nameof(FriendingStation.OnDisable))]
    private static class FriendingStationDisablePatch
    {
        private static void Postfix(FriendingStation __instance) =>
                activeInstance?.RemoveStationData(__instance);
    }
}