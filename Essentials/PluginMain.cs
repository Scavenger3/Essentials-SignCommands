using System;
using System.Collections.Generic;
using System.Reflection;
using System.Drawing;
using Terraria;
using Hooks;
using TShockAPI;
using TShockAPI.DB;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;
using System.Threading;
using System.Text;
using MySql.Data.MySqlClient;
using System.Data;

namespace Essentials
{
    [APIVersion(1, 11)]
    public class Essentials : TerrariaPlugin
    {
        public static List<esPlayer> esPlayers = new List<esPlayer>();
        public static SqlTableEditor SQLEditor;
        public static SqlTableCreator SQLWriter;
        public DateTime CountDown = DateTime.UtcNow;

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
            get { return new Version("1.2.3"); }
        }

        public override void Initialize()
        {
            GameHooks.Update += OnUpdate;
            GameHooks.Initialize += OnInitialize;
            NetHooks.GreetPlayer += OnGreetPlayer;
            ServerHooks.Leave += OnLeave;
            ServerHooks.Chat += OnChat;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Update -= OnUpdate;
                GameHooks.Initialize -= OnInitialize;
                NetHooks.GreetPlayer -= OnGreetPlayer;
                ServerHooks.Leave -= OnLeave;
                ServerHooks.Chat -= OnChat;
            }
            base.Dispose(disposing);
        }

        public Essentials(Main game)
            : base(game)
        {
        }

        public void OnInitialize()
        {
            SQLEditor = new SqlTableEditor(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
            SQLWriter = new SqlTableCreator(TShock.DB, TShock.DB.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());

            var table = new SqlTable("EssentialsUserHomes",
                new SqlColumn("LoginName", MySqlDbType.Int32) { Unique = true },
                new SqlColumn("HomeX", MySqlDbType.Int32),
                new SqlColumn("HomeY", MySqlDbType.Int32)
            );
            SQLWriter.EnsureExists(table);

            Commands.ChatCommands.Add(new Command("fillstacks", more, "maxstacks"));
            Commands.ChatCommands.Add(new Command("getposition", getpos, "pos"));
            Commands.ChatCommands.Add(new Command("tp", tppos, "tppos"));
            Commands.ChatCommands.Add(new Command("ruler", ruler, "ruler"));
            Commands.ChatCommands.Add(new Command("askadminhelp", helpop, "helpop"));
            Commands.ChatCommands.Add(new Command("commitsuicide", suicide, "suicide", "die"));
            Commands.ChatCommands.Add(new Command("setonfire", burn, "burn"));
            Commands.ChatCommands.Add(new Command("killnpcs", killnpc, "killnpc"));
            Commands.ChatCommands.Add(new Command("kickall", kickall, "kickall"));
            Commands.ChatCommands.Add(new Command("moonphase", moon, "moon"));
            Commands.ChatCommands.Add(new Command("convertbiomes", cbiome, "cbiome", "bconvert"));
            Commands.ChatCommands.Add(new Command("searchids", sitems, "sitem", "si", "searchitem"));
            Commands.ChatCommands.Add(new Command("searchids", spage, "spage", "sp"));
            Commands.ChatCommands.Add(new Command("searchids", snpcs, "snpc", "sn", "searchnpc"));
            Commands.ChatCommands.Add(new Command("myhome", setmyhome, "sethome"));
            Commands.ChatCommands.Add(new Command("myhome", gomyhome, "myhome"));

            Commands.ChatCommands.Add(new Command("backontp", back, "b")); //For These Commands \/
            Commands.ChatCommands.Add(new Command("backontp", tp, "btp"));
            Commands.ChatCommands.Add(new Command("backontp", Home, "bhome"));
            Commands.ChatCommands.Add(new Command("backontp", Spawn, "bspawn"));
            Commands.ChatCommands.Add(new Command("backontp", warp, "bwarp"));
        }

        #region Get Lists

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
        #endregion

        public void OnUpdate()
        {
            try
            {
                double tick1 = (DateTime.UtcNow - CountDown).TotalMilliseconds;
                if (tick1 > 1000)
                {
                    lock (esPlayers)
                    {
                        foreach (esPlayer play in esPlayers)
                        {
                            if (play.TSPlayer.Dead && !play.ondeath && play != null)
                            {
                                play.lastXondeath = play.TSPlayer.TileX;
                                play.lastYondeath = play.TSPlayer.TileY;
                                if (play.grpData.HasPermission("backondeath"))
                                    play.SendMessage("Type \"/b\" to return to your position before you died", Color.MediumSeaGreen);
                                play.ondeath = true;
                                play.lastaction = "death";
                            }
                            else if (!play.TSPlayer.Dead && play.ondeath && play != null)
                                play.ondeath = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error("This Essentials Error is a known bug!");
                Log.Error(ex.ToString());
            }
        }

        public static void BroadcastToAdmin(CommandArgs plrsent, string msgtosend)
        {
            plrsent.Player.SendMessage("To Admins> " + plrsent.Player.Name + ": " + msgtosend, Color.RoyalBlue);
            foreach (esPlayer player in esPlayers)
            {
                if (player.grpData.HasPermission("recieveadminhelp"))
                    player.SendMessage("[HO] " + plrsent.Player.Name + ": " + msgtosend, Color.RoyalBlue);
            }
        }

        public void OnGreetPlayer(int who, HandledEventArgs e)
        {
            lock (esPlayers)
                esPlayers.Add(new esPlayer(who));
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
        }

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
                Point pnts1 = args.Player.TempPoints[0];
                Point pnts2 = args.Player.TempPoints[1];
                if (pnts1.X == 0 && pnts1.Y == 0 && pnts2.X == 0 && pnts2.Y == 0)
                    args.Player.SendMessage("Invalid Points! To set poits use: /ruler [1/2]", Color.Red);
                else
                {
                    args.Player.SendMessage("Point 1 - X: " + pnts1.X + " Y: " + pnts1.Y, Color.LightGreen);
                    args.Player.SendMessage("Point 2 - X: " + pnts2.X + " Y: " + pnts2.Y, Color.LightGreen);
                    int changeX = pnts2.X - pnts1.X;
                    int changeY = pnts1.Y - pnts2.Y;
                    args.Player.SendMessage("From Point 1 to 2 - X: " + changeX + " Y: " + changeY, Color.LightGreen);
                }
            }
        }

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
        public static void suicide(CommandArgs args)
        {
            args.Player.DamagePlayer(9999);
        }

        public static void burn(CommandArgs args)
        {
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
        public static void kickall(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                foreach (esPlayer player in esPlayers)
                {
                    if (!player.grpData.HasPermission("immunetokickall"))
                    {
                        player.Kick("Everyone has been kicked from the server!");
                        TShock.Utils.Broadcast("Everyone has been kicked from the server");
                    }
                }
            }

            if (args.Parameters.Count > 0)
            {
                string text = "";

                foreach (string word in args.Parameters)
                {
                    text = text + word + " ";
                }

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
        public static void moon(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Usage: /moon [new | 1/4 | half | 3/4 | full]", Color.OrangeRed);
                return;
            }

            string subcmd = args.Parameters[0].ToLower();

            if (subcmd == "new")
            {
                Main.moonPhase = 4;
                args.Player.SendMessage("Moon Phase set to New Moon, This takes a while to update", Color.MediumSeaGreen);
            }
            else if (subcmd == "1/4")
            {
                Main.moonPhase = 3;
                args.Player.SendMessage("Moon Phase set to 1/4 Moon, This takes a while to update", Color.MediumSeaGreen);
            }
            else if (subcmd == "half")
            {
                Main.moonPhase = 2;
                args.Player.SendMessage("Moon Phase set to Half Moon, This takes a while to update", Color.MediumSeaGreen);
            }
            else if (subcmd == "3/4")
            {
                Main.moonPhase = 1;
                args.Player.SendMessage("Moon Phase set to 3/4 Moon, This takes a while to update", Color.MediumSeaGreen);
            }
            else if (subcmd == "full")
            {
                Main.moonPhase = 0;
                args.Player.SendMessage("Moon Phase set to Full Moon, This takes a while to update", Color.MediumSeaGreen);
            }
            else
                args.Player.SendMessage("Usage: /moon [new | 1/4 | half | 3/4 | full]", Color.OrangeRed);
        }

        public static void tp(CommandArgs args)
        {
            if (!args.Player.RealPlayer)
            {
                args.Player.SendMessage("You cannot use teleport commands!");
                return;
            }

            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /btp <player> ", Color.Red);
                return;
            }

            string plStr = String.Join(" ", args.Parameters);
            var players = TShock.Utils.FindPlayer(plStr);
            if (players.Count == 0)
                args.Player.SendMessage("Invalid player!", Color.Red);
            else if (players.Count > 1)
                args.Player.SendMessage("More than one player matched!", Color.Red);
            else if (!players[0].TPAllow && !args.Player.Group.HasPermission(Permissions.tpall))
            {
                var plr = players[0];
                args.Player.SendMessage(plr.Name + " Has Selected For Users Not To Teleport To Them");
                plr.SendMessage(args.Player.Name + " Attempted To Teleport To You");
            }
            else
            {
                var plr = players[0];
                esPlayer play = GetesPlayerByName(args.Player.Name);
                if (plr.Name == play.plrName)
                {
                    play.lastXtp = plr.TileX;
                    play.lastYtp = plr.TileY;
                    play.lastaction = "tp";
                }
                if (args.Player.Teleport(plr.TileX, plr.TileY + 3))
                    args.Player.SendMessage(string.Format("Teleported to {0}", plr.Name));
            }
        }

        private static void Home(CommandArgs args)
        {
            if (!args.Player.RealPlayer)
            {
                args.Player.SendMessage("You cannot use teleport commands!");
                return;
            }

            esPlayer play = GetesPlayerByName(args.Player.Name);
            if (args.Player.Name == play.plrName)
            {
                play.lastXtp = args.Player.TileX;
                play.lastYtp = args.Player.TileY;
                play.lastaction = "tp";
            }
            args.Player.Spawn();
            args.Player.SendMessage("Teleported to your spawnpoint.");
        }

        private static void Spawn(CommandArgs args)
        {
            if (!args.Player.RealPlayer)
            {
                args.Player.SendMessage("You cannot use teleport commands!");
                return;
            }
            esPlayer play = GetesPlayerByName(args.Player.Name);
            if (args.Player.Name == play.plrName)
            {
                play.lastXtp = args.Player.TileX;
                play.lastYtp = args.Player.TileY;
                play.lastaction = "tp";
            }
            if (args.Player.Teleport(Main.spawnTileX, Main.spawnTileY))
                args.Player.SendMessage("Teleported to the map's spawnpoint.");
        }

        private static void warp(CommandArgs args)
        {
            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /warp [name] or /warp list <page>", Color.Red);
                return;
            }

            if (args.Parameters[0].Equals("list"))
            {
                const int pagelimit = 15;
                const int perline = 5;
                int page = 0;

                if (args.Parameters.Count > 1)
                {
                    if (!int.TryParse(args.Parameters[1], out page) || page < 1)
                    {
                        args.Player.SendMessage(string.Format("Invalid page number ({0})", page), Color.Red);
                        return;
                    }
                    page--;
                }

                var warps = TShock.Warps.ListAllPublicWarps(Main.worldID.ToString());

                int pagecount = warps.Count / pagelimit;
                if (page > pagecount)
                {
                    args.Player.SendMessage(string.Format("Page number exceeds pages ({0}/{1})", page + 1, pagecount + 1), Color.Red);
                    return;
                }

                args.Player.SendMessage(string.Format("Current Warps ({0}/{1}):", page + 1, pagecount + 1), Color.Green);

                var nameslist = new List<string>();
                for (int i = (page * pagelimit); (i < ((page * pagelimit) + pagelimit)) && i < warps.Count; i++)
                {
                    nameslist.Add(warps[i].WarpName);
                }

                var names = nameslist.ToArray();
                for (int i = 0; i < names.Length; i += perline)
                {
                    args.Player.SendMessage(string.Join(", ", names, i, Math.Min(names.Length - i, perline)), Color.Yellow);
                }

                if (page < pagecount)
                {
                    args.Player.SendMessage(string.Format("Type /warp list {0} for more warps.", (page + 2)), Color.Yellow);
                }
            }
            else
            {
                string warpName = String.Join(" ", args.Parameters);
                var warp = TShock.Warps.FindWarp(warpName);
                if (warp.WarpPos != Vector2.Zero)
                {
                    esPlayer play = GetesPlayerByName(args.Player.Name);
                    if (args.Player.Name == play.plrName)
                    {
                        play.lastXtp = args.Player.TileX;
                        play.lastYtp = args.Player.TileY;
                        play.lastaction = "tp";
                    }
                    if (args.Player.Teleport((int)warp.WarpPos.X, (int)warp.WarpPos.Y + 3))
                    {
                        args.Player.SendMessage("Warped to " + warpName, Color.Yellow);
                    }
                }
                else
                {
                    args.Player.SendMessage("Specified warp not found", Color.Red);
                }
            }
        }

        private static void back(CommandArgs args)
        {
            esPlayer play = GetesPlayerByName(args.Player.Name);
            if (play.lastaction == "none")
            {
                args.Player.SendMessage("You do not have a /b position stored", Color.MediumSeaGreen);
            }
            else if (play.lastaction == "death" && play.grpData.HasPermission("backondeath"))
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

        public static void cbiome(CommandArgs args)
        {
            if (args.Parameters.Count < 2 || args.Parameters.Count > 3)
            {
                args.Player.SendMessage("Usage: /cbiome <from> <to> [region]", Color.IndianRed);
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
                                        Main.tile[x, y].type = 3;
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

        #region show search
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
        #endregion

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

        public static void setmyhome(CommandArgs args)
        {
            if (args.Player.IsLoggedIn)
            {
                int homecount = 0;
                homecount = SQLEditor.ReadColumn("EssentialsUserHomes", "LoginName", new List<SqlValue>()).Count;
                bool hashome = false;
                for (int i = 0; i < homecount; i++)
                {
                    int acname = Int32.Parse(SQLEditor.ReadColumn("EssentialsUserHomes", "LoginName", new List<SqlValue>())[i].ToString());
                    int homex = Int32.Parse(SQLEditor.ReadColumn("EssentialsUserHomes", "HomeX", new List<SqlValue>())[i].ToString());
                    int homey = Int32.Parse(SQLEditor.ReadColumn("EssentialsUserHomes", "HomeY", new List<SqlValue>())[i].ToString());

                    if (acname == args.Player.UserID)
                        hashome = true;
                }

                if (hashome)
                {
                    List<SqlValue> values = new List<SqlValue>();
                    values.Add(new SqlValue("HomeX", args.Player.TileX));
                    values.Add(new SqlValue("HomeY", args.Player.TileY));
                    List<SqlValue> where = new List<SqlValue>();
                    where.Add(new SqlValue("LoginName", args.Player.UserID));
                    SQLEditor.UpdateValues("EssentialsUserHomes", values, where);

                    args.Player.SendMessage("Updated your home position!", Color.MediumSeaGreen);
                }
                else
                {
                    List<SqlValue> list = new List<SqlValue>();
                    list.Add(new SqlValue("LoginName", args.Player.UserID));
                    list.Add(new SqlValue("HomeX", args.Player.TileX));
                    list.Add(new SqlValue("HomeY", args.Player.TileY));
                    SQLEditor.InsertValues("EssentialsUserHomes", list);

                    args.Player.SendMessage("Created your home!", Color.MediumSeaGreen);
                }
            }
            else
                args.Player.SendMessage("You must be logged in to do that!", Color.IndianRed);
        }

        public static void gomyhome(CommandArgs args)
        {
            if (args.Player.IsLoggedIn)
            {
                int homecount = 0;
                if ((homecount = SQLEditor.ReadColumn("EssentialsUserHomes", "LoginName", new List<SqlValue>()).Count) != 0)
                {
                    bool hashome = false;
                    int homeid = 0;
                    for (int i = 0; i < homecount; i++)
                    {
                        int acname = Int32.Parse(SQLEditor.ReadColumn("EssentialsUserHomes", "LoginName", new List<SqlValue>())[i].ToString());
                        int homex = Int32.Parse(SQLEditor.ReadColumn("EssentialsUserHomes", "HomeX", new List<SqlValue>())[i].ToString());
                        int homey = Int32.Parse(SQLEditor.ReadColumn("EssentialsUserHomes", "HomeY", new List<SqlValue>())[i].ToString());

                        if (acname == args.Player.UserID/*args.TPlayer.name*/)
                        {
                            hashome = true;
                            homeid = i;
                        }
                    }

                    if (hashome)
                    {
                        int homex = Int32.Parse(SQLEditor.ReadColumn("EssentialsUserHomes", "HomeX", new List<SqlValue>())[homeid].ToString());
                        int homey = Int32.Parse(SQLEditor.ReadColumn("EssentialsUserHomes", "HomeY", new List<SqlValue>())[homeid].ToString());
                        foreach (esPlayer play in esPlayers)
                        {
                            if (args.Player.Name == play.plrName)
                            {
                                play.lastXtp = args.Player.TileX;
                                play.lastYtp = args.Player.TileY;
                                play.lastaction = "tp";
                            }
                        }
                        args.Player.Teleport(homex, homey);
                        args.Player.SendMessage("Teleported to your home!", Color.MediumSeaGreen);
                    }
                    else
                    {
                        args.Player.SendMessage("You have not set a home. type: \"/sethome\" to set one.", Color.IndianRed);
                    }
                }
                else
                {
                    args.Player.SendMessage("You have not set a home. type: \"/sethome\" to set one.", Color.IndianRed);
                }
            }
            else
                args.Player.SendMessage("You must be logged in to do that!", Color.IndianRed);
        }

        #region esPlayer
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
    }

    public class esPlayer
    {

        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public string plrName { get { return TShock.Players[Index].Name; } }
        public string plrGroup { get { return TShock.Players[Index].Group.Name; } }
        public Group grpData { get { return TShock.Players[Index].Group; } }
        public int lastXtp = 0;
        public int lastYtp = 0;
        public int lastXondeath = 0;
        public int lastYondeath = 0;
        public string lastaction = "none";
        public bool ondeath = false;
        public string lastsearch = "";
        public string lastseachtype = "none";

        public esPlayer(int index)
        {
            Index = index;
        }

        public void SendMessage(string message, Color color)
        {
            NetMessage.SendData((int)PacketTypes.ChatText, Index, -1, message, 255, color.R, color.G, color.B);
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
#endregion
}