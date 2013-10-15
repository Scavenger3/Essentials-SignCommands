using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace SignCommands
{
	public class scConfig
	{
		public string DefineSignCommands = "[Sign Command]";
		public string CommandsStartWith = ">";
		public Dictionary<string, int> CooldownGroups = new Dictionary<string, int>();
		public bool ShowDestroyMessage = true;

		public static string SavePath = string.Empty;
		public static string ConfigPath = string.Empty;
		public scConfig Write(string path)
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
			return this;
		}
		public static scConfig Read(string path)
		{
			if (!File.Exists(path))
			{
				WriteExample(path);
			}
			return JsonConvert.DeserializeObject<scConfig>(File.ReadAllText(path));
		}
		public static void WriteExample(string path)
		{
			File.WriteAllText(ConfigPath, @"{
  ""DefineSignCommands"": ""[Sign Command]"",
  ""CommandsStartWith"": "">"",
  ""CooldownGroups"": {
    ""global-time"": 300,
    ""heal"": 60,
    ""message"": 10,
    ""damage"": 10,
    ""boss"": 300,
    ""item"": 300,
    ""buff"": 300,
    ""spawnmob"": 300,
    ""kit"": 300,
    ""command"": 300
  },
  ""ShowDestroyMessage"": true,
}");
		}

		public static void LoadConfig()
		{
			try
			{
				SavePath = Path.Combine(TShock.SavePath, "Essentials");
				ConfigPath = Path.Combine(SavePath, "SignCommandsConfig.json");
				if (!Directory.Exists(SavePath))
				{
					Directory.CreateDirectory(SavePath);
				}
				SignCommands.Config = scConfig.Read(ConfigPath).Write(ConfigPath);
			}
			catch (Exception ex)
			{
				Log.ConsoleError("[Sign Commands] Config Exception. Check logs for more details.");
				Log.Error(ex.ToString());
			}
		}
		public static void ReloadConfig(CommandArgs args)
		{
			try
			{
				if (!Directory.Exists(SavePath))
				{
					Directory.CreateDirectory(SavePath);
				}
				SignCommands.Config = scConfig.Read(ConfigPath).Write(ConfigPath);
				args.Player.SendSuccessMessage("[Sign Commands] Config reloaded successfully.");
			}
			catch (Exception ex)
			{
				args.Player.SendErrorMessage("[Sign Commands] Reload failed. Check logs for more details.");
				Log.Error(string.Concat("[Sign Commands] Config Exception:\n", ex.ToString()));
			}
		}
	}
}
