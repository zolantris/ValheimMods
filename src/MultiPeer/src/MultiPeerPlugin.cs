using System;
using System.Diagnostics;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace ValheimMultiPeer;

[BepInPlugin(Guid, Name, Version)]
public sealed class MultiPeerPlugin : BaseUnityPlugin
{
  public const string Guid = "com.zolantris.valheim.multipeer";
  public const string Name = "Valheim MultiPeer";
  public const string Version = "1.0.0";

  internal static MultiPeerPlugin Instance { get; private set; } = null!;

  internal ConfigEntry<bool> Enabled { get; private set; } = null!;
  internal ConfigEntry<bool> ForceCustomSocketHost { get; private set; } = null!;
  internal ConfigEntry<string> ConnectHost { get; private set; } = null!;
  internal ConfigEntry<int> ConnectPort { get; private set; } = null!;
  internal ConfigEntry<KeyboardShortcut> ConnectHotkey { get; private set; } = null!;
  internal ConfigEntry<bool> LogSessionId { get; private set; } = null!;

  private Harmony _harmony = null!;

  private void Awake()
  {
    Instance = this;

    Enabled = Config.Bind(
      "General",
      "Enabled",
      true,
      "Enable multi-instance LAN networking patches.");

    ForceCustomSocketHost = Config.Bind(
      "Host",
      "ForceCustomSocketHost",
      true,
      "When hosting an open server, use Valheim's built-in ZSocket2 TCP listener instead of Steam/PlayFab hosting.");

    ConnectHost = Config.Bind(
      "Client",
      "ConnectHost",
      "127.0.0.1",
      "LAN hostname or IPv4 address to connect to.");

    ConnectPort = Config.Bind(
      "Client",
      "ConnectPort",
      2456,
      "LAN server port. ZSocket2 may bind this port through port+10 if the configured port is unavailable.");

    ConnectHotkey = Config.Bind(
      "Client",
      "ConnectHotkey",
      new KeyboardShortcut(KeyCode.F8),
      "Connect to ConnectHost:ConnectPort using Valheim's CustomSocket backend.");

    LogSessionId = Config.Bind(
      "Debug",
      "LogSessionId",
      true,
      "Log the original and per-process Valheim session IDs.");

    _harmony = new Harmony(Guid);
    _harmony.PatchAll(typeof(MultiPeerPlugin).Assembly);

    Logger.LogInfo($"{Name} {Version} loaded.");
    Logger.LogInfo($"Process ID: {Process.GetCurrentProcess().Id}");
  }

  private void Update()
  {
    if (!Enabled.Value || !ConnectHotkey.Value.IsDown())
      return;

    TryConnect();
  }

  internal void TryConnect()
  {
    if (ZNet.instance == null)
    {
      Logger.LogWarning("Cannot connect yet: ZNet.instance is null.");
      return;
    }

    if (ZNet.instance.IsServer())
    {
      Logger.LogWarning("Cannot initiate a client connection while this instance is hosting.");
      return;
    }

    var host = ConnectHost.Value.Trim();
    var port = ConnectPort.Value;

    if (string.IsNullOrWhiteSpace(host))
    {
      Logger.LogError("ConnectHost is empty.");
      return;
    }

    if (port is < 1 or > 65535)
    {
      Logger.LogError($"ConnectPort is invalid: {port}");
      return;
    }

    try
    {
      ZNet.SetServerHost(host, port, OnlineBackendType.CustomSocket);
      ZNet.instance.ClientConnect();
      Logger.LogInfo($"Connecting to LAN server {host}:{port} using CustomSocket.");
    }
    catch (Exception ex)
    {
      Logger.LogError($"Failed to start LAN connection: {ex}");
    }
  }

  internal static long GetProcessSessionId(long original)
  {
    unchecked
    {
      // Keep the game's session identity as the base, but fold the process ID
      // into it so two Valheim processes cannot present the same peer UID.
      var x = (ulong)original;
      x ^= (ulong)(uint)Process.GetCurrentProcess().Id * 0x9E3779B97F4A7C15UL;
      x ^= x >> 30;
      x *= 0xBF58476D1CE4E5B9UL;
      x ^= x >> 27;
      x *= 0x94D049BB133111EBUL;
      x ^= x >> 31;

      var result = (long)x;
      return result == 0 ? 1 : result;
    }
  }

  internal void OpenCustomSocketHost(ZNet net)
  {
    if (!Enabled.Value || !ForceCustomSocketHost.Value || !ZNet.m_isServer)
      return;

    // Only replace a host socket that Valheim has not already opened.
    // This is intentionally a postfix because OpenServer performs the normal
    // world/server setup first.
    if (net.m_hostSocket != null)
      return;

    var port = ConnectPort.Value;
    if (port is < 1 or > 65535)
    {
      Logger.LogError($"Cannot start CustomSocket host: invalid ConnectPort {port}.");
      return;
    }

    var socket = new ZSocket2();
    if (!socket.BindSocket(port, Math.Min(65535, port + 10)))
    {
      socket.Dispose();
      Logger.LogError($"Could not bind Valheim CustomSocket host starting at port {port}.");
      return;
    }

    net.m_hostSocket = socket;
    ZNet.m_onlineBackend = OnlineBackendType.CustomSocket;

    Logger.LogInfo($"CustomSocket LAN host listening on {socket.GetHostPort()}.");
  }

  private void OnDestroy()
  {
    _harmony?.UnpatchSelf();
    if (ReferenceEquals(Instance, this))
      Instance = null!;
  }
}

[HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.GetSessionID))]
internal static class ZDOMan_GetSessionID_Patch
{
  private static void Postfix(ref long __result)
  {
    var plugin = MultiPeerPlugin.Instance;
    if (plugin == null || !plugin.Enabled.Value)
      return;

    var original = __result;
    __result = MultiPeerPlugin.GetProcessSessionId(original);

    if (plugin.LogSessionId.Value)
      UnityEngine.Debug.Log($"Valheim session UID: original={original}, process={Process.GetCurrentProcess().Id}, effective={__result}");
  }
}

[HarmonyPatch(typeof(ZNet), nameof(ZNet.OpenServer))]
internal static class ZNet_OpenServer_Patch
{
  private static void Postfix(ZNet __instance)
  {
    MultiPeerPlugin.Instance?.OpenCustomSocketHost(__instance);
  }
}