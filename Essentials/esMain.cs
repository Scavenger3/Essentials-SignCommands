using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;
using System.Linq;
using MySql.Data.MySqlClient;
using Terraria;
using TShockAPI;
using Hooks;
using System.Reflection;

namespace Essentials
{
	[APIVersion(1, 12)]
	public class Essentials : TerrariaPlugin
	{
		public Dictionary<string, int[]> disabled { get; set; }
		public static List<esPlayer> esPlayers { get; set; }
		public DateTime LastCheck { get; set; }

		public static esConfig getConfig { get; set; }
		internal static string ConfigPath { get { return Path.Combine(TShock.SavePath, "Essentials", "EssentialsConfig.json"); } }
		internal static string EssDirectory { get { return Path.Combine(TShock.SavePath, "Essentials"); } }

		public override string Name
		{
			get { return "Essentials"; }
		}

		public override string Author
		{
			get { return "by Scavenger"; }
		}

		public override string Description
		{
			get { return "Some Essential commands for TShock!"; }
		}

		public override Version Version
		{
			get { return Assembly.GetExecutingAssembly().GetName().Version; }
		}

		public override void Initialize()
		{
			GameHooks.Initialize += OnInitialize;
			NetHooks.GreetPlayer += OnGreetPlayer;
			ServerHooks.Leave += OnLeave;
			ServerHooks.Chat += OnChat;
			NetHooks.GetData += GetData;
			GameHooks.Update += OnUpdate;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				GameHooks.Initialize -= OnInitialize;
				NetHooks.GreetPlayer -= OnGreetPlayer;
				ServerHooks.Leave -= OnLeave;
				ServerHooks.Chat -= OnChat;
				NetHooks.GetData -= GetData;
				GameHooks.Update -= OnUpdate;
			}
			base.Dispose(disposing);
		}

		public Essentials(Main game) : base(game)
		{
			this.Order = -1;
			this.LastCheck = DateTime.UtcNow;
			this.disabled = new Dictionary<string, int[]>();
			Essentials.esPlayers = new List<esPlayer>();
			Essentials.getConfig = new esConfig();
		}

		public void OnInitialize()
		{
			#region Add Commands
			Commands.ChatCommands.Add(new Command("essentials.more", CMDmore, "more"));
			Commands.ChatCommands.Add(new Command("essentials.position.get", CMDpos, "pos", "getpos"));
			Commands.ChatCommands.Add(new Command("essentials.position.tp", CMDtppos, "tppos"));
			Commands.ChatCommands.Add(new Command("essentials.position.ruler", CMDruler, "ruler"));
			Commands.ChatCommands.Add(new Command("essentials.helpop.ask", CMDhelpop, "helpop"));
			Commands.ChatCommands.Add(new Command("essentials.suicide", CMDsuicide, "suicide", "die"));
			Commands.ChatCommands.Add(new Command("essentials.pvp.burn", CMDburn, "burn"));
			Commands.ChatCommands.Add(new Command("essentials.killnpc", CMDkillnpc, "killnpc"));
			Commands.ChatCommands.Add(new Command("essentials.kickall.kick", CMDkickall, "kickall"));
			Commands.ChatCommands.Add(new Command("essentials.moon", CMDmoon, "moon"));
			Commands.ChatCommands.Add(new Command("essentials.back.tp", CMDback, "b"));
			Commands.ChatCommands.Add(new Command("essentials.convertbiomes", CMDcbiome, "cbiome", "bconvert"));
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
			Commands.ChatCommands.Add(new Command("essentials.playertime.set", CMDptime, "ptime"));
			Commands.ChatCommands.Add(new Command("essentials.ping", CMDping, "ping", "pong", "echo"));
			Commands.ChatCommands.Add(new Command("essentials.sudo", CMDsudo, "sudo"));
			Commands.ChatCommands.Add(new Command("essentials.socialspy", CMDsocialspy, "socialspy"));
			Commands.ChatCommands.Add(new Command("essentials.near", CMDnear, "near"));
			Commands.ChatCommands.Add(new Command("essentials.nick.set", CMDnick, "nick"));
			Commands.ChatCommands.Add(new Command("essentials.realname", CMDrealname, "realname"));
			Commands.ChatCommands.Add(new Command("essentials.exacttime", CMDetime, "etime", "exacttime"));
			Commands.ChatCommands.Add(new Command("essentials.forcelogin", CMDforcelogin, "forcelogin"));
			#endregion

			LastCheck = DateTime.UtcNow;

			if (!Directory.Exists(EssDirectory))
				Directory.CreateDirectory(EssDirectory);

			esSQL.SetupDB();

			foreach (Group grp in TShock.Groups.groups)
			{
				if (grp.Name != "superadmin" && grp.HasPermission("essentials.back.death") && !grp.HasPermission("essentials.back.tp"))
					grp.AddPermission("essentials.back.tp");
			}

			if (!File.Exists(ConfigPath))
				esConfig.CreateExample();

			esConfig.LoadConfig();
		}

		#region esPlayer
		public void OnGreetPlayer(int who, HandledEventArgs e)
		{
			try
			{
				lock (esPlayers)
					esPlayers.Add(new esPlayer(who));


				if (TShock.Players[who] == null) return;

				if (disabled.ContainsKey(TShock.Players[who].Name))
				{
					var ply = esUtils.GetesPlayerByID(who);
					if (ply == null) return;
					ply.DisabledX = disabled[TShock.Players[who].Name][0];
					ply.DisabledY = disabled[TShock.Players[who].Name][1];
					ply.TSPlayer.Teleport(ply.DisabledX, ply.DisabledY + 3);
					ply.Disabled = true;
					ply.TSPlayer.Disable();
					ply.LastDisabledCheck = DateTime.UtcNow;
					ply.TSPlayer.SendWarningMessage("You are still disabled!");
				}

				string nickname;
				if (esSQL.GetNickname(TShock.Players[who].Name, out nickname))
				{
					var ply = esUtils.GetesPlayerByID(who);
					if (ply == null) return;
					ply.HasNickName = true;
					ply.OriginalName = ply.TSPlayer.Name;
					ply.Nickname = nickname;
				}
			}
			catch { }
		}

		public void OnLeave(int who)
		{
			try
			{
				lock (esPlayers)
				{
					for (int i = 0; i < esPlayers.Count; i++)
					{
						if (esPlayers[i].Index == who)
						{
							esPlayers.RemoveAt(i);
							break;
						}
					}
				}
			}
			catch { }
		}
		#endregion

		#region Chat
		public void OnChat(messageBuffer msg, int who, string text, HandledEventArgs e)
		{
			try
			{
				if (e.Handled)
					return;
				if (text == "/")
				{
					//TShock.Players[who].SendMessage("Yes, that is how you execute commands! type /help for a list of them!", Color.LightSeaGreen);
					e.Handled = true;
					return;
				}

				var ePly = esUtils.GetesPlayerByID(who);
				var tPly = TShock.Players[who];

				if (text.StartsWith("/") && text != "/=" && !text.StartsWith("/= ") && !text.StartsWith("/ "))
				{
					ePly.LastCMD = text;
				}

				if (text.StartsWith("/tp "))
				{
					#region /tp
					if (tPly.Group.HasPermission("tp") && tPly.RealPlayer)
					{
						/* Make sure the tp is valid */
						List<string> Params = esUtils.ParseParameters(text);
						Params.RemoveAt(0);

						string plStr = String.Join(" ", Params);
						var players = TShock.Utils.FindPlayer(plStr);

						if (Params.Count > 0 && players.Count == 1 && players[0].TPAllow && tPly.Group.HasPermission(Permissions.tpall))
						{
							ePly.LastBackX = tPly.TileX;
							ePly.LastBackY = tPly.TileY;
							ePly.LastBackAction = BackAction.TP;
						}
					}
					#endregion
				}
				else if (text == "/home" || text.StartsWith("/home ") || text == "/spawn" || text.StartsWith("/spawn "))
				{
					#region /home & /spawn
					if (tPly.Group.HasPermission("tp") && tPly.RealPlayer)
					{
						ePly.LastBackX = tPly.TileX;
						ePly.LastBackY = tPly.TileY;
						ePly.LastBackAction = BackAction.TP;
					}
					#endregion
				}
				else if (text.StartsWith("/warp "))
				{
					#region /warp
					if (tPly.Group.HasPermission("warp") && tPly.RealPlayer)
					{
						/* Make sure the warp is valid */
						List<string> Params = esUtils.ParseParameters(text);
						Params.RemoveAt(0);

						if (Params.Count > 0 && !Params[0].Equals("list"))
						{
							string warpName = String.Join(" ", Params);
							var warp = TShock.Warps.FindWarp(warpName);
							if (warp != null && warp.WarpPos != Vector2.Zero)
							{
								ePly.LastBackX = tPly.TileX;
								ePly.LastBackY = tPly.TileY;
								ePly.LastBackAction = BackAction.TP;
							}
						}
					}
					#endregion
				}
				else if (text.StartsWith("/whisper ") || text.StartsWith("/w ") || text.StartsWith("/tell ") || text.StartsWith("/reply ") || text.StartsWith("/r ") || text.StartsWith("/p "))
				{
					if (!tPly.Group.HasPermission("whisper")) return;
					foreach (esPlayer player in esPlayers)
					{
						if (player.SocialSpy && player != ePly)
						{
							if ((text.StartsWith("/reply ") || text.StartsWith("/r ")) && tPly.LastWhisper != null)
								player.TSPlayer.SendMessage(string.Format("[SocialSpy] from {0} to {1}: {2}", tPly.Name, tPly.LastWhisper.Name ?? "?", text), Color.Gray);
							else
								player.TSPlayer.SendMessage(string.Format("[SocialSpy] {0}: {1}", tPly.Name, text), Color.Gray);
						}
					}
				}
				else if (ePly.HasNickName && !text.StartsWith("/") && !tPly.mute)
				{
					e.Handled = true;
					string nick = getConfig.PrefixNicknamesWith + ePly.Nickname;
					TShock.Utils.Broadcast(String.Format(TShock.Config.ChatFormat, tPly.Group.Name, tPly.Group.Prefix, nick, tPly.Group.Suffix, text),
									tPly.Group.R, tPly.Group.G, tPly.Group.B);
				}
				else if (ePly.HasNickName && text.StartsWith("/me ") && !tPly.mute)
				{
					e.Handled = true;
					string nick = getConfig.PrefixNicknamesWith + ePly.Nickname;
					TShock.Utils.Broadcast(string.Format("*{0} {1}", nick, text.Remove(0, 4)), 205, 133, 63);
				}
			}
			catch { }
		}
		#endregion
		
		#region Teams
		public void GetData(GetDataEventArgs e)
		{
			try
			{
				if (e.MsgID != PacketTypes.PlayerTeam || !(getConfig.LockRedTeam || getConfig.LockGreenTeam || getConfig.LockBlueTeam || getConfig.LockYellowTeam)) return;
				using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
				{
					var reader = new BinaryReader(data);
					var play = reader.ReadByte();
					var team = reader.ReadByte();
					reader.Close();
					esPlayer ePly = esUtils.GetesPlayerByID(play);
					TSPlayer tPly = TShock.Players[play];
					if (ePly == null || tPly == null) return;
					switch (team)
					{
						case 1:
							if (getConfig.LockRedTeam && !tPly.Group.HasPermission(getConfig.RedTeamPermission) && (ePly.RedPassword != getConfig.RedTeamPassword || ePly.RedPassword == ""))
							{
								e.Handled = true;
								tPly.SetTeam(tPly.Team);
								if (getConfig.RedTeamPassword == "")
									tPly.SendMessage("You do not have permission to join that team!", Color.Red);
								else
									tPly.SendMessage("That team is locked, use \'/teamunlock red <password>\' to access it.", Color.Red);

							}
							break;
						case 2:
							if (getConfig.LockGreenTeam && !tPly.Group.HasPermission(getConfig.GreenTeamPermission) && (ePly.GreenPassword != getConfig.GreenTeamPassword || ePly.GreenPassword == ""))
							{
								e.Handled = true;
								tPly.SetTeam(tPly.Team);
								if (getConfig.GreenTeamPassword == "")
									tPly.SendMessage("You do not have permission to join that team!", Color.Red);
								else
									tPly.SendMessage("That team is locked, use \'/teamunlock green <password>\' to access it.", Color.Red);

							}
							break;
						case 3:
							if (getConfig.LockBlueTeam && !tPly.Group.HasPermission(getConfig.BlueTeamPermission) && (ePly.BluePassword != getConfig.BlueTeamPassword || ePly.BluePassword == ""))
							{
								e.Handled = true;
								tPly.SetTeam(tPly.Team);
								if (getConfig.BlueTeamPassword == "")
									tPly.SendMessage("You do not have permission to join that team!", Color.Red);
								else
									tPly.SendMessage("That team is locked, use \'/teamunlock blue <password>\' to access it.", Color.Red);

							}
							break;
						case 4:
							if (getConfig.LockYellowTeam && !tPly.Group.HasPermission(getConfig.YellowTeamPermission) && (ePly.YellowPassword != getConfig.YellowTeamPassword || ePly.YellowPassword == ""))
							{
								e.Handled = true;
								tPly.SetTeam(tPly.Team);
								if (getConfig.YellowTeamPassword == "")
									tPly.SendMessage("You do not have permission to join that team!", Color.Red);
								else
									tPly.SendMessage("That team is locked, use \'/teamunlock yellow <password>\' to access it.", Color.Red);

							}
							break;
					}
				}
			}
			catch (Exception ex)
			{
				Log.Error("[Essentials] Error with team lock: ");
				Log.Error(ex.ToString());
			}
		}
		#endregion

		#region Timer
		public void OnUpdate()
		{
			if ((DateTime.UtcNow - LastCheck).TotalMilliseconds >= 1000)
			{
				LastCheck = DateTime.UtcNow;
				try
				{
					lock (esPlayers)
					{
						foreach (esPlayer ePly in esPlayers)
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
									if (getConfig.ShowBackMessageOnDeath)
										ePly.TSPlayer.SendMessage("Type \"/b\" to return to your position before you died", Color.MediumSeaGreen);
								}
							}
							else if (ePly.SavedBackAction && !ePly.TSPlayer.Dead)
								ePly.SavedBackAction = false;

							if (ePly.pTime != -1)
							{
								if (ePly.pTime < 1)
								{
									ePly.pTime = 2;
									bool oldIsDay = Main.dayTime;
									double oldTime = Main.time;
									Main.dayTime = ePly.ptDay;
									Main.time = ePly.ptTime;
									ePly.TSPlayer.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
									Main.dayTime = oldIsDay;
									Main.time = oldTime;
								}
								else
									ePly.pTime--;
							}

							if (ePly.Disabled && ((DateTime.UtcNow - ePly.LastDisabledCheck).TotalMilliseconds) > 3000)
							{
								ePly.LastDisabledCheck = DateTime.UtcNow;
								ePly.TSPlayer.Disable();
								if ((ePly.TSPlayer.TileX > ePly.DisabledX + 5 || ePly.TSPlayer.TileX < ePly.DisabledX - 5) || (ePly.TSPlayer.TileY > ePly.DisabledY + 5 || ePly.TSPlayer.TileY < ePly.DisabledY - 5))
								{
									ePly.TSPlayer.Teleport(ePly.DisabledX, ePly.DisabledY + 3);
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
				Y = esUtils.GetTop(X) - 3;

			esPlayer ePly = esUtils.GetesPlayerByID(args.Player.Index);
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}
			if (args.Player.Teleport(X, Y + 3))
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

			foreach (esPlayer ePly in esPlayers)
			{
				if (ePly == null) continue;
				if (ePly.TSPlayer.Group.HasPermission("essentials.helpop.receive"))
				{
					online.Add(ePly.TSPlayer.Name);
					ePly.TSPlayer.SendMessage(string.Format("[HelpOp] {0}: {1}", args.Player.Name, text), Color.RoyalBlue);
				}
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

			NetMessage.SendData((int)PacketTypes.PlayerDamage, -1, -1, " decided it wasnt worth living.", args.Player.Index, ((new Random()).Next(-1, 1)), 9999, (float)0);
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

		#region killnpc
		private void CMDkillnpc(CommandArgs args)
		{
			if (args.Parameters.Count > 0)
			{

				var npcselected = TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);
				if (npcselected.Count == 0)
					args.Player.SendMessage("Invalid NPC!", Color.OrangeRed);
				else if (npcselected.Count > 1)
					args.Player.SendMessage("More than one NPC matched!", Color.OrangeRed);
				else
				{
					int killcount = 0;
					for (int i = 0; i < Main.npc.Length; i++)
					{
						if (Main.npc[i].active && Main.npc[i].type != 0 && Main.npc[i].name == npcselected[0].name)
						{
							TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
							killcount++;
						}
					}
					args.Player.SendMessage("Killed " + killcount + " " + npcselected[0].name + "!", Color.MediumSeaGreen);
				}
			}
			else
			{
				int killcount = 0;
				for (int i = 0; i < Main.npc.Length; i++)
				{
					if (Main.npc[i].active && Main.npc[i].type != 0 && !Main.npc[i].townNPC && !Main.npc[i].friendly)
					{
						TSPlayer.Server.StrikeNPC(i, 99999, 90f, 1);
						killcount++;
					}
				}
				args.Player.SendMessage("Killed " + killcount + " NPCs.", Color.MediumSeaGreen);
			}
		}
		#endregion

		#region Kickall
		private void CMDkickall(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				foreach (esPlayer ePly in esPlayers)
				{
					if (!ePly.TSPlayer.Group.HasPermission("essentials.kickall.immune"))
						ePly.TSPlayer.Disconnect("Everyone has been kicked from the server!");
				}
				TShock.Utils.Broadcast("Everyone has been kicked from the server!", Color.MediumSeaGreen);
				return;
			}

			string text = string.Join(" ", args.Parameters);

			foreach (esPlayer ePly in esPlayers)
			{
				if (!ePly.TSPlayer.Group.HasPermission("essentials.kickall.immune"))
					ePly.TSPlayer.Disconnect(string.Format("Everyone has been kicked ({0})", text));
			}
			TShock.Utils.Broadcast("Everyone has been kicked from the server!", Color.MediumSeaGreen);
		}
		#endregion

		#region moon
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

		#region back
		private void CMDback(CommandArgs args)
		{
			esPlayer ePly = esUtils.GetesPlayerByID(args.Player.Index);

			if (ePly.LastBackAction == BackAction.None)
				args.Player.SendMessage("You do not have a /b position stored", Color.OrangeRed);
			else if (ePly.LastBackAction == BackAction.TP)
			{
				if (args.Player.Teleport(ePly.LastBackX, ePly.LastBackY + 3))
					args.Player.SendMessage("Moved you to your position before you last teleported", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
			}
			else if (ePly.LastBackAction == BackAction.Death && args.Player.Group.HasPermission("essentials.back.death"))
			{
				if (args.Player.Teleport(ePly.LastBackX, ePly.LastBackY + 3))
					args.Player.SendMessage("Moved you to your position before you died!", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
			}
			else
				args.Player.SendMessage("You do not have permission to /b after death", Color.MediumSeaGreen);
		}
		#endregion

		#region cbiome
		private void CMDcbiome(CommandArgs args)
		{
			if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
			{
				args.Player.SendMessage("Usage: /cbiome <from> <to> [region]", Color.OrangeRed);
				args.Player.SendMessage("Possible Biomes: Corruption, Hallow, Normal", Color.OrangeRed);
				return;
			}

			string from = args.Parameters[0].ToLower();
			string to = args.Parameters[1].ToLower();
			string region = "";
			var regiondata = TShock.Regions.GetRegionByName("");
			bool doregion = false;

			if (args.Parameters.Count == 3)
			{
				region = args.Parameters[2];
				if (TShock.Regions.ZacksGetRegionByName(region) != null)
				{
					doregion = true;
					regiondata = TShock.Regions.GetRegionByName(region);
				}
			}

			if (from == "normal")
			{
				if (!doregion)
					args.Player.SendMessage("You must specify a valid region to convert a normal biome.", Color.OrangeRed);
				else if (to == "normal")
					args.Player.SendMessage("You cannot convert Normal to Normal.", Color.OrangeRed);
				else if (to == "hallow" && doregion)
				{
					args.Player.SendMessage("Server might lag for a moment.", Color.OrangeRed);
					for (int x = 0; x < Main.maxTilesX; x++)
					{
						for (int y = 0; y < Main.maxTilesY; y++)
						{
							if (doregion && x >= regiondata.Area.Left && x <= regiondata.Area.Right && y >= regiondata.Area.Top && y <= regiondata.Area.Bottom)
							{
								switch (Main.tile[x, y].type)
								{
									case 1:
										Main.tile[x, y].type = 117;
										break;
									case 2:
										Main.tile[x, y].type = 109;
										break;
									case 53:
										Main.tile[x, y].type = 116;
										break;
									case 3:
										Main.tile[x, y].type = 110;
										break;
									case 73:
										Main.tile[x, y].type = 113;
										break;
									case 52:
										Main.tile[x, y].type = 115;
										break;
									default:
										continue;
								}
							}
						}
					}
					WorldGen.CountTiles(0);
					TSPlayer.All.SendData(PacketTypes.UpdateGoodEvil);
					Netplay.ResetSections();
					args.Player.SendMessage("Converted Normal into Hallow!", Color.MediumSeaGreen);
				}
				else if (to == "corruption" && doregion)
				{
					args.Player.SendMessage("Server might lag for a moment.", Color.OrangeRed);
					for (int x = 0; x < Main.maxTilesX; x++)
					{
						for (int y = 0; y < Main.maxTilesY; y++)
						{
							if (doregion && x >= regiondata.Area.Left && x <= regiondata.Area.Right && y >= regiondata.Area.Top && y <= regiondata.Area.Bottom)
							{
								switch (Main.tile[x, y].type)
								{
									case 1:
										Main.tile[x, y].type = 25;
										break;
									case 2:
										Main.tile[x, y].type = 23;
										break;
									case 53:
										Main.tile[x, y].type = 112;
										break;
									case 3:
										Main.tile[x, y].type = 24;
										break;
									case 73:
										Main.tile[x, y].type = 24;
										break;
									default:
										continue;
								}
							}
						}
					}
					WorldGen.CountTiles(0);
					TSPlayer.All.SendData(PacketTypes.UpdateGoodEvil);
					Netplay.ResetSections();
					args.Player.SendMessage("Converted Normal into Corruption!", Color.MediumSeaGreen);
				}
			}
			else if (from == "hallow")
			{
				if (args.Parameters.Count == 3 && !doregion)
					args.Player.SendMessage("You must specify a valid region to convert a normal biome.", Color.OrangeRed);
				else if (to == "hallow")
					args.Player.SendMessage("You cannot convert Hallow to hallow.", Color.OrangeRed);
				else if (to == "corruption")
				{
					args.Player.SendMessage("Server might lag for a moment.", Color.OrangeRed);
					for (int x = 0; x < Main.maxTilesX; x++)
					{
						for (int y = 0; y < Main.maxTilesY; y++)
						{
							if (!doregion || (doregion && x >= regiondata.Area.Left && x <= regiondata.Area.Right && y >= regiondata.Area.Top && y <= regiondata.Area.Bottom))
							{
								switch (Main.tile[x, y].type)
								{
									case 117:
										Main.tile[x, y].type = 25;
										break;
									case 109:
										Main.tile[x, y].type = 23;
										break;
									case 116:
										Main.tile[x, y].type = 112;
										break;
									case 110:
										Main.tile[x, y].type = 24;
										break;
									case 113:
										Main.tile[x, y].type = 24;
										break;
									case 115:
										Main.tile[x, y].type = 52;
										break;
									default:
										continue;
								}
							}
						}
					}
					WorldGen.CountTiles(0);
					TSPlayer.All.SendData(PacketTypes.UpdateGoodEvil);
					Netplay.ResetSections();
					args.Player.SendMessage("Converted Hallow into Corruption!", Color.MediumSeaGreen);
				}
				else if (to == "normal")
				{
					args.Player.SendMessage("Server might lag for a moment.", Color.OrangeRed);
					for (int x = 0; x < Main.maxTilesX; x++)
					{
						for (int y = 0; y < Main.maxTilesY; y++)
						{
							if (!doregion || (doregion && x >= regiondata.Area.Left && x <= regiondata.Area.Right && y >= regiondata.Area.Top && y <= regiondata.Area.Bottom))
							{
								switch (Main.tile[x, y].type)
								{
									case 117:
										Main.tile[x, y].type = 1;
										break;
									case 109:
										Main.tile[x, y].type = 2;
										break;
									case 116:
										Main.tile[x, y].type = 53;
										break;
									case 110:
										Main.tile[x, y].type = 73;
										break;
									case 113:
										Main.tile[x, y].type = 73;
										break;
									case 115:
										Main.tile[x, y].type = 52;
										break;
									default:
										continue;
								}
							}
						}
					}
					WorldGen.CountTiles(0);
					TSPlayer.All.SendData(PacketTypes.UpdateGoodEvil);
					Netplay.ResetSections();
					args.Player.SendMessage("Converted Hallow into Normal!", Color.MediumSeaGreen);
				}
			}
			else if (from == "corruption")
			{
				if (args.Parameters.Count == 3 && !doregion)
					args.Player.SendMessage("You must specify a valid region to convert a normal biome.", Color.OrangeRed);
				else if (to == "corruption")
					args.Player.SendMessage("You cannot convert Corruption to Corruption.", Color.OrangeRed);
				else if (to == "hallow")
				{
					args.Player.SendMessage("Server might lag for a moment.", Color.OrangeRed);
					for (int x = 0; x < Main.maxTilesX; x++)
					{
						for (int y = 0; y < Main.maxTilesY; y++)
						{
							if (!doregion || (doregion && x >= regiondata.Area.Left && x <= regiondata.Area.Right && y >= regiondata.Area.Top && y <= regiondata.Area.Bottom))
							{
								switch (Main.tile[x, y].type)
								{
									case 25:
										Main.tile[x, y].type = 117;
										break;
									case 23:
										Main.tile[x, y].type = 109;
										break;
									case 112:
										Main.tile[x, y].type = 116;
										break;
									case 24:
										Main.tile[x, y].type = 110;
										break;
									case 32:
										Main.tile[x, y].type = 115;
										break;
									default:
										continue;
								}
							}
						}
					}
					WorldGen.CountTiles(0);
					TSPlayer.All.SendData(PacketTypes.UpdateGoodEvil);
					Netplay.ResetSections();
					args.Player.SendMessage("Converted Corruption into Hallow!", Color.MediumSeaGreen);
				}
				else if (to == "normal")
				{
					args.Player.SendMessage("Server might lag for a moment.", Color.OrangeRed);
					for (int x = 0; x < Main.maxTilesX; x++)
					{
						for (int y = 0; y < Main.maxTilesY; y++)
						{
							if (!doregion || (doregion && x >= regiondata.Area.Left && x <= regiondata.Area.Right && y >= regiondata.Area.Top && y <= regiondata.Area.Bottom))
							{
								switch (Main.tile[x, y].type)
								{
									case 25:
										Main.tile[x, y].type = 1;
										break;
									case 23:
										Main.tile[x, y].type = 2;
										break;
									case 112:
										Main.tile[x, y].type = 53;
										break;
									case 24:
										Main.tile[x, y].type = 3;
										break;
									case 32:
										Main.tile[x, y].type = 52;
										break;
									default:
										continue;
								}
							}
						}
					}
					WorldGen.CountTiles(0);
					TSPlayer.All.SendData(PacketTypes.UpdateGoodEvil);
					Netplay.ResetSections();
					args.Player.SendMessage("Converted Corruption into Normal!", Color.MediumSeaGreen);
				}
				else
					args.Player.SendMessage("Error, Useable values: Hallow, Corruption, Normal", Color.OrangeRed);
			}
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

			esPlayer ePly = esUtils.GetesPlayerByID(args.Player.Index);
			if (ePly == null) return;

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

			esPlayer ePly = esUtils.GetesPlayerByID(args.Player.Index);
			if (ePly == null) return;
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

			esPlayer ePly = esUtils.GetesPlayerByID(args.Player.Index);
			if (ePly == null) return;
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
			/* Make sure the player isnt in a SetHome Disabled region */
			if (!args.Player.Group.HasPermission("essentials.home.bypassdisabled"))
			{
				foreach (string r in getConfig.DisableSetHomeInRegions)
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

			esPlayer ePly = esUtils.GetesPlayerByID(args.Player.Index);
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}

			if (args.Player.Teleport(homePos.X, homePos.Y + 3))
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
			if (!getConfig.LockRedTeam && !getConfig.LockGreenTeam && !getConfig.LockBlueTeam && !getConfig.LockYellowTeam)
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

			List<string> PasswordArgs = args.Parameters;
			PasswordArgs.RemoveAt(0);

			string Password = string.Join(" ", PasswordArgs);

			esPlayer ePly = esUtils.GetesPlayerByID(args.Player.Index);

			switch (team)
			{
				case "red":
					{
						if (getConfig.LockRedTeam)
						{
							if (Password == getConfig.RedTeamPassword && getConfig.RedTeamPassword != "")
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
						if (getConfig.LockGreenTeam)
						{
							if (Password == getConfig.GreenTeamPassword && getConfig.GreenTeamPassword != "")
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
						if (getConfig.LockBlueTeam)
						{
							if (Password == getConfig.BlueTeamPassword && getConfig.BlueTeamPassword != "")
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
						if (getConfig.LockYellowTeam)
						{
							if (Password == getConfig.YellowTeamPassword && getConfig.YellowTeamPassword != "")
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
			var player = esUtils.GetesPlayerByID(args.Player.Index);

			if (player.LastCMD == "/=" || player.LastCMD.StartsWith("/= "))
			{
				args.Player.SendMessage("Error with last command!", Color.OrangeRed);
				return;
			}

			if (player.LastCMD == string.Empty)
				args.Player.SendMessage("You have not entered a command yet.", Color.OrangeRed);
			else
				TShockAPI.Commands.HandleCommand(args.Player, player.LastCMD);
		}
		#endregion

		#region Kill For a reason
		private void CMDkillr(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /killr <player> <reason>", Color.OrangeRed);
				return;
			}

			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count == 0)
			{
				args.Player.SendMessage("No players matched!!", Color.OrangeRed);
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.OrangeRed);
			}
			else
			{
				var plr = players[0];
				args.Parameters.RemoveAt(0); //remove player name
				string reason = " " + string.Join(" ", args.Parameters);

				NetMessage.SendData((int)PacketTypes.PlayerDamage, -1, -1, reason, plr.Index, ((new Random()).Next(-1, 1)), 999999, (float)0);
				args.Player.SendMessage(string.Format("You just killed {0}!", plr.Name), Color.MediumSeaGreen);
			}
		}
		#endregion

		#region Disable
		private void CMDdisable(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /disable <player/-list> <reason>", Color.OrangeRed);
				return;
			}

			if (args.Parameters[0] == "-list")
			{
				string[] names = new string[disabled.Count];
				int i = 0;
				foreach (var pair in disabled)
				{
					names[i] = pair.Key;
					i++;
				}

				if (disabled.Count < 1)
					args.Player.SendMessage("There are currently no players disabled!", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Disabled Players: " + string.Join(", ", names), Color.MediumSeaGreen);
				return;
			}


			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count == 0)
			{
				foreach (var pair in disabled)
				{
					if (pair.Key.ToLower().Contains(args.Parameters[0].ToLower()))
					{
						disabled.Remove(pair.Key);
						args.Player.SendMessage(string.Format("{0} is no longer disabled (even though he isnt online)", pair.Key), Color.MediumSeaGreen);
						return;
					}
				}
				args.Player.SendMessage("No players matched!", Color.OrangeRed);
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.OrangeRed);
			}
			else
			{
				var tPly = players[0];
				var ePly = esUtils.GetesPlayerByID(tPly.Index);

				if (!ePly.Disabled)
				{
					List<string> RParams = args.Parameters;
					RParams.RemoveAt(0);
					string Reason = string.Join(" ", RParams);

					ePly.DisabledX = tPly.TileX;
					ePly.DisabledY = tPly.TileY;
					ePly.Disabled = true;
					ePly.Disable();
					ePly.LastDisabledCheck = DateTime.UtcNow;

					int[] pos = new int[2];
					pos[0] = ePly.DisabledX;
					pos[1] = ePly.DisabledY;

					disabled.Add(tPly.Name, pos);

					args.Player.SendMessage(string.Format("You disabled {0}, He can not be enabled until you type \"/disable {0}\"", tPly.Name), Color.MediumSeaGreen);
					if (Reason == string.Empty)
						tPly.SendMessage(string.Format("You have been disabled by {0}", args.Player.Name), Color.MediumSeaGreen);
					else
						tPly.SendMessage(string.Format("You have been disabled by {0} for {1}", args.Player.Name, Reason), Color.MediumSeaGreen);
				}
				else
				{
					ePly.Disabled = false;
					ePly.DisabledX = 0;
					ePly.DisabledY = 0;

					disabled.Remove(tPly.Name);

					args.Player.SendMessage(string.Format("{0} is no longer disabled!", tPly.Name), Color.MediumSeaGreen);
					tPly.SendMessage("You are no longer disabled!", Color.MediumSeaGreen);
				}
			}
		}
		#endregion

		#region Top, Up and Down
		private void CMDtop(CommandArgs args)
		{
			int Y = esUtils.GetTop(args.Player.TileX);
			if (Y == -1)
			{
				args.Player.SendMessage("You are already on the top most block!", Color.OrangeRed);
				return;
			}
			esPlayer ePly = esUtils.GetesPlayerByID(args.Player.Index);
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.TP;
			}
			if (args.Player.Teleport(args.Player.TileX, Y))
				args.Player.SendMessage("Teleported you to the top-most block!", Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
		}

		private void CMDup(CommandArgs args)
		{
			int levels = 1;
			bool limit = false;
			if (args.Parameters.Count > 0)
				if (!int.TryParse(args.Parameters[0], out levels))
				{
					args.Player.SendMessage("Usage: /up [No. levels]", Color.OrangeRed);
					return;
				}

			int Y = -1;
			for (int i = 0; i < levels; i++)
			{
				if (i == 0) Y = esUtils.GetUp(args.Player.TileX, args.Player.TileY);
				else
				{
					int oY = Y;
					Y = esUtils.GetUp(args.Player.TileX, Y);
					if (Y == -1)
					{
						Y = oY;
						levels = i;
						limit = true;
						break;
					}
				}
			}
			
			if (Y == -1)
			{
				args.Player.SendMessage("You cannot go up anymore!", Color.OrangeRed);
				return;
			}
			if (args.Player.Teleport(args.Player.TileX, Y))
				if (limit)
					args.Player.SendMessage(string.Format("Teleported you up {0} level(s)! You cant go up any further!", levels), Color.MediumSeaGreen);
				else
					args.Player.SendMessage(string.Format("Teleported you up {0} level(s)!", levels), Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
		}

		private void CMDdown(CommandArgs args)
		{
			bool limit = false;
			int levels = 1;
			if (args.Parameters.Count > 0)
				if (!int.TryParse(args.Parameters[0], out levels))
				{
					args.Player.SendMessage("Usage: /down [No. levels]", Color.OrangeRed);
					return;
				}

			int Y = -1;
			for (int i = 0; i < levels; i++)
			{
				if (i == 0) Y = esUtils.GetDown(args.Player.TileX, args.Player.TileY);
				else
				{
					int oY = Y;
					Y = esUtils.GetDown(args.Player.TileX, Y);
					if (Y == -1)
					{
						Y = oY;
						levels = i;
						limit = true;
						break;
					}
				}
			}

			if (Y == -1)
			{
				args.Player.SendMessage("You cannot go down anymore!", Color.OrangeRed);
				return;
			}
			if (args.Player.Teleport(args.Player.TileX, Y))
				if (limit)
					args.Player.SendMessage(string.Format("Teleported you down {0} level(s)! You cant go down any further!", levels), Color.MediumSeaGreen);
				else
					args.Player.SendMessage(string.Format("Teleported you down {0} level(s)!", levels), Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.OrangeRed);
		}
		#endregion

		#region ptime
		private void CMDptime(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				if (args.Player.Group.HasPermission("essentials.playertime.setother"))
					args.Player.SendMessage("Usage: /ptime <day/night/noon/midnight/reset> [player]", Color.OrangeRed);
				else
					args.Player.SendMessage("Usage: /ptime <day/night/noon/midnight/reset>", Color.OrangeRed);
				return;
			}

			TSPlayer ply = args.Player;
			if (args.Player.Group.HasPermission("essentials.playertime.setother") && args.Parameters.Count == 2)
			{
				var players = TShock.Utils.FindPlayer(args.Parameters[1]);
				if (players.Count == 0)
				{
					args.Player.SendMessage("No players matched!", Color.OrangeRed);
					return;
				}
				else if (players.Count > 1)
				{
					args.Player.SendMessage("More than one player matched!", Color.OrangeRed);
					return;
				}

				ply = players[0];
			}
			else if (!args.Player.Group.HasPermission("essentials.playertime.setother") && args.Parameters.Count == 2)
			{
				args.Player.SendMessage("Usage: /ptime <day/night/noon/midnight/reset>", Color.OrangeRed);
				return;
			}

			bool same = args.Player.Equals(ply);
			esPlayer ePly = esUtils.GetesPlayerByID(ply.Index);

			bool oldIsDay = Main.dayTime;
			double oldTime = Main.time;
			switch (args.Parameters[0].ToLower())
			{
				case "day":
					{
						ePly.pTime = 5;
						Main.dayTime = true; ePly.ptDay = true;
						Main.time = 150.0; ePly.ptTime = 150.0;
						ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
						Main.dayTime = oldIsDay;
						Main.time = oldTime;
						args.Player.SendMessage(string.Format("Set {0}'s time to permanently day!", ply.Name), Color.MediumSeaGreen);
						if (!same)
							ply.SendMessage(string.Format("{0} set your time to permanently day!", args.Player.Name), Color.MediumSeaGreen);
					}
					break;
				case "night":
					{
						ePly.pTime = 5;
						Main.dayTime = false; ePly.ptDay = false;
						Main.time = 0.0; ePly.ptTime = 0.0;
						ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
						Main.dayTime = oldIsDay;
						Main.time = oldTime;
						args.Player.SendMessage(string.Format("Set {0}'s time to permanently night!", ply.Name), Color.MediumSeaGreen);
						if (!same)
							ply.SendMessage(string.Format("{0} set your time to permanently night!", args.Player.Name), Color.MediumSeaGreen);
					}
					break;
				case "noon":
					{
						ePly.pTime = 5;
						Main.dayTime = true; ePly.ptDay = true;
						Main.time = 27000.0; ePly.ptTime = 27000.0;
						ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
						Main.dayTime = oldIsDay;
						Main.time = oldTime;
						args.Player.SendMessage(string.Format("Set {0}'s time to permanently noon!", ply.Name), Color.MediumSeaGreen);
						if (!same)
							ply.SendMessage(string.Format("{0} set your time to permanently noon!", args.Player.Name), Color.MediumSeaGreen);
					}
					break;
				case "midnight":
					{
						ePly.pTime = 5;
						Main.dayTime = false; ePly.ptDay = false;
						Main.time = 16200.0; ePly.ptTime = 16200.0;
						ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
						Main.dayTime = oldIsDay;
						Main.time = oldTime;
						args.Player.SendMessage(string.Format("Set {0}'s time to permanently midnight!", ply.Name), Color.MediumSeaGreen);
						if (!same)
							ply.SendMessage(string.Format("{0} set your time to permanently midnight!", args.Player.Name), Color.MediumSeaGreen);
					}
					break;
				case "reset":
					{
						ePly.pTime = -1;
						ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
						args.Player.SendMessage(string.Format("{0}'s time is the same as the servers!", ply.Name), Color.MediumSeaGreen);
						if (!same)
							ply.SendMessage(string.Format("{0} reset your time to the same as the servers!", args.Player.Name), Color.MediumSeaGreen);
					}
					break;
				default:
					args.Player.SendMessage("Usage: /ptime <day/night/dusk/noon/midnight/reset> [player]", Color.OrangeRed);
					break;
			}
		}
		#endregion

		#region ping
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

			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count == 0)
			{
				args.Player.SendMessage("No players matched!", Color.OrangeRed);
				return;
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.OrangeRed);
				return;
			}

			TSPlayer ply = players[0];
			if (ply.Group.HasPermission("essentials.sudo.immune"))
			{
				args.Player.SendMessage("You cannot force that player to do a command!", Color.OrangeRed);
				return;
			}
			Group oldGroup = null;
			if (args.Player.Group.HasPermission("essentials.sudo.super"))
			{
				oldGroup = ply.Group;
				ply.Group = new SuperAdminGroup();
			}

			List<string> commandParams = args.Parameters;
			commandParams.RemoveAt(0);

			string command = string.Join(" ", commandParams);
			if (!command.StartsWith("/"))
				command = string.Format("/{0}", command);

			Commands.HandleCommand(ply, command);
			args.Player.SendMessage(string.Format("Forced {0} to execute: {1}", ply.Name, command), Color.MediumSeaGreen);
			
			if (oldGroup != null)
				ply.Group = oldGroup;
		}
		#endregion

		#region SocialSpy
		private void CMDsocialspy(CommandArgs args)
		{
			esPlayer ePly = esUtils.GetesPlayerByID(args.Player.Index);
			
			ePly.SocialSpy = !ePly.SocialSpy;
			args.Player.SendMessage(string.Format("Socialspy {0}abled", (ePly.SocialSpy ? "En" : "Dis")), Color.MediumSeaGreen);
		}
		#endregion

		#region near
		private void CMDnear(CommandArgs args)
		{
			Dictionary<string, int> players = new Dictionary<string, int>();
			esPlayer sender = esUtils.GetesPlayerByID(args.Player.Index);

			foreach (esPlayer player in esPlayers)
			{
				if (player == null || player.Equals(sender)) continue;
				int x = Math.Abs(args.Player.TileX - player.TSPlayer.TileX);
				int y = Math.Abs(args.Player.TileY - player.TSPlayer.TileY);
				int h = (int)Math.Sqrt((double)(Math.Pow(x, 2) + Math.Pow(y, 2)));
				players.Add(player.TSPlayer.Name, h);
			}
			if (players.Count == 0)
			{
				args.Player.SendMessage("Nearby Players: none found", Color.MediumSeaGreen);
				return;
			}
			int i = 0;
			int v = 1;
			string currentLine = string.Empty;
			foreach (var pair in players.OrderBy(pair => pair.Value))
			{
				if (i == 0)
				{
					currentLine = string.Format("Nearby Players: {0}({1}m)", pair.Key, pair.Value);
				}
				else if (i == 2)
				{
					currentLine += string.Format(", {0}({1}m),", pair.Key, pair.Value);
					args.Player.SendMessage(currentLine, Color.MediumSeaGreen);
					v = 0;
				}
				else if (v == 4 || (i == (players.Count - 1)))
				{
					if (v == 1)
						currentLine = string.Format("{0}({1}m),", pair.Key, pair.Value);
					else
						currentLine += string.Format(", {0}({1}m),", pair.Key, pair.Value);
					args.Player.SendMessage(currentLine, Color.MediumSeaGreen);
					v = 0;
				}
				else if (i != 0 && v == 1)
					currentLine = string.Format("{0}({1}m)", pair.Key, pair.Value);
				else if (i != 0 && v != 1)
					currentLine += string.Format(", {0}({1}m)", pair.Key, pair.Value);

				if (i == 0 && players.Count == 1)
					args.Player.SendMessage(string.Format("{0},", currentLine), Color.MediumSeaGreen);
				i++; v++;
			}
		}
		#endregion

		#region nick
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
				var findPly = TShock.Utils.FindPlayer(args.Parameters[0]);
				if (findPly.Count == 0)
				{
					args.Player.SendMessage("No players matched!", Color.OrangeRed);
					return;
				}
				else if (findPly.Count > 1)
				{
					args.Player.SendMessage("More than one player matched!", Color.OrangeRed);
					return;
				}

				NickPly = findPly[0];
			}

			esPlayer eNickPly = esUtils.GetesPlayerByID(NickPly.Index);

			bool self = NickPly.Equals(args.Player);

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
			if (alphanumeric.Match(nickname).Success == false)
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
				foreach (esPlayer player in esPlayers)
				{
					if (player != null && player.HasNickName)
					{
						Nicks.Add(string.Format("{0}{1}({2})", getConfig.PrefixNicknamesWith, player.Nickname, player.OriginalName));
					}
				}

				if (Nicks.Count < 1)
					args.Player.SendMessage("No players online have nicknames!", Color.OrangeRed);
				else
					args.Player.SendMessage(string.Join(", ", Nicks), Color.MediumSeaGreen);
				return;
			}
			if (search.StartsWith(getConfig.PrefixNicknamesWith))
				search = search.Remove(0, getConfig.PrefixNicknamesWith.Length);

			List<esPlayer> found = new List<esPlayer>();
			foreach (esPlayer player in esPlayers)
			{
				if (player.HasNickName && player.Nickname.ToLower() == search)
				{
					found = new List<esPlayer> { player };
				}
				else if (player.HasNickName && player.Nickname.ToLower().Contains(search))
				{
					found.Add(player);
				}
			}
			if (found.Count == 0)
			{
				args.Player.SendMessage("No players matched!", Color.OrangeRed);
				return;
			}
			else if (found.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.OrangeRed);
				return;
			}

			esPlayer ply = found[0];

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
			if (TShock.Config.ServerSideInventory)
			{
				args.Player.SendWarningMessage("This command cannot be used when Server Side Inventory is enabled");
				return;
			}
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

			var player = args.Player;
			if (args.Parameters.Count == 2)
			{
				var found = TShock.Utils.FindPlayer(args.Parameters[1]);
				if (found.Count != 1)
				{
					args.Player.SendWarningMessage(string.Format("{0} matched!", found.Count < 1 ? "No players" : "More than one player"));
					return;
				}
				player = found[0];
			}

			player.Group = group;
			player.UserAccountName = user.Name;
			player.UserID = TShock.Users.GetUserID(player.UserAccountName);
			player.IsLoggedIn = true;
			player.IgnoreActionsForInventory = "none";

			player.SendSuccessMessage(string.Format("{0} in as {1}", (player != args.Player ? "{0} Logged you".SFormat(args.Player.Name) : "Logged"), user.Name));
			if (player != args.Player)
				args.Player.SendSuccessMessage("Logged " + player.Name + " in as " + user.Name);
			Log.ConsoleInfo(string.Format("{0} forced logged in {1}as user: {2}.", args.Player.Name, args.Player != player ? player.Name + " " : string.Empty, user.Name));
		}
		#endregion
	}
}