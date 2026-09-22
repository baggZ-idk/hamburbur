using System;
using ExitGames.Client.Photon;
using hamburbur.Managers;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.GorillaMystery;

public static class MysteryTagNetwork
{
    private const float KillVelocity = 50f;

    public static int LocalPlayerId => PhotonNetwork.LocalPlayer?.ActorNumber ?? 0;

    public static void Send(string eventName, params object[] data)
    {
        if (!PhotonNetwork.InRoom)
            return;

        object[] content = new object[data.Length + 1];
        content[0] = eventName;
        Array.Copy(data, 0, content, 1, data.Length);

        PhotonNetwork.RaiseEvent(
                CustomMapUtils.EventCode,
                content,
                new RaiseEventOptions { Receivers = ReceiverGroup.All, },
                SendOptions.SendReliable);
    }

    public static void Kill(VRRig rig, bool includeAlreadyDead = false)
    {
        if (rig?.Creator == null || !includeAlreadyDead && !MysteryTagState.IsAlive(rig.Creator.ActorNumber))
            return;

        Vector3 position = rig.transform.position;
        Vector3 velocity = GetKillVelocity(position);
        double  skeleton = SpawnKillSkeletons.IsEnabled ? 0d : 1d;

        Send(
                MysteryTagEvents.PlayerDeath,
                (double)rig.Creator.ActorNumber,
                (double)position.x,
                (double)position.y,
                (double)position.z,
                skeleton,
                (double)velocity.x,
                (double)velocity.y,
                (double)velocity.z);
    }

    public static void KillSelf()
    {
        if (LocalPlayerId == 0 || GorillaTagger.Instance?.bodyCollider == null)
            return;

        Vector3 position = GorillaTagger.Instance.bodyCollider.transform.position;
        Vector3 velocity = GetKillVelocity(position);
        double skeleton = SpawnKillSkeletons.IsEnabled ? 0d : 1d;

        Send(MysteryTagEvents.PlayerDeath,
                (double)LocalPlayerId,
                (double)position.x, (double)position.y, (double)position.z,
                skeleton,
                (double)velocity.x, (double)velocity.y, (double)velocity.z);
    }

    public static void SheriffKill(VRRig rig, bool includeAlreadyDead = false)
    {
        if (rig?.Creator == null || !includeAlreadyDead && !MysteryTagState.IsAlive(rig.Creator.ActorNumber))
            return;

        Vector3 position = rig.transform.position;
        Vector3 velocity = GetKillVelocity(position);
        double  skeleton = SpawnKillSkeletons.IsEnabled ? 0d : 1d;

        Send(
                MysteryTagEvents.SheriffHit,
                (double)rig.Creator.ActorNumber,
                position,
                skeleton,
                velocity);
    }

    public static void DropGun(Vector3 position) =>
            Send(MysteryTagEvents.DropGun, (double)position.x, (double)position.y, (double)position.z);

    public static void PlaySheriffShot(Vector3 position) => Send(MysteryTagEvents.SheriffShot, position);

    public static void SetHand(int playerId, bool leftHand) =>
            Send(MysteryTagEvents.HandChanged, (double)playerId, leftHand ? 1d : 0d);

    private static Vector3 GetKillVelocity(Vector3 position)
    {
        Vector3 origin = GorillaTagger.Instance?.bodyCollider != null
                                 ? GorillaTagger.Instance.bodyCollider.transform.position
                                 : position - Vector3.up;

        Vector3 direction = position - origin;
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector3.up;

        return direction.normalized * KillVelocity + Vector3.up * (KillVelocity * 0.4f);
    }

    public static bool RequireMasterClient()
    {
        if (Tools.Utils.IsMasterClient)
            return true;

        NotificationManager.SendNotification(
                "<color=red>Error</color>",
                "You must be master client to use this Gorilla Mystery mod.",
                5f,
                false,
                false);

        return false;
    }
}
