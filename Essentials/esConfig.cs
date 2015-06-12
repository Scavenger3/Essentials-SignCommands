using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Essentials
{
	public class EsConfig
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
		public int BackCooldown = 0;

        public void Write(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }

        public static EsConfig Read(string path)
        {
            return !File.Exists(path)
                ? new EsConfig() 
                : JsonConvert.DeserializeObject<EsConfig>(File.ReadAllText(path));
        }
	}
}
