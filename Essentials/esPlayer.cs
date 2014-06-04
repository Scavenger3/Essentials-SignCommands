using System;
using System.Collections.Generic;
using TShockAPI;

namespace Essentials
{
	public class EsPlayer
	{
		public int Index { get; set; }
		public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
		public int LastBackX { get; set; }
		public int LastBackY { get; set; }
		public BackAction LastBackAction { get; set; }
		public bool SavedBackAction { get; set; }
		public int BackCooldown { get; set; }
		public List<object> LastSearchResults { get; set; }
		public string RedPassword { get; set; }
		public string GreenPassword { get; set; }
		public string BluePassword { get; set; }
		public string YellowPassword { get; set; }
		public string LastCmd { get; set; }
		public bool Disabled { get; set; }
		public int DisabledX { get; set; }
		public int DisabledY { get; set; }
		public DateTime LastDisabledCheck { get; set; }
		public bool SocialSpy { get; set; }
		public bool HasNickName { get; set; }
		public string Nickname { get; set; }
		public string OriginalName { get; set; }
		public double PtTime { get; set; }
		public bool PtDay { get; set; }
		public PlayerData InvSee { get; set; }

		public EsPlayer(int index)
		{
			Index = index;
			LastBackX = -1;
			LastBackY = -1;
			LastBackAction = BackAction.None;
			SavedBackAction = false;
			BackCooldown = 0;
			LastSearchResults = new List<object>();
			RedPassword = string.Empty;
			GreenPassword = string.Empty;
			BluePassword = string.Empty;
			YellowPassword = string.Empty;
			LastCmd = string.Empty;
			Disabled = false;
			DisabledX = -1;
			DisabledY = -1;
			LastDisabledCheck = DateTime.UtcNow;
			SocialSpy = false;
			HasNickName = false;
			Nickname = string.Empty;
			OriginalName = string.Empty;
			PtTime = -1.0;
			PtDay = true;
			InvSee = null;
		}

		public void Disable()
		{
			TSPlayer.LastThreat = DateTime.UtcNow;
			TSPlayer.SetBuff(33, 330, true); //Weak
			TSPlayer.SetBuff(32, 330, true); //Slow
            TSPlayer.SetBuff(23, 330, true); //Cursed	
            TSPlayer.SetBuff(47, 330, true); //Frozen
		}
	}
	public enum BackAction
	{
		None = 0,
		Tp = 1,
		Death = 2
	}
}