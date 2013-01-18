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

		internal static string ConfigPath { get { return Path.Combine(TShock.SavePath, "Essentials", "EssentialsConfig.json"); } }
		public static esConfig Read()
		{
			if (!File.Exists(ConfigPath))
				return new esConfig();
			using (var fs = new FileStream(ConfigPath, FileMode.Open, FileAccess.Read, FileShare.Read))
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

		public static Action<esConfig> ConfigRead;

		public static void LoadConfig()
		{
			try
			{
				DoLoad();
			}
			catch (Exception ex)
			{
				Log.ConsoleError("[Essentials] Config Exception! Check logs for more details.");
				Log.Error(ex.ToString());
			}
		}

		public static void ReloadConfig(CommandArgs args)
		{
			try
			{
				if (!Directory.Exists(Essentials.PluginDirectory))
					Directory.CreateDirectory(Essentials.PluginDirectory);

				DoLoad();
				args.Player.SendMessage("[Essentials] Config reloaded successfully!", Color.MediumSeaGreen);
			}
			catch (Exception ex)
			{
				args.Player.SendWarningMessage("[Essnetials] Reload failed! Check logs for more details.");
				Log.Error("[Essentials] Config Exception:");
				Log.Error(ex.ToString());
			}
		}

		public static void DoLoad()
		{
			if (!File.Exists(ConfigPath))
				CreateExample();
			
			Essentials.getConfig = esConfig.Read();
			Essentials.getConfig.Write();
		}

		public static void CreateExample()
		{
			File.WriteAllText(ConfigPath,
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