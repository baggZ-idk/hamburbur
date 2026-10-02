using System;
using hamburbur.Mod_Backend;
using Photon.Pun;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.XenoGorilla;

public abstract class XenoPollVote : hamburburmod
{
    private const int MaxVotesPerRoom = 150;
    private static string roomName;
    private static int votesSent;
    private static long nextId;
    private float nextVote;

    protected abstract int Option { get; }

    protected override void Update()
    {
        if (!XenoNetwork.IsInMap || Time.time < nextVote) return;
        string currentRoom = PhotonNetwork.CurrentRoom?.Name;
        if (currentRoom != roomName)
        {
            roomName = currentRoom;
            votesSent = 0;
            nextId = 1000000000000L + DateTime.UtcNow.Ticks % 1000000000000L;
        }

        if (votesSent >= MaxVotesPerRoom)
        {
            SetEnabledFromSystem(false);
            return;
        }

        nextVote = Time.time + 0.2f;
        if (XenoNetwork.Vote(Option, ++nextId)) votesSent++;
    }
}

[hamburburmod("Vote Classic Ghosts", "Adds reported unique IDs to the Xeno poll's Classic Ghosts option",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVoteClassicGhosts : XenoPollVote
{
    protected override int Option => 1;
}

[hamburburmod("Vote Undiscovered Ghosts", "Adds reported unique IDs to the Xeno poll's Undiscovered Ghosts option",
        ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class XenoVoteUndiscoveredGhosts : XenoPollVote
{
    protected override int Option => 2;
}
