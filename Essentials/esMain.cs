using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Essentials
{
	[ApiVersion(1, 14)]
	public class Essentials : TerrariaPlugin
	{
		public override string Name { get { return "Essentials"; } }
		public override string Author { get { return "Scavenger"; } }
		public override string Description { get { return "Some Essential commands for TShock!"; } }
		public override Version Version { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

		public Dictionary<string, int[]> Disabled = new Dictionary<string, int[]>();
		public esPlayer[] esPlayers = new esPlayer[256];
		public DateTime LastCheck = DateTime.UtcNow;
		public static esConfig Config = new esConfig();
		public static string SavePath = string.Empty;

		public Essentials(Main game) : base(game)
		{
			base.Order = 5; 
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			ServerApi.Hooks.ServerChat.Register(this, OnChat);
			PlayerHooks.PlayerCommand += OnPlayerCommand;
			ServerApi.Hooks.NetGetData.Register(this, OnGetData);
			ServerApi.Hooks.NetSendBytes.Register(this, OnSendBytes);
			ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
		}

		protected override void Dispose(bool Disposing)
		{
			if (Disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
				PlayerHooks.PlayerCommand -= OnPlayerCommand;
				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
				ServerApi.Hooks.NetSendBytes.Deregister(this, OnSendBytes);
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
			}
			base.Dispose(Disposing);
		}

		public void OnInitialize(EventArgs args)
		{
			#region Add Commands
			Commands.ChatCommands.Add(new Command("essentials.more", CMDmore, "more"));
			Commands.ChatCommands.Add(new Command(new List<string> { "essentials.position.get", "essentials.position.getother" }, CMDpos, "pos", "getpos"));
			Commands.ChatCommands.Add(new Command("essentials.position.tp", CMDtppos, "tppos"));
			Commands.ChatCommands.Add(new Command("essentials.position.ruler", CMDruler, "ruler"));
			Commands.ChatCommands.Add(new Command("essentials.helpop.ask", CMDhelpop, "helpop"));
			Commands.ChatCommands.Add(new Command("essentials.suicide", CMDsuicide, "suicide", "die"));
			Commands.ChatCommands.Add(new Command("essentials.pvp.burn", CMDburn, "burn"));
			Commands.ChatCommands.Add(new Command("essentials.kickall.kick", CMDkickall, "kickall"));
			Commands.ChatCommands.Add(new Command("essentials.moon", CMDmoon, "moon"));
			Commands.ChatCommands.Add(new Command(new List<string> { "essentials.back.tp", "essentials.back.death" }, CMDback, "b"));
			Commands.ChatCommands.Add(new Command("essentials.ids.search", CMDsitems, "sitem", "si", "searchitem"));
			Commands.ChatCommands.Add(new Command("essentials.ids.search", CMDspage, "spage", "sp"));
			Commands.ChatCommands.Add(new Command("essentials.ids.search", CMDsnpcs, "snpc", "sn", "searchnpc"));
			Commands.ChatCommands.Add(new Command("essentials.home", CMDsethome, "sethome"));
			Commands.ChatCommands.Add(new Command("essentials.home", CMDmyhome, "myhome"));
			Commands.ChatCommands.Add(new Command("essentials.home", CMDdelhome, "delhome"));
			Commands.ChatCommands.Add(new Command("essentials.essentials", CMDessentials, "essentials"));
			Commands.ChatCommands.Add(new Command(/*no permission*/ CMDteamunlock, "teamunlock"));
			Commands.ChatCommands.Add(new Command("essentials.lastcommand", CMDequals, "=") { DoLog = false });
			Commands.ChatCommands.Add(new Command("essentials.pvp.killr", CMDkillr, "killr"));
			Commands.ChatCommands.Add(new Command("essentials.disable", CMDdisable, "disable"));
			Commands.ChatCommands.Add(new Command("essentials.level.top", CMDtop, "top"));
			Commands.ChatCommands.Add(new Command("essentials.level.up", CMDup, "up"));
			Commands.ChatCommands.Add(new Command("essentials.level.down", CMDdown, "down"));
			Commands.ChatCommands.Add(new Command("essentials.level.side", CMDleft, "left"));
			Commands.ChatCommands.Add(new Command("essentials.level.side", CMDright, "right"));
			Commands.ChatCommands.Add(new Command(new List<string> { "essentials.playertime.set", "essentials.playertime.setother" }, CMDptime, "ptime"));
			Commands.ChatCommands.Add(new Command("essentials.ping", CMDping, "ping", "pong", "echo"));
			Commands.ChatCommands.Add(new Command("essentials.sudo", CMDsudo, "sudo"));
			Commands.ChatCommands.Add(new Command("essentials.socialspy", CMDsocialspy, "socialspy"));
			Commands.ChatCommands.Add(new Command("essentials.near", CMDnear, "near"));
			Commands.ChatCommands.Add(new Command(new List<string> { "essentials.nick.set", "essentials.nick.setother" }, CMDnick, "nick"));
			Commands.ChatCommands.Add(new Command("essentials.realname", CMDrealname, "realname"));
			Commands.ChatCommands.Add(new Command("essentials.exacttime", CMDetime, "etime", "exacttime"));
			Commands.ChatCommands.Add(new Command("essentials.forcelogin", CMDforcelogin, "forcelogin"));
			Commands.ChatCommands.Add(new Command("essentials.inventory.see", CMDinvsee, "invsee"));
			#endregion

			SavePath = Path.Combine(TShock.SavePath, "Essentials");
			if (!Directory.Exists(SavePath))
			{
				Directory.CreateDirectory(SavePath);
			}

			esSQL.SetupDB();
			esConfig.LoadConfig();
		}

		#region esPlayer Join / Leave
		public void OnJoin(JoinEventArgs args)
		{
			try
			{
				esPlayers[args.Who] = new esPlayer(args.Who);

				if (Disabled.ContainsKey(TShock.Players[args.Who].Name))
				{
					var ePly = esPlayers[args.Who];
					ePly.DisabledX = Disabled[TShock.Players[args.Who].Name][0];
					ePly.DisabledY = Disabled[TShock.Players[args.Who].Name][1];
					ePly.TSPlayer.Teleport(ePly.DisabledX * 16F, ePly.DisabledY * 16F);
					ePly.Disabled = true;
					ePly.TSPlayer.Disable();
					ePly.LastDisabledCheck = DateTime.UtcNow;
					ePly.TSPlayer.SendWarningMessage("You are still disabled!");
				}

				string nickname;
				if (esSQL.GetNickname(TShock.Players[args.Who].Name, out nickname))
				{
					var ePly = esPlayers[args.Who];
					ePly.HasNickName = true;
					ePly.OriginalName = ePly.TSPlayer.Name;
					ePly.Nickname = nickname;
				}
			}
			catch { }
		}

		public void OnLeave(LeaveEventArgs args)
		{
			try
			{
				esPlayers[args.Who] = null;
			}
			catch { }
		}
		#endregion

		#region Chat / Command
		public void OnChat(ServerChatEventArgs e)
		{
			if (e.Handled)
			{
				return;
			}
			if (e.Text == "/")
			{
				e.Handled = true;
				return;
			}

			var ePly = esPlayers[e.Who];
			var tPly = TShock.Players[e.Who];

			if (ePly.HasNickName && !e.Text.StartsWith("/") && !tPly.mute)
			{
				e.Handled = true;
				string nick = Config.PrefixNicknamesWith + ePly.Nickname;
				TShock.Utils.Broadcast(String.Format(TShock.Config.ChatFormat, tPly.Group.Name, tPly.Group.Prefix, nick, tPly.Group.Suffix, e.Text),
								tPly.Group.R, tPly.Group.G, tPly.Group.B);
			}
			else if (ePly.HasNickName && e.Text.StartsWith("/me ") && !tPly.mute)
			{
				e.Handled = true;
				string nick = Config.PrefixNicknamesWith + ePly.Nickname;
				TShock.Utils.Broadcast(string.Format("*{0} {1}", nick, e.Text.Remove(0, 4)), 205, 133, 63);
			}
		}
		public void OnPlayerCommand(PlayerCommandEventArgs e)
		{
			if (e.Handled)
			{
				return;
			}

			if (e.Player.Index >= 0 && e.Player.Index <= 255 && e.CommandName != "=")
			{
				esPlayers[e.Player.Index].LastCMD = string.Concat("/", e.CommandText);
			}

			if ((e.CommandName == "tp" && e.Player.Group.HasPermission(Permissions.tp)) ||
				(e.CommandName == "home" && e.Player.Group.HasPermission(Permissions.home)) ||
				(e.CommandName == "spawn" && e.Player.Group.HasPermission(Permissions.spawn)) ||
				(e.CommandName == "warp" && e.Player.Group.HasPermission(Permissions.warp)))
			{
				var ePly = esPlayers[e.Player.Index];
				ePly.LastBackX = e.Player.TileX;
				ePly.LastBackY = e.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}
			else if (e.CommandName == "whisper" || e.CommandName == "w" || e.CommandName == "tell" ||
				e.CommandName == "reply" || e.CommandName == "r")
			{
				if (!e.Player.Group.HasPermission(Permissions.whisper))
				{
					return;
				}
				foreach (var player in esPlayers)
				{
					if (player == null || !player.SocialSpy || player.Index == e.Player.Index)
					{
						continue;
					}
					if ((e.CommandName == "reply" || e.CommandName == "r") && e.Player.LastWhisper != null)
					{
						player.TSPlayer.SendMessage(string.Format("[SocialSpy] from {0} to {1}: /{2}", e.Player.Name, e.Player.LastWhisper.Name ?? "?", e.CommandText), Color.Gray);
					}
					else
					{
						player.TSPlayer.SendMessage(string.Format("[SocialSpy] {0}: /{1}", e.Player.Name, e.CommandText), Color.Gray);
					}
				}
			}
		}
		#endregion

		#region Get Data
		public void OnGetData(GetDataEventArgs e)
		{
			try
			{
				if (e.MsgID != PacketTypes.PlayerTeam || !(Config.LockRedTeam || Config.LockGreenTeam || Config.LockBlueTeam || Config.LockYellowTeam)) return;

				var who = e.Msg.readBuffer[e.Index];
				var team = e.Msg.readBuffer[e.Index + 1];

				var ePly = esPlayers[who];
				var tPly = TShock.Players[who];
				if (ePly == null || tPly == null) return;
				switch (team)
				{
					#region Red
					case 1:
						if (Config.LockRedTeam && !tPly.Group.HasPermission(Config.RedTeamPermission) && (ePly.RedPassword != Config.RedTeamPassword || ePly.RedPassword == ""))
						{
							e.Handled = true;
							tPly.SetTeam(tPly.Team);
							if (Config.RedTeamPassword == "")
								tPly.SendMessage("You do not have permission to join that team!", Color.Red);
							else
								tPly.SendMessage("That team is locked, use \'/teamunlock red <password>\' to access it.", Color.Red);

						}
						break;
					#endregion

					#region Green
					case 2:
						if (Config.LockGreenTeam && !tPly.Group.HasPermission(Config.GreenTeamPermission) && (ePly.GreenPassword != Config.GreenTeamPassword || ePly.GreenPassword == ""))
						{
							e.Handled = true;
							tPly.SetTeam(tPly.Team);
							if (Config.GreenTeamPassword == "")
								tPly.SendMessage("You do not have permission to join that team!", Color.Red);
							else
								tPly.SendMessage("That team is locked, use \'/teamunlock green <password>\' to access it.", Color.Red);

						}
						break;
					#endregion

					#region Blue
					case 3:
						if (Config.LockBlueTeam && !tPly.Group.HasPermission(Config.BlueTeamPermission) && (ePly.BluePassword != Config.BlueTeamPassword || ePly.BluePassword == ""))
						{
							e.Handled = true;
							tPly.SetTeam(tPly.Team);
							if (Config.BlueTeamPassword == "")
								tPly.SendMessage("You do not have permission to join that team!", Color.Red);
							else
								tPly.SendMessage("That team is locked, use \'/teamunlock blue <password>\' to access it.", Color.Red);

						}
						break;
					#endregion

					#region Yellow
					case 4:
						if (Config.LockYellowTeam && !tPly.Group.HasPermission(Config.YellowTeamPermission) && (ePly.YellowPassword != Config.YellowTeamPassword || ePly.YellowPassword == ""))
						{
							e.Handled = true;
							tPly.SetTeam(tPly.Team);
							if (Config.YellowTeamPassword == "")
								tPly.SendMessage("You do not have permission to join that team!", Color.Red);
							else
								tPly.SendMessage("That team is locked, use \'/teamunlock yellow <password>\' to access it.", Color.Red);

						}
						break;
					#endregion
				}
			}
			catch (Exception ex)
			{
				Log.Error("[Essentials] Team Lock Exception:");
				Log.Error(ex.ToString());
			}
		}
		#endregion

		#region Send Bytes
		void OnSendBytes(SendBytesEventArgs args)
		{
			try
			{
				if (esPlayers[args.Socket.whoAmI].ptTime < 0.0) return;
				switch (args.Buffer[4])
				{
					case 7:
						Buffer.BlockCopy(BitConverter.GetBytes((int)esPlayers[args.Socket.whoAmI].ptTime), 0, args.Buffer, 5, 4);
						args.Buffer[9] = (byte)(esPlayers[args.Socket.whoAmI].ptDay ? 1 : 0);
						break;
					case 18:
						args.Buffer[5] = (byte)(esPlayers[args.Socket.whoAmI].ptDay ? 1 : 0);
						Buffer.BlockCopy(BitConverter.GetBytes((int)esPlayers[args.Socket.whoAmI].ptTime), 0, args.Buffer, 6, 4);
						break;
				}
			}
			catch { }
		}
		#endregion

		#region Timer
		public void OnUpdate(EventArgs args)
		{
			if ((DateTime.UtcNow - LastCheck).TotalMilliseconds >= 1000)
			{
				LastCheck = DateTime.UtcNow;
				try
				{
					lock (esPlayers)
					{
						foreach (var ePly in esPlayers)
						{
							if (ePly == null) continue;

							if (!ePly.SavedBackAction && ePly.TSPlayer.Dead)
							{
								if (ePly.TSPlayer.Group.HasPermission("essentials.back.death"))
								{
									ePly.LastBackX = ePly.TSPlayer.TileX;
									ePly.LastBackY = ePly.TSPlayer.TileY;
									ePly.LastBackAction = BackAction.Death;
									ePly.SavedBackAction = true;
									if (Config.ShowBackMessageOnDeath)
										ePly.TSPlayer.SendMessage("Type \"/b\" to return to your position before you died", Color.MediumSeaGreen);
								}
							}
							else if (ePly.SavedBackAction && !ePly.TSPlayer.Dead)
								ePly.SavedBackAction = false;

							if (ePly.ptTime > -1.0)
							{
								ePly.ptTime += 60.0;
								if (!ePly.ptDay && ePly.ptTime > 32400.0)
								{
									ePly.ptDay = true; ePly.ptTime = 0.0;
								}
								else if (ePly.ptDay && ePly.ptTime > 54000.0)
								{
									ePly.ptDay = false; ePly.ptTime = 0.0;
								}
							}

							if (ePly.Disabled && ((DateTime.UtcNow - ePly.LastDisabledCheck).TotalMilliseconds) > 3000)
							{
								ePly.LastDisabledCheck = DateTime.UtcNow;
								ePly.TSPlayer.Disable();
								if ((ePly.TSPlayer.TileX > ePly.DisabledX + 5 || ePly.TSPlayer.TileX < ePly.DisabledX - 5) || (ePly.TSPlayer.TileY > ePly.DisabledY + 5 || ePly.TSPlayer.TileY < ePly.DisabledY - 5))
								{
									ePly.TSPlayer.Teleport(ePly.DisabledX * 16F, ePly.DisabledY * 16F);
								}
							}
						}
					}
				}
				catch { }
			}
		}
		#endregion

		/* Commands: */

		#region More
		private void CMDmore(CommandArgs args)
		{
			if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "all")
			{
				bool full = true;
				int i = 0;
				foreach (Item item in args.TPlayer.inventory)
				{
					if (item == null || item.stack == 0) continue;
					int amtToAdd = item.maxStack - item.stack;
					if (item.stack > 0 && amtToAdd > 0 && !item.name.ToLower().Contains("coin"))
					{
						full = false;
						args.Player.GiveItem(item.type, item.name, item.width, item.height, amtToAdd);
					}
					i++;
				}
				if (!full)
					args.Player.SendMessage("Filled all your items!", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Your inventory is already full!", Color.OrangeRed);
			}
			else
			{
				Item holding = args.Player.TPlayer.inventory[args.TPlayer.selectedItem];
				int amtToAdd = holding.maxStack - holding.stack;
				if (holding.stack > 0 && amtToAdd > 0)
					args.Player.GiveItem(holding.type, holding.name, holding.width, holding.height, amtToAdd);
				if (amtToAdd == 0)
					args.Player.SendMessage(string.Format("You're {0} is already full!", holding.name), Color.OrangeRed);
				else
					args.Player.SendMessage(string.Format("Filled up you're {0}!", holding.name), Color.MediumSeaGreen);
			}
		}
		#endregion

		#region Position Commands
		private void CMDpos(CommandArgs args)
		{
			if (args.Player.Group.HasPermission("essentials.position.getother") && args.Parameters.Count > 0)
			{
				var players = TShock.Utils.FindPlayer(string.Join(" ", args.Parameters));
				if (players.Count == 1)
				{
					var play = players[0];
					args.Player.SendMessage(string.Format("Position for {0}:", play.Name), Color.MediumSeaGreen);
					args.Player.SendMessage(string.Format("X Position: {0} - Y Position: {1}", play.TileX, play.TileY), Color.MediumSeaGreen);
				}
				else
				{
					if (players.Count < 1)
						args.Player.SendMessage("No players matched!", Color.OrangeRed);
					else
						args.Player.SendMessage("More than one player matched!", Color.OrangeRed);
				}
				return;
			}
			args.Player.SendMessage(string.Format("X Position: {0} - Y Position: {1}", args.Player.TileX, args.Player.TileY), Color.MediumSeaGreen);
		}

		private void CMDtppos(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendMessage("Usage: /tppos <X> [Y]", Color.OrangeRed);
				return;
			}

			int X = 0, Y = 0;
			if (!int.TryParse(args.Parameters[0], out X) || (args.Parameters.Count == 2 && !int.TryParse(args.Parameters[1], out Y)))
			{
				args.Player.SendMessage("Usage: /tppos <X> [Y]", Color.OrangeRed);
				return;
			}

			if (args.Parameters.Count == 1)
				Y = esUtils.GetTop(X);

			var ePly = esPlayers[args.Player.Index];
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}

			if (args.Player.Teleport(X * 16F, Y * 16F))
				args.Player.SendMessage(string.Format("Teleported you to X: {0} - Y: {1}", X, Y), Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
		}

		private void CMDruler(CommandArgs args)
		{
			int choice = 0;

			if (args.Parameters.Count == 1 &&
				int.TryParse(args.Parameters[0], out choice) &&
				choice >= 1 && choice <= 2)
			{
				args.Player.SendMessage("Hit a block to Set Point " + choice, Color.Yellow);
				args.Player.AwaitingTempPoint = choice;
			}
			else
			{
				if (args.Player.TempPoints[0] == Point.Zero || args.Player.TempPoints[1] == Point.Zero)
					args.Player.SendMessage("Invalid Points! To set points use: /ruler [1/2]", Color.OrangeRed);
				else
				{
					var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
					var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);
					args.Player.SendMessage(string.Format("Area Height: {0} Width: {1}", height, width), Color.MediumSeaGreen);
					args.Player.TempPoints[0] = Point.Zero; args.Player.TempPoints[1] = Point.Zero;
				}
			}
		}
		#endregion

		#region HelpOp
		private void CMDhelpop(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /helpop <message>", Color.OrangeRed);
				return;
			}

			string text = string.Join(" ", args.Parameters);

			List<string> online = new List<string>();

			foreach (var ePly in esPlayers)
			{
				if (ePly == null || !ePly.TSPlayer.Group.HasPermission("essentials.helpop.receive")) continue;
				online.Add(ePly.TSPlayer.Name);
				ePly.TSPlayer.SendMessage(string.Format("[HelpOp] {0}: {1}", args.Player.Name, text), Color.RoyalBlue);
			}
			if (online.Count < 1)
				args.Player.SendMessage("[HelpOp] There are no operators online to receive your message!", Color.RoyalBlue);
			else
			{
				string to = string.Join(", ", online);
				args.Player.SendMessage(string.Format("[HelpOp] Your message has been received by the operator(s): {0}", to), Color.RoyalBlue);
			}
		}
		#endregion

		#region Suicide
		private void CMDsuicide(CommandArgs args)
		{
			if (!args.Player.RealPlayer)
				return;

			NetMessage.SendData(26, -1, -1, " decided it wasnt worth living.", args.Player.Index, 0, short.MaxValue, 0F);
		}
		#endregion

		#region Burn
		private void CMDburn(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendMessage("Usage: /burn <player> [time]", Color.OrangeRed);
				return;
			}

			int duration = 1800;
			if (args.Parameters.Count == 2 && !int.TryParse(args.Parameters[1], out duration))
				duration = 1800;

			var player = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (player.Count == 0)
				args.Player.SendMessage("No players matched!!", Color.OrangeRed);
			else if (player.Count > 1)
				args.Player.SendMessage("More than one player matched!", Color.OrangeRed);
			else
			{
				player[0].SetBuff(24, duration);
				args.Player.SendMessage(player[0].Name + " Has been set on fire! for " + (duration / 60) + " seconds", Color.MediumSeaGreen);
			}
		}
		#endregion

		#region KickAll
		private void CMDkickall(CommandArgs args)
		{
			string Reason = string.Empty;
			if (args.Parameters.Count > 0)
				Reason = " (" + string.Join(" ", args.Parameters) + ")";

			foreach (var ePly in esPlayers)
			{
				if (ePly == null || ePly.TSPlayer.Group.HasPermission("essentials.kickall.immune")) continue;
				ePly.TSPlayer.Disconnect(string.Format("Everyone has been kicked{0}", Reason));
			}
			TShock.Utils.Broadcast("Everyone has been kicked from the server!", Color.MediumSeaGreen);
		}
		#endregion

		#region Moon
		private void CMDmoon(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /moon [ new | 1/4 | half | 3/4 | full ]", Color.OrangeRed);
				return;
			}

			string subcmd = args.Parameters[0].ToLower();

			switch (subcmd)
			{
				case "new":
					Main.moonPhase = 4;
					break;
				case "1/4":
					Main.moonPhase = 3;
					break;
				case "half":
					Main.moonPhase = 2;
					break;
				case "3/4":
					Main.moonPhase = 1;
					break;
				case "full":
					Main.moonPhase = 0;
					break;
				default:
					args.Player.SendMessage("Usage: /moon [ new | 1/4 | half | 3/4 | full ]", Color.OrangeRed);
					break;
			}
			args.Player.SendMessage(string.Format("Moon Phase set to {0} Moon, This takes a while to update!", subcmd), Color.MediumSeaGreen);
		}
		#endregion

		#region Back
		private void CMDback(CommandArgs args)
		{
			var ePly = esPlayers[args.Player.Index];

			if (ePly.LastBackAction == BackAction.None)
				args.Player.SendMessage("You do not have a /b position stored", Color.OrangeRed);
			else if (ePly.LastBackAction == BackAction.TP)
			{
				if (args.Player.Teleport(ePly.LastBackX * 16F, ePly.LastBackY * 16F))
					args.Player.SendMessage("Moved you to your position before you last teleported", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
			}
			else if (ePly.LastBackAction == BackAction.Death && args.Player.Group.HasPermission("essentials.back.death"))
			{
				if (args.Player.Teleport(ePly.LastBackX * 16F, ePly.LastBackY * 16F))
					args.Player.SendMessage("Moved you to your position before you died!", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
			}
			else
				args.Player.SendMessage("You do not have permission to /b after death", Color.MediumSeaGreen);
		}
		#endregion

		#region Seach IDs
		private void CMDspage(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendMessage("Usage: /spage <page>", Color.OrangeRed);
				return;
			}

			int Page = 1;
			if (!int.TryParse(args.Parameters[0], out Page))
			{
				args.Player.SendMessage(string.Format("Specified page ({0}) invalid!", args.Parameters[0]), Color.OrangeRed);
				return;
			}

			var ePly = esPlayers[args.Player.Index];

			esUtils.DisplaySearchResults(args.Player, ePly.LastSearchResults, Page);
		}

		private void CMDsitems(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /sitem <search term>", Color.OrangeRed);
				return;
			}

			string Search = string.Join(" ", args.Parameters);
			List<object> Results = esUtils.ItemIdSearch(Search);

			if (Results.Count < 1)
			{
				args.Player.SendMessage("Could not find any matching Items!", Color.OrangeRed);
				return;
			}

			esPlayer ePly = esPlayers[args.Player.Index];

			ePly.LastSearchResults = Results;
			esUtils.DisplaySearchResults(args.Player, Results, 1);
		}

		private void CMDsnpcs(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /snpc <search term>", Color.OrangeRed);
				return;
			}

			string Search = string.Join(" ", args.Parameters);
			List<object> Results = esUtils.NPCIdSearch(Search);

			if (Results.Count < 1)
			{
				args.Player.SendMessage("Could not find any matching NPCs !", Color.OrangeRed);
				return;
			}

			esPlayer ePly = esPlayers[args.Player.Index];

			ePly.LastSearchResults = Results;
			esUtils.DisplaySearchResults(args.Player, Results, 1);
		}
		#endregion

		#region MyHome
		private void CMDsethome(CommandArgs args)
		{
			/* Chek if the player is a real player */
			if (!args.Player.RealPlayer)
			{
				args.Player.SendMessage("You must be a real player!", Color.OrangeRed);
				return;
			}
			/* Make sure the player is logged in */
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendMessage("You must be logged in to do that!", Color.OrangeRed);
				return;
			}
			/* Make sure the player isn't in a SetHome Disabled region */
			if (!args.Player.Group.HasPermission("essentials.home.bypassdisabled"))
			{
				foreach (string r in Config.DisableSetHomeInRegions)
				{
					var region = TShock.Regions.GetRegionByName(r);
					if (region == null) continue;
					if (region.InArea(args.Player.TileX, args.Player.TileY))
					{
						args.Player.SendMessage("You cannot set your home in this region!", Color.OrangeRed);
						return;
					}
				}
			}

			/* get a list of the player's homes */
			List<string> homes = esSQL.GetNames(args.Player.UserID, Main.worldID);
			/* how many homes is the user allowed to set */
			int CanSet = esUtils.NoOfHomesCanSet(args.Player);

			if (homes.Count < 1)
			{
				/* the player doesn't have a home, Create one! */
				if (args.Parameters.Count < 1 || (args.Parameters.Count > 0 && CanSet == 1))
				{
					/* they dont specify a name OR they specify a name but they only have permission to set 1, use a default name */
					if (esSQL.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, "1", Main.worldID))
						args.Player.SendMessage("Set home!", Color.MediumSeaGreen);
					else
						args.Player.SendMessage("An error occurred while setting your home!", Color.OrangeRed);
				}
				else if (args.Parameters.Count == 1 && !args.Parameters[0].Contains(" ") && (CanSet == -1 || CanSet > 1))
				{
					/* they specify a name And have permission to specify more than 1 */
					string name = args.Parameters[0].ToLower();
					if (esSQL.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, name, Main.worldID))
						args.Player.SendMessage(string.Format("Set home {0}!", name), Color.MediumSeaGreen);
					else
						args.Player.SendMessage("An error occurred while setting your home!", Color.OrangeRed);
				}
				else
				{
					/* homes cant have more than 1 word */
					args.Player.SendMessage("Error: Homes cannot contain spaces!", Color.OrangeRed);
				}
			}
			else if (homes.Count == 1)
			{
				/* If they only have 1 home */
				if (args.Parameters.Count == 1 && !args.Parameters[0].Contains(" ") && (1 < CanSet || CanSet == -1))
				{
					/* They Specify a name and can set more than 1  */
					string name = args.Parameters[0].ToLower();
					if (homes.Contains(name))
					{
						/* They want to update a home */
						if (esSQL.UpdateHome(args.Player.TileX, args.Player.TileY, args.Player.UserID, name, Main.worldID))
							args.Player.SendMessage(string.Format("Updated home {0}!", name), Color.MediumSeaGreen);
						else
							args.Player.SendMessage("An error occurred while updating your home!", Color.OrangeRed);
					}
					else
					{
						/* They want to add a new home */
						if (esSQL.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, name, Main.worldID))
							args.Player.SendMessage(string.Format("Set home {0}!", name), Color.MediumSeaGreen);
						else
							args.Player.SendMessage("An error occurred while setting your home!", Color.OrangeRed);
					}
				}
				else if (args.Parameters.Count < 1 && (1 < CanSet || CanSet == -1))
				{
					/* if they dont specify a name & can set more than 1  - add a new home*/
					if (esSQL.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, esUtils.NextHome(homes), Main.worldID))
						args.Player.SendMessage("Set home!", Color.MediumSeaGreen);
					else
						args.Player.SendMessage("An error occurred while setting your home!", Color.OrangeRed);
				}
				else if (args.Parameters.Count > 0 && CanSet == 1)
				{
					/* They specify a name but can only set 1 home, update their current home */
					if (esSQL.UpdateHome(args.Player.TileX, args.Player.TileY, args.Player.UserID, homes[0], Main.worldID))
						args.Player.SendMessage("Updated home!", Color.MediumSeaGreen);
					else
						args.Player.SendMessage("An error occurred while updating your home!", Color.OrangeRed);
				}
				else
				{
					/* homes cant have more than 1 word */
					args.Player.SendMessage("Error: Homes cannot contain spaces!", Color.OrangeRed);
				}
			}
			else
			{
				/* If they have more than 1 home */
				if (args.Parameters.Count < 1)
				{
					/* they didnt specify a name */
					if (homes.Count < CanSet || CanSet == -1)
					{
						/* They can set more homes */
						if (esSQL.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, esUtils.NextHome(homes), Main.worldID))
							args.Player.SendMessage("Set home!", Color.MediumSeaGreen);
						else
							args.Player.SendMessage("An error occurred while setting your home!", Color.OrangeRed);
					}
					else
					{
						/* they cant set any more homes */
						args.Player.SendMessage(string.Format("You are only allowed to set {0} homes", CanSet.ToString()), Color.OrangeRed);
						args.Player.SendMessage(string.Format("Homes: {0}", string.Join(", ", homes)), Color.OrangeRed);
					}
				}
				else if (args.Parameters.Count == 1 && !args.Parameters[0].Contains(" "))
				{
					/* they want to set / update another home and specified a name */
					string name = args.Parameters[0].ToLower();
					if (homes.Contains(name))
					{
						/* they want to update a home */
						if (esSQL.UpdateHome(args.Player.TileX, args.Player.TileY, args.Player.UserID, name, Main.worldID))
							args.Player.SendMessage("Updated home!", Color.MediumSeaGreen);
						else
							args.Player.SendMessage("An error occurred while updating your home!", Color.OrangeRed);
					}
					else
					{
						/* they want to add a new home */
						if (homes.Count < CanSet || CanSet == -1)
						{
							/* they can set more homes */
							if (esSQL.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, name, Main.worldID))
								args.Player.SendMessage(string.Format("Set home {0}!", name), Color.MediumSeaGreen);
							else
								args.Player.SendMessage("An error occurred while setting your home!", Color.OrangeRed);
						}
						else
						{
							/* they cant set any more homes */
							args.Player.SendMessage(string.Format("You are only allowed to set {0} homes", CanSet.ToString()), Color.OrangeRed);
							args.Player.SendMessage(string.Format("Homes: {0}", string.Join(", ", homes)), Color.OrangeRed);
						}
					}
				}
				else
				{
					/* homes cant have more than 1 word */
					args.Player.SendMessage("Error: Homes cannot contain spaces!", Color.OrangeRed);
				}
			}
		}

		private void CMDmyhome(CommandArgs args)
		{
			/* Chek if the player is a real player */
			if (!args.Player.RealPlayer)
			{
				args.Player.SendMessage("You must be a real player!", Color.OrangeRed);
				return;
			}
			/* Make sure the player is logged in */
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendMessage("You must be logged in to do that!", Color.OrangeRed);
				return;
			}

			/* get a list of the player's homes */
			List<string> homes = esSQL.GetNames(args.Player.UserID, Main.worldID);
			/* Set home position variable */
			Point homePos = Point.Zero;

			if (homes.Count < 1)
			{
				/* they do not have a home */
				args.Player.SendMessage("You have not set a home. type /sethome to set one.", Color.OrangeRed);
				return;
			}
			else if (homes.Count == 1)
			{
				/* they have 1 home */
				homePos = esSQL.GetHome(args.Player.UserID, homes[0], Main.worldID);
			}
			else if (homes.Count > 1)
			{
				/* they have more than 1 home */
				if (args.Parameters.Count < 1)
				{
					/* they didnt specify the name */
					args.Player.SendMessage("Usage: /myhome <home>", Color.OrangeRed);
					args.Player.SendMessage(string.Format("Homes: {0}", string.Join(", ", homes)), Color.OrangeRed);
					return;
				}
				else if (args.Parameters.Count == 1 && !args.Parameters[0].Contains(" "))
				{
					string name = args.Parameters[0].ToLower();
					if (homes.Contains(name))
					{
						homePos = esSQL.GetHome(args.Player.UserID, name, Main.worldID);
					}
					else
					{
						/* could not find the name */
						args.Player.SendMessage("Usage: /myhome <home>", Color.OrangeRed);
						args.Player.SendMessage(string.Format("Homes: {0}", string.Join(", ", homes)), Color.OrangeRed);
						return;
					}
				}
				else
				{
					/* could not find the name */
					args.Player.SendMessage("Usage: /myhome <home>", Color.OrangeRed);
					args.Player.SendMessage(string.Format("Homes: {0}", string.Join(", ", homes)), Color.OrangeRed);
					return;
				}
			}

			/* teleport home */
			if (homePos == Point.Zero)
			{
				args.Player.SendMessage("There is an error with your home!", Color.OrangeRed);
				return;
			}

			esPlayer ePly = esPlayers[args.Player.Index];
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}

			if (args.Player.Teleport(homePos.X * 16F, homePos.Y * 16F))
				args.Player.SendMessage("Teleported home!", Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport failed!", Color.OrangeRed);
		}

		private void CMDdelhome(CommandArgs args)
		{
			/* Chek if the player is a real player */
			if (!args.Player.RealPlayer)
			{
				args.Player.SendMessage("You must be a real player!", Color.OrangeRed);
				return;
			}
			/* Make sure the player is logged in */
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendMessage("You must be logged in to do that!", Color.OrangeRed);
				return;
			}

			/* get a list of the player's homes */
			List<string> homes = esSQL.GetNames(args.Player.UserID, Main.worldID);

			if (homes.Count < 1)
			{
				/* they do not have a home */
				args.Player.SendMessage("You have not set a home. type /sethome to set one.", Color.OrangeRed);
			}
			else if (homes.Count == 1)
			{
				/* they have 1 home */
				if (esSQL.RemoveHome(args.Player.UserID, homes[0], Main.worldID))
					args.Player.SendMessage("Removed home!", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("An error occurred while removing your home!", Color.OrangeRed);
			}
			else if (homes.Count > 1)
			{
				/* they have more than 1 home */
				if (args.Parameters.Count < 1)
				{
					/* they didnt specify the name */
					args.Player.SendMessage("Usage: /delhome <home>", Color.OrangeRed);
					args.Player.SendMessage(string.Format("Homes: {0}", string.Join(", ", homes)), Color.OrangeRed);
				}
				else if (args.Parameters.Count == 1 && !args.Parameters[0].Contains(" "))
				{
					string name = args.Parameters[0].ToLower();
					if (homes.Contains(name))
					{
						if (esSQL.RemoveHome(args.Player.UserID, name, Main.worldID))
							args.Player.SendMessage(string.Format("Removed home {0}!", name), Color.MediumSeaGreen);
						else
							args.Player.SendMessage("An error occurred while removing your home!", Color.OrangeRed);
					}
					else
					{
						/* could not find the name */
						args.Player.SendMessage("Usage: /delhome <home>", Color.OrangeRed);
						args.Player.SendMessage(string.Format("Homes: {0}", string.Join(", ", homes)), Color.OrangeRed);
					}
				}
				else
				{
					/* could not find the name */
					args.Player.SendMessage("Usage: /delhome <home>", Color.OrangeRed);
					args.Player.SendMessage(string.Format("Homes: {0}", string.Join(", ", homes)), Color.OrangeRed);
				}
			}
		}
		#endregion

		#region essentials
		private void CMDessentials(CommandArgs args)
		{
			esConfig.ReloadConfig(args);
		}
		#endregion

		#region Team Unlock
		private void CMDteamunlock(CommandArgs args)
		{
			if (!Config.LockRedTeam && !Config.LockGreenTeam && !Config.LockBlueTeam && !Config.LockYellowTeam)
			{
				args.Player.SendMessage("Teams are not locked!", Color.OrangeRed);
				return;
			}

			if (args.Parameters.Count < 2)
			{
				args.Player.SendMessage("Usage: /teamunlock <color> <password>", Color.OrangeRed);
				return;
			}

			string team = args.Parameters[0].ToLower();

			args.Parameters.RemoveAt(0);
			string Password = string.Join(" ", args.Parameters);

			var ePly = esPlayers[args.Player.Index];

			switch (team)
			{
				case "red":
					{
						if (Config.LockRedTeam)
						{
							if (Password == Config.RedTeamPassword && Config.RedTeamPassword != "")
							{
								args.Player.SendMessage("You can now join red team!", Color.MediumSeaGreen);
								ePly.RedPassword = Password;
							}
							else
								args.Player.SendMessage("Incorrect Password!", Color.OrangeRed);
						}
						else
							args.Player.SendMessage("The red team isn't locked!", Color.OrangeRed);
					}
					break;
				case "green":
					{
						if (Config.LockGreenTeam)
						{
							if (Password == Config.GreenTeamPassword && Config.GreenTeamPassword != "")
							{
								args.Player.SendMessage("You can now join green team!", Color.MediumSeaGreen);
								ePly.GreenPassword = Password;
							}
							else
								args.Player.SendMessage("Incorrect Password!", Color.OrangeRed);
						}
						else
							args.Player.SendMessage("The green team isn't locked!", Color.OrangeRed);
					}
					break;
				case "blue":
					{
						if (Config.LockBlueTeam)
						{
							if (Password == Config.BlueTeamPassword && Config.BlueTeamPassword != "")
							{
								args.Player.SendMessage("You can now join blue team!", Color.MediumSeaGreen);
								ePly.BluePassword = Password;
							}
							else
								args.Player.SendMessage("Incorrect Password!", Color.OrangeRed);
						}
						else
							args.Player.SendMessage("The blue team isn't lock!", Color.OrangeRed);
					}
					break;
				case "yellow":
					{
						if (Config.LockYellowTeam)
						{
							if (Password == Config.YellowTeamPassword && Config.YellowTeamPassword != "")
							{
								args.Player.SendMessage("You can now join yellow team!", Color.MediumSeaGreen);
								ePly.YellowPassword = Password;
							}
							else
								args.Player.SendMessage("Incorrect Password!", Color.OrangeRed);
						}
						else
							args.Player.SendMessage("The yellow team isn't locked!", Color.OrangeRed);
					}
					break;
				default:
					args.Player.SendMessage("Usage: /teamunlock <red/green/blue/yellow> <password>", Color.OrangeRed);
					break;
			}
		}
		#endregion

		#region Last Command
		private void CMDequals(CommandArgs args)
		{
			var ePly = esPlayers[args.Player.Index];

			if (ePly.LastCMD == "/=" || ePly.LastCMD.StartsWith("/= "))
			{
				args.Player.SendMessage("Error with last command!", Color.OrangeRed);
				return;
			}

			if (ePly.LastCMD == string.Empty)
				args.Player.SendMessage("You have not entered a command yet.", Color.OrangeRed);
			else
				TShockAPI.Commands.HandleCommand(args.Player, ePly.LastCMD);
		}
		#endregion

		#region Kill Reason
		private void CMDkillr(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /killr <player> <reason>", Color.OrangeRed);
				return;
			}

			var PlayersFound = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (PlayersFound.Count != 1)
			{
				args.Player.SendWarningMessage(PlayersFound.Count < 1 ? "No players matched!" : "More than one player matched!");
				return;
			}

			var Ply = PlayersFound[0];
			args.Parameters.RemoveAt(0); //remove player name
			string reason = " " + string.Join(" ", args.Parameters);

			NetMessage.SendData(26, -1, -1, reason, Ply.Index, 0, short.MaxValue, 0F);
			args.Player.SendMessage(string.Format("You just killed {0}!", Ply.Name), Color.MediumSeaGreen);
		}
		#endregion

		#region Disable
		private void CMDdisable(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /disable <player/-list> [reason]", Color.OrangeRed);
				return;
			}

			if (args.Parameters[0] == "-list")
			{
				List<string> Names = new List<string>(Disabled.Keys);
				if (Disabled.Count < 1)
					args.Player.SendMessage("There are currently no players disabled!", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Disabled Players: " + string.Join(", ", Names), Color.MediumSeaGreen);
				return;
			}


			var PlayersFound = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (PlayersFound.Count < 1)
			{
				foreach (var pair in Disabled)
				{
					if (pair.Key.ToLower().Contains(args.Parameters[0].ToLower()))
					{
						Disabled.Remove(pair.Key);
						args.Player.SendMessage(string.Format("{0} is no longer disabled (even though he isn't online)", pair.Key), Color.MediumSeaGreen);
						return;
					}
				}
				args.Player.SendMessage("No players matched!", Color.OrangeRed);
			}
			else if (PlayersFound.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.OrangeRed);
				return;
			}

			var tPly = PlayersFound[0];
			var ePly = esPlayers[tPly.Index];

			if (!Disabled.ContainsKey(tPly.Name))
			{
				string Reason = string.Empty;
				if (args.Parameters.Count > 1)
				{
					args.Parameters.RemoveAt(0);
					Reason = string.Join(" ", args.Parameters);
				}
				ePly.DisabledX = tPly.TileX;
				ePly.DisabledY = tPly.TileY;
				ePly.Disabled = true;
				ePly.Disable();
				ePly.LastDisabledCheck = DateTime.UtcNow;
				int[] pos = new int[2];
				pos[0] = ePly.DisabledX;
				pos[1] = ePly.DisabledY;
				Disabled.Add(tPly.Name, pos);
				args.Player.SendMessage(string.Format("You disabled {0}, He can not be enabled until you type \"/disable {0}\"", tPly.Name), Color.MediumSeaGreen);
				if (Reason == string.Empty)
					tPly.SendMessage(string.Format("You have been disabled by {0}", args.Player.Name), Color.MediumSeaGreen);
				else
					tPly.SendMessage(string.Format("You have been disabled by {0} for {1}", args.Player.Name, Reason), Color.MediumSeaGreen);
			}
			else
			{
				ePly.Disabled = false;
				ePly.DisabledX = -1;
				ePly.DisabledY = -1;

				Disabled.Remove(tPly.Name);

				args.Player.SendMessage(string.Format("{0} is no longer disabled!", tPly.Name), Color.MediumSeaGreen);
				tPly.SendMessage("You are no longer disabled!", Color.MediumSeaGreen);
			}
		}
		#endregion

		#region Top, Up and Down
		private void CMDtop(CommandArgs args)
		{
			int Y = esUtils.GetTop(args.Player.TileX);
			if (Y == -1)
			{
				args.Player.SendMessage("You are already on the top!", Color.OrangeRed);
				return;
			}
			esPlayer ePly = esPlayers[args.Player.Index];
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}
			if (args.Player.Teleport(args.Player.TileX * 16F, Y * 16F))
				args.Player.SendMessage("Teleported to top!", Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
		}
		private void CMDup(CommandArgs args)
		{
			int levels = 1;
			if (args.Parameters.Count > 0 && !int.TryParse(args.Parameters[0], out levels))
			{
				args.Player.SendMessage("Usage: /up [No. levels]", Color.OrangeRed);
				return;
			}

			int Y = esUtils.GetUp(args.Player.TileX, args.Player.TileY);
			if (Y == -1)
			{
				args.Player.SendMessage("You are already on the top!", Color.OrangeRed);
				return;
			}
			bool limit = false;
			for (int i = 1; i < levels; i++)
			{
				int newY = esUtils.GetUp(args.Player.TileX, Y);
				if (newY == -1)
				{
					levels = i;
					limit = true;
					break;
				}
				Y = newY;
			}

			esPlayer ePly = esPlayers[args.Player.Index];
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}
			if (args.Player.Teleport(args.Player.TileX * 16F, Y * 16F))
				args.Player.SendMessage(string.Format("Teleported you up {0} level(s)!{1}", levels, limit ? " You cant go up any further!" : string.Empty), Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
		}
		private void CMDdown(CommandArgs args)
		{
			int levels = 1;
			if (args.Parameters.Count > 0 && !int.TryParse(args.Parameters[0], out levels))
			{
				args.Player.SendMessage("Usage: /down [No. levels]", Color.OrangeRed);
				return;
			}

			int Y = esUtils.GetDown(args.Player.TileX, args.Player.TileY);
			if (Y == -1)
			{
				args.Player.SendMessage("You are already on the bottom!", Color.OrangeRed);
				return;
			}
			bool limit = false;
			for (int i = 1; i < levels; i++)
			{
				int newY = esUtils.GetDown(args.Player.TileX, Y);
				if (newY == -1)
				{
					levels = i;
					limit = true;
					break;
				}
				Y = newY;
			}

			esPlayer ePly = esPlayers[args.Player.Index];
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}
			if (args.Player.Teleport(args.Player.TileX * 16F, Y * 16F))
				args.Player.SendMessage(string.Format("Teleported you down {0} level(s)!{1}", levels, limit ? " You can't go down any further!" : string.Empty), Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
		}
		#endregion

		#region Left & Right
		private void CMDleft(CommandArgs args)
		{
			int levels = 1;
			if (args.Parameters.Count > 0 && !int.TryParse(args.Parameters[0], out levels))
			{
				args.Player.SendMessage("Usage: /left [No. times]", Color.OrangeRed);
				return;
			}

			int X = esUtils.GetLeft(args.Player.TileX, args.Player.TileY);
			if (X == -1)
			{
				args.Player.SendMessage("You cannot go any further left!", Color.OrangeRed);
				return;
			}
			bool limit = false;
			for (int i = 1; i < levels; i++)
			{
				int newX = esUtils.GetLeft(X, args.Player.TileY);
				if (newX == -1)
				{
					levels = i;
					limit = true;
					break;
				}
				X = newX;
			}

			esPlayer ePly = esPlayers[args.Player.Index];
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}
			if (args.Player.Teleport(X * 16F, args.Player.TileY * 16F))
				args.Player.SendMessage(string.Concat("Teleported you to the left", levels != 1 ? " " + levels.ToString() + " times!" : "!", limit ? " You can't go any further!" : string.Empty), Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
		}
		private void CMDright(CommandArgs args)
		{
			int levels = 1;
			if (args.Parameters.Count > 0 && !int.TryParse(args.Parameters[0], out levels))
			{
				args.Player.SendMessage("Usage: /right [No. times]", Color.OrangeRed);
				return;
			}

			int X = esUtils.GetRight(args.Player.TileX, args.Player.TileY);
			if (X == -1)
			{
				args.Player.SendMessage("You cannot go any further right!", Color.OrangeRed);
				return;
			}
			bool limit = false;
			for (int i = 1; i < levels; i++)
			{
				int newX = esUtils.GetRight(X, args.Player.TileY);
				if (newX == -1)
				{
					levels = i;
					limit = true;
					break;
				}
				X = newX;
			}

			esPlayer ePly = esPlayers[args.Player.Index];
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}
			if (args.Player.Teleport(X * 16F, args.Player.TileY * 16F))
				args.Player.SendMessage(string.Concat("Teleported you to the right", levels != 1 ? " " + levels.ToString() + " times!" : "!", limit ? " You can't go any further!" : string.Empty), Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
		}
		#endregion

		#region ptime
		private void CMDptime(CommandArgs args)
		{
			if (!args.Player.Group.HasPermission("essentials.playertime.setother") && args.Parameters.Count != 1)
			{
				args.Player.SendMessage("Usage: /ptime <day/night/noon/midnight/reset>", Color.OrangeRed);
				return;
			}
			else if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendMessage("Usage: /ptime <day/night/noon/midnight/reset> [player]", Color.OrangeRed);
				return;
			}

			var Ply = args.Player;
			if (args.Player.Group.HasPermission("essentials.playertime.setother") && args.Parameters.Count == 2)
			{
				var PlayersFound = TShock.Utils.FindPlayer(args.Parameters[1]);
				if (PlayersFound.Count != -1)
				{
					args.Player.SendWarningMessage(PlayersFound.Count < 1 ? "No players matched!" : "More than one player matched!");
					return;
				}
				Ply = PlayersFound[0];
			}

			esPlayer ePly = esPlayers[Ply.Index];
			string Time = args.Parameters[0].ToLower();
			switch (Time)
			{
				case "day":
					{
						ePly.ptDay = true;
						ePly.ptTime = 150.0;
						Ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
					}
					break;
				case "night":
					{
						ePly.ptDay = false;
						ePly.ptTime = 0.0;
						Ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
					}
					break;
				case "noon":
					{
						ePly.ptDay = true;
						ePly.ptTime = 27000.0;
						Ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
					}
					break;
				case "midnight":
					{
						ePly.ptDay = false;
						ePly.ptTime = 16200.0;
						Ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
					}
					break;
				case "reset":
					{
						ePly.ptTime = -1.0;
						Ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
						args.Player.SendMessage(string.Format("{0} time is the same as the server!", Ply == args.Player ? "Your" : Ply.Name + "'s"), Color.MediumSeaGreen);
						if (Ply != args.Player)
							Ply.SendMessage(string.Format("{0} Set your time to the server time!", args.Player.Name), Color.MediumSeaGreen);
					}
					return;
				default:
					args.Player.SendMessage("Usage: /ptime <day/night/dusk/noon/midnight/reset> [player]", Color.OrangeRed);
					return;
			}
			args.Player.SendMessage(string.Format("Set {0} time to {1}!", args.Player == Ply ? "your" : Ply.Name + "'s", Time), Color.MediumSeaGreen);
			if (Ply != args.Player)
				Ply.SendMessage(string.Format("{0} set your time to {1}!", args.Player.Name, Time), Color.MediumSeaGreen);
		}
		#endregion

		#region Ping
		private void CMDping(CommandArgs args)
		{
			args.Player.SendMessage("pong!", Color.White);
		}
		#endregion

		#region sudo
		private void CMDsudo(CommandArgs args)
		{
			if (args.Parameters.Count < 2)
			{
				args.Player.SendMessage("Usage: /sudo <player> <command>", Color.OrangeRed);
				return;
			}

			var PlayersFound = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (PlayersFound.Count != 1)
			{
				args.Player.SendWarningMessage(PlayersFound.Count < 1 ? "No Players matched!" : "More than one player matched!");
				return;
			}

			var Ply = PlayersFound[0];
			if (Ply.Group.HasPermission("essentials.sudo.immune"))
			{
				args.Player.SendMessage("You cannot force that player to do a command!", Color.OrangeRed);
				return;
			}
			Group OldGroup = null;
			if (args.Player.Group.HasPermission("essentials.sudo.super"))
			{
				OldGroup = Ply.Group;
				Ply.Group = new SuperAdminGroup();
			}

			args.Parameters.RemoveAt(0);
			string command = string.Join(" ", args.Parameters);
			if (!command.StartsWith("/"))
				command = string.Concat("/", command);

			Commands.HandleCommand(Ply, command);
			args.Player.SendMessage(string.Format("Forced {0} to execute: {1}", Ply.Name, command), Color.MediumSeaGreen);

			if (OldGroup != null)
				Ply.Group = OldGroup;
		}
		#endregion

		#region SocialSpy
		private void CMDsocialspy(CommandArgs args)
		{
			esPlayer ePly = esPlayers[args.Player.Index];

			ePly.SocialSpy = !ePly.SocialSpy;
			args.Player.SendMessage(string.Format("Socialspy {0}abled", (ePly.SocialSpy ? "En" : "Dis")), Color.MediumSeaGreen);
		}
		#endregion

		#region Near
		private void CMDnear(CommandArgs args)
		{
			var Players = new Dictionary<string, int>();
			foreach (var ePly in esPlayers)
			{
				if (ePly == null || ePly.Index == args.Player.Index) continue;
				int x = Math.Abs(args.Player.TileX - ePly.TSPlayer.TileX);
				int y = Math.Abs(args.Player.TileY - ePly.TSPlayer.TileY);
				int h = (int)Math.Sqrt((double)(Math.Pow(x, 2) + Math.Pow(y, 2)));
				Players.Add(ePly.TSPlayer.Name, h);
			}
			if (Players.Count == 0)
			{
				args.Player.SendMessage("No players found!", Color.MediumSeaGreen);
				return;
			}
			List<string> Names = new List<string>();
			Players.OrderBy(pair => pair.Value).ForEach(pair => Names.Add(pair.Key));
			List<string> Results = new List<string>();
			var Line = new StringBuilder();
			int Added = 0;
			for (int i = 0; i < Names.Count; i++)
			{
				if (Line.Length == 0)
					Line.Append(string.Format("{0}({1}m)", Names[i], Players[Names[i]]));
				else
					Line.Append(string.Format(", {0}({1}m)", Names[i], Players[Names[i]]));
				Added++;
				if (Added == 5)
				{
					Results.Add(Line.ToString());
					Line.Clear();
					Added = 0;
				}
			}
			if (Results.Count <= 6)
			{
				args.Player.SendInfoMessage("Nearby Players:");
				foreach (var Result in Results)
				{
					args.Player.SendMessage(Result, Color.MediumSeaGreen);
				}
			}
			else
			{
				int page = 1;
				if (args.Parameters.Count > 0 && !int.TryParse(args.Parameters[0], out page))
					page = 1;
				page--;
				const int pagelimit = 6;

				int pagecount = Results.Count / pagelimit;
				if (page > pagecount)
				{
					args.Player.SendMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1), Color.Red);
					return;
				}

				args.Player.SendInfoMessage("Nearby Players - Page {0} of {1} | /near [page]".SFormat(page + 1, pagecount + 1));
				for (int i = (page * pagelimit); (i < ((page * pagelimit) + pagelimit)) && i < Results.Count; i++)
					args.Player.SendMessage(Results[i], Color.MediumSeaGreen);
			}
		}
		#endregion

		#region Nick
		private void CMDnick(CommandArgs args)
		{
			if (args.Parameters.Count != 1 && !args.Player.Group.HasPermission("essentials.nick.setother"))
			{
				args.Player.SendMessage("Usage: /nick <nickname / off>", Color.OrangeRed);
				return;
			}
			else if ((args.Parameters.Count < 1 || args.Parameters.Count > 2) && args.Player.Group.HasPermission("essentials.nick.setother"))
			{
				args.Player.SendMessage("Usage: /nick [player] <nickname / off>", Color.OrangeRed);
				return;
			}

			TSPlayer NickPly = args.Player;

			if (args.Parameters.Count > 1 && args.Player.Group.HasPermission("essentials.nick.setother"))
			{
				var PlayersFound = TShock.Utils.FindPlayer(args.Parameters[0]);
				if (PlayersFound.Count != 1)
				{
					args.Player.SendWarningMessage(PlayersFound.Count < 1 ? "No players matched" : "More than one player matched!");
					return;
				}

				NickPly = PlayersFound[0];
			}

			esPlayer eNickPly = esPlayers[NickPly.Index];

			bool self = NickPly == args.Player;

			string nickname = args.Parameters[args.Parameters.Count - 1];

			if (nickname.ToLower() == "off")
			{
				if (eNickPly.HasNickName)
				{
					esSQL.RemoveNickname(NickPly.Name);

					eNickPly.HasNickName = false;
					eNickPly.Nickname = string.Empty;

					if (self)
						args.Player.SendMessage("Removed your nickname!", Color.MediumSeaGreen);
					else
					{
						args.Player.SendMessage(string.Format("Removed {0}'s Nickname", NickPly.Name), Color.MediumSeaGreen);
						NickPly.SendMessage(string.Format("Your nickname was removed by {0}!", args.Player.Name), Color.MediumSeaGreen);
					}
				}
				else
				{
					if (self)
						args.Player.SendMessage("You do not have a nickname to remove!", Color.OrangeRed);
					else
						args.Player.SendMessage("That player does not have a nickname to remove!", Color.OrangeRed);
				}
				return;
			}

			/*System.Text.RegularExpressions.Regex alphanumeric = new System.Text.RegularExpressions.Regex("^[a-zA-Z0-9_ ]*$");
			if (!alphanumeric.Match(nickname).Success)
			{
				args.Player.SendMessage("Nicknames must be Alphanumeric!", Color.OrangeRed);
				return;
			}*/

			if (!eNickPly.HasNickName)
			{
				eNickPly.OriginalName = NickPly.Name;
				eNickPly.HasNickName = true;
			}

			eNickPly.Nickname = nickname;

			string curNickname;
			if (esSQL.GetNickname(NickPly.Name, out curNickname))
				esSQL.UpdateNickname(NickPly.Name, nickname);
			else
				esSQL.AddNickname(NickPly.Name, nickname);

			if (self)
				args.Player.SendMessage(string.Format("Set your nickname to \'{0}\'!", nickname), Color.MediumSeaGreen);
			else
			{
				args.Player.SendMessage(string.Format("Set {0}'s Nickname to \'{1}\'", eNickPly.OriginalName, nickname), Color.MediumSeaGreen);
				NickPly.SendMessage(string.Format("{0} Set your nickname to \'{1}\'!", args.Player.Name, nickname), Color.MediumSeaGreen);
			}
		}
		#endregion

		#region realname
		private void CMDrealname(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /realname <player/-all>", Color.OrangeRed);
				return;
			}
			string search = args.Parameters[0].ToLower();
			if (search == "-all")
			{
				List<string> Nicks = new List<string>();
				foreach (var player in esPlayers)
				{
					if (player == null || !player.HasNickName) continue;
					Nicks.Add(string.Concat(Config.PrefixNicknamesWith, player.Nickname, "(", player.OriginalName, ")"));
				}

				if (Nicks.Count < 1)
					args.Player.SendMessage("No players online have nicknames!", Color.OrangeRed);
				else
					args.Player.SendMessage(string.Join(", ", Nicks), Color.MediumSeaGreen);
				return;
			}
			if (search.StartsWith(Config.PrefixNicknamesWith))
				search = search.Remove(0, Config.PrefixNicknamesWith.Length);

			List<esPlayer> PlayersFound = new List<esPlayer>();
			foreach (var player in esPlayers)
			{
				if (player == null || !player.HasNickName) continue;
				if (player.Nickname.ToLower() == search)
				{
					PlayersFound = new List<esPlayer> { player };
					break;
				}
				else if (player.Nickname.ToLower().Contains(search))
					PlayersFound.Add(player);
			}
			if (PlayersFound.Count != 1)
			{
				args.Player.SendWarningMessage(PlayersFound.Count < 1 ? "No players matched!" : "More than one player matched!");
				return;
			}

			esPlayer ply = PlayersFound[0];

			args.Player.SendMessage(string.Format("The user \'{0}\' has the nickname \'{1}\'!", ply.OriginalName, ply.Nickname), Color.MediumSeaGreen);

		}
		#endregion

		#region Exact Time
		private void CMDetime(CommandArgs args)
		{
			if (args.Parameters.Count != 1 || !args.Parameters[0].Contains(':'))
			{
				args.Player.SendMessage("Usage: /etime <hours>:<minutes>", Color.OrangeRed);
				return;
			}

			string[] split = args.Parameters[0].Split(':');
			string sHours = split[0];
			string sMinutes = split[1];

			bool PM = false;
			int Hours = -1;
			int Minutes = -1;
			if (!int.TryParse(sHours, out Hours) || !int.TryParse(sMinutes, out Minutes))
			{
				args.Player.SendMessage("Usage: /etime <hours>:<minutes>", Color.OrangeRed);
				return;
			}
			if (Hours < 0 || Hours > 24)
			{
				args.Player.SendMessage("Hours is out of range.", Color.OrangeRed);
				return;
			}
			if (Minutes < 0 || Minutes > 59)
			{
				args.Player.SendMessage("Minutes is out of range.", Color.OrangeRed);
				return;
			}

			int TFHour = Hours;

			if (TFHour == 24 || TFHour == 0)
			{
				Hours = 12;
			}
			if (TFHour >= 12 && TFHour < 24)
			{
				PM = true;
				if (Hours > 12)
					Hours -= 12;
			}

			int THour = Hours;

			Hours = TFHour;
			if (Hours == 24)
				Hours = 0;

			double TimeMinutes = Minutes / 60.0;
			double MainTime = TimeMinutes + Hours;

			if (MainTime >= 4.5 && MainTime < 24)
				MainTime -= 24.0;

			MainTime = MainTime + 19.5;
			MainTime = MainTime / 24.0 * 86400.0;

			bool Day = false;
			if ((!PM && ((THour > 4 || (THour == 4 && Minutes >= 30))) && THour < 12) || (PM && ((THour < 7 || (THour == 7 && Minutes < 30)) || THour == 12)))
				Day = true;

			if (!Day)
				MainTime -= 54000.0;

			TSPlayer.Server.SetTime(Day, MainTime);

			string min = Minutes.ToString();
			if (Minutes < 10)
				min = "0" + Minutes.ToString();
			TShock.Utils.Broadcast(string.Format("{0} set time to {1}:{2} {3}", args.Player.Name, THour, min, (PM ? "PM" : "AM")), Color.LightSeaGreen);
		}
		#endregion

		#region Force Login
		private void CMDforcelogin(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendWarningMessage("Usage: /forcelogin <account> [player]");
				return;
			}
			var user = TShock.Users.GetUserByName(args.Parameters[0]);
			if (user == null)
			{
				args.Player.SendWarningMessage("User {0} does not exist!".SFormat(args.Parameters[0]));
				return;
			}
			var group = TShock.Utils.GetGroup(user.Group);

			var PlayersFound = new List<TSPlayer>() { args.Player };
			if (args.Parameters.Count == 2)
			{
				PlayersFound = TShock.Utils.FindPlayer(args.Parameters[1]);
				if (PlayersFound.Count != 1)
				{
					args.Player.SendWarningMessage(PlayersFound.Count < 1 ? "No players matched" : "More than one player matched!");
					return;
				}
			}

			var Player = PlayersFound[0];
			Player.Group = group;
			Player.UserAccountName = user.Name;
			Player.UserID = TShock.Users.GetUserID(Player.UserAccountName);
			Player.IsLoggedIn = true;
			Player.IgnoreActionsForInventory = "none";

			Player.SendSuccessMessage(string.Format("{0} in as {1}", (Player != args.Player ? args.Player.Name + " Logged you" : "Logged"), user.Name));
			if (Player != args.Player)
				args.Player.SendSuccessMessage(string.Format("Logged {0} in as {1}", Player.Name, user.Name));
			Log.ConsoleInfo(string.Format("{0} forced logged in {1}as user: {2}.", args.Player.Name, args.Player != Player ? Player.Name + " " : string.Empty, user.Name));
		}
		#endregion
		
		#region Invsee
		private void CMDinvsee(CommandArgs args)
		{
			if (!TShock.Config.ServerSideCharacter)
			{
				args.Player.SendWarningMessage("Server Side Character must be enabled!");
				return;
			}
			if (args.Parameters.Count < 1)
			{
				args.Player.SendWarningMessage("Usage: /invsee [player name] -- See another player's inventory");
				args.Player.SendWarningMessage("Usage: /invsee -restore -- Restores your inventory");
			}
			var ePly = esPlayers[args.Player.Index];
			if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "-restore")
			{
				if (ePly.InvSee == null)
				{
					args.Player.SendWarningMessage("You are not viewing another player's inventory!");
					return;
				}
				ePly.InvSee.RestoreCharacter(args.Player);
				ePly.InvSee = null;
				args.Player.SendSuccessMessage("Restored your inventory!");
				return;
			}
			var PlayersFound = TShock.Utils.FindPlayer(string.Join(" ", args.Parameters));
			if (PlayersFound.Count != 1)
			{
				args.Player.SendWarningMessage(PlayersFound.Count < 1 ? "No Players matched!" : "More than one player matched!");
				return;
			}

			var PlayerChar = new PlayerData(args.Player);
			PlayerChar.CopyCharacter(args.Player);
			ePly.InvSee = PlayerChar;

			var CopyChar = new PlayerData(PlayersFound[0]);
			CopyChar.CopyCharacter(PlayersFound[0]);
			CopyChar.health = PlayerChar.health;
			CopyChar.maxHealth = PlayerChar.maxHealth;
			CopyChar.mana = PlayerChar.mana;
			CopyChar.maxMana = PlayerChar.maxMana;
			CopyChar.spawnX = PlayerChar.spawnX;
			CopyChar.spawnY = PlayerChar.spawnY;
			CopyChar.RestoreCharacter(args.Player);

			args.Player.SendSuccessMessage("Copied {0}'s inventory", PlayersFound[0].Name);
		}
		#endregion
	}
}
