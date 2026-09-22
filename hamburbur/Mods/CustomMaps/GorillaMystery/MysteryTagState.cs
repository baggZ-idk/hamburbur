using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;

namespace hamburbur.Mods.CustomMaps.GorillaMystery;

public enum MysteryTagRole
{
    Innocent,
    Sheriff,
    Murderer,
}

public static class MysteryTagState
{
    private static readonly HashSet<int> DeadPlayers = [];

    private static bool   initialized;
    private static Photon.Realtime.Room currentRoom;
    private static float  nextStatusRequestTime;
    private static bool   wasInRoom;

    public static int? MurdererActorNumber { get; private set; }
    public static int? SheriffActorNumber  { get; private set; }
    public static bool GameActive          { get; private set; }
    public static bool VotingActive        { get; private set; }

    public static void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized                                  =  true;
        PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
        PrepareForCurrentRoom();
    }

    public static MysteryTagRole GetRole(int actorNumber)
    {
        PrepareForCurrentRoom();

        if (MurdererActorNumber == actorNumber)
            return MysteryTagRole.Murderer;

        return SheriffActorNumber == actorNumber ? MysteryTagRole.Sheriff : MysteryTagRole.Innocent;
    }

    public static bool IsAlive(int actorNumber)
    {
        PrepareForCurrentRoom();

        return !DeadPlayers.Contains(actorNumber);
    }

    public static void PollGameStatus()
    {
        EnsureInitialized();
        PrepareForCurrentRoom();

        if (!PhotonNetwork.InRoom || PhotonNetwork.LocalPlayer == null)
        {
            wasInRoom = false;
            return;
        }

        if (!wasInRoom)
        {
            wasInRoom = true;
            ResetRoundState();
            VotingActive = false;
            nextStatusRequestTime = 0f;
        }

        if (UnityEngine.Time.time < nextStatusRequestTime)
            return;

        nextStatusRequestTime = UnityEngine.Time.time + 2f;
        MysteryTagNetwork.Send(
                MysteryTagEvents.WhatGameStatus,
                (double)MysteryTagNetwork.LocalPlayerId);
    }

    private static void PrepareForCurrentRoom()
    {
        Photon.Realtime.Room room = PhotonNetwork.InRoom ? PhotonNetwork.CurrentRoom : null;

        if (ReferenceEquals(room, currentRoom))
            return;

        currentRoom = room;
        nextStatusRequestTime = 0f;
        wasInRoom = PhotonNetwork.InRoom;
        ResetRoundState();
        VotingActive = false;
    }

    private static void OnEventReceived(EventData eventData)
    {
        if (eventData.Code != CustomMapUtils.EventCode || eventData.CustomData is not object[] content ||
            content.Length == 0                        || content[0] is not string eventName)
            return;

        PrepareForCurrentRoom();

        switch (eventName)
        {
            case MysteryTagEvents.StartGame when IsMasterSender(eventData.Sender):
                ResetRoundState();
                GameActive   = true;
                VotingActive = false;

                break;

            case MysteryTagEvents.EndGame when IsMasterSender(eventData.Sender):
                ResetRoundState();
                VotingActive = false;

                break;

            case MysteryTagEvents.ChooseMurder when IsMasterSender(eventData.Sender) &&
                                                    TryGetInt(content, 1, out int murderer):
                MurdererActorNumber = murderer;
                DeadPlayers.Remove(murderer);

                break;

            case MysteryTagEvents.ChooseSheriff when TryGetInt(content, 1, out int sheriff) &&
                                                     IsAuthorizedSheriffChoice(eventData.Sender, sheriff):
                SheriffActorNumber = sheriff;
                DeadPlayers.Remove(sheriff);

                break;

            case MysteryTagEvents.ChangeWeaponState when TryGetInt(content, 1, out int weaponType):
                InferRoleFromWeaponEvent(eventData.Sender, weaponType);

                break;

            case MysteryTagEvents.PlayerDeath when TryGetInt(content, 1, out int knifeVictim):
            case MysteryTagEvents.SheriffHit when TryGetInt(content,  1, out knifeVictim):
                DeadPlayers.Add(knifeVictim);
                if (SheriffActorNumber == knifeVictim)
                    SheriffActorNumber = null;

                break;

            case MysteryTagEvents.DropGun:
                SheriffActorNumber = null;

                break;

            case MysteryTagEvents.SheriffShot:
                SheriffActorNumber ??= eventData.Sender;

                break;

            case MysteryTagEvents.PlayerVote:
            case MysteryTagEvents.StartVoting:
                if (!GameActive)
                    VotingActive = true;

                break;

            case MysteryTagEvents.GameStatus when IsMasterSender(eventData.Sender):
                ApplyGameStatus(content);

                break;
        }
    }

    private static void ApplyGameStatus(object[] content)
    {
        if (!TryGetInt(content, 1, out int gameStatus) || !TryGetInt(content, 2, out int votingStatus))
            return;

        if (content.Length            > 4     && TryGetInt(content, 4, out int target) &&
            PhotonNetwork.LocalPlayer != null && target != PhotonNetwork.LocalPlayer.ActorNumber)
            return;

        GameActive   = gameStatus == 1;
        VotingActive = !GameActive && votingStatus == 1;
    }

    private static bool IsMasterSender(int sender) =>
            PhotonNetwork.MasterClient == null || PhotonNetwork.MasterClient.ActorNumber == sender;

    private static bool IsAuthorizedSheriffChoice(int sender, int sheriff) =>
            IsMasterSender(sender) || SheriffActorNumber == null && sender == sheriff;

    private static void InferRoleFromWeaponEvent(int sender, int weaponType)
    {
        if (!GameActive)
            return;

        switch (weaponType)
        {
            case 1 when MurdererActorNumber == null:
                MurdererActorNumber = sender;

                break;

            case 2 when SheriffActorNumber == null:
                SheriffActorNumber = sender;

                break;
        }
    }

    private static bool TryGetInt(object[] content, int index, out int value)
    {
        value = 0;

        if (index < 0 || index >= content.Length || content[index] == null)
            return false;

        try
        {
            value = Convert.ToInt32(content[index]);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static void ResetRoundState()
    {
        MurdererActorNumber = null;
        SheriffActorNumber  = null;
        GameActive          = false;
        DeadPlayers.Clear();
    }
}
