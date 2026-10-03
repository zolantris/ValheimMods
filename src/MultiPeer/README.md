# Valheim MultiPeer

BepInEx client/server mod for local Valheim mod testing with multiple Valheim processes.

## What it does

- Uses Valheim's existing `OnlineBackendType.CustomSocket` / `ZSocket2` TCP transport.
- Lets a normal game-hosted server accept direct LAN TCP connections.
- Gives each Valheim process a distinct effective Valheim session UID, avoiding Valheim's `Already connected to peer with UID` collision when multiple local clients originate from the same Steam profile.
- Provides an F8 hotkey to connect to the configured LAN host/port.

## Important limitation

This does **not** spoof or bypass Steam authentication/licensing. Steam is still initialized normally. The mod moves the actual game connection onto Valheim's built-in TCP `CustomSocket` backend and changes Valheim's network session UID only.

For testing, each process should still have whatever Steam/account/licensing state Valheim itself requires.

## Configuration

After first launch:

`BepInEx/config/com.zolantris.valheim.multiinstancelan.cfg`

Default:

- Host mode: `ForceCustomSocketHost = true`
- Client host: `127.0.0.1`
- Client port: `2456`
- Connect hotkey: `F8`

For a LAN host at `192.168.1.50`, set `ConnectHost = 192.168.1.50` and press F8 on each client.

## Build

Reference the current Valheim publicized assembly plus BepInEx/Harmony/Unity assemblies used by the mod build environment. The source intentionally targets the current Valheim API shown by the supplied decompilation:

- `ZNet.SetServerHost(string, int, OnlineBackendType)`
- `ZNet.ClientConnect()`
- `ZNet.m_hostSocket`
- `ZSocket2.BindSocket(...)`
- `ZDOMan.GetSessionID()`
