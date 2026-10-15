using System;
using Photon.Realtime;
using Unity.Collections;
using UnityEngine;

namespace hamburbur.Mods.CustomMaps.SlapBattles;

// New update to slap battles make it reject events from yourself, so to see the modded objects you have to manually call the spawn methods from the lua vm
internal static class SlapBattlesLocalVisual
{
    private static float nextWarning;

    internal static void Spawn(string tag, object[] data)
    {
        if (!SlapBattlesNetwork.IsInMap || CustomGameMode.gameScriptRunner?.ShouldTick != true)
            return;

        switch (tag)
        {
            case "wallRaised":
                Invoke("spawnWall", data[0], data[1], data.Length > 3 && data[3] is double wallKind && wallKind == 1d);

                break;

            case "busSpawned":
                string kind = data[2] is double busKind ? busKind switch
                                                          {
                                                                  2d => "silverwall", 3d => "goldwall", 4d => "boosterwall", var _ => "bus",
                                                          } : "bus";

                Invoke("spawnBus", data[0], data[1], false, kind);

                break;

            case "beesSpawned" when data[1] is Player beeTarget:
                Invoke("spawnBees", data[0], (double)beeTarget.ActorNumber, false);

                break;

            case "blackholeSpawned":
                Invoke("spawnBh", data[0], false);

                break;

            case "empShock":
                Invoke("spawnEmpShock", data[0]);

                break;

            case "pumpkinThrown":
                Invoke("spawnPumpkin", data[0], data[1], false);

                break;

            case "sludgeThrown":
                Invoke("spawnSludge", data[0], data[1], false);

                break;

            case "moaiSpawned":
                Invoke("spawnMoai", data[0], data[1], false);

                break;

            case "nukeDropped":
                Invoke("spawnNuke", data[0], false);

                break;

            case "orbThrown":
                Invoke("spawnOrb", data[0], data[1], data[2] is double orbKind && orbKind == 1d, false);

                break;

            case "planetSpawned":
                Invoke("spawnPlanet", data[0], false);

                break;

            case "lucySpawned" when data[1] is Player lucyTarget && data[2] is Player lucyCaster:
                Invoke("spawnLucy", data[0], (double)lucyTarget.ActorNumber, (double)lucyCaster.ActorNumber, data[3]);

                break;
        }
    }

    private static unsafe bool Invoke(string function, params object[] args)
    {
        LuauScriptRunner runner = CustomGameMode.gameScriptRunner;

        if (runner?.ShouldTick != true || runner.L == null) return false;

        lua_State* state = runner.L;
        int        top   = Luau.lua_gettop(state);
        try
        {
            Luau.lua_getglobal(state, function);

            if (Luau.lua_type(state, -1) != 7) return false;

            foreach (object arg in args)
            {
                switch (arg)
                {
                    case Vector3 vector:
                        *Luau.lua_class_push<Vector3>(state, new FixedString32Bytes("Vec3")) = vector;

                        break;

                    case Quaternion rotation:
                        *Luau.lua_class_push<Quaternion>(state, new FixedString32Bytes("Quat")) = rotation;

                        break;

                    case double number:
                        Luau.lua_pushnumber(state, number);

                        break;

                    case bool boolean:
                        Luau.lua_pushboolean(state, boolean ? 1 : 0);

                        break;

                    case string text:
                        Luau.lua_pushstring(state, text);

                        break;

                    case null:
                        Luau.lua_pushnil(state);

                        break;

                    default:
                        return false;
                }
            }

            if (Luau.lua_pcall(state, args.Length, 1, 0) == 0)
            {
                if (Luau.lua_type(state, -1) != 1 || Luau.lua_toboolean(state, -1) != 0)
                    return true;

                Warn($"{function} could not find or clone its map object.");

                return false;

            }
        }
        catch (Exception error)
        {
            Warn($"{function}: {error.Message}");
        }
        finally
        {
            Luau.lua_settop(state, top);
        }

        Warn($"{function} failed in the current map script.");

        return false;
    }

    private static void Warn(string message)
    {
        if (Time.realtimeSinceStartup < nextWarning) return;
        nextWarning = Time.realtimeSinceStartup + 5f;
        Debug.LogWarning($"Slap Battles local spawn: {message}");
    }
}