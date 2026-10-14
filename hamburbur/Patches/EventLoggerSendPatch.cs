using System;
using hamburbur.Mods.Misc;
using HarmonyLib;
using Photon.Realtime;
using UnityEngine;

namespace hamburbur.Patches;

[HarmonyPatch(typeof(LoadBalancingClient), nameof(LoadBalancingClient.OpRaiseEvent))]
public static class EventLoggerSendPatch
{
    private static void Prefix(ref (DateTimeOffset time, int frame, float frameDeltaMs) __state) => __state = (DateTimeOffset.UtcNow, Time.frameCount, Time.unscaledDeltaTime * 1000f);

    private static void Postfix(bool                                                 __result, byte eventCode, object customEventContent,
                                (DateTimeOffset time, int frame, float frameDeltaMs) __state)
    {
        if (!__result)
            return;

        try
        {
            EventLogger.Instance?.OnLocalEventSent(eventCode,
                    customEventContent, __state.time, __state.frame, __state.frameDeltaMs);
        }
        catch
        {
            // ignored
        }
    }
}