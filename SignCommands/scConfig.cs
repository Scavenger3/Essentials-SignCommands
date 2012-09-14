using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using TShockAPI;

namespace SignCommands
{
	public class scConfig
	{
		public string DefineSignCommands = "[Sign Command]";
		public string CommandsStartWith = ">";
		public Dictionary<string, int> CooldownGroups = new Dictionary<string, int> {
			{ "global-time", 300 },
			{ "heal", 60 },
			{ "message", 10 },
			{ "damage", 10 },
			{ "boss", 300 },
			{ "item", 300 },
			{ "buff", 300 },
			{ "spawnmob", 300 },
			{ "kit", 300 },
			{ "command", 300}
		};

		public static scConfig Read(string path)
		{
			if (!File.Exists(path))
				return new scConfig();
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				return Read(fs);
			}
		}

		public static scConfig Read(Stream stream)
		{
			using (var sr = new StreamReader(stream))
			{
				var cf = JsonConvert.DeserializeObject<scConfig>(sr.ReadToEnd());
				if (ConfigRead != null)
					ConfigRead(cf);
				return cf;
			}
		}

		public void Write(string path)
		{
			using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
			{
				Write(fs);
			}
		}

		public void Write(Stream stream)
		{
			var str = JsonConvert.SerializeObject(this, Formatting.Indented);
			using (var sw = new StreamWriter(stream))
			{
				sw.Write(str);
			}
		}

		public static Action<scConfig> ConfigRead;

		public static void LoadConfig()
		{
			try
			{
				if (File.Exists(SignCommands.ConfigPath))
				{
					SignCommands.getConfig = scConfig.Read(SignCommands.ConfigPath);
				}
				List<string> groups = new List<string>(SignCommands.getConfig.CooldownGroups.Keys);
				foreach (string g in groups)
				{
					int useless = -1;
					if (int.TryParse(g, out useless))
						SignCommands.getConfig.CooldownGroups.Remove(g);
				}
				SignCommands.getConfig.Write(SignCommands.ConfigPath);
			}
			catch (Exception ex)
			{
				Log.ConsoleError("Config Exception in Sign Commands config file");
				Log.Error(ex.ToString());
			}
		}

		public static void ReloadConfig(CommandArgs args)
		{
			try
			{
				if (File.Exists(SignCommands.ConfigPath))
				{
					SignCommands.getConfig = scConfig.Read(SignCommands.ConfigPath);
				}
				List<string> groups = new List<string>(SignCommands.getConfig.CooldownGroups.Keys);
				foreach (string g in groups)
				{
					int useless = -1;
					if (int.TryParse(g, out useless))
						SignCommands.getConfig.CooldownGroups.Remove(g);
				}
				SignCommands.getConfig.Write(SignCommands.ConfigPath);
				args.Player.SendMessage("Sign Command Config Reloaded Successfully.", Color.MediumSeaGreen);
			}
			catch (Exception ex)
			{
				args.Player.SendMessage("Error: Could not reload Sign Command config, Check log for more details.", Color.OrangeRed);
				Log.Error("Config Exception in Sign Commands config file");
				Log.Error(ex.ToString());
			}
		}
	}
}