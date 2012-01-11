using Terraria;
using System;
using Hooks;
using TShockAPI;
using System.Reflection;
using System.ComponentModel;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using Newtonsoft.Json;
using config;

namespace SignCommands
{
    [APIVersion(1, 10)]
    public class SignCommands : TerrariaPlugin
    {
        public static scConfig getConfig { get; set; }
        internal static string TempConfigPath { get { return Path.Combine(TShock.SavePath, "SignCommandConfig.json"); } }
        public static List<scPlayer> scPlayers = new List<scPlayer>();
        public static scPlayer scplay;
        public static scPlayer scply;
        public static scPlayer edplay;
        public DateTime CountDown = DateTime.UtcNow;

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
            GameHooks.Update += OnUpdate;
            GameHooks.Initialize += OnInitialize;
            NetHooks.GetData += GetData;
            NetHooks.GreetPlayer += OnGreetPlayer;
            ServerHooks.Leave += OnLeave;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                GameHooks.Update -= OnUpdate;
                GameHooks.Initialize -= OnInitialize;
                NetHooks.GetData -= GetData;
                NetHooks.GreetPlayer -= OnGreetPlayer;
                ServerHooks.Leave -= OnLeave;
            }
            base.Dispose(disposing);
        }

        public SignCommands(Main game)
            : base(game)
        {
            getConfig = new scConfig(); 
        }

        public static void SetupConfig()
        {
            try
            {
                if (File.Exists(TempConfigPath))
                {
                    getConfig = scConfig.Read(TempConfigPath);
                }
                getConfig.Write(TempConfigPath);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Error in Sign Commands config file");
                Console.ForegroundColor = ConsoleColor.Gray;
                Log.Error("Config Exception in Sign Commands config file");
                Log.Error(ex.ToString());
            }
        }

        public static void ReloadConfig(CommandArgs args)
        {
            try
            {
                if (File.Exists(TempConfigPath))
                {
                    getConfig = scConfig.Read(TempConfigPath);
                }
                getConfig.Write(TempConfigPath);
                args.Player.SendMessage("Config Reloaded Successfully.", Color.MediumSeaGreen);
            }
            catch (Exception ex)
            {
                args.Player.SendMessage("Error: Could not reload config, Check log for more details.", Color.IndianRed);
                Log.Error("Config Exception in Sign Commands config file");
                Log.Error(ex.ToString());
            }
        }

        public void OnGreetPlayer(int who, HandledEventArgs e)
        {
            lock (scPlayers)
                scPlayers.Add(new scPlayer(who));
        }

        public void OnLeave(int ply)
        {
            lock (scPlayers)
            {
                for (int i = 0; i < scPlayers.Count; i++)
                {
                    if (scPlayers[i].Index == ply)
                    {
                        scPlayers.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        public void OnUpdate()
        {
            double tick1 = (DateTime.UtcNow - CountDown).TotalMilliseconds;
            if (tick1 > 1000)
            {
                foreach (scPlayer play in scPlayers)
                {
                    if (play.CooldownBoss > 0)
                        play.CooldownBoss--;

                    if (play.CooldownDamage > 0)
                        play.CooldownDamage--;
                    
                    if (play.CooldownHeal > 0)
                        play.CooldownHeal--;

                    if (play.CooldownMsg > 0)
                        play.CooldownMsg--;

                    if (play.CooldownTime > 0)
                        play.CooldownTime--;

                    if (play.CooldownItem > 0)
                        play.CooldownItem--;

                    if (play.checkforsignedit)
                    {
                        if (Main.sign[play.signeditid].text.ToLower().Contains(getConfig.DefineSignCommands.ToLower()))
                        {
                            play.TSPlayer.SendMessage("You do not have permission to create/edit sign commands!", Color.IndianRed);
                            Main.sign[play.signeditid].text = edplay.originalsigntext;
                        }
                        play.checkforsignedit = false;
                    }
                }
                CountDown = DateTime.UtcNow;
            }
        }

        #region Commands
        public void OnInitialize()
        {
            SetupConfig();
            Commands.ChatCommands.Add(new Command("destroysigncommand", destsign, "destsign"));
            Commands.ChatCommands.Add(new Command("reloadsigncommands", screload, "screload"));
        }

        public static void destsign(CommandArgs args)
        {
            foreach (scPlayer play in scPlayers)
            {
                if (play.TSPlayer.Name == args.Player.Name)
                {
                    play.destsign = true;
                    args.Player.SendMessage("You can now destroy a sign command", Color.MediumSeaGreen);
                }
            }
        }

        public static void screload(CommandArgs args)
        {
            ReloadConfig(args);
        }
        #endregion


        public void GetData(GetDataEventArgs e)
        {
            try
            {
                switch (e.MsgID)
                {

                    #region On Hit
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

                        if (id != -1 && Main.sign[id].text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower()))
                        {
                            foreach (scPlayer play in scPlayers)
                            {
                                if (play.TSPlayer.Name == tplayer.Name)
                                    scplay = play;
                            }

                            if (!tplayer.Group.HasPermission("destroysigncommand"))
                            {
                                dosigncmd(id, tplayer);
                                tplayer.SendTileSquare(x, y);
                                e.Handled = true;
                            }
                            else if (tplayer.Group.HasPermission("destroysigncommand") && !scplay.destsign)
                            {
                                if (scplay.tolddestsign == 10)
                                {
                                    tplayer.SendMessage("To destroy this sign, Type /destsign", Color.IndianRed);
                                    scplay.tolddestsign = 0;
                                }
                                else
                                    scplay.tolddestsign++;

                                dosigncmd(id, tplayer);
                                tplayer.SendTileSquare(x, y);
                                e.Handled = true;
                            }
                        }
                    }
                    break;
                    #endregion

                    #region On Kill
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
                        foreach (scPlayer play in scPlayers)
                        {
                            if (play.TSPlayer.Name == tplayer.Name)
                                scplay = play;
                        }
                        if (id != -1 && Main.sign[id].text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower()))
                        {
                            if (!tplayer.Group.HasPermission("destroysigncommand"))
                            {
                                dosigncmd(id, tplayer);
                                tplayer.SendTileSquare(x, y);
                                e.Handled = true;
                            }
                            else if (tplayer.Group.HasPermission("destroysigncommand"))
                            {
                                if (scplay.destsign == true)
                                {
                                    tplayer.SendMessage("You have destroyed this sign command!", Color.IndianRed);
                                    scplay.destsign = false;
                                }
                                else
                                {
                                    if (scplay.tolddestsign == 10)
                                    {
                                        tplayer.SendMessage("To destroy this sign, Type /destsign", Color.IndianRed);
                                        scplay.tolddestsign = 0;
                                    }
                                    else
                                        scplay.tolddestsign++;

                                    dosigncmd(id, tplayer);
                                    tplayer.SendTileSquare(x, y);
                                    e.Handled = true;
                                }
                            }
                        }
                    }
                    break;
                    #endregion

                    #region On Edit
                    case PacketTypes.SignNew:
                    if (!e.Handled)
                    {
                        using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
                        {
                            var reader = new BinaryReader(data);
                            var signId = reader.ReadInt16();
                            var x = reader.ReadInt32();
                            var y = reader.ReadInt32();
                            reader.Close();

                            var id = Terraria.Sign.ReadSign(x, y);
                            var tplayer = TShock.Players[e.Msg.whoAmI];

                            if (!Main.sign[id].text.ToLower().Contains(getConfig.DefineSignCommands.ToLower()) && !tplayer.Group.HasPermission("createsigncommand"))
                            {
                                foreach (scPlayer ply in scPlayers)
                                    if (ply.TSPlayer.Name == tplayer.Name)
                                        edplay = ply;

                                edplay.originalsigntext = Main.sign[id].text;
                                edplay.checkforsignedit = true;
                                edplay.signeditid = id;
                            }
                            else if (Main.sign[id].text.ToLower().Contains(getConfig.DefineSignCommands.ToLower()) && !tplayer.Group.HasPermission("createsigncommand"))
                            {
                                tplayer.SendMessage("You do not have permission to create/edit sign commands!", Color.IndianRed);
                                e.Handled = true;
                                tplayer.SendData(PacketTypes.SignNew, "", id);
                                return;
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

        #region Do Commands
        public static void dosigncmd(int id, TSPlayer tplayer)
        {
            foreach (scPlayer play in scPlayers)
            {
                if (play.TSPlayer.Name == tplayer.Name)
                    scply = play;
            }

            if (tplayer.Group.HasPermission("usesigntime") && (tplayer.Group.HasPermission("nosccooldown") || scplay.CooldownTime <= 0))
            {
                //SignCmd:
                //Time Sign commands!
                if (Main.sign[id].text.ToLower().Contains("time day"))
                {
                    TSPlayer.Server.SetTime(true, 150.0);
                    scplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time night"))
                {
                    TSPlayer.Server.SetTime(false, 0.0);
                    scplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time dusk"))
                {
                    TSPlayer.Server.SetTime(false, 0.0);
                    scplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time noon"))
                {
                    TSPlayer.Server.SetTime(true, 27000.0);
                    scplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time midnight"))
                {
                    TSPlayer.Server.SetTime(false, 16200.0);
                    scplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time fullmoon"))
                {
                    TSPlayer.Server.SetFullMoon(true);
                    scplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time bloodmoon"))
                {
                    TSPlayer.Server.SetBloodMoon(true);
                    scplay.CooldownTime = getConfig.TimeCooldown;
                }
            }
            else if (scply.toldperm == 5 && !tplayer.Group.HasPermission("usesigntime") && Main.sign[id].text.ToLower().Contains("time "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm == 5 && tplayer.Group.HasPermission("usesigntime") && !tplayer.Group.HasPermission("nosccooldown") && scplay.CooldownTime > 0 && Main.sign[id].text.ToLower().Contains("time "))
            {
                tplayer.SendMessage("You have to wait another " + scplay.CooldownTime + " seconds before using this sign", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm != 5 && Main.sign[id].text.ToLower().Contains("time "))
                scplay.toldperm++;

            //SignCmd:
            //Heal Sign command!
            if (tplayer.Group.HasPermission("usesignheal") && (tplayer.Group.HasPermission("nosccooldown") || scplay.CooldownHeal <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("heal"))
                {
                    Item heart = TShock.Utils.GetItemById(58);
                    Item star = TShock.Utils.GetItemById(184);
                    for (int ic = 0; ic < 20; ic++)
                        tplayer.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                    for (int ic = 0; ic < 10; ic++)
                        tplayer.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                    scplay.CooldownHeal = getConfig.HealCooldown;
                }
            }
            else if (scply.toldperm == 5 && !tplayer.Group.HasPermission("usesignheal") && Main.sign[id].text.ToLower().Contains("heal"))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm == 5 && tplayer.Group.HasPermission("usesignheal") && !tplayer.Group.HasPermission("nosccooldown") && scplay.CooldownHeal > 0 && Main.sign[id].text.ToLower().Contains("heal"))
            {
                tplayer.SendMessage("You have to wait another " + scplay.CooldownHeal + " seconds before using this sign", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm != 5 && Main.sign[id].text.ToLower().Contains("heal"))
                scplay.toldperm++;

            //SignCmd:
            //Show Message Sign commands!
            if (tplayer.Group.HasPermission("usesignmessage") && (tplayer.Group.HasPermission("nosccooldown") || scplay.CooldownMsg <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("show motd"))
                {
                    TShock.Utils.ShowFileToUser(tplayer, "motd.txt");
                    scplay.CooldownMsg = getConfig.ShowMsgCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("show rules"))
                {
                    TShock.Utils.ShowFileToUser(tplayer, "rules.txt");
                    scplay.CooldownMsg = getConfig.ShowMsgCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("show playing"))
                {
                    tplayer.SendMessage(string.Format("Current players: {0}.", TShock.Utils.GetPlayers()), 255, 240, 20);
                    scplay.CooldownMsg = getConfig.ShowMsgCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("show message") && File.Exists("scMeassge.txt"))
                {
                    TShock.Utils.ShowFileToUser(tplayer, "scMessage.txt");
                    scplay.CooldownMsg = getConfig.ShowMsgCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("show message") && !File.Exists("scMeassge.txt"))
                {
                    tplayer.SendMessage("Could not find message", Color.IndianRed);
                    scplay.CooldownMsg = getConfig.ShowMsgCooldown;
                }
            }
            else if (scply.toldperm == 5 && !tplayer.Group.HasPermission("usesignmessage") && Main.sign[id].text.ToLower().Contains("show "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm == 5 && tplayer.Group.HasPermission("usesignmessage") && !tplayer.Group.HasPermission("nosccooldown") && scplay.CooldownMsg > 0 && Main.sign[id].text.ToLower().Contains("show "))
            {
                tplayer.SendMessage("You have to wait another " + scplay.CooldownTime + " seconds before using this sign", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm != 5 && Main.sign[id].text.ToLower().Contains("show "))
                scplay.toldperm++;

            //SignCmd:
            //Damage Sign command!
            if (tplayer.Group.HasPermission("usesigndamage") && (tplayer.Group.HasPermission("nosccooldown") || scplay.CooldownDamage <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("damage "))
                {
                    try
                    {
                        string[] linesplit = Main.sign[id].text.Split('\'', '\"');
                        int amtdamage = 0;
                        bool dmgisint = int.TryParse(linesplit[1], out amtdamage);
                        if (dmgisint)
                        {
                            tplayer.DamagePlayer(amtdamage);
                            scplay.CooldownDamage = getConfig.DamageCooldown;
                        }
                        else
                            tplayer.SendMessage("Invalid damage value!", Color.IndianRed);
                    }
                    catch (Exception)
                    {
                        tplayer.SendMessage("Could not parse Damage Amount - Correct Format: \"<Amount to Damage>\"", Color.IndianRed);
                    }
                }
            }
            else if (scply.toldperm == 5 && !tplayer.Group.HasPermission("usesigndamage") && Main.sign[id].text.ToLower().Contains("damage "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm == 5 && tplayer.Group.HasPermission("usesigndamage") && !tplayer.Group.HasPermission("nosccooldown") && scplay.CooldownDamage > 0 && (Main.sign[id].text.ToLower().Contains("damage") || Main.sign[id].text.ToLower().Contains("kill")))
            {
                tplayer.SendMessage("You have to wait another " + scplay.CooldownDamage + " seconds before using this sign", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm != 5 && (Main.sign[id].text.ToLower().Contains("damage") || Main.sign[id].text.ToLower().Contains("kill")))
                scplay.toldperm++;

            //SignCmd:
            //Spawn Boss Sign commands!
            if (tplayer.Group.HasPermission("usesignspawnboss") && (tplayer.Group.HasPermission("nosccooldown") || scplay.CooldownBoss <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("boss eater"))
                {
                    NPC npc = TShock.Utils.GetNPCById(13);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss eye"))
                {
                    NPC npc = TShock.Utils.GetNPCById(4);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss king"))
                {
                    NPC npc = TShock.Utils.GetNPCById(50);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss skeletron") && !Main.sign[id].text.ToLower().Contains("boss skeletronprime"))
                {
                    NPC npc = TShock.Utils.GetNPCById(35);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss wof"))
                {
                    if (Main.wof >= 0 || (tplayer.Y / 16f < (float)(Main.maxTilesY - 205)))
                    {
                        if (scply.toldperm == 10)
                        {
                            tplayer.SendMessage("Can't spawn Wall of Flesh!", Color.Red);
                            scply.toldperm = 0;
                        }
                        else
                            scply.toldperm++;
                        return;
                    }
                    NPC.SpawnWOF(new Vector2(tplayer.X, tplayer.Y));
                    scplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss twins"))
                {
                    NPC retinazer = TShock.Utils.GetNPCById(125);
                    NPC spaz = TShock.Utils.GetNPCById(126);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, 1, tplayer.TileX, tplayer.TileY);
                    TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, 1, tplayer.TileX, tplayer.TileY);
                    scplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss destroyer"))
                {
                    NPC npc = TShock.Utils.GetNPCById(134);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss skeletronprime"))
                {
                    NPC npc = TShock.Utils.GetNPCById(127);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scplay.CooldownBoss = getConfig.BossCooldown;
                }
            }
            else if (scply.toldperm == 5 && !tplayer.Group.HasPermission("usesignspawnboss") && Main.sign[id].text.ToLower().Contains("boss "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm == 5 && tplayer.Group.HasPermission("usesignspawnboss") && !tplayer.Group.HasPermission("nosccooldown") && scplay.CooldownBoss > 0 && Main.sign[id].text.ToLower().Contains("boss "))
            {
                tplayer.SendMessage("You have to wait another " + scplay.CooldownBoss + " seconds before using this sign", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm != 5 && Main.sign[id].text.ToLower().Contains("boss "))
                scplay.toldperm++;

            //SignCmd:
            //Warp Sign command!
            if (tplayer.Group.HasPermission("usesignwarp"))
            {
                if (Main.sign[id].text.ToLower().Contains("warp "))
                {
                    try
                    {
                        string[] linesplit = Main.sign[id].text.Split('\'', '\"');
                        var warpxy = TShock.Warps.FindWarp(linesplit[1]);

                        if (warpxy.WarpName == "" || warpxy.WarpPos.X == 0 || warpxy.WarpPos.Y == 0 || warpxy.WorldWarpID == "")
                        {
                            if (scply.toldwarp == 5)
                            {
                                tplayer.SendMessage("Could not find warp!", Color.IndianRed);
                                scply.toldwarp = 0;
                            }
                            else
                                scply.toldwarp++;
                        }
                        else
                            tplayer.Teleport((int)warpxy.WarpPos.X, (int)warpxy.WarpPos.Y);
                    }
                    catch (Exception)
                    {
                        tplayer.SendMessage("Could not parse Warp - Correct Format: \"<Warp Name>\"", Color.IndianRed);
                    }
                }
            }
            else if (scply.toldperm == 5 && !tplayer.Group.HasPermission("usesignwarp") && Main.sign[id].text.ToLower().Contains("warp "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm != 5 && Main.sign[id].text.ToLower().Contains("warp "))
                scplay.toldperm++;

            //SignCmd:
            //Item Sign command!
            if (tplayer.Group.HasPermission("usesignitem") && (tplayer.Group.HasPermission("nosccooldown") || scplay.CooldownItem <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("item "))
                {
                    try
                    {
                        string[] linesplit = Main.sign[id].text.Split('\'', '\"');
                        string[] datasplit = linesplit[1].Split(',');
                        var items = TShock.Utils.GetItemByIdOrName(datasplit[0]);
                        if (items.Count == 0)
                        {
                            tplayer.SendMessage("Invalid item type!", Color.Red);
                        }
                        else if (items.Count > 1)
                        {
                            tplayer.SendMessage(string.Format("More than one ({0}) item matched!", items.Count), Color.Red);
                        }
                        else
                        {

                            int itemAmount = 0;
                            int.TryParse(datasplit[1], out itemAmount);
                            var item = items[0];
                            if (item.type >= 1 && item.type < Main.maxItemTypes)
                            {
                                if (tplayer.InventorySlotAvailable || item.name.Contains("Coin"))
                                {
                                    if (itemAmount == 0 || itemAmount > item.maxStack)
                                        itemAmount = item.maxStack;
                                    tplayer.GiveItem(item.type, item.name, item.width, item.height, itemAmount, 0);
                                    tplayer.SendMessage(string.Format("Gave {0} {1}(s).", itemAmount, item.name));
                                    scplay.CooldownItem = getConfig.ItemCooldown;
                                }
                                else
                                {
                                    tplayer.SendMessage("You don't have free slots!", Color.Red);
                                }
                            }
                            else
                            {
                                tplayer.SendMessage("Invalid item type!", Color.Red);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        tplayer.SendMessage("Could not parse Item / Amount - Correct Format: \"<Item Name>, <Amount>\"", Color.IndianRed);
                    }
                }
            }
            else if (scply.toldperm == 5 && !tplayer.Group.HasPermission("usesignitem") && Main.sign[id].text.ToLower().Contains("item "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm == 5 && tplayer.Group.HasPermission("usesignitem") && !tplayer.Group.HasPermission("nosccooldown") && scplay.CooldownItem > 0 && Main.sign[id].text.ToLower().Contains("item "))
            {
                tplayer.SendMessage("You have to wait another " + scplay.CooldownItem + " seconds before using this sign", Color.IndianRed);
                scply.toldperm = 0;
            }
            else if (scply.toldperm != 5 && Main.sign[id].text.ToLower().Contains("item "))
                scplay.toldperm++;

            #region sign shop
            /*if (tplayer.Group.HasPermission("usesignshop"))
            {
                string[] shopsplit = Main.sign[id].text.Split('\'', '\"');
                if (Main.sign[id].text.ToLower().Contains("shop"))
                {
                    string[] shopbuy = shopsplit[1].Split(' ');

                    int buya = 0;
                    bool buyanum = int.TryParse(shopbuy[0], out buya);
                    if (!buyanum)
                        //buya = 1;

                    string buyis = "";
                    foreach (string itmpart in shopbuy)
                    {
                        buyis = buyis + itmpart + " ";
                    }
                    
                    
                    [Sign Shop]
                    Buy: "25 Glass"
                    For: "15 Shards"
                    
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
            }*/
            #endregion sign shop
        }
        #endregion
    }

    public class scPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public bool destsign = false;
        public int tolddestsign = 10;
        public int toldwarp = 5;
        public int toldperm = 5;
        public int CooldownTime = 0;
        public int CooldownHeal = 0;
        public int CooldownMsg = 0;
        public int CooldownDamage = 0;
        public int CooldownBoss = 0;
        public int CooldownItem = 0;
        public bool checkforsignedit = false;
        public string originalsigntext = "";
        public int signeditid = 0;

        public scPlayer(int index)
        {
            Index = index;
        }
    }
}

namespace config
{
    public class scConfig
    {
        public string DefineSignCommands = "[Sign Command]";
        public int TimeCooldown = 20;
        public int HealCooldown = 20;
        public int ShowMsgCooldown = 20;
        public int DamageCooldown = 20;
        public int BossCooldown = 20;
        public int ItemCooldown = 60;

        public static scConfig Read(string path)
        {
            if (!File.Exists(path))
                return new scConfig();
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                return Read(fs);
            }
        }

        public static scConfig Read(Stream stream)
        {
            using (var sr = new StreamReader(stream))
            {
                var cf = JsonConvert.DeserializeObject<scConfig>(sr.ReadToEnd());
                if (ConfigRead != null)
                    ConfigRead(cf);
                return cf;
            }
        }

        public void Write(string path)
        {
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Write))
            {
                Write(fs);
            }
        }

        public void Write(Stream stream)
        {
            var str = JsonConvert.SerializeObject(this, Formatting.Indented);
            using (var sw = new StreamWriter(stream))
            {
                sw.Write(str);
            }
        }

        public static Action<scConfig> ConfigRead;
    }
}