using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ExitGames.Client.Photon;
using hamburbur.Managers;
using hamburbur.Mod_Backend;
using hamburbur.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace hamburbur.Mods.Misc;

[hamburburmod("Event Logger", "Logs all events", ButtonType.Togglable, AccessSetting.Public, EnabledType.Disabled, 0)]
public class EventLogger : hamburburmod
{
    private const           float                                      Cooldown       = 1.5f;
    private const           int                                        MaxPendingRows = 10000;
    private static readonly byte[]                                     GameEventCodes = [0, 1, 2, 3, 4, 5, 8, 9, 10, 50, 51, 176, 199, 189,];
    private readonly        Queue<string>                              pendingRows    = new();
    private readonly        Dictionary<(int sender, byte code), float> recentEvents   = new();
    private                 string                                     filePath;
    private                 bool                                       hasReportedRecordFailure;
    private                 bool                                       hasReportedWriteFailure;
    private                 bool                                       isEnabled;
    public static           EventLogger                                Instance { get; private set; }

    protected override void Start()
    {
        Instance = this;
        TryCreateFile();
        PhotonNetwork.NetworkingClient.EventReceived += OnEventReceived;
        CoroutineManager.Instance.StartCoroutine(FlushLoop());
    }

    protected override void OnEnable() => isEnabled = true;

    protected override void OnDisable()
    {
        isEnabled = false;
        Flush();
    }

    private IEnumerator FlushLoop()
    {
        WaitForSecondsRealtime delay = new(0.25f);
        while (true)
        {
            yield return delay;
            Flush();
        }
    }

    private void TryCreateFile()
    {
        try
        {
            filePath                = FileManager.Instance.CreateEventLoggerFile();
            hasReportedWriteFailure = false;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            ReportWriteFailure(error);
        }
    }

    private void ReportWriteFailure(Exception error)
    {
        if (hasReportedWriteFailure)
            return;

        hasReportedWriteFailure = true;
        Debug.LogWarning($"[Event Logger] Could not write .pel file: {error.Message}");
    }

    private static bool IsGameEvent(byte code) => code >= 200 || GameEventCodes.Contains(code);

    private void OnEventReceived(EventData eventData)
    {
        try
        {
            RecordReceived(eventData);
        }
        catch (Exception error)
        {
            try
            {
                EnqueueFallback("received", eventData.Code, eventData.Sender, null, null, error);
            }
            catch
            {
                // ignored
            }

            ReportRecordFailure(error);
        }
    }

    private void RecordReceived(EventData eventData)
    {
        DateTimeOffset observed   = DateTimeOffset.UtcNow;
        Player         sender     = PhotonNetwork.CurrentRoom?.GetPlayer(eventData.Sender);
        string         senderName = sender?.NickName ?? eventData.Sender.Rig()?.Creator?.SanitizedNickName ?? "UNKNOWN";
        Enqueue(CreateRow("received", eventData.Code, eventData.CustomData, eventData.Sender,
                sender,               observed,       Time.frameCount,      null, null, null));

        if (!isEnabled || IsGameEvent(eventData.Code))
            return;

        (int, byte) key = (eventData.Sender, eventData.Code);
        float       now = Time.unscaledTime;

        if (recentEvents.TryGetValue(key, out float recent) && now - recent < Cooldown)
            return;

        recentEvents[key] = now;
        NotificationManager.SendNotification("<color=#00ff99>Event Logger</color>",
                $"Received event {eventData.Code} from {senderName}\n{FormatValue(eventData.CustomData)}",
                5f, false, false);
    }

    public void OnLocalEventSent(byte code, object payload, DateTimeOffset sent, int frame, float frameDeltaMs)
    {
        try
        {
            Player local = PhotonNetwork.LocalPlayer;
            Enqueue(CreateRow("sent",      code,            payload, local?.ActorNumber ?? 0, local,
                    DateTimeOffset.UtcNow, Time.frameCount, sent,    frame,                   frameDeltaMs));
        }
        catch (Exception error)
        {
            try
            {
                EnqueueFallback("sent", code, PhotonNetwork.LocalPlayer?.ActorNumber ?? 0, sent, frame, error);
            }
            catch
            {
                // ignored
            }

            ReportRecordFailure(error);
        }
    }

    private void EnqueueFallback(string direction, byte      code, int actor, DateTimeOffset? sent,
                                 int?   sentFrame, Exception error)
    {
        DateTimeOffset observed = DateTimeOffset.UtcNow;
        Enqueue(new JObject
        {
                ["schema"]         = "pel/1",
                ["direction"]      = direction,
                ["eventCode"]      = code,
                ["isGameEvent"]    = IsGameEvent(code),
                ["eventCategory"]  = IsGameEvent(code) ? "game" : "other",
                ["senderActor"]    = actor,
                ["sentUtc"]        = sent?.ToString("O"),
                ["sentUnixMs"]     = sent?.ToUnixTimeMilliseconds(),
                ["sentFrame"]      = sentFrame,
                ["receivedUtc"]    = direction == "received" ? observed.ToString("O") : null,
                ["receivedUnixMs"] = direction == "received" ? observed.ToUnixTimeMilliseconds() : null,
                ["receivedFrame"]  = direction == "received" ? Time.frameCount : null,
                ["observedUtc"]    = observed.ToString("O"),
                ["observedUnixMs"] = observed.ToUnixTimeMilliseconds(),
                ["observedFrame"]  = Time.frameCount,
                ["recordError"]    = error.GetType().Name,
        });
    }

    private void ReportRecordFailure(Exception error)
    {
        if (hasReportedRecordFailure)
            return;

        hasReportedRecordFailure = true;
        Debug.LogWarning($"[Event Logger] Recorded a reduced event row: {error.Message}");
    }

    private static JObject CreateRow(string          direction, byte           code,      object payload, int actor,
                                     Player          sender,    DateTimeOffset observed,  int    observedFrame,
                                     DateTimeOffset? sent,      int?           sentFrame, float? sentFrameDeltaMs)
    {
        VRRig     rig              = actor.Rig();
        Hashtable properties       = sender?.CustomProperties;
        JObject   customProperties = new();
        string    senderVersion    = null;
        string    senderPlatform   = null;
        if (properties != null)
            foreach (DictionaryEntry entry in properties)
            {
                string key   = entry.Key?.ToString() ?? "null";
                string value = FormatValue(entry.Value);
                customProperties[key] = value;
                if (senderVersion == null && key.IndexOf("version", StringComparison.OrdinalIgnoreCase) >= 0)
                    senderVersion = value;

                if (senderPlatform == null && key.IndexOf("platform", StringComparison.OrdinalIgnoreCase) >= 0)
                    senderPlatform = value;
            }

        string          data     = FormatValue(payload);
        DateTimeOffset? received = direction == "received" ? observed : null;

        return new JObject
        {
                ["schema"]                   = "pel/1",
                ["direction"]                = direction,
                ["eventCode"]                = code,
                ["isGameEvent"]              = IsGameEvent(code),
                ["eventCategory"]            = IsGameEvent(code) ? "game" : "other",
                ["senderActor"]              = actor,
                ["senderName"]               = sender?.NickName ?? rig?.Creator?.SanitizedNickName,
                ["senderUserId"]             = sender?.UserId   ?? rig?.Creator?.UserId,
                ["senderIsLocal"]            = sender?.IsLocal,
                ["senderIsMaster"]           = sender?.IsMasterClient,
                ["senderVersion"]            = senderVersion,
                ["senderPlatform"]           = senderPlatform,
                ["senderCustomProperties"]   = customProperties,
                ["senderCosmetics"]          = rig?._playerOwnedCosmetics.Concat(),
                ["senderFps"]                = rig?.fps,
                ["senderPingMs"]             = !rig ? null : rig.Ping(),
                ["sentUtc"]                  = sent?.ToString("O"),
                ["sentUnixMs"]               = sent?.ToUnixTimeMilliseconds(),
                ["sentFrame"]                = sentFrame,
                ["sentFrameDeltaMs"]         = sentFrameDeltaMs,
                ["receivedUtc"]              = received?.ToString("O"),
                ["receivedUnixMs"]           = received?.ToUnixTimeMilliseconds(),
                ["receivedFrame"]            = received == null ? null : observedFrame,
                ["observedUtc"]              = observed.ToString("O"),
                ["observedUnixMs"]           = observed.ToUnixTimeMilliseconds(),
                ["observedFrame"]            = observedFrame,
                ["observedUnityTimeSeconds"] = Time.unscaledTime,
                ["frameDeltaMs"]             = Time.unscaledDeltaTime * 1000f,
                ["photonServerTimestampMs"]  = PhotonNetwork.ServerTimestamp,
                ["room"]                     = PhotonNetwork.CurrentRoom?.Name,
                ["region"]                   = PhotonNetwork.CloudRegion,
                ["playerCount"]              = PhotonNetwork.CurrentRoom?.PlayerCount,
                ["gameMode"]                 = NetworkSystem.Instance?.GameModeString,
                ["pluginVersion"]            = Constants.PluginVersion,
                ["payloadType"]              = payload?.GetType().FullName,
                ["payload"]                  = data,
                ["payloadUtf8Bytes"]         = Encoding.UTF8.GetByteCount(data),
        };
    }

    private void Enqueue(JObject row)
    {
        if (pendingRows.Count >= MaxPendingRows)
            Flush();

        if (pendingRows.Count >= MaxPendingRows)
            return;

        pendingRows.Enqueue(row.ToString(Formatting.None));
    }

    private void Flush()
    {
        if (pendingRows.Count == 0)
            return;

        try
        {
            if (filePath == null)
                TryCreateFile();

            if (filePath == null)
                return;

            using FileStream   stream = new(filePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter writer = new(stream, new UTF8Encoding(false));
            int                count  = pendingRows.Count;
            foreach (string row in pendingRows)
                writer.WriteLine(row);

            writer.Flush();
            for (int i = 0; i < count; i++)
                pendingRows.Dequeue();

            hasReportedWriteFailure = false;
        }
        catch (IOException error)
        {
            ReportWriteFailure(error);
        }
        catch (UnauthorizedAccessException error)
        {
            ReportWriteFailure(error);
        }
    }

    private static string FormatValue(object value, int depth = 0)
    {
        if (value == null)
            return "null";

        if (depth > 6)
            return "...";

        switch (value)
        {
            case string text:
                return text.Length > 16384 ? text[..16384] + "..." : text;

            case IDictionary dictionary:
            {
                List<string> items = [];
                foreach (DictionaryEntry entry in dictionary.Cast<DictionaryEntry>().TakeWhile(entry => items.Count < 128))
                    items.Add($"{FormatValue(entry.Key, depth + 1)}: {FormatValue(entry.Value, depth + 1)}");

                return "{" + string.Join(", ", items) + "}";
            }

            case IEnumerable enumerable:
            {
                List<string> items = [];
                foreach (object item in enumerable)
                {
                    if (items.Count >= 128)
                        break;

                    items.Add(FormatValue(item, depth + 1));
                }

                return "[" + string.Join(", ", items) + "]";
            }

            default:
                try
                {
                    return value.ToString();
                }
                catch
                {
                    return value.GetType().FullName;
                }

        }

    }
}