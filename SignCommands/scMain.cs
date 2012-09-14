using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using Terraria;
using TShockAPI;
using Hooks;
using System.Text;
using System.Data;
using MySql.Data.MySqlClient;
using Mono.Data.Sqlite;
using TShockAPI.DB;

namespace SignCommands
{
	[APIVersion(1, 12)]
	public class SignCommands : TerrariaPlugin
	{
		public static scConfig getConfig { get; set; }
		internal static string ConfigPath { get { return Path.Combine(TShock.SavePath, "PluginConfigs", "SignCommandsConfig.json"); } }

		public static List<scPlayer> scPlayers = new List<scPlayer>();
		public static Dictionary<string, int> GlobalCooldowns = new Dictionary<string, int>();
		public static Dictionary<string, Dictionary<int, int>> LoggedOutCooldowns = new Dictionary<string, Dictionary<int, int>>();

		/*public bool UsingInfiniteSigns = false;*/
		public DateTime lastCooldown = DateTime.UtcNow;

		public override string Name
		{
			get { return "Sign Commands"; }
		}

		public override string Author
		{
			get { return "by Scavenger"; }
		}

		public override string Description
		{
			get { return "Put commands on signs!"; }
		}

		public override Version Version
		{
			get { return new Version("1.3.9"); }
		}

		public override void Initialize()
		{
			GameHooks.Initialize += OnInitialize;
			NetHooks.GetData += GetData;
			NetHooks.GreetPlayer += OnGreetPlayer;
			ServerHooks.Leave += OnLeave;
			GameHooks.Update += OnUpdate;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				GameHooks.Initialize -= OnInitialize;
				NetHooks.GetData -= GetData;
				NetHooks.GreetPlayer -= OnGreetPlayer;
				ServerHooks.Leave -= OnLeave;
				GameHooks.Update -= OnUpdate;

				/* UnLoad Infinite Signs Delegates /
				if (UsingInfiniteSigns)
				{
					try
					{
						InfiniteSigns.InfiniteSigns.SignEdit -= OnSignEdit;
						InfiniteSigns.InfiniteSigns.SignHit -= OnSignHit;
						InfiniteSigns.InfiniteSigns.SignKill -= OnSignKill;
					}
					catch { }
				}*/
			}
			base.Dispose(disposing);
		}

		public SignCommands(Main game) : base(game)
		{
			getConfig = new scConfig();
			Order = -1;
		}

		#region Initialize
		public void OnInitialize()
		{
			/* Add Commands */
			Commands.ChatCommands.Add(new Command("essentials.signs.break", CMDdestsign, "destsign"));
			Commands.ChatCommands.Add(new Command("essentials.signs.reload", CMDscreload, "screload"));

			/* Load Config */
			if (!Directory.Exists(@"tshock/PluginConfigs/"))
				Directory.CreateDirectory(@"tshock/PluginConfigs/");

			scConfig.LoadConfig();

			/* Check for InfiniteSigns.dll /
			if (File.Exists(@"serverplugins/InfiniteSigns.dll"))
				UsingInfiniteSigns = true;

			if (UsingInfiniteSigns)
			{
				/* Load Delegates /
				try
				{
					InfiniteSigns.InfiniteSigns.SignEdit += OnSignEdit;
					InfiniteSigns.InfiniteSigns.SignHit += OnSignHit;
					InfiniteSigns.InfiniteSigns.SignKill += OnSignKill;
				}
				catch { }
			}*/
		}
		#endregion

		#region Commands
		private void CMDdestsign(CommandArgs args)
		{
			scPlayer play = scUtils.GetscPlayerByID(args.Player.Index);
			if (play == null) return;

			play.DestroyMode = !play.DestroyMode;

			if (play.DestroyMode)
				args.Player.SendMessage("You are now in destroy mode, Type \"/destsign\" to return to use mode!", Color.LightGreen);
			else
				args.Player.SendMessage("You are now in use mode, Type \"/destsign\" to return to destroy mode!", Color.LightGreen);
		}

		private void CMDscreload(CommandArgs args)
		{
			scConfig.ReloadConfig(args);
		}
		#endregion

		#region scPlayers
		public void OnGreetPlayer(int who, HandledEventArgs e)
		{
			lock (scPlayers)
				scPlayers.Add(new scPlayer(who));

			if (LoggedOutCooldowns.ContainsKey(TShock.Players[who].Name))
			{
				scPlayer ply = scUtils.GetscPlayerByID(who);
				if (ply == null) return;
				lock (ply.Cooldowns)
					ply.Cooldowns = LoggedOutCooldowns[ply.TSPlayer.Name];
				lock (LoggedOutCooldowns)
					LoggedOutCooldowns.Remove(ply.TSPlayer.Name);
			}
		}

		public void OnLeave(int who)
		{
			lock (scPlayers)
				for (int i = 0; i < scPlayers.Count; i++)
					if (scPlayers[i].Index == who)
					{
						scPlayer ply = scUtils.GetscPlayerByID(who);
						if (ply != null && ply.Cooldowns.Count > 0)
							LoggedOutCooldowns.Add(ply.TSPlayer.Name, ply.Cooldowns);

						scPlayers.RemoveAt(i);
						break;
					}
		}
		#endregion

		#region Timer
		private void OnUpdate()
		{
			if ((DateTime.UtcNow - lastCooldown).TotalMilliseconds >= 1000)
			{
				lastCooldown = DateTime.UtcNow;

				List<string> CooldownGroups = new List<string>(SignCommands.GlobalCooldowns.Keys);
				lock (SignCommands.GlobalCooldowns)
				{
					foreach (string g in CooldownGroups)
					{
						if (SignCommands.GlobalCooldowns[g] > 0)
							SignCommands.GlobalCooldowns[g]--;
						else if (SignCommands.GlobalCooldowns[g] == 0)
							SignCommands.GlobalCooldowns.Remove(g);
					}
				}

				lock (scPlayers)
				{
					foreach (scPlayer sPly in scPlayers)
					{
						if (sPly.AlertCooldownCooldown > 0)
							sPly.AlertCooldownCooldown--;

						if (sPly.AlertPermissionCooldown > 0)
							sPly.AlertPermissionCooldown--;

						if (sPly.AlertDestroyCooldown > 0)
							sPly.AlertDestroyCooldown--;

						List<int> CooldownIds = new List<int>(sPly.Cooldowns.Keys);
						lock (sPly.Cooldowns)
						{
							foreach (int id in CooldownIds)
							{
								if (sPly.Cooldowns[id] > 0)
									sPly.Cooldowns[id]--;
								else if (sPly.Cooldowns[id] == 0)
									sPly.Cooldowns.Remove(sPly.Cooldowns[id]);
							}
						}
					}
				}
			}
		}
		#endregion

		#region OnSignEdit
		/*private void OnSignEdit(InfiniteSigns.SignEventArgs args)
		{
			args.Handled = OnSignEdit(args.X, args.Y, args.text, args.who, args.id);
		}*/
		private bool OnSignEdit(int X, int Y, string text, int who, int id)
		{
			if (!text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower())) return false;
			TSPlayer tPly = TShock.Players[who];
			scSign sign = new scSign(text, id);

			if (tPly == null) return false;

			if (scUtils.CanCreate(tPly, sign)) return false;

			tPly.SendMessage("You do not have permission to create that sign command!", Color.IndianRed);
			return true;
		}
		#endregion

		#region OnSignHit
		/*private void OnSignHit(InfiniteSigns.SignEventArgs args)
		{
			args.Handled = OnSignHit(args.X, args.Y, args.text, args.who, args.id);
		}*/
		private bool OnSignHit(int X, int Y, string text, int who, int id)
		{
			if (!text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower())) return false;
			TSPlayer tPly = TShock.Players[who];
			scPlayer sPly = scUtils.GetscPlayerByID(who);
			scSign sign = new scSign(text, id);

			if (tPly == null || sPly == null) return false;

			bool CanBreak = scUtils.CanBreak(tPly, sign);
			if (sPly.DestroyMode && CanBreak) return false;

			if (CanBreak && sPly.AlertDestroyCooldown == 0)
			{
				tPly.SendMessage("To destroy this sign, Type \'/destsign\'", Color.Orange);
				sPly.AlertDestroyCooldown = 10;
			}

			sign.ExecuteCommands(sPly);

			return true;
		}
		#endregion

		#region OnSignKill
		/*private void OnSignKill(InfiniteSigns.SignEventArgs args)
		{
			args.Handled = OnSignKill(args.X, args.Y, args.text, args.who, args.id);
		}*/
		private bool OnSignKill(int X, int Y, string text, int who, int id)
		{
			if (!text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower())) return false;

			scPlayer sPly = scUtils.GetscPlayerByID(who);
			scSign sign = new scSign(text, id);

			if (sPly == null) return false;
			if (sPly.DestroyMode && scUtils.CanBreak(sPly.TSPlayer, sign))
			{
				foreach (scPlayer ply in scPlayers)
				{
					lock (ply.Cooldowns)
						if (ply.Cooldowns.ContainsKey(id))
							ply.Cooldowns.Remove(id);
				}

				lock (SignCommands.LoggedOutCooldowns)
				{
					List<string> Users = new List<string>(SignCommands.LoggedOutCooldowns.Keys);
					foreach (string user in Users)
					{
						if (SignCommands.LoggedOutCooldowns[user].ContainsKey(id))
							SignCommands.LoggedOutCooldowns[user].Remove(id);
					}
				}
				return false;
			}
			else
			{
				sign.ExecuteCommands(sPly);
				return true;
			}
		}
		#endregion

		#region GetData
		public void GetData(GetDataEventArgs e)
		{
			if (e.Handled == true) return;
			try
			{
				switch (e.MsgID)
				{
					#region Sign Edit
					case PacketTypes.SignNew:
						{
							/*if (!UsingInfiniteSigns)
							{*/
							int X = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 2);
							int Y = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 6);
							string text = Encoding.UTF8.GetString(e.Msg.readBuffer, e.Index + 10, e.Length - 11);

							int id = Terraria.Sign.ReadSign(X, Y);
							if (id == -1 || Main.sign[id] == null) return;
							if (OnSignEdit(X, Y, text, e.Msg.whoAmI, id))
							{
								e.Handled = true;
								TShock.Players[e.Msg.whoAmI].SendData(PacketTypes.SignNew, "", id);
							}
							/*}*/
						}
						break;
					#endregion

					#region Tile Modify
					case PacketTypes.Tile:
						{
							/*if (!UsingInfiniteSigns)
							{*/
							int X = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 1);
							int Y = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 5);

							if (Main.tile[X, Y].type != 55) return;

							int id = Terraria.Sign.ReadSign(X, Y);
							if (id == -1 || Main.sign[id] == null) return;

							string text = Main.sign[id].text;

							bool handle = false;
							if (e.Msg.readBuffer[e.Index] == 0 && e.Msg.readBuffer[e.Index + 9] == 0)
								handle = OnSignKill(X, Y, text, e.Msg.whoAmI, id);
							else
								handle = OnSignHit(X, Y, text, e.Msg.whoAmI, id);

							if (handle)
							{
								e.Handled = true;
								TShock.Players[e.Msg.whoAmI].SendTileSquare(X, Y);
							}
							/*}*/
						}
						break;
					#endregion
				}
			}
			catch (Exception ex)
			{
				Log.Error("[Sign Commands] Exception while recieving data: ");
				Log.Error(ex.ToString());
			}
		}
		#endregion
	}
}