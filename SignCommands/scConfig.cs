using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace config
{
	public class scConfig
	{
		public string DefineSignCommands = "[Sign Command]";
		public string CommandsStartWith = ">";
		public bool GlobalTimeCooldown = false;
		public int TimeCooldown = 20;
		public int HealCooldown = 20;
		public int ShowMsgCooldown = 20;
		public int DamageCooldown = 20;
		public bool GlobalBossCooldown = false;
		public int BossCooldown = 20;
		public int ItemCooldown = 60;
		public int BuffCooldown = 20;
		public bool GlobalSpawnMobCooldown = false;
		public int SpawnMobCooldown = 20;
		public bool GlobalKitCooldown = false;
		public int KitCooldown = 20;
		public bool GlobalDoCommandCooldown = false;
		public int DoCommandCooldown = 20;

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
	}
}