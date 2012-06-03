using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.Text;
using System.IO;
using MySql.Data.MySqlClient;
using Terraria;
using TShockAPI.DB;
using TShockAPI;
using Hooks;

namespace Essentials
{
	[APIVersion(1, 12)]
	public class Essentials : TerrariaPlugin
	{
		#region Variables
		public static Dictionary<string, int[]> disabled = new Dictionary<string, int[]>();
		public static List<esPlayer> esPlayers = new List<esPlayer>();
		public static SqlTableEditor SQLEditor;
		public static SqlTableCreator SQLWriter;
		public static DateTime TCheck = DateTime.UtcNow;
		public static bool DoCheck = true;

		public static esConfig getConfig { get; set; }
		internal static string TempConfigPath { get { return Path.Combine(TShock.SavePath, "PluginConfigs/EssentialsConfig.json"); } }
		#endregion

		#region Plugin Main
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
			get { return "some Essential commands for TShock!"; }
		}

		public override Version Version
		{
			get { return new Version("1.3.8.1"); }
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

		public Essentials(Main game)
			: base(game)
		{
			Order = -1;
			getConfig = new esConfig();
		}
		#endregion

		#region Hooks
		public void OnInitialize()
		{
			#region Add Commands
			Commands.ChatCommands.Add(new Command("fillstacks", more, "maxstacks"));
			Commands.ChatCommands.Add(new Command("getposition", getpos, "pos"));
			Commands.ChatCommands.Add(new Command("tppos", tppos, "tppos"));
			Commands.ChatCommands.Add(new Command("ruler", ruler, "ruler"));
			Commands.ChatCommands.Add(new Command("askadminhelp", helpop, "helpop"));
			Commands.ChatCommands.Add(new Command("commitsuicide", suicide, "suicide", "die"));
			Commands.ChatCommands.Add(new Command("pvpfun", burn, "burn"));
			Commands.ChatCommands.Add(new Command("butcher", killnpc, "killnpc"));
			Commands.ChatCommands.Add(new Command("kickall", kickall, "kickall"));
			Commands.ChatCommands.Add(new Command("moonphase", moon, "moon"));
			Commands.ChatCommands.Add(new Command("convertbiomes", cbiome, "cbiome", "bconvert"));
			Commands.ChatCommands.Add(new Command("searchids", sitems, "sitem", "si", "searchitem"));
			Commands.ChatCommands.Add(new Command("searchids", spage, "spage", "sp"));
			Commands.ChatCommands.Add(new Command("searchids", snpcs, "snpc", "sn", "searchnpc"));
			Commands.ChatCommands.Add(new Command("myhome", setmyhome, "sethome"));
			Commands.ChatCommands.Add(new Command("myhome", gomyhome, "myhome"));
			Commands.ChatCommands.Add(new Command("backontp", back, "b"));
			Commands.ChatCommands.Add(new Command("essentials", ReloadConfig, "essentials"));
			Commands.ChatCommands.Add(new Command(null, TeamUnlock, "teamunlock"));
			Commands.ChatCommands.Add(new Command("lastcommand", lastcmd, "="));
			Commands.ChatCommands.Add(new Command("kill", KillReason, "killr"));
			Commands.ChatCommands.Add(new Command("disable", Disable, "disable"));
			Commands.ChatCommands.Add(new Command("level-top", top, "top"));
			Commands.ChatCommands.Add(new Command("level-up", up, "up"));
			Commands.ChatCommands.Add(new Command("level-down", down, "down"));
			#endregion

			TCheck = DateTime.UtcNow;

			#region Setup SQL
			esSQL.SetupDB();
			#endregion

			#region Fix Permissions
			foreach (Group grp in TShock.Groups.groups)
			{
				if (grp.Name != "superadmin" && grp.HasPermission("backondeath") && !grp.HasPermission("backontp"))
					grp.AddPermission("backontp");
			}
			#endregion

			#region Check Config
			if (!Directory.Exists(@"tshock/PluginConfigs/"))
				Directory.CreateDirectory(@"tshock/PluginConfigs/");
			SetupConfig();
			#endregion
		}

		public void OnGreetPlayer(int who, HandledEventArgs e)
		{
			lock (esPlayers)
				esPlayers.Add(new esPlayer(who));


			if (TShock.Players[who] == null) return;
			if (disabled.ContainsKey(TShock.Players[who].Name))
			{
				var ply = GetesPlayerByID(who);
				if (ply == null) return;
				ply.disX = disabled[TShock.Players[who].Name][0];
				ply.disY = disabled[TShock.Players[who].Name][1];
				ply.TSPlayer.Teleport(ply.disX, ply.disY);
				ply.disabled = true;
				ply.TSPlayer.Disable();
				ply.lastdis = DateTime.UtcNow;
				ply.SendMessage("You are still disabled!", Color.IndianRed);
			}
		}

		public void OnLeave(int ply)
		{
			lock (esPlayers)
			{
				for (int i = 0; i < esPlayers.Count; i++)
				{
					if (esPlayers[i].Index == ply)
					{
						esPlayers.RemoveAt(i);
						break;
					}
				}
			}
		}

		public void OnChat(messageBuffer msg, int ply, string text, HandledEventArgs e)
		{

			if (text == "/")
				e.Handled = true;

			if (text.StartsWith("/") && text != "/=" && !text.StartsWith("/= ") && !text.StartsWith("/ "))
			{
				var player = GetesPlayerByID(ply);
				player.lastcmd = text;
			}

			if (text.StartsWith("/tp "))
			{
				#region /tp
				var player = TShock.Players[ply];
				if (player.Group.HasPermission("tp") && player.RealPlayer)
				{

					List<string> parms = new List<string>();
					string[] texts = text.Split(' ');
					for (int i = 1; i < texts.Length; i++)
					{
						parms.Add(texts[i]);
					}

					string plStr = String.Join(" ", parms);
					var players = TShock.Utils.FindPlayer(plStr);

					if (parms.Count > 0 && players.Count == 1 && players[0].TPAllow && player.Group.HasPermission(Permissions.tpall))
					{
						esPlayer play = GetesPlayerByName(player.Name);
						play.lastXtp = player.TileX;
						play.lastYtp = player.TileY;
						play.lastaction = "tp";
					}
				}
				#endregion
			}
			else if (text == "/home" || text.StartsWith("/home "))
			{
				#region /home
				var player = TShock.Players[ply];
				if (player.Group.HasPermission("tp") && player.RealPlayer)
				{
					esPlayer play = GetesPlayerByName(player.Name);
					play.lastXtp = player.TileX;
					play.lastYtp = player.TileY;
					play.lastaction = "tp";
				}
				#endregion
			}
			else if (text == "/spawn" || text.StartsWith("/spawn "))
			{
				#region /spawn
				var player = TShock.Players[ply];
				if (player.Group.HasPermission("tp") && player.RealPlayer)
				{
					esPlayer play = GetesPlayerByName(player.Name);
					play.lastXtp = player.TileX;
					play.lastYtp = player.TileY;
					play.lastaction = "tp";
				}
				#endregion
			}
			else if (text.StartsWith("/warp "))
			{
				#region /warp
				var player = TShock.Players[ply];
				if (player.Group.HasPermission("warp"))
				{
					List<string> parms = new List<string>();
					string[] textsp = text.Split(' ');
					for (int i = 1; i < textsp.Length; i++)
					{
						parms.Add(textsp[i]);
					}

					if (parms.Count > 0 && !parms[0].Equals("list"))
					{
						string warpName = String.Join(" ", parms);
						var warp = TShock.Warps.FindWarp(warpName);
						if (warp.WarpPos != Vector2.Zero)
						{
							esPlayer play = GetesPlayerByName(player.Name);
							play.lastXtp = player.TileX;
							play.lastYtp = player.TileY;
							play.lastaction = "tp";
						}
					}
				}
				#endregion
			}
		}

		public void GetData(GetDataEventArgs e)
		{
			try
			{
				switch (e.MsgID)
				{
					case PacketTypes.PlayerTeam:
						using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
						{
							var reader = new BinaryReader(data);
							var play = reader.ReadByte();
							var team = reader.ReadByte();
							reader.Close();
							esPlayer ply = GetesPlayerByID(play);
							var tply = TShock.Players[play];
							if (ply == null || tply == null) return;
							switch (team)
							{

								case 1: if (((getConfig.UsePermissonsForTeams && !tply.Group.HasPermission("jointeamred")) || (!getConfig.UsePermissonsForTeams && ply.redpass != getConfig.RedPassword)) && tply.Group.Name != "superadmin")
									{

										e.Handled = true;
										if (getConfig.UsePermissonsForTeams)
											tply.SendMessage("You do not have permission to join this team.", Color.Red);
										else
											tply.SendMessage("This team is locked, use /teamunlock red <password> to access it.", Color.Red);
										tply.SetTeam(tply.Team);

									} break;
								case 2: if (((getConfig.UsePermissonsForTeams && !tply.Group.HasPermission("jointeamgreen")) || (!getConfig.UsePermissonsForTeams && ply.greenpass != getConfig.GreenPassword)) && tply.Group.Name != "superadmin")
									{

										e.Handled = true;
										if (getConfig.UsePermissonsForTeams)
											tply.SendMessage("You do not have permission to join this team.", Color.Red);
										else
											tply.SendMessage("This team is locked, use /teamunlock green <password> to access it.", Color.Red);
										tply.SetTeam(tply.Team);

									} break;
								case 3: if (((getConfig.UsePermissonsForTeams && !tply.Group.HasPermission("jointeamblue")) || (!getConfig.UsePermissonsForTeams && ply.bluepass != getConfig.BluePassword)) && tply.Group.Name != "superadmin")
									{

										e.Handled = true;
										if (getConfig.UsePermissonsForTeams)
											tply.SendMessage("You do not have permission to join this team.", Color.Red);
										else
											tply.SendMessage("This team is locked, use /teamunlock blue <password> to access it.", Color.Red);
										tply.SetTeam(tply.Team);

									} break;
								case 4: if (((getConfig.UsePermissonsForTeams && !tply.Group.HasPermission("jointeamyellow")) || (!getConfig.UsePermissonsForTeams && ply.yellowpass != getConfig.YellowPassword)) && tply.Group.Name != "superadmin")
									{

										e.Handled = true;
										if (getConfig.UsePermissonsForTeams)
											tply.SendMessage("You do not have permission to join this team.", Color.Red);
										else
											tply.SendMessage("This team is locked, use /teamunlock yellow <password> to access it.", Color.Red);
										tply.SetTeam(tply.Team);

									} break;

							}
						}
						break;
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
		public static void OnUpdate()
		{
			if ((DateTime.UtcNow - TCheck).TotalMilliseconds >= 1000)
			{
				TCheck = DateTime.UtcNow;
				try
				{
					lock (esPlayers)
					{
						foreach (esPlayer play in esPlayers)
						{
							if (play == null) return;
							if (!play.ondeath && play.TSPlayer.Dead)
							{
								if (play.grpData.HasPermission("backondeath"))
								{
									play.lastXondeath = play.TSPlayer.TileX;
									play.lastYondeath = play.TSPlayer.TileY;
									play.ondeath = true;
									play.lastaction = "death";
									if (getConfig.ShowBackMessageOnDeath)
										play.SendMessage("Type \"/b\" to return to your position before you died", Color.MediumSeaGreen);
								}
							}
							else if (play.ondeath && !play.TSPlayer.Dead)
								play.ondeath = false;


							if (play.disabled && ((DateTime.UtcNow - play.lastdis).TotalMilliseconds) > 3000)
							{
								play.lastdis = DateTime.UtcNow;
								play.TSPlayer.Disable();
								int xc = play.TSPlayer.TileX;
								int xo = play.disX;
								int yc = play.TSPlayer.TileY;
								int yo = play.disY;
								if ((xc > xo + 5 || xc < xo - 5) || (yc > yo + 5 || yc < yo - 5))
								{
									play.TSPlayer.Teleport(play.disX, play.disY);
								}
							}
						}
					}
				}
				catch { }
			}
		}
		#endregion

		#region Config
		public static void SetupConfig()
		{
			try
			{
				if (File.Exists(TempConfigPath))
				{
					getConfig = esConfig.Read(TempConfigPath);
				}
				getConfig.Write(TempConfigPath);
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Error in Essentials config file");
				Console.ForegroundColor = ConsoleColor.Gray;
				Log.Error("Config Exception in Essentials config file");
				Log.Error(ex.ToString());
			}
		}

		public static void ReloadConfig(CommandArgs args)
		{
			try
			{
				if (!Directory.Exists(@"tshock/PluginConfigs/"))
				{
					Directory.CreateDirectory(@"tshock/PluginConfigs/");
				}

				if (File.Exists(TempConfigPath))
				{
					getConfig = esConfig.Read(TempConfigPath);
				}
				getConfig.Write(TempConfigPath);
				args.Player.SendMessage("Essentials Config Reloaded Successfully.", Color.MediumSeaGreen);
			}
			catch (Exception ex)
			{
				args.Player.SendMessage("Error: Could not reload Essentials config, Check log for more details.", Color.IndianRed);
				Log.Error("Config Exception in Essentials config file");
				Log.Error(ex.ToString());
			}
		}
		#endregion

		#region Methods
		//GET esPlayer!
		public static esPlayer GetesPlayerByID(int id)
		{
			esPlayer player = null;
			foreach (esPlayer ply in esPlayers)
			{
				if (ply.Index == id)
					return ply;
			}
			return player;
		}

		public static esPlayer GetesPlayerByName(string name)
		{
			var player = TShock.Utils.FindPlayer(name)[0];
			if (player != null)
			{
				foreach (esPlayer ply in esPlayers)
				{
					if (ply.TSPlayer == player)
						return ply;
				}
			}
			return null;
		}

		//HELPOP - BC to Admin
		public static void BroadcastToAdmin(CommandArgs plrsent, string msgtosend)
		{
			plrsent.Player.SendMessage("To Admins> " + plrsent.Player.Name + ": " + msgtosend, Color.RoyalBlue);
			foreach (esPlayer player in esPlayers)
			{
				if (player.grpData.HasPermission("recieveadminhelp"))
					player.SendMessage("[HO] " + plrsent.Player.Name + ": " + msgtosend, Color.RoyalBlue);
			}
		}

		//SEACH IDs
		public static List<Item> GetItemByName(string name)
		{
			var found = new List<Item>();
			for (int i = -24; i < Main.maxItemTypes; i++)
			{
				try
				{
					Item item = new Item();
					item.netDefaults(i);
					if (item.name.ToLower().Contains(name.ToLower()))
						found.Add(item);
				}
				catch { }
			}
			return found;
		}

		public static List<NPC> GetNPCByName(string name)
		{
			var found = new List<NPC>();
			for (int i = 1; i < Main.maxNPCTypes; i++)
			{
				NPC npc = new NPC();
				npc.netDefaults(i);
				if (npc.name.ToLower().Contains(name.ToLower()))
					found.Add(npc);
			}
			return found;
		}

		//^String Builder
		public static void BCsearchitem(CommandArgs args, List<Item> list, int page)
		{
			args.Player.SendMessage("Item Search:", Color.Yellow);
			var sb = new StringBuilder();
			if (list.Count > (8 * (page - 1)))
			{
				for (int j = (8 * (page - 1)); j < (8 * page); j++)
				{
					if (sb.Length != 0)
						sb.Append(" | ");
					sb.Append(list[j].netID).Append(": ").Append(list[j].name);
					if (j == list.Count - 1)
					{
						args.Player.SendMessage(sb.ToString(), Color.MediumSeaGreen);
						break;
					}
					if ((j + 1) % 2 == 0)
					{
						args.Player.SendMessage(sb.ToString(), Color.MediumSeaGreen);
						sb.Clear();
					}
				}
			}
			if (list.Count > (8 * page))
			{
				args.Player.SendMessage(string.Format("Type /spage {0} for more Results.", (page + 1)), Color.Yellow);
			}
		}

		public static void BCsearchnpc(CommandArgs args, List<NPC> list, int page)
		{
			args.Player.SendMessage("NPC Search:", Color.Yellow);
			var sb = new StringBuilder();
			if (list.Count > (8 * (page - 1)))
			{
				for (int j = (8 * (page - 1)); j < (8 * page); j++)
				{
					if (sb.Length != 0)
						sb.Append(" | ");
					sb.Append(list[j].netID).Append(": ").Append(list[j].name);
					if (j == list.Count - 1)
					{
						args.Player.SendMessage(sb.ToString(), Color.MediumSeaGreen);
						break;
					}
					if ((j + 1) % 2 == 0)
					{
						args.Player.SendMessage(sb.ToString(), Color.MediumSeaGreen);
						sb.Clear();
					}
				}
			}
			if (list.Count > (8 * page))
			{
				args.Player.SendMessage(string.Format("Type /spage {0} for more Results.", (page + 1)), Color.Yellow);
			}
		}

		//Top, Up and Down Methods:
		public static int GetTop(int TileX)
		{
			for (int y = 0; y < Main.maxTilesY; y++)
			{
				if (WorldGen.SolidTile(TileX, y))
					return y - 1;
			}
			return -1;
		}

		public static int GetUp(int TileX, int TileY)
		{
			for (int y = TileY - 2; y > 0; y--)
			{
				if (!WorldGen.SolidTile(TileX, y))
				{
					if (WorldGen.SolidTile(TileX, y + 1) && !WorldGen.SolidTile(TileX, y - 1) && !WorldGen.SolidTile(TileX, y - 2))
					{
						return y;
					}
				}
			}
			return -1;
		}

		public static int GetDown(int TileX, int TileY)
		{
			for (int y = TileY + 4; y < Main.maxTilesY; y++)
			{
				if (!WorldGen.SolidTile(TileX, y))
				{
					if (WorldGen.SolidTile(TileX, y + 1) && !WorldGen.SolidTile(TileX, y - 1) && !WorldGen.SolidTile(TileX, y - 2))
					{
						return y;
					}
				}
			}
			return -1;
		}
		#endregion

		/* Commands: */

		#region Fill Items
		public static void more(CommandArgs args)
		{
			int i = 0;
			foreach (Item item in args.TPlayer.inventory)
			{
				int togive = item.maxStack - item.stack;
				if (item.stack != 0 && i <= 39)
					args.Player.GiveItem(item.type, item.name, item.width, item.height, togive);
				i++;
			}
		}
		#endregion

		#region Position Commands
		public static void getpos(CommandArgs args)
		{
			args.Player.SendMessage("X Position: " + args.Player.TileX + " - Y Position: " + args.Player.TileY, Color.Yellow);
		}

		public static void tppos(CommandArgs args)
		{
			if (args.Parameters.Count != 2)
				args.Player.SendMessage("Format is: /tppos <X> <Y>", Color.Red);
			else
			{
				int xcord = 0;
				int ycord = 0;
				int.TryParse(args.Parameters[0], out xcord);
				int.TryParse(args.Parameters[1], out ycord);
				esPlayer play = GetesPlayerByName(args.Player.Name);
				play.lastXtp = args.Player.TileX;
				play.lastYtp = args.Player.TileY;
				play.lastaction = "tp";
				if (args.Player.Teleport(xcord, ycord))
					args.Player.SendMessage("Teleported you to X: " + xcord + " - Y: " + ycord, Color.MediumSeaGreen);
			}
		}

		public static void ruler(CommandArgs args)
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
					args.Player.SendMessage("Invalid Points! To set points use: /ruler [1/2]", Color.Red);
				else
				{
					var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
					var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);
					args.Player.SendMessage("Area Height: " + height + " Width: " + width, Color.LightGreen);
					args.Player.TempPoints[0] = Point.Zero; args.Player.TempPoints[1] = Point.Zero;
				}
			}
		}
		#endregion

		#region Help OP
		public static void helpop(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /helpop <message>", Color.Red);
				return;
			}

			if (args.Parameters.Count > 0)
			{
				string text = "";

				foreach (string word in args.Parameters)
				{
					text = text + word + " ";
				}

				BroadcastToAdmin(args, text);
			}
			else
			{
				args.Player.SendMessage("Usage: /helpop <message>", Color.Red);
			}

		}
		#endregion

		#region Suicide
		public static void suicide(CommandArgs args)
		{
			if (!args.Player.RealPlayer)
				return;

			NetMessage.SendData((int)PacketTypes.PlayerDamage, -1, -1, " decided it wasnt worth living.", args.Player.Index, ((new Random()).Next(-1, 1)), 9999, (float)0);
		}

		public static void burn(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /burn <player> [time]", Color.IndianRed);
				return;
			}
			int duration = 1800;
			foreach (string parameter in args.Parameters)
			{
				int isduration = 0;
				bool IsaNum = int.TryParse(parameter, out isduration);
				if (IsaNum)
					duration = isduration * 60;
			}

			var player = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (player.Count == 0)
				args.Player.SendMessage("Invalid player!", Color.Red);
			else if (player.Count > 1)
				args.Player.SendMessage("More than one player matched!", Color.Red);
			else
			{
				player[0].SetBuff(24, duration);
				args.Player.SendMessage(player[0].Name + " Has been set on fire! for " + (duration / 60) + " seconds", Color.MediumSeaGreen);
			}
		}
		#endregion

		#region killnpc
		public static void killnpc(CommandArgs args)
		{
			if (args.Parameters.Count != 0)
			{

				var npcselected = TShock.Utils.GetNPCByIdOrName(args.Parameters[0]);
				if (npcselected.Count == 0)
					args.Player.SendMessage("Invalid NPC!", Color.Red);
				else if (npcselected.Count > 1)
					args.Player.SendMessage("More than one NPC matched!", Color.Red);
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
		public static void kickall(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				foreach (esPlayer player in esPlayers)
				{
					if (!player.grpData.HasPermission("immunetokickall"))
					{
						player.Kick("Everyone has been kicked from the server!");
					}
				}
			}

			if (args.Parameters.Count > 0)
			{
				string text = "";

				foreach (string word in args.Parameters)
					text = text + word + " ";

				foreach (esPlayer player in esPlayers)
				{
					if (!player.grpData.HasPermission("immunetokickall"))
						player.Kick("Everyone has been kicked (" + text + ")");
				}
				TShock.Utils.Broadcast("Everyone has been kicked from the server!");
			}
			else
			{
				foreach (esPlayer player in esPlayers)
				{
					if (!player.grpData.HasPermission("immunetokickall"))
						player.Kick("Everyone has been kicked from the server!");
				}
				TShock.Utils.Broadcast("Everyone has been kicked from the server!");
			}
		}
		#endregion

		#region Moon Phase
		public static void moon(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /moon [ new | 1/4 | half | 3/4 | full ]", Color.OrangeRed);
				return;
			}

			string subcmd = args.Parameters[0].ToLower();

			if (subcmd == "new")
			{
				Main.moonPhase = 4;
				args.Player.SendMessage("Moon Phase set to New Moon, This takes a while to update!", Color.MediumSeaGreen);
			}
			else if (subcmd == "1/4")
			{
				Main.moonPhase = 3;
				args.Player.SendMessage("Moon Phase set to 1/4 Moon, This takes a while to update!", Color.MediumSeaGreen);
			}
			else if (subcmd == "half")
			{
				Main.moonPhase = 2;
				args.Player.SendMessage("Moon Phase set to Half Moon, This takes a while to update!", Color.MediumSeaGreen);
			}
			else if (subcmd == "3/4")
			{
				Main.moonPhase = 1;
				args.Player.SendMessage("Moon Phase set to 3/4 Moon, This takes a while to update!", Color.MediumSeaGreen);
			}
			else if (subcmd == "full")
			{
				Main.moonPhase = 0;
				args.Player.SendMessage("Moon Phase set to Full Moon, This takes a while to update!", Color.MediumSeaGreen);
			}
			else
				args.Player.SendMessage("Usage: /moon [ new | 1/4 | half | 3/4 | full ]", Color.OrangeRed);
		}
		#endregion

		#region Back
		private static void back(CommandArgs args)
		{
			esPlayer play = GetesPlayerByName(args.Player.Name);
			if (play.lastaction == "none")
				args.Player.SendMessage("You do not have a /b position stored", Color.MediumSeaGreen);
			else if (play.lastaction == "death")
			{
				int Xdeath = play.lastXondeath;
				int Ydeath = play.lastYondeath;
				if (args.Player.Teleport(Xdeath, Ydeath))
					args.Player.SendMessage("Moved you to your position before you died!", Color.MediumSeaGreen);
			}
			else if (play.grpData.HasPermission("backontp"))
			{
				int Xtp = play.lastXtp;
				int Ytp = play.lastYtp;
				if (args.Player.Teleport(Xtp, Ytp))
					args.Player.SendMessage("Moved you to your position before you last teleported", Color.MediumSeaGreen);
			}
			else if (play.lastaction == "death" && !play.grpData.HasPermission("backondeath"))
			{
				args.Player.SendMessage("You do not have permission to /b after death", Color.MediumSeaGreen);
			}
		}
		#endregion

		#region Convert Biomes
		public static void cbiome(CommandArgs args)
		{
			if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
			{
				args.Player.SendMessage("Usage: /cbiome <from> <to> [region]", Color.IndianRed);
				args.Player.SendMessage("Possible Biomes: Corruption, Hallow, Normal", Color.IndianRed);
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
					args.Player.SendMessage("You must specify a valid region to convert a normal biome.", Color.IndianRed);
				else if (to == "normal")
					args.Player.SendMessage("You cannot convert Normal to Normal.", Color.IndianRed);
				else if (to == "hallow" && doregion)
				{
					args.Player.SendMessage("Server might lag for a moment.", Color.IndianRed);
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
					args.Player.SendMessage("Server might lag for a moment.", Color.IndianRed);
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
					args.Player.SendMessage("You must specify a valid region to convert a normal biome.", Color.IndianRed);
				else if (to == "hallow")
					args.Player.SendMessage("You cannot convert Hallow to hallow.", Color.IndianRed);
				else if (to == "corruption")
				{
					args.Player.SendMessage("Server might lag for a moment.", Color.IndianRed);
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
					args.Player.SendMessage("Server might lag for a moment.", Color.IndianRed);
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
					args.Player.SendMessage("You must specify a valid region to convert a normal biome.", Color.IndianRed);
				else if (to == "corruption")
					args.Player.SendMessage("You cannot convert Corruption to Corruption.", Color.IndianRed);
				else if (to == "hallow")
				{
					args.Player.SendMessage("Server might lag for a moment.", Color.IndianRed);
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
					args.Player.SendMessage("Server might lag for a moment.", Color.IndianRed);
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
				{
					args.Player.SendMessage("Error, Useable values: Hallow, Corruption, Normal", Color.IndianRed);
				}
			}
		}
		#endregion

		#region Seach IDs
		public static void spage(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /spage <page>", Color.IndianRed);
				return;
			}

			if (args.Parameters.Count == 1)
			{
				int pge = 1;
				bool PageIsNum = int.TryParse(args.Parameters[0], out pge);
				if (!PageIsNum)
				{
					args.Player.SendMessage("Specified page invalid!", Color.IndianRed);
					return;
				}

				foreach (esPlayer play in esPlayers)
				{
					if (play.plrName == args.Player.Name)
					{
						if (play.lastseachtype == "none")
							args.Player.SendMessage("You must complete a Item/NPC id search first!", Color.IndianRed);
						else if (play.lastseachtype == "Item")
						{
							var items = GetItemByName(play.lastsearch);
							BCsearchitem(args, items, pge);
						}
						else if (play.lastseachtype == "NPC")
						{
							var npcs = GetNPCByName(play.lastsearch);
							BCsearchnpc(args, npcs, pge);
						}
					}
				}
			}
		}

		public static void sitems(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /sitem <search term>", Color.IndianRed);
				return;
			}

			if (args.Parameters.Count > 0)
			{
				string sterm = "";

				if (args.Parameters.Count == 1)
					sterm = args.Parameters[0];
				else
				{
					foreach (string wrd in args.Parameters)
						sterm = sterm + wrd + " ";

					sterm = sterm.Remove(sterm.Length - 1);
				}

				var items = GetItemByName(sterm);

				if (items.Count == 0)
				{
					args.Player.SendMessage("Could not find any matching items!", Color.IndianRed);
					return;
				}
				else
				{
					foreach (esPlayer play in esPlayers)
					{
						if (play.plrName == args.Player.Name)
						{
							play.lastsearch = sterm;
							play.lastseachtype = "Item";
							BCsearchitem(args, items, 1);
						}
					}
				}
			}
		}

		public static void snpcs(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Usage: /snpc <search term>", Color.IndianRed);
				return;
			}

			if (args.Parameters.Count > 0)
			{
				string sterm = "";

				if (args.Parameters.Count == 1)
					sterm = args.Parameters[0];
				else
				{
					foreach (string wrd in args.Parameters)
						sterm = sterm + wrd + " ";

					sterm = sterm.Remove(sterm.Length - 1);
				}

				var npcs = GetNPCByName(sterm);

				if (npcs.Count == 0)
				{
					args.Player.SendMessage("Could not find any matching items!", Color.IndianRed);
					return;
				}
				else
				{
					foreach (esPlayer play in esPlayers)
					{
						if (play.plrName == args.Player.Name)
						{
							play.lastsearch = sterm;
							play.lastseachtype = "NPC";
							BCsearchnpc(args, npcs, 1);
						}
					}
				}
			}
		}
		#endregion

		#region MyHome
		public static void setmyhome(CommandArgs args)
		{
			if (args.Player.IsLoggedIn)
			{
				/* Check if the player has a home */
				bool hashome = esSQL.HasHome(args.Player.UserID, Main.worldID);

				if (hashome)
				{
					/* If the player has a home, Update it */
					esSQL.UpdateHome(args.Player.TileX, args.Player.TileY, args.Player.UserID, Main.worldID);

					args.Player.SendMessage("Updated your home position!", Color.MediumSeaGreen);
				}
				else
				{
					/* If the player doesn't have a home, Create one */
					esSQL.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, Main.worldID);

					args.Player.SendMessage("Created your home!", Color.MediumSeaGreen);
				}
			}
			else
			{
				args.Player.SendMessage("You must be logged in to do that!", Color.IndianRed);
			}
		}

		public static void gomyhome(CommandArgs args)
		{
			if (args.Player.IsLoggedIn)
			{
				/* Check if the player has a home */
				bool hashome = esSQL.HasHome(args.Player.UserID, Main.worldID);

				if (hashome)
				{
					int homeX = Main.spawnTileX; int homeY = Main.spawnTileY;

					/* Get the player's home co-ords */
					esSQL.GetHome(args.Player.UserID, Main.worldID, out homeX, out homeY);

					esPlayer play = GetesPlayerByName(args.Player.Name);
					play.lastXtp = args.Player.TileX;
					play.lastYtp = args.Player.TileY;
					play.lastaction = "tp";

					args.Player.Teleport(homeX, homeY);
					args.Player.SendMessage("Teleported to your home!", Color.MediumSeaGreen);
				}
				else
				{
					args.Player.SendMessage("You have not set a home. type: \"/sethome\" to set one.", Color.IndianRed);
				}
			}
			else
			{
				args.Player.SendMessage("You must be logged in to do that!", Color.IndianRed);
			}
		}
		#endregion

		#region Essentials Reload
		/* will add again when I have a use for this command other than reloading the config.
        public static void cmdessentials(CommandArgs args)
        {
            ReloadConfig(args);
        }*/
		#endregion

		#region jointeam
		public static void TeamUnlock(CommandArgs args)
		{
			if (getConfig.UsePermissonsForTeams)
			{
				args.Player.SendMessage("You are not able to unlock teams!", Color.Red);
				return;
			}

			if (args.Parameters.Count < 2)
			{
				args.Player.SendMessage("Usage: /teamunlock <color> <password>", Color.Red);
				return;
			}

			string subcmd = args.Parameters[0].ToLower();
			string password = "";
			for (int i = 1; i < args.Parameters.Count; i++)
			{
				password = password + args.Parameters[i] + " ";
			}
			password = password.Remove(password.Length - 1, 1);

			esPlayer ply = GetesPlayerByID(args.Player.Index);

			if (subcmd == "red")
			{
				ply.redpass = password;
				if (password == getConfig.RedPassword)
					args.Player.SendMessage("You can now join red team!", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Incorrect Password!", Color.IndianRed);
			}
			else if (subcmd == "green")
			{
				ply.greenpass = password;
				if (password == getConfig.GreenPassword)
					args.Player.SendMessage("You can now join green team!", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Incorrect Password!", Color.IndianRed);
			}
			else if (subcmd == "blue")
			{
				ply.bluepass = password;
				if (password == getConfig.BluePassword)
					args.Player.SendMessage("You can now join blue team!", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Incorrect Password!", Color.IndianRed);
			}
			else if (subcmd == "yellow")
			{
				ply.yellowpass = password;
				if (password == getConfig.YellowPassword)
					args.Player.SendMessage("You can now join yellow team!", Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Incorrect Password!", Color.IndianRed);
			}
			else
			{
				args.Player.SendMessage("Usage: /teamunlock <red/green/blue/yellow> <password>", Color.Red);
				return;
			}
		}
		#endregion

		#region Last Command
		public static void lastcmd(CommandArgs args)
		{
			var player = GetesPlayerByID(args.Player.Index);

			if (player.lastcmd == "/=" || player.lastcmd.StartsWith("/= "))
			{
				args.Player.SendMessage("Error with last command!", Color.IndianRed);
				return;
			}

			if (player.lastcmd == "")
				args.Player.SendMessage("You have not entered a command yet.", Color.IndianRed);
			else
				TShockAPI.Commands.HandleCommand(args.Player, player.lastcmd);
		}
		#endregion

		#region Kill For a reason
		public static void KillReason(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /killr <player> <reason>", Color.Red);
				return;
			}

			var players = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (players.Count == 0)
			{
				args.Player.SendMessage("Invalid player!", Color.Red);
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.Red);
			}
			else
			{
				var plr = players[0];
				string reason = " ";
				for (int i = 1; i < args.Parameters.Count; i++)
					reason = reason + args.Parameters[i] + " ";

				NetMessage.SendData((int)PacketTypes.PlayerDamage, -1, -1, reason, plr.Index, ((new Random()).Next(-1, 1)), 999999, (float)0);
				args.Player.SendMessage(string.Format("You just killed {0}!", plr.Name));
				plr.SendMessage(string.Format("{0} just killed you!", args.Player.Name));
			}
		}
		#endregion

		#region Disable
		public static void Disable(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendMessage("Invalid syntax! Proper syntax: /disable <player> <reason>", Color.Red);
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
				args.Player.SendMessage("Invalid player!", Color.Red);
			}
			else if (players.Count > 1)
			{
				args.Player.SendMessage("More than one player matched!", Color.Red);
			}
			else
			{
				var plr = players[0];
				var plre = GetesPlayerByID(plr.Index);

				if (!plre.disabled)
				{
					string reason = "";
					for (int i = 1; i < args.Parameters.Count; i++)
						reason = reason + args.Parameters[i] + " ";

					plre.disX = plr.TileX;
					plre.disY = plr.TileY;
					plre.disabled = true;
					plr.Disable(reason);
					plre.lastdis = DateTime.UtcNow;

					int[] pos = new int[2];
					pos[0] = plre.disX;
					pos[1] = plre.disY;

					disabled.Add(plr.Name, pos);

					args.Player.SendMessage(string.Format("You disabled {0}, He can not be enabled until you type /disable {1}", plr.Name, args.Parameters[0]));
					if (reason == "")
						plr.SendMessage(string.Format("You have been disabled by {0}", args.Player.Name, reason));
					else
						plr.SendMessage(string.Format("You have been disabled by {0} for {1}", args.Player.Name, reason));
				}
				else
				{
					plre.disabled = false;
					plre.disX = 0;
					plre.disY = 0;

					disabled.Remove(plr.Name);

					args.Player.SendMessage(string.Format("{0} is no longer disabled!", plr.Name));
					plr.SendMessage("You are no longer disabled!");
				}
			}
		}
		#endregion

		#region Top, Up and Down
		public static void top(CommandArgs args)
		{
			int Y = GetTop(args.Player.TileX);
			if (Y == -1)
			{
				args.Player.SendMessage("You are already on the top most block!", Color.IndianRed);
				return;
			}
			esPlayer play = GetesPlayerByName(args.Player.Name);
			play.lastXtp = args.Player.TileX;
			play.lastYtp = args.Player.TileY;
			play.lastaction = "tp";
			if (args.Player.Teleport(args.Player.TileX, Y))
				args.Player.SendMessage("Teleported you to the top-most block!", Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.IndianRed);
		}

		public static void up(CommandArgs args)
		{
			int levels = 1;
			bool limit = false;
			if (args.Parameters.Count > 0)
				int.TryParse(args.Parameters[0], out levels);

			int Y = -1;
			for (int i = 0; i < levels; i++)
			{
				if (i == 0) Y = GetUp(args.Player.TileX, args.Player.TileY);
				else
				{
					int oY = Y;
					Y = GetUp(args.Player.TileX, Y);
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
				args.Player.SendMessage("You cannot go up anymore!", Color.IndianRed);
				return;
			}
			if (args.Player.Teleport(args.Player.TileX, Y))
				if (limit)
					args.Player.SendMessage("Teleported you up {0} level(s)! You cant go up any further!".SFormat(levels), Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Teleported you up {0} level(s)!".SFormat(levels), Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.IndianRed);
		}

		public static void down(CommandArgs args)
		{
			bool limit = false;
			int levels = 1;
			if (args.Parameters.Count > 0)
				int.TryParse(args.Parameters[0], out levels);

			int Y = -1;
			for (int i = 0; i < levels; i++)
			{
				if (i == 0) Y = GetDown(args.Player.TileX, args.Player.TileY);
				else
				{
					int oY = Y;
					Y = GetDown(args.Player.TileX, Y);
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
				args.Player.SendMessage("You cannot go down anymore!", Color.IndianRed);
				return;
			}
			if (args.Player.Teleport(args.Player.TileX, Y))
				if (limit)
					args.Player.SendMessage("Teleported you down {0} level(s)! You cant go down any further!".SFormat(levels), Color.MediumSeaGreen);
				else
					args.Player.SendMessage("Teleported you down {0} level(s)!".SFormat(levels), Color.MediumSeaGreen);
			else
				args.Player.SendMessage("Teleport Failed!", Color.IndianRed);
		}
		#endregion
	}
}