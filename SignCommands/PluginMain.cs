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
            if (tick1 >= 1000)
            {
                CountDown = DateTime.UtcNow;
                lock (scPlayers)
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

                        if (play.CooldownBuff > 0)
                            play.CooldownBuff--;

                        if (play.CooldownAShop > 0)
                            play.CooldownAShop--;

                        if (play.checkforsignedit)
                        {
                            if (Main.sign[play.signeditid].text.ToLower().Contains(getConfig.DefineSignCommands.ToLower()))
                            {
                                play.TSPlayer.SendMessage("You do not have permission to create/edit sign commands!", Color.IndianRed);
                                Main.sign[play.signeditid].text = play.originalsigntext;
                            }
                        } //RM
                            /*else if (Main.sign[play.signeditid].text.ToLower().Contains(getConfig.DefineAdminSignShop.ToLower()))
                            {
                                play.TSPlayer.SendMessage("You do not have permission to create/edit sign shops!", Color.IndianRed);
                                Main.sign[play.signeditid].text = play.originalsigntext;
                            }

                            play.checkforsignedit = false;
                        }
                        if (play.lastcheck)
                        {
                            //foreach (Item todestroy in Main.item)
                            for (int i = 0; i < 200; i++)
                            {
                                if (Main.item[i].name == play.lastSitem.name && Main.item[i].stack == play.lastSamt
                                    && ((play.lastSignX * 16 + 48) >= Main.item[i].position.X && (play.lastSignX * 16 - 48) <= Main.item[i].position.X)
                                    && ((play.lastSignY * 16 + 48) >= Main.item[i].position.Y && (play.lastSignY * 16 - 48) <= Main.item[i].position.Y))
                                {
                                    Main.item[i].active = false;
                                    NetMessage.SendData(0x15, -1, -1, "", i, 0f, 0f, 0f, 0);

                                    play.lastcheck = false;
                                    play.lastBitem = new Item();
                                    play.lastBamt = 0;
                                    play.lastSitem = new Item();
                                    play.lastSamt = 0;
                                    play.lastSignX = 0;
                                    play.lastSignY = 0;
                                }
                                i++;
                            }
                        }

                        if (play.CheckForDrop)
                        {
                            if (play.warntransaction <= 0)
                            {
                                play.TSPlayer.SendMessage("Transaction still in progress!", Color.IndianRed);
                                play.warntransaction = 7;
                            }
                            else
                                play.warntransaction--;
                        }*/
                    }
                }
            }

            /*foreach (scPlayer play in scPlayers)
            {
                if (play.CheckForDrop)
                {
                    //foreach (Item todestroy in Main.item)
                    for (int i = 0; i < 200; i++)
                    {
                        if (Main.item[i].name == play.Sitem.name && Main.item[i].stack == play.Samt
                            && ((play.SignX * 16 + 48) >= Main.item[i].position.X && (play.SignX * 16 - 48) <= Main.item[i].position.X)
                            && ((play.SignY * 16 + 48) >= Main.item[i].position.Y && (play.SignY * 16 - 48) <= Main.item[i].position.Y))
                        {
                            Main.item[i].active = false;
                            NetMessage.SendData(0x15, -1, -1, "", i, 0f, 0f, 0f, 0);

                            int tpx = play.SignX;
                            int tpy = play.SignY;
                            if (play.TSPlayer.Teleport(tpx, tpy))
                                play.TSPlayer.GiveItem(play.Bitem.type, play.Bitem.name, play.Bitem.width, play.Bitem.height, play.Bamt);

                            play.lastcheck = true;
                            play.lastBitem = play.Bitem;
                            play.lastBamt = play.Bamt;
                            play.lastSitem = play.Sitem;
                            play.lastSamt = play.Samt;
                            play.lastSignX = play.SignX;
                            play.lastSignY = play.SignY;

                            play.TSPlayer.SendMessage("Transaction Complete!", Color.MediumSeaGreen);
                            play.CheckForDrop = false;
                            play.Bitem = new Item();
                            play.Bamt = 0;
                            play.Sitem = new Item();
                            play.Samt = 0;
                            play.SignX = 0;
                            play.SignY = 0;
                            play.warntransaction = 7;
                            return;
                        }
                        i++;
                    }
                }
            }*/
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
            scPlayer play = GetscPlayerByName(args.Player.Name);
            if (play.destsign)
            {
                play.destsign = false;
                args.Player.SendMessage("You are now in use mode, Type \"/destsign\" to return to destroy mode!", Color.RoyalBlue);
            }
            else
            {
                play.destsign = true;
                args.Player.SendMessage("You are now in destroy mode, Type \"/destsign\" to return to use mode!", Color.RoyalBlue);
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

                        if (id != -1 && (Main.sign[id].text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower()) || Main.sign[id].text.ToLower().StartsWith(getConfig.DefineAdminSignShop.ToLower())))
                        {
                            scPlayer hitplay = GetscPlayerByName(tplayer.Name);

                            if (!tplayer.Group.HasPermission("destroysigncommand"))
                            {
                                if (Main.sign[id].text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower()))
                                    dosigncmd(id, tplayer);
                                /*else if (Main.sign[id].text.ToLower().StartsWith(getConfig.DefineAdminSignShop.ToLower()))
                                    dosignshop(id, tplayer);*/

                                tplayer.SendTileSquare(x, y);
                                e.Handled = true;
                            }
                            else if (tplayer.Group.HasPermission("destroysigncommand") && !hitplay.destsign)
                            {
                                if (hitplay.tolddestsign == 10)
                                {
                                    tplayer.SendMessage("To destroy this sign, Type /destsign", Color.IndianRed);
                                    hitplay.tolddestsign = 0;
                                }
                                else
                                    hitplay.tolddestsign++;

                                if (Main.sign[id].text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower()))
                                    dosigncmd(id, tplayer);
                                /*else if (Main.sign[id].text.ToLower().StartsWith(getConfig.DefineAdminSignShop.ToLower()))
                                    dosignshop(id, tplayer);*/

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
                        if (id != -1 && (Main.sign[id].text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower()) || Main.sign[id].text.ToLower().StartsWith(getConfig.DefineAdminSignShop.ToLower())))
                        {
                            if (!tplayer.Group.HasPermission("destroysigncommand"))
                            {
                                if (Main.sign[id].text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower()))
                                    dosigncmd(id, tplayer);
                                /*else if (Main.sign[id].text.ToLower().StartsWith(getConfig.DefineAdminSignShop.ToLower()))
                                    dosignshop(id, tplayer);*/

                                tplayer.SendTileSquare(x, y);
                                e.Handled = true;
                            }
                            else if (tplayer.Group.HasPermission("destroysigncommand"))
                            {
                                if (!killplay.destsign)
                                {
                                    if (killplay.tolddestsign == 10)
                                    {
                                        tplayer.SendMessage("To destroy this sign, Type /destsign", Color.IndianRed);
                                        killplay.tolddestsign = 0;
                                    }
                                    else
                                        killplay.tolddestsign++;

                                    if (Main.sign[id].text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower()))
                                        dosigncmd(id, tplayer);
                                    /*else if (Main.sign[id].text.ToLower().StartsWith(getConfig.DefineAdminSignShop.ToLower()))
                                        dosignshop(id, tplayer);*/
                                    
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

                            if (/*(*/!Main.sign[id].text.ToLower().Contains(getConfig.DefineSignCommands.ToLower()) && !tplayer.Group.HasPermission("createsigncommand")/*) || (!Main.sign[id].text.ToLower().Contains(getConfig.DefineAdminSignShop.ToLower()) && !tplayer.Group.HasPermission("createadminshop"))*/)
                            {
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
                            /*else if (Main.sign[id].text.ToLower().Contains(getConfig.DefineSignCommands.ToLower()) && !tplayer.Group.HasPermission("createadminshop"))
                            {
                                tplayer.SendMessage("You do not have permission to create/edit sign shops!", Color.IndianRed);
                                e.Handled = true;
                                tplayer.SendData(PacketTypes.SignNew, "", id);
                                return;
                            }*/
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
            scPlayer doplay = GetscPlayerByName(tplayer.Name);

            if (tplayer.Group.HasPermission("usesigntime") && (tplayer.Group.HasPermission("nosccooldown") || doplay.CooldownTime <= 0))
            {
                //SignCmd:
                //Time Sign commands!
                if (Main.sign[id].text.ToLower().Contains("time day"))
                {
                    TSPlayer.Server.SetTime(true, 150.0);
                    doplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time night"))
                {
                    TSPlayer.Server.SetTime(false, 0.0);
                    doplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time dusk"))
                {
                    TSPlayer.Server.SetTime(false, 0.0);
                    doplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time noon"))
                {
                    TSPlayer.Server.SetTime(true, 27000.0);
                    doplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time midnight"))
                {
                    TSPlayer.Server.SetTime(false, 16200.0);
                    doplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time fullmoon"))
                {
                    TSPlayer.Server.SetFullMoon(true);
                    doplay.CooldownTime = getConfig.TimeCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("time bloodmoon"))
                {
                    TSPlayer.Server.SetBloodMoon(true);
                    doplay.CooldownTime = getConfig.TimeCooldown;
                }
            }
            else if (doplay.toldperm == 5 && !tplayer.Group.HasPermission("usesigntime") && Main.sign[id].text.ToLower().Contains("time "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm == 5 && tplayer.Group.HasPermission("usesigntime") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownTime > 0 && Main.sign[id].text.ToLower().Contains("time "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownTime + " seconds before using this sign", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm != 5 && Main.sign[id].text.ToLower().Contains("time "))
                doplay.toldperm++;

            //SignCmd:
            //Heal Sign command!
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
            else if (doplay.toldperm == 5 && !tplayer.Group.HasPermission("usesignheal") && Main.sign[id].text.ToLower().Contains("heal"))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm == 5 && tplayer.Group.HasPermission("usesignheal") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownHeal > 0 && Main.sign[id].text.ToLower().Contains("heal"))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownHeal + " seconds before using this sign", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm != 5 && Main.sign[id].text.ToLower().Contains("heal"))
                doplay.toldperm++;

            //SignCmd:
            //Show Message Sign commands!
            if (tplayer.Group.HasPermission("usesignmessage") && (tplayer.Group.HasPermission("nosccooldown") || doplay.CooldownMsg <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("show motd"))
                {
                    TShock.Utils.ShowFileToUser(tplayer, "motd.txt");
                    doplay.CooldownMsg = getConfig.ShowMsgCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("show rules"))
                {
                    TShock.Utils.ShowFileToUser(tplayer, "rules.txt");
                    doplay.CooldownMsg = getConfig.ShowMsgCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("show playing"))
                {
                    tplayer.SendMessage(string.Format("Current players: {0}.", TShock.Utils.GetPlayers()), 255, 240, 20);
                    doplay.CooldownMsg = getConfig.ShowMsgCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("show message") && File.Exists("scMeassge.txt"))
                {
                    TShock.Utils.ShowFileToUser(tplayer, "scMessage.txt");
                    doplay.CooldownMsg = getConfig.ShowMsgCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("show message") && !File.Exists("scMeassge.txt"))
                {
                    tplayer.SendMessage("Could not find message", Color.IndianRed);
                    doplay.CooldownMsg = getConfig.ShowMsgCooldown;
                }
            }
            else if (doplay.toldperm == 5 && !tplayer.Group.HasPermission("usesignmessage") && Main.sign[id].text.ToLower().Contains("show "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm == 5 && tplayer.Group.HasPermission("usesignmessage") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownMsg > 0 && Main.sign[id].text.ToLower().Contains("show "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownTime + " seconds before using this sign", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm != 5 && Main.sign[id].text.ToLower().Contains("show "))
                doplay.toldperm++;

            //SignCmd:
            //Damage Sign command!
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
                            tplayer.SendMessage("Invalid damage value!", Color.IndianRed);
                    }
                    catch (Exception)
                    {
                        tplayer.SendMessage("Could not parse Damage Amount - Correct Format: \"<Amount to Damage>\"", Color.IndianRed);
                    }
                }
            }
            else if (doplay.toldperm == 5 && !tplayer.Group.HasPermission("usesigndamage") && Main.sign[id].text.ToLower().Contains("damage "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm == 5 && tplayer.Group.HasPermission("usesigndamage") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownDamage > 0 && (Main.sign[id].text.ToLower().Contains("damage") || Main.sign[id].text.ToLower().Contains("kill")))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownDamage + " seconds before using this sign", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm != 5 && (Main.sign[id].text.ToLower().Contains("damage") || Main.sign[id].text.ToLower().Contains("kill")))
                doplay.toldperm++;

            //SignCmd:
            //Spawn Boss Sign commands!
            if (tplayer.Group.HasPermission("usesignspawnboss") && (tplayer.Group.HasPermission("nosccooldown") || doplay.CooldownBoss <= 0))
            {
                if (Main.sign[id].text.ToLower().Contains("boss eater"))
                {
                    NPC npc = TShock.Utils.GetNPCById(13);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    doplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss eye"))
                {
                    NPC npc = TShock.Utils.GetNPCById(4);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    doplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss king"))
                {
                    NPC npc = TShock.Utils.GetNPCById(50);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    doplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss skeletron") && !Main.sign[id].text.ToLower().Contains("boss skeletronprime"))
                {
                    NPC npc = TShock.Utils.GetNPCById(35);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    doplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss wof"))
                {
                    if (Main.wof >= 0 || (tplayer.Y / 16f < (float)(Main.maxTilesY - 205)))
                    {
                        if (doplay.toldperm == 10)
                        {
                            tplayer.SendMessage("Can't spawn Wall of Flesh!", Color.Red);
                            doplay.toldperm = 0;
                        }
                        else
                            doplay.toldperm++;
                        return;
                    }
                    NPC.SpawnWOF(new Vector2(tplayer.X, tplayer.Y));
                    doplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss twins"))
                {
                    NPC retinazer = TShock.Utils.GetNPCById(125);
                    NPC spaz = TShock.Utils.GetNPCById(126);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, 1, tplayer.TileX, tplayer.TileY);
                    TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, 1, tplayer.TileX, tplayer.TileY);
                    doplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss destroyer"))
                {
                    NPC npc = TShock.Utils.GetNPCById(134);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    doplay.CooldownBoss = getConfig.BossCooldown;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss skeletronprime"))
                {
                    NPC npc = TShock.Utils.GetNPCById(127);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    doplay.CooldownBoss = getConfig.BossCooldown;
                }
            }
            else if (doplay.toldperm == 5 && !tplayer.Group.HasPermission("usesignspawnboss") && Main.sign[id].text.ToLower().Contains("boss "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm == 5 && tplayer.Group.HasPermission("usesignspawnboss") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownBoss > 0 && Main.sign[id].text.ToLower().Contains("boss "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownBoss + " seconds before using this sign", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm != 5 && Main.sign[id].text.ToLower().Contains("boss "))
                doplay.toldperm++;

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
                            if (doplay.toldwarp == 5)
                            {
                                tplayer.SendMessage("Could not find warp!", Color.IndianRed);
                                doplay.toldwarp = 0;
                            }
                            else
                                doplay.toldwarp++;
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
            else if (doplay.toldperm == 5 && !tplayer.Group.HasPermission("usesignwarp") && Main.sign[id].text.ToLower().Contains("warp "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm != 5 && Main.sign[id].text.ToLower().Contains("warp "))
                doplay.toldperm++;

            //SignCmd:
            //Item Sign command!
            if (tplayer.Group.HasPermission("usesignitem") && (tplayer.Group.HasPermission("nosccooldown") || doplay.CooldownItem <= 0))
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
                                    doplay.CooldownItem = getConfig.ItemCooldown;
                                }
                                else
                                    tplayer.SendMessage("You don't have free slots!", Color.Red);
                            }
                            else
                                tplayer.SendMessage("Invalid item type!", Color.Red);
                        }
                    }
                    catch (Exception)
                    {
                        tplayer.SendMessage("Could not parse Item / Amount - Correct Format: \"<Item Name>, <Amount>\"", Color.IndianRed);
                    }
                }
            }
            else if (doplay.toldperm == 5 && !tplayer.Group.HasPermission("usesignitem") && Main.sign[id].text.ToLower().Contains("item "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm == 5 && tplayer.Group.HasPermission("usesignitem") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownItem > 0 && Main.sign[id].text.ToLower().Contains("item "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownItem + " seconds before using this sign", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm != 5 && Main.sign[id].text.ToLower().Contains("item "))
                doplay.toldperm++;

            //SignCmd:
            //Buff Sign command!
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

                        if (Main.sign[id].text.Contains(", "))
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
                                doplay.TSPlayer.SendMessage("Invalid buff name!", Color.Red);
                                return;
                            }
                            else if (found.Count > 1)
                            {
                                doplay.TSPlayer.SendMessage(string.Format("More than one ({0}) buff matched!", found.Count), Color.Red);
                                return;
                            }
                            bid = found[0];
                            
                        }

                        if (bid > 0 && bid < Main.maxBuffs)
                        {
                            if (time < 0 || time > short.MaxValue)
                                time = 60;
                            doplay.TSPlayer.SetBuff(bid, time * 60);
                            doplay.TSPlayer.SendMessage(string.Format("You have buffed yourself with {0}({1}) for {2} seconds!",
                                TShock.Utils.GetBuffName(bid), TShock.Utils.GetBuffDescription(bid), (time)), Color.Green);
                        }
                        else
                            doplay.TSPlayer.SendMessage("Invalid buff ID!", Color.Red);
                    }
                    catch (Exception)
                    {
                        tplayer.SendMessage("Could not parse Buff / Duration - Correct Format: \"<Buff Name>, <Duration>\"", Color.IndianRed);
                    }
                }
            }
            else if (doplay.toldperm == 5 && !tplayer.Group.HasPermission("usesignbuff") && Main.sign[id].text.ToLower().Contains("buff "))
            {
                tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm == 5 && tplayer.Group.HasPermission("usesignbuff") && !tplayer.Group.HasPermission("nosccooldown") && doplay.CooldownBuff > 0 && Main.sign[id].text.ToLower().Contains("buff "))
            {
                tplayer.SendMessage("You have to wait another " + doplay.CooldownBuff + " seconds before using this sign", Color.IndianRed);
                doplay.toldperm = 0;
            }
            else if (doplay.toldperm != 5 && Main.sign[id].text.ToLower().Contains("buff "))
                doplay.toldperm++;
        }
        #endregion


        #region Do Shop
        /*public static void dosignshop(int id, TSPlayer tplayer)
        {
            scPlayer sdoplay = GetscPlayerByName(tplayer.Name);

            //SignShop:
            //Admin Shop!
            if (tplayer.Group.HasPermission("useadminshop") && (tplayer.Group.HasPermission("nosccooldown") || sdoplay.CooldownAShop <= 0))
            {
                try
                {
                    if (sdoplay.CheckForDrop)
                    {
                        tplayer.SendMessage("Transaction Cancled!", Color.MediumSeaGreen);
                        sdoplay.CheckForDrop = false;
                        sdoplay.Bitem = new Item();
                        sdoplay.Bamt = 0;
                        sdoplay.Sitem = new Item();
                        sdoplay.Samt = 0;
                        sdoplay.SignX = 0;
                        sdoplay.SignY = 0;
                        sdoplay.warntransaction = 7;
                        return;
                    }
                    else
                    {
                        string[] varsplit = Main.sign[id].text.Split('\'', '\"');
                        string[] datasplitB = varsplit[1].Split(',');
                        string[] datasplitS = varsplit[3].Split(',');

                        var Bitm = TShock.Utils.GetItemByIdOrName(datasplitB[0]);
                        if (Bitm.Count == 0)
                        {
                            tplayer.SendMessage("[Buy] Invalid item type!", Color.Red);
                            return;
                        }
                        else if (Bitm.Count > 1)
                        {
                            tplayer.SendMessage(string.Format("[Buy] More than one ({0}) item matched!", Bitm.Count), Color.Red);
                            return;
                        }
                        else
                            sdoplay.Bitem = Bitm[0];

                        int Bamt = 0;
                        if (!int.TryParse(datasplitB[1], out Bamt))
                        {
                            tplayer.SendMessage("Could not parse Buy Amount", Color.IndianRed);
                            return;
                        }

                        sdoplay.Bamt = Bamt;

                        var Sitm = TShock.Utils.GetItemByIdOrName(datasplitS[0]);
                        if (Sitm.Count == 0)
                        {
                            tplayer.SendMessage("[Sell] Invalid item type!", Color.Red);
                            return;
                        }
                        else if (Sitm.Count > 1)
                        {
                            tplayer.SendMessage(string.Format("[Sell] More than one ({0}) item matched!", Sitm.Count), Color.Red);
                            return;
                        }
                        else
                            sdoplay.Sitem = Sitm[0];

                        int Samt = 0;
                        if (!int.TryParse(datasplitS[1], out Samt))
                        {
                            tplayer.SendMessage("Could not parse Sell Amount", Color.IndianRed);
                            return;
                        }
                        sdoplay.Samt = Samt;

                        sdoplay.SignX = Main.sign[id].x;
                        sdoplay.SignY = Main.sign[id].y;

                        sdoplay.CheckForDrop = true;

                        tplayer.SendMessage("Transaction started, please drop the following onto the sign:", Color.RoyalBlue);
                        tplayer.SendMessage("Item: " + Sitm[0].name + " - Amount: " + Samt, Color.RoyalBlue);
                        tplayer.SendMessage("Hit the sign again to cancel transaction!", Color.RoyalBlue);

                        sdoplay.CooldownAShop = getConfig.AdminShopCooldown;
                        return;
                    }
                }
                catch (Exception)
                {
                    tplayer.SendMessage("Could not parse  Item / Amount - Correct Format: \"<Item Name>, <Amount>\"", Color.IndianRed);
                }
            }
            else if (sdoplay.toldperm == 5 && !tplayer.Group.HasPermission("useadminshop"))
            {
                tplayer.SendMessage("You do not have permission to use this sign shop!", Color.IndianRed);
                sdoplay.toldperm = 0;
            }
            else if (sdoplay.toldperm == 5 && tplayer.Group.HasPermission("useadminshop") && !tplayer.Group.HasPermission("nosccooldown") && sdoplay.CooldownAShop > 0)
            {
                tplayer.SendMessage("You have to wait another " + sdoplay.CooldownAShop + " seconds before using this shop", Color.IndianRed);
                sdoplay.toldperm = 0;
            }
            else if (sdoplay.toldperm != 5)
                sdoplay.toldperm++;
        }*/
        #endregion

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
        public int tolddestsign = 10;
        public int toldwarp = 5;
        public int toldperm = 5;
        public int CooldownTime = 0;
        public int CooldownHeal = 0;
        public int CooldownMsg = 0;
        public int CooldownDamage = 0;
        public int CooldownBoss = 0;
        public int CooldownItem = 0;
        public int CooldownBuff = 0;
        public int CooldownAShop = 0;
        public bool checkforsignedit = false;
        public bool DestroyingSign = false;
        public string originalsigntext = "";
        public int signeditid = 0;
        /*public bool CheckForDrop = false;
        public Item Bitem = new Item();
        public int Bamt = 0;
        public Item Sitem = new Item();
        public int Samt = 0;
        public int SignX = 0;
        public int SignY = 0;
        public int warntransaction = 7;*/

        /*public bool lastcheck = false;
        public Item lastBitem = new Item();
        public int lastBamt = 0;
        public Item lastSitem = new Item();
        public int lastSamt = 0;
        public int lastSignX = 0;
        public int lastSignY = 0;*/

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
        public string DefineAdminSignShop = "[Sign Shop]";
        public int TimeCooldown = 20;
        public int HealCooldown = 20;
        public int ShowMsgCooldown = 20;
        public int DamageCooldown = 20;
        public int BossCooldown = 20;
        public int ItemCooldown = 60;
        public int BuffCooldown = 20;
        public int AdminShopCooldown = 300;

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