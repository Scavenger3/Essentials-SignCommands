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

		public static string ConfigPath = string.Empty;
		public esConfig Write(string path)
		{
			File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
			return this;
		}
		public static esConfig Read(string path)
		{
			if (!File.Exists(path))
			{
				WriteExample(path);
			}
			return JsonConvert.DeserializeObject<esConfig>(File.ReadAllText(path));
		}
		public static void WriteExample(string path)
		{
			File.WriteAllText(path, @"{
  ""ShowBackMessageOnDeath"": true,
  ""PrefixNicknamesWith"": ""~"",
  ""LockRedTeam"": false,
  ""RedTeamPassword"": "",
  ""RedTeamPermission"": ""essentials.team.red"",
  ""LockGreenTeam"": true,
  ""GreenTeamPassword"": "",
  ""GreenTeamPermission"": ""essentials.team.green"",
  ""LockBlueTeam"": true,
  ""BlueTeamPassword"": "",
  ""BlueTeamPermission"": ""essentials.team.blue"",
  ""LockYellowTeam"": false,
  ""YellowTeamPassword"": "",
  ""YellowTeamPermission"": ""essentials.team.yellow"",
  ""DisableSetHomeInRegions"": [
    ""example""
  ]
}");
		}

		public static void LoadConfig()
		{
			try
			{
				ConfigPath = Path.Combine(Essentials.SavePath, "EssentialsConfig.json");
				Essentials.Config = esConfig.Read(ConfigPath).Write(ConfigPath);
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
				if (!Directory.Exists(Essentials.SavePath))
				{
					Directory.CreateDirectory(Essentials.SavePath);
				}
				Essentials.Config = esConfig.Read(ConfigPath).Write(ConfigPath);
				args.Player.SendSuccessMessage("[Essentials] Config reloaded successfully!");
			}
			catch (Exception ex)
			{
				args.Player.SendWarningMessage("[Essnetials] Reload failed! Check logs for more details.");
				Log.Error(string.Concat("[Essentials] Config Exception:\n", ex.ToString()));
			}
		}
	}
}
