using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;

namespace QuickConnect;

internal class Servers
{
	public class Entry
	{
		public string name;

		public string ip;

		public int port;

		public string pass;

		public override string ToString()
		{
			return string.Format("Server(name={0},ip={1},port={2}}", name, ip, port);
		}
	}

	public static string ConfigPath = Path.GetDirectoryName(Paths.BepInExConfigPath) + Path.DirectorySeparatorChar + "quick_connect_servers.cfg";

	public static List<Entry> entries = new List<Entry>();

	public static void Init()
	{
		entries.Clear();
		try
		{
			if (!File.Exists(ConfigPath))
			{
				return;
			}
			using StreamReader streamReader = new StreamReader(ConfigPath);
			string text;
			while ((text = streamReader.ReadLine()) != null)
			{
				text = text.Trim();
				if (text.Length == 0 || text.StartsWith("#"))
				{
					continue;
				}
				string[] array = text.Split(':');
				if (array.Length >= 3)
				{
					string name = array[0];
					string ip = array[1];
					int port = int.Parse(array[2]);
					string pass = null;
					if (array.Length >= 4)
					{
						pass = array[3];
					}
					entries.Add(new Entry
					{
						name = name,
						ip = ip,
						port = port,
						pass = pass
					});
				}
				else
				{
					Mod.Log.LogWarning("Invalid config line: " + text);
				}
			}
			Mod.Log.LogInfo($"Loaded {entries.Count} server entries");
		}
		catch (Exception arg)
		{
			Mod.Log.LogError($"Error loading config {arg}");
		}
	}
}
