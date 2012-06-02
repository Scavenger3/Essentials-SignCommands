using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace SignCommands
{
	public class scPlayer
	{
		public int Index { get; set; }
		public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
		public string plrName { get { return TShock.Players[Index].Name; } }
		public bool destsign = false;
		public int toldperm { get; set; }
		public int toldcool { get; set; }
		public int tolddest { get; set; }
		public int toldalert { get; set; }
		public int CooldownTime { get; set; }
		public int CooldownHeal { get; set; }
		public int CooldownMsg { get; set; }
		public int CooldownDamage { get; set; }
		public int CooldownBoss { get; set; }
		public int CooldownItem { get; set; }
		public int CooldownBuff { get; set; }
		public int CooldownSpawnMob { get; set; }
		public int CooldownKit { get; set; }
		public int CooldownCommand { get; set; }
		public int handlecmd { get; set; }
		public bool checkforsignedit { get; set; }
		public bool DestroyingSign { get; set; }
		public string originalsigntext { get; set; }
		public int signeditid { get; set; }

		public scPlayer(int index)
		{
			Index = index;
			//TSPlayer = TShock.Players[Index];
			//plrName = TShock.Players[Index].Name;
			destsign = false;
			toldperm = 0;
			toldcool = 0;
			tolddest = 0;
			toldalert = 0;
			CooldownTime = 0;
			CooldownHeal = 0;
			CooldownMsg = 0;
			CooldownDamage = 0;
			CooldownBoss = 0;
			CooldownItem = 0;
			CooldownBuff = 0;
			CooldownSpawnMob = 0;
			CooldownKit = 0;
			CooldownCommand = 0;
			handlecmd = 0;
			checkforsignedit = false;
			DestroyingSign = false;
			originalsigntext = string.Empty;
			signeditid = 0;
		}

		public bool HasCooldown()
		{
			if (CooldownTime > 0 || CooldownHeal > 0 || CooldownMsg > 0 || CooldownDamage > 0 || CooldownBoss > 0 ||
				CooldownItem > 0 || CooldownBuff > 0 || CooldownSpawnMob > 0 || CooldownKit > 0 || CooldownCommand > 0)
			{
				return true;
			}

			return false;
		}

		public List<int> Cooldowns()
		{
			List<int> r = new List<int>();
			r.Add(CooldownBoss);
			r.Add(CooldownBuff);
			r.Add(CooldownCommand);
			r.Add(CooldownDamage);
			r.Add(CooldownHeal);
			r.Add(CooldownItem);
			r.Add(CooldownKit);
			r.Add(CooldownMsg);
			r.Add(CooldownSpawnMob);
			r.Add(CooldownTime);
			return r;
		}
	}
}