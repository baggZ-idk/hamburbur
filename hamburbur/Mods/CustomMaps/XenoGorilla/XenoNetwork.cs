using System;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.XenoGorilla;

internal static class XenoNetwork
{
    internal static bool IsInMap => PhotonNetwork.InRoom && CustomGameMode.GameModeInitialized &&
                                    CustomGameMode.LuaScript?.Contains("XENO_GORILLA", StringComparison.OrdinalIgnoreCase) == true;

    internal static int LocalActor => PhotonNetwork.LocalPlayer?.ActorNumber ?? 0;

    internal static bool IsMaster => IsInMap && PhotonNetwork.IsMasterClient;

    internal static Vector3 BodyPosition => GorillaTagger.Instance?.bodyCollider != null
                                                    ? GorillaTagger.Instance.bodyCollider.transform.position
                                                    : Vector3.zero;

    internal static bool Send(string name, params object[] data)
    {
        if (!IsInMap) return false;

        object[] content = new object[data.Length + 1];
        content[0] = name;
        Array.Copy(data, 0, content, 1, data.Length);

        return PhotonNetwork.RaiseEvent(CustomMapUtils.EventCode, content,
                new RaiseEventOptions { Receivers = ReceiverGroup.All, }, SendOptions.SendReliable);
    }

    internal static bool Summon(string ghost, int targetActor) =>
            IsMaster && targetActor > 0 && Send("MC_S" + ghost, (double)targetActor);

    internal static bool Vote(int option, long uniqueId)
    {
        if (option is not (1 or 2) || uniqueId < 1 || !IsInMap) 
            return false;
        
        return Send("UpdateMap", 10d, new Vector3(125f, 125f, 125f), (double)uniqueId, (double)option);
    }
}