using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;
using System.Drawing;
using System.Timers;
using System.IO;
using TShockAPI;
using Terraria;
using System;
using config;
using Hooks;
using Kits;

namespace SignCommands
{
    [APIVersion(1, 11)]
    public class SignCommands : TerrariaPlugin
    {
        public static scConfig getConfig { get; set; }
        internal static string TempConfigPath { get { return Path.Combine(TShock.SavePath, "SignCommandConfig.json"); } }

        public static List<scPlayer> scPlayers = new List<scPlayer>();
        public static KitList kits;
        public static String savepath = "";

        public static int GlobalBossCooldown = 0;
        public static int GlobalTimeCooldown = 0;
        public static int GlobalSpawnMobCooldown = 0;
        public static int GlobalCommandCooldown = 0;

        public static Timer Cooldown = new Timer(1000);

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
            get { return new Version("1.3.2"); }
        }

        public override void Initialize()
        {
            GameHooks.Initialize += OnInitialize;
            NetHooks.GetData += GetData;
            NetHooks.GreetPlayer += OnGreetPlayer;
            ServerHooks.Leave += OnLeave;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
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

            Order = 4;

            /*
             * Full Credit for kits goes to Olink's Kit Plugin!
            */
            savepath = Path.Combine(TShockAPI.TShock.SavePath, "kits.cfg");

            KitReader reader = new KitReader();
            if (File.Exists(savepath))
            {
                kits = reader.readFile(Path.Combine(TShockAPI.TShock.SavePath, "kits.cfg"));
                //Console.WriteLine(kits.kits.Count + " kits have been loaded.");
            }
            else
            {
                kits = reader.writeFile(savepath);
                //Console.WriteLine("Basic kit file being created.  1 kit containing copper armor created. ");
            }
        }

        #region Commands
        public void OnInitialize()
        {
            SetupConfig();
            /*Why?
            GlobalBossCooldown = getConfig.BossCooldown;
            GlobalTimeCooldown = getConfig.BossCooldown;
            GlobalSpawnMobCooldown = getConfig.SpawnMobCooldown;
            GlobalCommandCooldown = getConfig.DoCommandCooldown;*/

            Cooldown.Elapsed += new ElapsedEventHandler(Cooldown_Elapsed);
            Cooldown.Start();

            Commands.ChatCommands.Add(new Command("destroysigncommand", destsign, "destsign"));
            Commands.ChatCommands.Add(new Command("reloadsigncommands", screload, "screload"));
        }

        public static void destsign(CommandArgs args)
        {
            scPlayer play = GetscPlayerByName(args.Player.Name);
            if (play.destsign)
            {
                play.destsign = false;
                args.Player.SendMessage("You are now in use mode, Type \"/destsign\" to return to destroy mode!", Color.LightGreen);
            }
            else
            {
                play.destsign = true;
                args.Player.SendMessage("You are now in destroy mode, Type \"/destsign\" to return to use mode!", Color.LightGreen);
            }
        }

        public static void screload(CommandArgs args)
        {
            ReloadConfig(args);
        }
        #endregion

        #region Config
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
            bool iskits = false;
            try
            {
                if (File.Exists(TempConfigPath))
                {
                    getConfig = scConfig.Read(TempConfigPath);
                }
                getConfig.Write(TempConfigPath);
                args.Player.SendMessage("Sign Command Config Reloaded Successfully.", Color.MediumSeaGreen);

                iskits = true;
                KitReader reader = new KitReader();
                kits = reader.readFile(Path.Combine(TShockAPI.TShock.SavePath, "kits.cfg"));              
                return;
            }
            catch (Exception ex)
            {
                if (iskits)
                {
                    args.Player.SendMessage("However, Kits failed to reload!", Color.IndianRed);
                }
                else
                {
                    args.Player.SendMessage("Error: Could not reload Sign Command config, Check log for more details.", Color.IndianRed);
                    Log.Error("Config Exception in Sign Commands config file");
                    Log.Error(ex.ToString());
                }
            }
        }
        #endregion

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

        #region Timer
        static void Cooldown_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Global Cooldowns:
            if (getConfig.GlobalBossCooldown && GlobalBossCooldown > 0)
                GlobalBossCooldown--;

            if (getConfig.GlobalTimeCooldown && GlobalTimeCooldown > 0)
                GlobalTimeCooldown--;

            if (getConfig.GlobalSpawnMobCooldown && GlobalSpawnMobCooldown > 0)
                GlobalSpawnMobCooldown--;

            if (getConfig.GlobalDoCommandCooldown && GlobalCommandCooldown > 0)
                GlobalCommandCooldown--;

            lock (scPlayers)
            {
                foreach (scPlayer play in scPlayers)
                {
                    //Player Cooldowns:
                    if (play.CooldownBoss > 0)
                        play.CooldownBoss--;

                    if (play.CooldownTime > 0)
                        play.CooldownTime--;

                    if (play.CooldownSpawnMob > 0)
                        play.CooldownSpawnMob--;

                    if (play.CooldownDamage > 0)
                        play.CooldownDamage--;

                    if (play.CooldownHeal > 0)
                        play.CooldownHeal--;

                    if (play.CooldownMsg > 0)
                        play.CooldownMsg--;

                    if (play.CooldownItem > 0)
                        play.CooldownItem--;

                    if (play.CooldownBuff > 0)
                        play.CooldownBuff--;

                    if (play.CooldownKit > 0)
                        play.CooldownKit--;

                    if (play.CooldownCommand > 0)
                        play.CooldownCommand--;

                    //Message Cooldowns:
                    if (play.toldcool > 0)
                        play.toldcool--;

                    if (play.toldperm > 0)
                        play.toldperm--;

                    if (play.toldalert > 0)
                        play.toldalert--;

                    if (play.tolddest > 0)
                        play.tolddest--;

                    if (play.handlecmd > 0)
                        play.handlecmd--;

                    if (play.checkforsignedit)
                    {
                        if (Main.sign[play.signeditid].text.ToLower().Contains(getConfig.DefineSignCommands.ToLower()) && Main.sign[play.signeditid].text != play.originalsigntext)
                        {
                            play.TSPlayer.SendMessage("You do not have permission to create/edit sign commands!", Color.IndianRed);
                            //Main.sign[play.signeditid].text = play.originalsigntext;
                            Sign.TextSign(play.signeditid, play.originalsigntext);

                        }
                        play.checkforsignedit = false;
                    }
                }
            }
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
                            scPlayer hitplay = GetscPlayerByName(tplayer.Name);

                            if (!tplayer.Group.HasPermission("destroysigncommand"))
                            {
                                dosigncmd(id, tplayer);

                                tplayer.SendTileSquare(x, y);
                                e.Handled = true;
                            }
                            else if (tplayer.Group.HasPermission("destroysigncommand") && !hitplay.destsign)
                            {
                                if (hitplay.tolddest <= 0)
                                {
                                    tplayer.SendMessage("To destroy this sign, Type /destsign", Color.IndianRed);
                                    hitplay.tolddest = 10;
                                }

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
                        scPlayer killplay = GetscPlayerByName(tplayer.Name);
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
                                if (!killplay.destsign)
                                {
                                    if (killplay.tolddest <= 0)
                                    {
                                        tplayer.SendMessage("To destroy this sign, Type /destsign", Color.IndianRed);
                                        killplay.tolddest = 10;
                                    }

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
                            scPlayer edplay = GetscPlayerByName(tplayer.Name);

                            if (!Main.sign[id].text.ToLower().Contains(getConfig.DefineSignCommands.ToLower()) && !tplayer.Group.HasPermission("createsigncommand"))
                            {
                                edplay.originalsigntext = Main.sign[id].text;
                                edplay.signeditid = id;
                                edplay.checkforsignedit = true;
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

        public static void dosigncmd(int id, TSPlayer tplayer)
        {
            scPlayer doplay = GetscPlayerByName(tplayer.Name);
            
            #region Time
            if (tplayer.Group.HasPermission("usesigntime") && (tplayer.Group.HasPermission("nosccooldown") || ((getConfig.GlobalTimeCooldown && GlobalTimeCooldown <= 0) || (!getConfig.GlobalTimeCooldown && doplay.CooldownTime <= 0))))
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

                if (getConfig.GlobalTimeCooldown)
                    GlobalTimeCooldown = getConfig.TimeCooldown;
                else
                    doplay.CooldownTime = getConfig.TimeCooldown;
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesigntime") && Main.sign[id].text.ToLower().Contains("time "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesigntime") && (!tplayer.Group.HasPermission("nosccooldown") && (!getConfig.GlobalTimeCooldown && doplay.CooldownTime > 0)) && Main.sign[id].text.ToLower().Contains("time "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownTime + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesigntime") && (!tplayer.Group.HasPermission("nosccooldown") && (getConfig.GlobalTimeCooldown && GlobalTimeCooldown > 0)) && Main.sign[id].text.ToLower().Contains("time "))
            {
                tplayer.SendMessage("Everyone has to wait another " + GlobalTimeCooldown + " seconds before using this sign", Color.IndianRed); 
                doplay.toldcool = 5;
            }
                #endregion

            #region Heal
            if (tplayer.Group.HasPermission("usesignheal") && (tplayer.Group.HasPermission("nosccooldown") || doplay.CooldownHeal <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("heal"))
                {
                    Item heart = TShock.Utils.GetItemById(58);
                    Item star = TShock.Utils.GetItemById(184);
                    for (int ic = 0; ic < 20; ic++)
                        tplayer.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
                    for (int ic = 0; ic < 10; ic++)
                        tplayer.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
                    doplay.CooldownHeal = getConfig.HealCooldown;
                }
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesignheal") && Main.sign[id].text.ToLower().Contains("heal"))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesignheal") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownHeal > 0 && Main.sign[id].text.ToLower().Contains("heal"))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownHeal + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            #endregion

            #region Message
            if (tplayer.Group.HasPermission("usesignmessage") && (tplayer.Group.HasPermission("nosccooldown") || doplay.CooldownMsg <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("show motd"))
                    TShock.Utils.ShowFileToUser(tplayer, "motd.txt");
                else if (Main.sign[id].text.ToLower().Contains("show rules"))
                    TShock.Utils.ShowFileToUser(tplayer, "rules.txt");
                else if (Main.sign[id].text.ToLower().Contains("show playing"))
                    tplayer.SendMessage(string.Format("Current players: {0}.", TShock.Utils.GetPlayers()), 255, 240, 20);
                else if (Main.sign[id].text.ToLower().Contains("show message") && File.Exists("scMeassge.txt"))
                    TShock.Utils.ShowFileToUser(tplayer, "scMessage.txt");
                else if (Main.sign[id].text.ToLower().Contains("show message") && !File.Exists("scMeassge.txt"))
                    tplayer.SendMessage("Could not find message", Color.IndianRed);

                doplay.CooldownMsg = getConfig.ShowMsgCooldown;
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesignmessage") && Main.sign[id].text.ToLower().Contains("show "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesignmessage") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownMsg > 0 && Main.sign[id].text.ToLower().Contains("show "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownTime + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            #endregion

            #region Damage
            if (tplayer.Group.HasPermission("usesigndamage") && (tplayer.Group.HasPermission("nosccooldown") || doplay.CooldownDamage <= 0))
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
                            doplay.CooldownDamage = getConfig.DamageCooldown;
                        }
                        else
                        {
                            if (doplay.toldalert <= 0)
                            {
                                doplay.toldalert = 3;
                                tplayer.SendMessage("Invalid damage value!", Color.IndianRed);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (doplay.toldalert <= 0)
                        {
                            doplay.toldalert = 3;
                            tplayer.SendMessage("Could not parse Damage Amount - Correct Format: \"<Amount to Damage>\"", Color.IndianRed);
                        }
                    }
                }
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesigndamage") && Main.sign[id].text.ToLower().Contains("damage "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesigndamage") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownDamage > 0 && Main.sign[id].text.ToLower().Contains("damage "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownDamage + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            #endregion

            #region Boss
            if (tplayer.Group.HasPermission("usesignspawnboss") && (tplayer.Group.HasPermission("nosccooldown") || ((getConfig.GlobalBossCooldown && GlobalBossCooldown <= 0) || (!getConfig.GlobalBossCooldown && doplay.CooldownBoss <= 0))))
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
                else if (Main.sign[id].text.ToLower().Contains("boss skeletron") && !Main.sign[id].text.ToLower().Contains("boss skeletronprime"))
                {
                    NPC npc = TShock.Utils.GetNPCById(35);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                }
                else if (Main.sign[id].text.ToLower().Contains("boss wof"))
                {
                    if (Main.wof >= 0 || (tplayer.Y / 16f < (float)(Main.maxTilesY - 205)))
                    {
                        if (doplay.toldalert <= 0)
                        {
                            doplay.toldalert = 3;
                            tplayer.SendMessage("Can't spawn Wall of Flesh!", Color.Red);
                        }
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

                if (getConfig.GlobalBossCooldown)
                    GlobalBossCooldown = getConfig.BossCooldown;
                else
                    doplay.CooldownBoss = getConfig.BossCooldown;
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesignspawnboss") && Main.sign[id].text.ToLower().Contains("boss "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesignspawnboss") && (!tplayer.Group.HasPermission("nosccooldown") && (!getConfig.GlobalBossCooldown && doplay.CooldownBoss > 0)) && Main.sign[id].text.ToLower().Contains("boss "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownBoss + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesignspawnboss") && (!tplayer.Group.HasPermission("nosccooldown") && (getConfig.GlobalBossCooldown && GlobalBossCooldown > 0)) && Main.sign[id].text.ToLower().Contains("boss "))
            {
                tplayer.SendMessage("Everyone has to wait another " + GlobalBossCooldown + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            #endregion

            #region Spawn Mob
            if (tplayer.Group.HasPermission("usesignspawnmob") && (tplayer.Group.HasPermission("nosccooldown") || ((getConfig.GlobalSpawnMobCooldown && GlobalSpawnMobCooldown <= 0) || (!getConfig.GlobalSpawnMobCooldown && doplay.CooldownSpawnMob <= 0))))
            {
                if (Main.sign[id].text.ToLower().Contains("spawn mob ") || Main.sign[id].text.ToLower().Contains("spawnmob ") || Main.sign[id].text.ToLower().Contains("spawn "))
                {
                    try
                    {
                        string[] linesplit = Main.sign[id].text.Split('\'', '\"');
                        bool containstime = false;
                        string[] datasplit;

                        if (linesplit[1].Contains(","))
                        {
                            datasplit = linesplit[1].Split(',');
                            containstime = true;
                        }
                        else
                            datasplit = null;

                        var npcs = new List<NPC>();
                        int amount = 1;
                        amount = Math.Min(amount, Main.maxNPCs);

                        if (!containstime)
                        {
                            amount = 1;
                            npcs = TShock.Utils.GetNPCByIdOrName(linesplit[1]);
                        }
                        else
                        {
                            int.TryParse(datasplit[1], out amount);
                            npcs = TShock.Utils.GetNPCByIdOrName(datasplit[0]);
                        }

                        if (npcs.Count == 0)
                        {
                            if (doplay.toldalert <= 0)
                            {
                                doplay.toldalert = 3;
                                tplayer.SendMessage("Invalid mob type!", Color.Red);
                            }
                        }
                        else if (npcs.Count > 1)
                        {
                            if (doplay.toldalert <= 0)
                            {
                                doplay.toldalert = 3;
                                tplayer.SendMessage(string.Format("More than one ({0}) mob matched!", npcs.Count), Color.Red);
                            }
                        }
                        else
                        {
                            var npc = npcs[0];
                            if (npc.type >= 1 && npc.type < Main.maxNPCTypes && npc.type != 113)
                            {
                                TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, tplayer.TileX, tplayer.TileY, 50, 20);
                                if (getConfig.GlobalSpawnMobCooldown)
                                    GlobalSpawnMobCooldown = getConfig.SpawnMobCooldown;
                                else
                                    doplay.CooldownSpawnMob = getConfig.SpawnMobCooldown;
                            }
                            else if (npc.type == 113)
                            {
                                if (doplay.toldalert <= 0)
                                {
                                    doplay.toldalert = 3;
                                    tplayer.SendMessage("Sorry, you can't spawn Wall of Flesh! Try the sign command \"boss wof\"");
                                }
                            }
                            else
                            {
                                if (doplay.toldalert <= 0)
                                {
                                    doplay.toldalert = 3;
                                    tplayer.SendMessage("Invalid mob type!", Color.Red);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (doplay.toldalert <= 0)
                        {
                            doplay.toldalert = 3;
                            tplayer.SendMessage("Could not parse Mob / Amount - Correct Format: \"<Mob Name>, <Amount>\"", Color.IndianRed);
                        }
                    }
                }
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesignspawnmob") && Main.sign[id].text.ToLower().Contains("spawnmob"))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesignspawnmob") && (!tplayer.Group.HasPermission("nosccooldown") && (!getConfig.GlobalSpawnMobCooldown && doplay.CooldownSpawnMob > 0)) &&
                (Main.sign[id].text.ToLower().Contains("spawn mob ") || Main.sign[id].text.ToLower().Contains("spawnmob ") || Main.sign[id].text.ToLower().Contains("spawn ")))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownSpawnMob + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesignspawnmob") && (!tplayer.Group.HasPermission("nosccooldown") && (getConfig.GlobalSpawnMobCooldown && GlobalSpawnMobCooldown > 0)) &&
                (Main.sign[id].text.ToLower().Contains("spawn mob ") || Main.sign[id].text.ToLower().Contains("spawnmob ") || Main.sign[id].text.ToLower().Contains("spawn ")))
            {
                tplayer.SendMessage("Everyone has to wait another " + GlobalSpawnMobCooldown + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            #endregion

            #region Warp
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
                            if (doplay.toldalert <= 0)
                            {
                                doplay.toldalert = 3;
                                tplayer.SendMessage("Could not find warp!", Color.IndianRed);
                            }
                        }
                        else
                            tplayer.Teleport((int)warpxy.WarpPos.X, (int)warpxy.WarpPos.Y);
                    }
                    catch (Exception)
                    {
                        if (doplay.toldalert <= 0)
                        {
                            doplay.toldalert = 3;
                            tplayer.SendMessage("Could not parse Warp - Correct Format: \"<Warp Name>\"", Color.IndianRed);
                        }
                    }
                }
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesignwarp") && Main.sign[id].text.ToLower().Contains("warp "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            #endregion

            #region Item
            if (tplayer.Group.HasPermission("usesignitem") && (tplayer.Group.HasPermission("nosccooldown") || doplay.CooldownItem <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("item "))
                {
                    try
                    {
                        string[] linesplit = Main.sign[id].text.Split('\'', '\"');
                        bool containsamount = false;
                        string[] datasplit;

                        if (linesplit[1].Contains(","))
                        {
                            datasplit = linesplit[1].Split(',');
                            containsamount = true;
                        }
                        else
                            datasplit = null;

                        int itemAmount = 0;
                        var items = new List<Item>();

                        if (!containsamount)
                        {
                            itemAmount = 1;
                            items = TShock.Utils.GetItemByIdOrName(linesplit[1]);
                        }
                        else
                        {
                            int.TryParse(datasplit[1], out itemAmount);
                            items = TShock.Utils.GetItemByIdOrName(datasplit[0]);
                        }

                        if (items.Count <= 0)
                        {
                            if (doplay.toldalert <= 0)
                            {
                                doplay.toldalert = 3;
                                tplayer.SendMessage("Invalid item type!", Color.Red);
                            }
                        }
                        else if (items.Count > 1)
                        {
                            if (doplay.toldalert <= 0)
                            {
                                doplay.toldalert = 3;
                                tplayer.SendMessage(string.Format("More than one ({0}) item matched!", items.Count), Color.Red);
                            }
                        }
                        else
                        {
                            var item = items[0];
                            if (item.type >= 1 && item.type < Main.maxItemTypes)
                            {
                                if (tplayer.InventorySlotAvailable || item.name.Contains("Coin"))
                                {
                                    if (itemAmount <= 0 || itemAmount > item.maxStack)
                                        itemAmount = item.maxStack;
                                    tplayer.GiveItem(item.type, item.name, item.width, item.height, itemAmount, 0);
                                    if (doplay.toldalert <= 0)
                                    {
                                        doplay.toldalert = 3;
                                        tplayer.SendMessage(string.Format("Gave {0} {1}(s).", itemAmount, item.name));
                                    }
                                    doplay.CooldownItem = getConfig.ItemCooldown;
                                }
                                else
                                {
                                    if (doplay.toldalert <= 0)
                                    {
                                        doplay.toldalert = 3;
                                        tplayer.SendMessage("You don't have free slots!", Color.Red);
                                    }
                                }
                            }
                            else
                            {
                                if (doplay.toldalert <= 0)
                                {
                                    doplay.toldalert = 3;
                                    tplayer.SendMessage("Invalid item type!", Color.Red);
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (doplay.toldalert <= 0)
                        {
                            doplay.toldalert = 3;
                            tplayer.SendMessage("Could not parse Item / Amount - Correct Format: \"<Item Name>, <Amount>\"", Color.IndianRed);
                        }
                    }
                }
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesignitem") && Main.sign[id].text.ToLower().Contains("item "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesignitem") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownItem > 0 && Main.sign[id].text.ToLower().Contains("item "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownItem + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            #endregion

            #region Buff
            if (tplayer.Group.HasPermission("usesignbuff") && (tplayer.Group.HasPermission("nosccooldown") || doplay.CooldownItem <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("buff "))
                {
                    try
                    {
                        string[] linesplit = Main.sign[id].text.Split('\'', '\"');

                        int bid = 0;
                        int time = 0;
                        bool containstime = false;
                        string[] datasplit;

                        if (linesplit[1].Contains(","))
                        {
                            datasplit = linesplit[1].Split(',');
                            containstime = true;
                        }
                        else
                            datasplit = null;

                        string buffvalue = "";

                        if (!containstime)
                        {
                            time = 60;
                            buffvalue = linesplit[1];
                        }
                        else
                        {
                            int.TryParse(datasplit[1], out time);
                            buffvalue = datasplit[0];
                        }

                        if (!int.TryParse(buffvalue, out bid))
                        {
                            var found = TShock.Utils.GetBuffByName(buffvalue);
                            if (found.Count == 0)
                            {
                                if (doplay.toldalert <= 0)
                                {
                                    doplay.toldalert = 3;
                                    doplay.TSPlayer.SendMessage("Invalid buff name!", Color.Red);
                                }
                                return;
                            }
                            else if (found.Count > 1)
                            {
                                if (doplay.toldalert <= 0)
                                {
                                    doplay.toldalert = 3;
                                    doplay.TSPlayer.SendMessage(string.Format("More than one ({0}) buff matched!", found.Count), Color.Red);
                                }
                                return;
                            }
                            bid = found[0];

                        }

                        if (bid > 0 && bid < Main.maxBuffs)
                        {
                            if (time < 0 || time > short.MaxValue)
                                time = 60;
                            doplay.TSPlayer.SetBuff(bid, time * 60);
                            if (doplay.toldalert <= 0)
                            {
                                doplay.toldalert = 3;
                                doplay.TSPlayer.SendMessage(string.Format("You have buffed yourself with {0}({1}) for {2} seconds!",
                                    TShock.Utils.GetBuffName(bid), TShock.Utils.GetBuffDescription(bid), (time)), Color.Green);
                            }
                            doplay.CooldownBuff = getConfig.BuffCooldown;
                        }
                        else
                        {
                            if (doplay.toldalert <= 0)
                            {
                                doplay.toldalert = 3;
                                doplay.TSPlayer.SendMessage("Invalid buff ID!", Color.Red);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        tplayer.SendMessage("Could not parse Buff / Duration - Correct Format: \"<Buff Name>, <Duration>\"", Color.IndianRed);
                    }
                }
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesignbuff") && Main.sign[id].text.ToLower().Contains("buff "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesignbuff") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownBuff > 0 && Main.sign[id].text.ToLower().Contains("buff "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownBuff + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            #endregion

            #region Kit
            if (tplayer.Group.HasPermission("usesignkit") && (tplayer.Group.HasPermission("nosccooldown") || doplay.CooldownKit <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("kit "))
                {
                    try
                    {
                        string[] linesplit = Main.sign[id].text.Split('\'', '\"');
                        if (tplayer == null)
                            return;

                        String kitname = linesplit[1];

                        Kit k = FindKit(kitname.ToLower());

                        if (k == null)
                        {
                            if (doplay.toldalert <= 0)
                            {
                                doplay.toldalert = 3;
                                tplayer.SendMessage(String.Format("The {0} kit does not exist.", kitname), Color.Red);
                            }
                            return;
                        }

                        if (tplayer.Group.HasPermission(k.getPerm()))
                        {
                            k.giveItems(tplayer);
                        }
                        else
                        {
                            if (doplay.toldalert <= 0)
                            {
                                doplay.toldalert = 3;
                                tplayer.SendMessage(String.Format("You do not have access to the {0} kit.", kitname), Color.Red);
                            }
                        }
                    }
                    catch (Exception)
                    {
                        if (doplay.toldalert <= 0)
                        {
                            doplay.toldalert = 3;
                            tplayer.SendMessage("Could not parse Kit - Correct Format: \"<Kit Name>\"", Color.IndianRed);
                        }
                    }
                }
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesignkit") && Main.sign[id].text.ToLower().Contains("kit "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesignkit") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownKit > 0 && Main.sign[id].text.ToLower().Contains("kit "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownKit + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            #endregion

            #region Do Command
            if (tplayer.Group.HasPermission("usesigndocommand") && (tplayer.Group.HasPermission("nosccooldown") ||
                ((getConfig.GlobalDoCommandCooldown && GlobalCommandCooldown <= 0) || (!getConfig.GlobalDoCommandCooldown && doplay.CooldownCommand <= 0))))
            {
                if (Main.sign[id].text.ToLower().Contains("command ") || Main.sign[id].text.ToLower().Contains("cmd "))
                {
                    try
                    {
                        string[] linesplit = Main.sign[id].text.Split('\'', '\"');
                        if (doplay.handlecmd == 0)
                        {
                            TShockAPI.Commands.HandleCommand(tplayer, "/" + linesplit[1]);
                            doplay.handlecmd = 2;
                        }

                    }
                    catch (Exception)
                    {
                        if (doplay.toldalert <= 0)
                        {
                            doplay.toldalert = 3;
                            tplayer.SendMessage("Could not parse Command - Correct Format: \"<Command>\"", Color.IndianRed);
                        }
                    }
                }
            }
            else if (doplay.toldperm <= 0 && !tplayer.Group.HasPermission("usesigndocommand") && (Main.sign[id].text.ToLower().Contains("Command ") || Main.sign[id].text.ToLower().Contains("Cmd ")))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesigndocommand") && (!tplayer.Group.HasPermission("nosccooldown") && (!getConfig.GlobalDoCommandCooldown && doplay.CooldownCommand > 0)) && (Main.sign[id].text.ToLower().Contains("command ") || Main.sign[id].text.ToLower().Contains("cmd ")))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownCommand + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            else if (doplay.toldcool <= 0 && tplayer.Group.HasPermission("usesigndocommand") && (!tplayer.Group.HasPermission("nosccooldown") && (getConfig.GlobalDoCommandCooldown && GlobalCommandCooldown > 0)) && (Main.sign[id].text.ToLower().Contains("command ") || Main.sign[id].text.ToLower().Contains("cmd ")))
            {
                tplayer.SendMessage("Everyone has to wait another " + GlobalCommandCooldown + " seconds before using this sign", Color.IndianRed);
                doplay.toldcool = 5;
            }
            #endregion
        }

        public static Kit FindKit(String name)
        {
            return kits.findKit(name);
        }

        public static scPlayer GetscPlayerByID(int id)
        {
            scPlayer player = null;
            foreach (scPlayer ply in scPlayers)
            {
                if (ply.Index == id)
                    return ply;
            }
            return player;
        }
        public static scPlayer GetscPlayerByName(string name)
        {
            var player = TShock.Utils.FindPlayer(name)[0];
            if (player != null)
            {
                foreach (scPlayer ply in scPlayers)
                {
                    if (ply.TSPlayer == player)
                        return ply;
                }
            }
            return null;
        }
    }

    public class scPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public bool destsign = false;
        public int toldperm = 0;
        public int toldcool = 0;
        public int tolddest = 0;
        public int toldalert = 0;
        public int CooldownTime = 0;
        public int CooldownHeal = 0;
        public int CooldownMsg = 0;
        public int CooldownDamage = 0;
        public int CooldownBoss = 0;
        public int CooldownItem = 0;
        public int CooldownBuff = 0;
        public int CooldownSpawnMob = 0;
        public int CooldownKit = 0;
        public int CooldownCommand = 0;
        public int handlecmd = 0;
        public bool checkforsignedit = false;
        public bool DestroyingSign = false;
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
        public bool GlobalTimeCooldown = false;
        public int TimeCooldown = 20;
        public int HealCooldown = 20;
        public int ShowMsgCooldown = 20;
        public int DamageCooldown = 20;
        public bool GlobalBossCooldown = false;
        public int BossCooldown = 20;
        public int ItemCooldown = 60;
        public int BuffCooldown = 20;
        public bool GlobalSpawnMobCooldown = false;
        public int SpawnMobCooldown = 20;
        public int KitCooldown = 20;
        public bool GlobalDoCommandCooldown = false;
        public int DoCommandCooldown = 20;

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