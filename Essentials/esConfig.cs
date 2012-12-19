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
		public bool LockRedTeam = false;
		public string RedTeamPassword = "";
		public string RedTeamPermission = "essentials.team.red";
		public bool LockGreenTeam = false;
		public string GreenTeamPassword = "";
		public string GreenTeamPermission = "essentials.team.green";
		public bool LockBlueTeam = false;
		public string BlueTeamPassword = "";
		public string BlueTeamPermission = "essentials.team.blue";
		public bool LockYellowTeam = false;
		public string YellowTeamPassword = "";
		public string YellowTeamPermission = "essentials.team.yellow";
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
				if (!Directory.Exists(Essentials.EssDirectory))
				{
					Directory.CreateDirectory(Essentials.EssDirectory);
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
				"  \"LockRedTeam\": false," + Environment.NewLine +
				"  \"RedTeamPassword\": \"\"," + Environment.NewLine +
				"  \"RedTeamPermission\": \"essentials.team.red\"," + Environment.NewLine +
				"  \"LockGreenTeam\": true," + Environment.NewLine +
				"  \"GreenTeamPassword\": \"\"," + Environment.NewLine +
				"  \"GreenTeamPermission\": \"essentials.team.green\"," + Environment.NewLine +
				"  \"LockBlueTeam\": true," + Environment.NewLine +
				"  \"BlueTeamPassword\": \"\"," + Environment.NewLine +
				"  \"BlueTeamPermission\": \"essentials.team.blue\"," + Environment.NewLine +
				"  \"LockYellowTeam\": false," + Environment.NewLine +
				"  \"YellowTeamPassword\": \"\"," + Environment.NewLine +
				"  \"YellowTeamPermission\": \"essentials.team.yellow\"," + Environment.NewLine +
				"  \"DisableSetHomeInRegions\": [" + Environment.NewLine +
				"    \"example\"" + Environment.NewLine +
				"  ]" + Environment.NewLine +
				"}");
		}
	}
}