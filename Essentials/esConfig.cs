using System;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using TShockAPI;

namespace Essentials
{
	public class esConfig
	{
		public bool ShowBackMessageOnDeath = true;
		public string PrefixNicknamesWith = "~";
		public bool UsePermissonsForTeams = false;
		public string RedPassword = "";
		public string GreenPassword = "";
		public string BluePassword = "";
		public string YellowPassword = "";
		public List<string> DisableSetHomeInRegions = new List<string>();


		public static esConfig Read(string path)
		{
			if (!File.Exists(path))
				return new esConfig();
			using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
			{
				return Read(fs);
			}
		}

		public static esConfig Read(Stream stream)
		{
			using (var sr = new StreamReader(stream))
			{
				var cf = JsonConvert.DeserializeObject<esConfig>(sr.ReadToEnd());
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

		public static Action<esConfig> ConfigRead;

		public static void LoadConfig()
		{
			try
			{
				if (File.Exists(Essentials.ConfigPath))
				{
					Essentials.getConfig = esConfig.Read(Essentials.ConfigPath);
				}
				Essentials.getConfig.Write(Essentials.ConfigPath);
			}
			catch (Exception ex)
			{
				Log.ConsoleError("Error in Essentials config file, Check the logs for more details!");
				Log.Error(ex.ToString());
			}
		}

		public static void ReloadConfig(CommandArgs args)
		{
			try
			{
				if (!Directory.Exists(@"tshock/Essentials/"))
				{
					Directory.CreateDirectory(@"tshock/Essentials/");
				}

				if (File.Exists(Essentials.ConfigPath))
				{
					Essentials.getConfig = esConfig.Read(Essentials.ConfigPath);
				}
				Essentials.getConfig.Write(Essentials.ConfigPath);
				args.Player.SendMessage("Essentials Config Reloaded Successfully.", Color.MediumSeaGreen);
			}
			catch (Exception ex)
			{
				args.Player.SendMessage("Error: Could not reload Essentials config, Check log for more details.", Color.IndianRed);
				Log.Error("Config Exception in Essentials config file");
				Log.Error(ex.ToString());
			}
		}

		public static void CreateExample()
		{
			File.WriteAllText(Essentials.ConfigPath,
				"{" + Environment.NewLine +
				"  \"ShowBackMessageOnDeath\": true," + Environment.NewLine +
				"  \"PrefixNicknamesWith\": \"~\"," + Environment.NewLine +
				"  \"UsePermissonsForTeams\": false," + Environment.NewLine +
				"  \"RedPassword\": \"\"," + Environment.NewLine +
				"  \"GreenPassword\": \"\"," + Environment.NewLine +
				"  \"BluePassword\": \"\"," + Environment.NewLine +
				"  \"YellowPassword\": \"\"," + Environment.NewLine +
				"  \"DisableSetHomeInRegions\": [" + Environment.NewLine +
				"	\"example\"" + Environment.NewLine +
				"  ]" + Environment.NewLine +
				"}");
		}
	}
}