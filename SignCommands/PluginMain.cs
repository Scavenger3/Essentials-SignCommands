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
        public static Sign signcommand = Main.sign[0];

        public override string Name
        {
            get { return "Sign Commands"; }
        }

        public override string Author
        {
            get { return "Created by Scavenger."; }
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

        public void OnInitialize()
        {
        }

        public void GetData(GetDataEventArgs e)
        {
            try
            {
                switch (e.MsgID)
                {
                    case PacketTypes.SignRead:
                        using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                        {
                            var reader = new BinaryReader(data);
                            int x = reader.ReadInt32();
                            int y = reader.ReadInt32();
                            reader.Close();

                            int id = Terraria.Sign.ReadSign(x, y);
                            TSPlayer tplayer = TShock.Players[e.Msg.whoAmI];
                            //Debug broadcast:
                            //TShock.Utils.Broadcast("X1: " + x + " - Y1: " + y + " - ID: " + id + " - Who: " + tplayer.Name, Color.IndianRed);

                            if (Main.sign[id].text.ToLower().StartsWith("[sign command]"))
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

                                if (tplayer.Group.HasPermission("usesignmessage"))
                                {
                                    if (Main.sign[id].text.ToLower().Contains("show motd"))
                                        TShock.Utils.ShowFileToUser(tplayer, "motd.txt");
                                    else if (Main.sign[id].text.ToLower().Contains("show rules"))
                                        TShock.Utils.ShowFileToUser(tplayer, "rules.txt");
                                    else if (Main.sign[id].text.ToLower().Contains("show playing"))
                                        tplayer.SendMessage(string.Format("Current players: {0}.", TShock.Utils.GetPlayers()), 255, 240, 20);
                                }

                                if (tplayer.Group.HasPermission("usesigndamage"))
                                {
                                    if (Main.sign[id].text.ToLower().Contains("damage"))
                                        tplayer.DamagePlayer(50);
                                    else if (Main.sign[id].text.ToLower().Contains("kill"))
                                        tplayer.DamagePlayer(9999);
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
                                    tplayer.SendMessage("You do not have permission to use sign commands!", Color.IndianRed);
                                }
                            }
                        }
                    break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}