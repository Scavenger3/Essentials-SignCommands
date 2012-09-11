using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;

namespace Essentials
{
	public class esPlayer
	{
		public int Index { get; set; }
		public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
		public int lastXtp = -1;
		public int lastYtp = -1;
		public int lastXondeath = -1;
		public int lastYondeath = -1;
		public string lastaction = "none";
		public bool ondeath = false;
		public string lastsearch = string.Empty;
		public string lastseachtype = "none";
		public string RedPassword = string.Empty;
		public string GreenPassword = string.Empty;
		public string BluePassword = string.Empty;
		public string YellowPassword = string.Empty;
		public string LastCMD = string.Empty;
		public bool Disabled = false;
		public int DisabledX = -1;
		public int DisabledY = -1;
		public DateTime LastDisabledCheck = DateTime.UtcNow;
		public bool SocialSpy = false;
		public bool HasNickName = false;
		public string Nickname = string.Empty;
		public string OriginalName = string.Empty;

		public esPlayer(int index)
		{
			Index = index;
		}

		public void SendMessage(string message, Color color)
		{
			TShock.Players[Index].SendMessage(message, color);
		}

		public void Kick(string reason)
		{
			TShock.Players[Index].Disconnect(reason);
		}

		public void Teleport(int xtile, int ytile)
		{
			TShock.Players[Index].Teleport(xtile, ytile);
		}
	}
}