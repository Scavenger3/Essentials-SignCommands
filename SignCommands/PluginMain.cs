using Terraria;
using System;
using Hooks;
using TShockAPI;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace SignCommands
{
    [APIVersion(1, 10)]
    public class SignCommands : TerrariaPlugin
    {
        public static string name1thatcandestroy = "";
        public static string name2thatcandestroy = "";
        public static int timestold = 0;

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
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }

        public override void Initialize()
        {
            GameHooks.Initialize += OnInitialize;
            NetHooks.GetData += GetData;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Initialize -= OnInitialize;
                NetHooks.GetData += GetData;
            }
            base.Dispose(disposing);
        }

        public SignCommands(Main game)
            : base(game)
        {
        }

        #region destsign Command
        public void OnInitialize()
        {
            Commands.ChatCommands.Add(new Command("destroysigncommand", destsign, "destsign"));
        }

        public static void destsign(CommandArgs args)
        {
            if (name1thatcandestroy == "" || name2thatcandestroy != "")
            {
                args.Player.SendMessage("You can now destroy a sign command", Color.IndianRed);
                name1thatcandestroy = args.Player.Name;
            }
            else if (name2thatcandestroy == "" || name1thatcandestroy != "")
            {
                args.Player.SendMessage("You can now destroy a sign command", Color.IndianRed);
                name1thatcandestroy = args.Player.Name;
            }
        }
        #endregion


        public void GetData(GetDataEventArgs e)
        {
            try
            {
                switch (e.MsgID)
                {

                    #region On Hit tile but not kill!
                    case PacketTypes.Tile:
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        var reader = new BinaryReader(data);
                        if (e.MsgID == PacketTypes.Tile)
                        {
                            var type = reader.ReadByte();
                            if (!(type == 0 || type == 4))
                                return;
                        }
                        var x = reader.ReadInt32();
                        var y = reader.ReadInt32();
                        reader.Close();

                        var id = Terraria.Sign.ReadSign(x, y);
                        var tplayer = TShock.Players[e.Msg.whoAmI];
                        //TShock.Utils.Broadcast(id + ": " + x + ", " + y + " - Who: " + tplayer.Name, Color.HotPink);
                        if (id != -1 && Main.sign[id].text.ToLower().StartsWith("[sign command]"))
                        {
                            if (!tplayer.Group.HasPermission("destroysigncommand"))
                            {
                                dosigncmd(id, tplayer);
                                //Cancel Sign Destroy!
                                tplayer.SendTileSquare(x, y);
                                e.Handled = true;
                            }
                            else if (tplayer.Group.HasPermission("destroysigncommand") && (tplayer.Name != name1thatcandestroy && tplayer.Name != name2thatcandestroy))
                            {
                                if (timestold == 15)
                                {
                                    tplayer.SendMessage("To destroy this sign, Type /destsign", Color.IndianRed);
                                    timestold = 0;
                                }
                                else
                                    timestold++;
                                dosigncmd(id, tplayer);

                                //Cancel Sign Destroy!
                                tplayer.SendTileSquare(x, y);
                                e.Handled = true;
                            }
                        }
                    }
                    break;
                    #endregion

                    #region On Kill Tile
                    case PacketTypes.TileKill:
                    using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                    {
                        var reader = new BinaryReader(data);
                        if (e.MsgID == PacketTypes.Tile)
                        {
                            var type = reader.ReadByte();
                            if (!(type == 0 || type == 4))
                                return;
                        }
                        var x = reader.ReadInt32();
                        var y = reader.ReadInt32();
                        reader.Close();

                        var id = Terraria.Sign.ReadSign(x, y);
                        var tplayer = TShock.Players[e.Msg.whoAmI];
                        //TShock.Utils.Broadcast(id + ": " + x + ", " + y + " - Who: " + tplayer.Name, Color.HotPink);
                        if (id != -1 && Main.sign[id].text.ToLower().StartsWith("[sign command]"))
                        {
                            if (!tplayer.Group.HasPermission("destroysigncommand"))
                            {
                                dosigncmd(id, tplayer);

                                //Cancel Sign Destroy!
                                tplayer.SendTileSquare(x, y);
                                e.Handled = true;
                            }
                            else if (tplayer.Group.HasPermission("destroysigncommand"))
                            {
                                if (tplayer.Name == name1thatcandestroy)
                                {
                                    tplayer.SendMessage("You have destroyed this sign command!", Color.IndianRed);
                                    name1thatcandestroy = "";
                                }
                                else if (tplayer.Name == name2thatcandestroy)
                                {
                                    tplayer.SendMessage("You have destroyed this sign command!", Color.IndianRed);
                                    name2thatcandestroy = "";
                                }
                                else
                                {
                                    dosigncmd(id, tplayer);

                                    //Cancel Sign Destroy!
                                    tplayer.SendTileSquare(x, y);
                                    e.Handled = true;
                                }
                            }
                        }
                    }
                    break;
                    #endregion
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #region SIGN COMMANDS!
        public static void dosigncmd(int id, TSPlayer tplayer)
        {
            if (tplayer.Group.HasPermission("usesigntime"))
            {
                if (Main.sign[id].text.ToLower().Contains("time day"))
                    TSPlayer.Server.SetTime(true, 150.0);
                else if (Main.sign[id].text.ToLower().Contains("time night"))
                    TSPlayer.Server.SetTime(false, 0.0);
                else if (Main.sign[id].text.ToLower().Contains("time dusk"))
                    TSPlayer.Server.SetTime(false, 0.0);
                else if (Main.sign[id].text.ToLower().Contains("time noon"))
                    TSPlayer.Server.SetTime(true, 27000.0);
                else if (Main.sign[id].text.ToLower().Contains("time midnight"))
                    TSPlayer.Server.SetTime(false, 16200.0);
                else if (Main.sign[id].text.ToLower().Contains("time fullmoon"))
                    TSPlayer.Server.SetFullMoon(true);
                else if (Main.sign[id].text.ToLower().Contains("time bloodmoon"))
                    TSPlayer.Server.SetBloodMoon(true);
            }
            else
            {
                if (timestold == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    timestold = 0;
                }
                else
                    timestold++;
            }

            if (tplayer.Group.HasPermission("usesignheal"))
            {
                if (Main.sign[id].text.ToLower().Contains("heal"))
                {
                    Item heart = TShock.Utils.GetItemById(58);
                    Item star = TShock.Utils.GetItemById(184);
                    for (int ic = 0; ic < 20; ic++)
                        tplayer.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                    for (int ic = 0; ic < 10; ic++)
                        tplayer.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                    tplayer.SendMessage("You just got healed!", Color.MediumSeaGreen);
                }
            }
            else
            {
                if (timestold == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    timestold = 0;
                }
                else
                    timestold++;
            }

            if (tplayer.Group.HasPermission("usesignmessage"))
            {
                if (Main.sign[id].text.ToLower().Contains("show motd"))
                    TShock.Utils.ShowFileToUser(tplayer, "motd.txt");
                else if (Main.sign[id].text.ToLower().Contains("show rules"))
                    TShock.Utils.ShowFileToUser(tplayer, "rules.txt");
                else if (Main.sign[id].text.ToLower().Contains("show playing"))
                    tplayer.SendMessage(string.Format("Current players: {0}.", TShock.Utils.GetPlayers()), 255, 240, 20);
            }
            else
            {
                if (timestold == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    timestold = 0;
                }
                else
                    timestold++;
            }

            if (tplayer.Group.HasPermission("usesigndamage"))
            {
                if (Main.sign[id].text.ToLower().Contains("damage"))
                    tplayer.DamagePlayer(50);
                else if (Main.sign[id].text.ToLower().Contains("kill"))
                    tplayer.DamagePlayer(9999);
            }
            else
            {
                if (timestold == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    timestold = 0;
                }
                else
                    timestold++;
            }

            if (tplayer.Group.HasPermission("usesignspawnboss"))
            {
                if (Main.sign[id].text.ToLower().Contains("boss eater"))
                {
                    NPC npc = TShock.Utils.GetNPCById(13);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                }
                else if (Main.sign[id].text.ToLower().Contains("boss eye"))
                {
                    NPC npc = TShock.Utils.GetNPCById(4);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                }
                else if (Main.sign[id].text.ToLower().Contains("boss king"))
                {
                    NPC npc = TShock.Utils.GetNPCById(50);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                }
                else if (Main.sign[id].text.ToLower().Contains("boss skeletron"))
                {
                    NPC npc = TShock.Utils.GetNPCById(35);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                }
                else if (Main.sign[id].text.ToLower().Contains("boss wof"))
                {
                    if (Main.wof >= 0 || (tplayer.Y / 16f < (float)(Main.maxTilesY - 205)))
                    {
                        tplayer.SendMessage("Can't spawn Wall of Flesh!", Color.Red);
                        return;
                    }
                    NPC.SpawnWOF(new Vector2(tplayer.X, tplayer.Y));
                }
                else if (Main.sign[id].text.ToLower().Contains("boss twins"))
                {
                    NPC retinazer = TShock.Utils.GetNPCById(125);
                    NPC spaz = TShock.Utils.GetNPCById(126);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, 1, tplayer.TileX, tplayer.TileY);
                    TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, 1, tplayer.TileX, tplayer.TileY);
                }
                else if (Main.sign[id].text.ToLower().Contains("boss destroyer"))
                {
                    NPC npc = TShock.Utils.GetNPCById(134);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                }
                else if (Main.sign[id].text.ToLower().Contains("boss skeletronprime"))
                {
                    NPC npc = TShock.Utils.GetNPCById(127);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                }
            }
            else
            {
                if (timestold == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    timestold = 0;
                }
                else
                    timestold++;
            }
        }
        #endregion
    }
}