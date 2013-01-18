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
		public double ptTime { get; set; }
		public bool ptDay { get; set; }

		public esPlayer(int index)
		{
			this.Index = index;
			this.LastBackX = -1;
			this.LastBackY = -1;
			this.LastBackAction = BackAction.None;
			this.SavedBackAction = false;
			this.LastSearchResults = new List<object>();
			this.RedPassword = string.Empty;
			this.GreenPassword = string.Empty;
			this.BluePassword = string.Empty;
			this.YellowPassword = string.Empty;
			this.LastCMD = string.Empty;
			this.Disabled = false;
			this.DisabledX = -1;
			this.DisabledY = -1;
			this.LastDisabledCheck = DateTime.UtcNow;
			this.SocialSpy = false;
			this.HasNickName = false;
			this.Nickname = string.Empty;
			this.OriginalName = string.Empty;
			this.ptTime = -1.0;
			this.ptDay = true;
		}

		public void Disable()
		{
			this.TSPlayer.SetBuff(33, 330, true); //Weak
			this.TSPlayer.SetBuff(32, 330, true); //Slow
			this.TSPlayer.SetBuff(23, 330, true); //Cursed
		}
	}
	public enum BackAction
	{
		None = 0,
		TP = 1,
		Death = 2
	}
}