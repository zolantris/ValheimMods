using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace QuickConnect;

internal class QuickConnectUI : MonoBehaviour
{
	public static QuickConnectUI instance;

	private Rect windowRect;

	private Rect lastRect;

	private Rect errorWinRect;

	private Rect dragRect = new Rect(0f, 0f, 10000f, 20f);

	private int buttonFontSize;

	private int labelFontSize;

	private GUIStyle buttonStyle;

	private GUIStyle labelStyle;

	private Task<IPHostEntry> resolveTask;

	private Servers.Entry connecting;

	private string errorMsg;

	private void Update()
	{
		if (resolveTask == null)
		{
			return;
		}
		if (resolveTask.IsFaulted)
		{
			Mod.Log.LogError($"Error resolving IP: {resolveTask.Exception}");
			if (resolveTask.Exception != null)
			{
				ShowError(resolveTask.Exception.InnerException.Message);
			}
			else
			{
				ShowError(resolveTask.Exception.Message);
			}
			resolveTask = null;
			connecting = null;
		}
		else if (resolveTask.IsCanceled)
		{
			resolveTask = null;
			connecting = null;
		}
		else
		{
			if (!resolveTask.IsCompleted)
			{
				return;
			}
			IPAddress[] addressList = resolveTask.Result.AddressList;
			foreach (IPAddress iPAddress in addressList)
			{
				if (iPAddress.AddressFamily == AddressFamily.InterNetwork)
				{
					Mod.Log.LogInfo($"Resolved: {iPAddress}");
					resolveTask = null;
					ZSteamMatchmaking.instance.QueueServerJoin($"{iPAddress}:{connecting.port}");
					return;
				}
			}
			resolveTask = null;
			connecting = null;
			ShowError("Server DNS resolved to no valid addresses");
		}
	}

	private void Awake()
	{
		windowRect.x = Mod.windowPosX.Value;
		windowRect.y = Mod.windowPosY.Value;
		buttonFontSize = Mod.buttonFontSize.Value;
		labelFontSize = Mod.labelFontSize.Value;
		windowRect.width = Mod.windowWidth.Value;
		windowRect.height = Mod.windowHeight.Value;
		lastRect = windowRect;
	}

	private void OnGUI()
	{
		if (buttonStyle == null)
		{
			buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontSize = buttonFontSize;
			labelStyle = new GUIStyle(GUI.skin.label);
			labelStyle.fontSize = labelFontSize;
		}
		if (errorMsg != null)
		{
			errorWinRect = GUILayout.Window(1586464, errorWinRect, DrawErrorWindow, "Error");
			return;
		}
		windowRect = GUILayout.Window(1586463, windowRect, DrawConnectWindow, "Quick Connect");
		if (!lastRect.Equals(windowRect))
		{
			Mod.windowPosX.Value = windowRect.x;
			Mod.windowPosY.Value = windowRect.y;
			lastRect = windowRect;
		}
	}

	private void DrawConnectWindow(int windowID)
	{
		GUI.DragWindow(dragRect);
		if (connecting != null)
		{
			GUILayout.Label("Connecting to " + connecting.name, labelStyle);
			if (GUILayout.Button("Cancel", buttonStyle))
			{
				AbortConnect();
			}
			return;
		}
		if (Servers.entries.Count > 0)
		{
			GUILayout.Label("Choose A Server:", labelStyle);
			{
				foreach (Servers.Entry entry in Servers.entries)
				{
					if (GUILayout.Button(entry.name, buttonStyle))
					{
						DoConnect(entry);
					}
				}
				return;
			}
		}
		GUILayout.Label("No servers defined", labelStyle);
		GUILayout.Label("Add quick_connect_servers.cfg", labelStyle);
	}

	private void DrawErrorWindow(int windowID)
	{
		GUILayout.Label(errorMsg, labelStyle);
		if (GUILayout.Button("Close", buttonStyle))
		{
			errorMsg = null;
		}
	}

	private void DoConnect(Servers.Entry server)
	{
		connecting = server;
		try
		{
			IPAddress.Parse(server.ip);
			ZSteamMatchmaking.instance.QueueServerJoin($"{server.ip}:{server.port}");
		}
		catch (FormatException)
		{
			Mod.Log.LogInfo("Resolving: " + server.ip);
			resolveTask = Dns.GetHostEntryAsync(server.ip);
		}
	}

	public string CurrentPass()
	{
		if (connecting != null)
		{
			return connecting.pass;
		}
		return null;
	}

	public void JoinServerFailed()
	{
		ShowError("Server connection failed");
		connecting = null;
	}

	public void ShowError(string msg)
	{
		errorMsg = msg;
		errorWinRect = new Rect(Screen.width / 2 - 125, Screen.height / 2, 250f, 30f);
	}

	public void AbortConnect()
	{
		connecting = null;
		resolveTask = null;
	}
}
