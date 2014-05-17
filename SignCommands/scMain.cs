using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace SignCommands
{
	[ApiVersion(1, 16)]
	public class SignCommands : TerrariaPlugin
	{
		public override string Name { get { return "Sign Commands"; } }
		public override string Author { get { return "Scavenger"; } }
		public override string Description { get { return "Put commands on signs!"; } }
		public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

		public static scConfig Config = new scConfig();
		public static scPlayer[] scPlayers = new scPlayer[256];
		public static Dictionary<string, DateTime> GlobalCooldowns = new Dictionary<string, DateTime>();
		public static Dictionary<string, Dictionary<string, DateTime>> OfflineCooldowns = new Dictionary<string, Dictionary<string, DateTime>>();
		public static bool UsingInfiniteSigns { get; set; }
		public static bool UsingSEConomy { get; set; }
		DateTime LastCooldown = DateTime.UtcNow;
		DateTime LastPurge = DateTime.UtcNow;

		public SignCommands(Main game)
			: base(game)
		{
			UsingInfiniteSigns = File.Exists(Path.Combine("ServerPlugins", "InfiniteSigns.dll"));
			UsingSEConomy = File.Exists(Path.Combine("ServerPlugins", "Wolfje.Plugins.SEconomy.dll"));
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			if (!UsingInfiniteSigns)
			{
				ServerApi.Hooks.NetGetData.Register(this, OnGetData);
			}
			ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
		}

		protected override void Dispose(bool Disposing)
		{
			if (Disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				if (!UsingInfiniteSigns)
				{
					ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
				}
				ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
			}
			base.Dispose(Disposing);
		}

		public void OnInitialize(EventArgs args)
		{
			Commands.ChatCommands.Add(new Command("essentials.signs.break", CMDdestsign, "destsign"));
			Commands.ChatCommands.Add(new Command("essentials.signs.reload", CMDscreload, "screload"));

			scConfig.LoadConfig();
		}

		#region Commands
		private void CMDdestsign(CommandArgs args)
		{
			scPlayer sPly = scPlayers[args.Player.Index];

			sPly.DestroyMode = true;
			args.Player.SendSuccessMessage("You can now destroy a sign.");
		}

		private void CMDscreload(CommandArgs args)
		{
			scConfig.ReloadConfig(args);
		}
		#endregion

		#region scPlayers
		public void OnJoin(JoinEventArgs args)
		{
			scPlayers[args.Who] = new scPlayer(args.Who);

			if (OfflineCooldowns.ContainsKey(TShock.Players[args.Who].Name))
			{
				scPlayers[args.Who].Cooldowns = OfflineCooldowns[TShock.Players[args.Who].Name];
				OfflineCooldowns.Remove(TShock.Players[args.Who].Name);
			}
		}

		public void OnLeave(LeaveEventArgs args)
		{
			if (scPlayers[args.Who] != null && scPlayers[args.Who].Cooldowns.Count > 0)
				OfflineCooldowns.Add(TShock.Players[args.Who].Name, scPlayers[args.Who].Cooldowns);
			scPlayers[args.Who] = null;
		}
		#endregion

		#region Timer
		private void OnUpdate(EventArgs args)
		{
			if ((DateTime.UtcNow - LastCooldown).TotalMilliseconds >= 1000)
			{
				LastCooldown = DateTime.UtcNow;
				foreach (var sPly in scPlayers)
				{
					if (sPly == null) continue;
					if (sPly.AlertCooldownCooldown > 0)
						sPly.AlertCooldownCooldown--;
					if (sPly.AlertPermissionCooldown > 0)
						sPly.AlertPermissionCooldown--;
					if (sPly.AlertDestroyCooldown > 0)
						sPly.AlertDestroyCooldown--;
				}

				if ((DateTime.UtcNow - LastPurge).TotalMinutes >= 5)
				{
					LastPurge = DateTime.UtcNow;

					List<string> CooldownGroups = new List<string>(GlobalCooldowns.Keys);
					foreach (string g in CooldownGroups)
					{
						if (DateTime.UtcNow > GlobalCooldowns[g])
							GlobalCooldowns.Remove(g);
					}

					List<string> OfflinePlayers = new List<string>(OfflineCooldowns.Keys);
					foreach (string p in OfflinePlayers)
					{
						List<string> OfflinePlayerCooldowns = new List<string>(OfflineCooldowns[p].Keys);
						foreach (string g in OfflinePlayerCooldowns)
						{
							if (DateTime.UtcNow > OfflineCooldowns[p][g])
								OfflineCooldowns[p].Remove(g);
						}
						if (OfflineCooldowns[p].Count == 0)
							OfflineCooldowns.Remove(p);
					}

					foreach (var sPly in scPlayers)
					{
						if (sPly == null) continue;
						List<string> CooldownIds = new List<string>(sPly.Cooldowns.Keys);
						foreach (string id in CooldownIds)
						{
							if (DateTime.UtcNow > sPly.Cooldowns[id])
								sPly.Cooldowns.Remove(id);
						}
					}
				}
			}
		}
		#endregion

		#region OnSignEdit
		private bool OnSignEdit(int X, int Y, string text, int who)
		{
			if (!text.ToLower().StartsWith(Config.DefineSignCommands.ToLower())) return false;

			TSPlayer tPly = TShock.Players[who];
			scSign sign = new scSign(text, new Point(X, Y));

			if (scUtils.CanCreate(tPly, sign)) return false;

			tPly.SendErrorMessage("You do not have permission to create that sign command.");
			return true;
		}
		#endregion

		#region OnSignHit
		private bool OnSignHit(int X, int Y, string text, int who)
		{
			if (!text.ToLower().StartsWith(Config.DefineSignCommands.ToLower())) return false;
			TSPlayer tPly = TShock.Players[who];
			scPlayer sPly = scPlayers[who];
			scSign sign = new scSign(text, new Point(X, Y));

			bool CanBreak = scUtils.CanBreak(tPly, sign);
			if (sPly.DestroyMode && CanBreak) return false;

			if (Config.ShowDestroyMessage && CanBreak && sPly.AlertDestroyCooldown == 0)
			{
				tPly.SendInfoMessage("To destroy this sign, Type \"/destsign\".");
				sPly.AlertDestroyCooldown = 10;
			}

			sign.ExecuteCommands(sPly);

			return true;
		}
		#endregion

		#region OnSignKill
		private bool OnSignKill(int X, int Y, string text, int who)
		{
			if (!text.ToLower().StartsWith(Config.DefineSignCommands.ToLower())) return false;

			var sPly = scPlayers[who];
			scSign sign = new scSign(text, new Point(X, Y));

			if (sPly.DestroyMode && scUtils.CanBreak(sPly.TSPlayer, sign))
			{
				sPly.DestroyMode = false;
				string id = new Point(X, Y).ToString();
				List<string> OfflinePlayers = new List<string>(OfflineCooldowns.Keys);
				foreach (var p in OfflinePlayers)
				{
					if (OfflineCooldowns[p].ContainsKey(id))
						OfflineCooldowns[p].Remove(id);
				}

				foreach (var Ply in scPlayers)
				{
					if (Ply == null || !Ply.Cooldowns.ContainsKey(id)) continue;
					Ply.Cooldowns.Remove(id);
				}
				return false;
			}
			sign.ExecuteCommands(sPly);
			return true;
		}
		#endregion

		#region OnGetData
		public void OnGetData(GetDataEventArgs e)
		{
			if (e.Handled)
			{
				return;
			}
			switch (e.MsgID)
			{
				#region Sign Edit
				case PacketTypes.SignNew:
					{
						int X = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 2);
						int Y = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 6);
						string text = Encoding.UTF8.GetString(e.Msg.readBuffer, e.Index + 10, e.Length - 11);

						int id = Terraria.Sign.ReadSign(X, Y);
						if (id < 0 || Main.sign[id] == null) return;
						X = Main.sign[id].x;
						Y = Main.sign[id].y;
						if (OnSignEdit(X, Y, text, e.Msg.whoAmI))
						{
							e.Handled = true;
							TShock.Players[e.Msg.whoAmI].SendData(PacketTypes.SignNew, "", id);
						}
					}
					break;
				#endregion

				#region Tile Modify
				case PacketTypes.Tile:
					{
						int X = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 1);
						int Y = BitConverter.ToInt32(e.Msg.readBuffer, e.Index + 5);

						if (Main.tile[X, Y].type != 55) return;

						int id = Terraria.Sign.ReadSign(X, Y);
						if (id < 0 || Main.sign[id] == null) return;
						X = Main.sign[id].x;
						Y = Main.sign[id].y;
						string text = Main.sign[id].text;

						bool handle = false;
						if (e.Msg.readBuffer[e.Index] == 0 && e.Msg.readBuffer[e.Index + 9] == 0)
							handle = OnSignKill(X, Y, text, e.Msg.whoAmI);
						else
							handle = OnSignHit(X, Y, text, e.Msg.whoAmI);

						if (handle)
						{
							e.Handled = true;
							TShock.Players[e.Msg.whoAmI].SendTileSquare(X, Y);
						}
					}
					break;
				#endregion
			}
		}
		#endregion
	}
}