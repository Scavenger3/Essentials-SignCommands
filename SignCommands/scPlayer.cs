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
		public Dictionary<string, int> Cooldowns { get; set; }
		public bool DestroyMode { get; set; }
		public int AlertCooldownCooldown { get; set; }
		public int AlertPermissionCooldown { get; set; }
		public int AlertDestroyCooldown { get; set; }

		public scPlayer(int index)
		{
			this.Index = index;
			this.Cooldowns = new Dictionary<string, int>();
			this.DestroyMode = false;
			this.AlertDestroyCooldown = 0;
			this.AlertPermissionCooldown = 0;
			this.AlertCooldownCooldown = 0;
		}
	}
}