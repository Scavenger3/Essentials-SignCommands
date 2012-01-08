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

namespace Essentials
{
    [APIVersion(1, 10)]
    public class Essentials : TerrariaPlugin
    {
        public static List<esPlayer> esPlayers = new List<esPlayer>();
        public static Sign signcommand;
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
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
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
            Commands.ChatCommands.Add(new Command("tphere", tph, "btphere"));
            Commands.ChatCommands.Add(new Command("tp", tp, "btp"));
            Commands.ChatCommands.Add(new Command("tp", Home, "bhome"));
            Commands.ChatCommands.Add(new Command("tp", Spawn, "bspawn"));
            Commands.ChatCommands.Add(new Command("backontp", back, "b"));
        }

        public void OnUpdate()
        {
            foreach (esPlayer play in esPlayers)
            {
                if (play.TSPlayer.Dead && !play.ondeath && play != null)
                {
                    play.lastXondeath = play.TSPlayer.TileX;
                    play.lastYondeath = play.TSPlayer.TileY;
                    if (play.grpData.HasPermission("backondeath") && !play.ondeath)
                        play.SendMessage("Type \"/b\" to return to your position before you died", Color.MediumSeaGreen);
                    play.ondeath = true;
                    play.lastaction = "death";
                }
                if (!play.TSPlayer.Dead && play.ondeath && play != null)
                {
                    play.ondeath = false;
                }
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
        }
        public static void more(CommandArgs args)
        {
            int i = 0;
            foreach (Item item in args.TPlayer.inventory)
            {
                int togive = item.maxStack - item.stack;
                if (item.stack != 0 && i <=39)
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
                        if (Main.npc[i].active && Main.npc[i].type != 0 && !Main.npc[i].townNPC && !Main.npc[i].friendly && Main.npc[i].name == npcselected[0].name)
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
                    {
                        player.Kick("Everyone has been kicked (" + text + ")");
                    }
                }
                TShock.Utils.Broadcast("Everyone has been kicked from the server!");
            }
            else
            {
                foreach (esPlayer player in esPlayers)
                {
                    if (!player.grpData.HasPermission("immunetokickall"))
                    {
                        player.Kick("Everyone has been kicked from the server!");
                    }
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
        public static void tph(CommandArgs args)
        {
            if (!args.Player.RealPlayer)
            {
                args.Player.SendMessage("You cannot use teleport commands!");
                return;
            }

            if (args.Parameters.Count < 1)
            {
                args.Player.SendMessage("Invalid syntax! Proper syntax: /tph <player> ", Color.Red);
                return;
            }

            string plStr = String.Join(" ", args.Parameters);

            if (plStr == "all" || plStr == "*")
            {
                args.Player.SendMessage(string.Format("You brought all players here."));
                for (int i = 0; i < Main.maxPlayers; i++)
                {
                    if (Main.player[i].active && (Main.player[i] != args.TPlayer))
                    {
                        foreach (esPlayer play in esPlayers)
                        {
                            if (TShock.Players[i].Name == play.plrName)
                            {
                                play.lastXtp = TShock.Players[i].TileX;
                                play.lastYtp = TShock.Players[i].TileY;
                                play.lastaction = "tp";
                            }
                        }
                        if (TShock.Players[i].Teleport(args.Player.TileX, args.Player.TileY + 3))
                            TShock.Players[i].SendMessage(string.Format("You were teleported to {0}.", args.Player.Name));
                    }
                }
                return;
            }

            var players = TShock.Utils.FindPlayer(plStr);
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
                foreach (esPlayer play in esPlayers)
                {
                    if (plr.Name == play.plrName)
                    {
                        play.lastXtp = plr.TileX;
                        play.lastYtp = plr.TileY;
                        play.lastaction = "tp";
                    }
                }
                if (plr.Teleport(args.Player.TileX, args.Player.TileY + 3))
                {
                    plr.SendMessage(string.Format("You were teleported to {0}.", args.Player.Name));
                    args.Player.SendMessage(string.Format("You brought {0} here.", plr.Name));
                }
            }
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
                foreach (esPlayer play in esPlayers)
                {
                    if (plr.Name == play.plrName)
                    {
                        play.lastXtp = plr.TileX;
                        play.lastYtp = plr.TileY;
                        play.lastaction = "tp";
                    }
                }
                if (args.Player.Teleport(plr.TileX, plr.TileY + 3))
                {
                    args.Player.SendMessage(string.Format("Teleported to {0}", plr.Name));
                    if (!args.Player.Group.HasPermission(Permissions.tphide))
                        plr.SendMessage(args.Player.Name + " Teleported To You");
                }
            }
        }

        private static void Home(CommandArgs args)
        {
            if (!args.Player.RealPlayer)
            {
                args.Player.SendMessage("You cannot use teleport commands!");
                return;
            }

            foreach (esPlayer play in esPlayers)
            {
                if (args.Player.Name == play.plrName)
                {
                    play.lastXtp = args.Player.TileX;
                    play.lastYtp = args.Player.TileY;
                    play.lastaction = "tp";
                }
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
            foreach (esPlayer play in esPlayers)
            {
                if (args.Player.Name == play.plrName)
                {
                    play.lastXtp = args.Player.TileX;
                    play.lastYtp = args.Player.TileY;
                    play.lastaction = "tp";
                }
            }
            if (args.Player.Teleport(Main.spawnTileX, Main.spawnTileY))
                args.Player.SendMessage("Teleported to the map's spawnpoint.");
        }
        private static void back(CommandArgs args)
        {
            foreach (esPlayer play in esPlayers)
            {
                if (play.plrName == args.Player.Name)
                {
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
                        if(args.Player.Teleport(Xtp, Ytp))
                            args.Player.SendMessage("Moved you to your position before you last teleported", Color.MediumSeaGreen);
                    }
                    else if (play.lastaction == "death" && !play.grpData.HasPermission("backondeath"))
                    {
                        args.Player.SendMessage("You do not have permission to /b after death", Color.MediumSeaGreen);
                    }
                }
            }
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
}