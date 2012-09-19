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
		public int LastBackX { get; set; }
		public int LastBackY { get; set; }
		public BackAction LastBackAction { get; set; }
		public bool SavedBackAction { get; set; }
		public List<object> LastSearchResults { get; set; }
		public string RedPassword { get; set; }
		public string GreenPassword { get; set; }
		public string BluePassword { get; set; }
		public string YellowPassword { get; set; }
		public string LastCMD { get; set; }
		public bool Disabled { get; set; }
		public int DisabledX { get; set; }
		public int DisabledY { get; set; }
		public DateTime LastDisabledCheck { get; set; }
		public bool SocialSpy { get; set; }
		public bool HasNickName { get; set; }
		public string Nickname { get; set; }
		public string OriginalName { get; set; }

		public esPlayer(int index)
		{
			Index = index;
			LastBackX = -1;
			LastBackY = -1;
			LastBackAction = BackAction.None;
			SavedBackAction = false;
			LastSearchResults = new List<object>();
			RedPassword = string.Empty;
			GreenPassword = string.Empty;
			BluePassword = string.Empty;
			YellowPassword = string.Empty;
			LastCMD = string.Empty;
			Disabled = false;
			DisabledX = Main.spawnTileX;
			DisabledY = Main.spawnTileY;
			LastDisabledCheck = DateTime.UtcNow;
			SocialSpy = false;
			HasNickName = false;
			Nickname = string.Empty;
			OriginalName = string.Empty;
		}

		public void Disable()
		{
			TSPlayer.SetBuff(33, 330, true); //Weak
			TSPlayer.SetBuff(32, 330, true); //Slow
			TSPlayer.SetBuff(23, 330, true); //Cursed
		}
	}
	public enum BackAction
	{
		None, TP, Death
	}
}