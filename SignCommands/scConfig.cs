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
		public Dictionary<string, int> CooldownGroups = new Dictionary<string, int>();
		public bool ShowDestroyMessage = true;

		internal static string ConfigDirectory { get { return Path.Combine(TShock.SavePath, "PluginConfigs"); } }
		internal static string ConfigPath { get { return Path.Combine(TShock.SavePath, "PluginConfigs", "SignCommandsConfig.json"); } }
		public static scConfig Read()
		{
			if (!File.Exists(ConfigPath))
				return new scConfig();
			using (var fs = new FileStream(ConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read))
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

		public void Write()
		{
			using (var fs = new FileStream(ConfigPath, FileMode.Create, FileAccess.Write, FileShare.Write))
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
				DoLoad();
			}
			catch (Exception ex)
			{
				Log.ConsoleError("[Sign Commands] Config Exception! Check logs for more details.");
				Log.Error(ex.ToString());
			}
		}

		public static void ReloadConfig(CommandArgs args)
		{
			try
			{
				DoLoad();
				args.Player.SendMessage("[Sign Commands] Config reloaded successfully!", Color.MediumSeaGreen);
			}
			catch (Exception ex)
			{
				args.Player.SendWarningMessage("[Sign Commands] Reload failed! Check logs for more details.");
				Log.Error("[Sign Commands] Config Exception:");
				Log.Error(ex.ToString());
			}
		}

		public static void DoLoad()
		{
			if (!Directory.Exists(ConfigDirectory))
				Directory.CreateDirectory(ConfigDirectory);

			if (!File.Exists(ConfigPath))
				scConfig.CreateExample();

			SignCommands.getConfig = scConfig.Read();
			SignCommands.getConfig.Write();
		}

		public static void CreateExample()
		{
			File.WriteAllText(ConfigPath,
				"{" + Environment.NewLine +
				"  \"DefineSignCommands\": \"[Sign Command]\"," + Environment.NewLine +
				"  \"CommandsStartWith\": \">\"," + Environment.NewLine +
				"  \"CooldownGroups\": {" + Environment.NewLine +
				"    \"global-time\": 300," + Environment.NewLine +
				"    \"heal\": 60," + Environment.NewLine +
				"    \"message\": 10," + Environment.NewLine +
				"    \"damage\": 10," + Environment.NewLine +
				"    \"boss\": 300," + Environment.NewLine +
				"    \"item\": 300," + Environment.NewLine +
				"    \"buff\": 300," + Environment.NewLine +
				"    \"spawnmob\": 300," + Environment.NewLine +
				"    \"kit\": 300," + Environment.NewLine +
				"    \"command\": 300" + Environment.NewLine +
				"  }," + Environment.NewLine +
				"  \"ShowDestroyMessage\": true," + Environment.NewLine +
				"}");
		}
	}
}