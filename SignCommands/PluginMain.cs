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
        public DateTime MBReset = DateTime.UtcNow;

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
                Console.WriteLine("Error in config file");
                Console.ForegroundColor = ConsoleColor.Gray;
                Log.Error("Config Exception");
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
            double tick1 = (DateTime.UtcNow - MBReset).TotalMilliseconds;
            if (tick1 > (getConfig.MaxBossesResetDuration * 1000))
            {
                foreach (scPlayer play in scPlayers)
                {
                    play.bossspawns = 0;
                }
            }
        }

        #region destsign Command
        public void OnInitialize()
        {
            Commands.ChatCommands.Add(new Command("destroysigncommand", destsign, "destsign"));
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
                if (scply.toldperm == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    scply.toldperm = 0;
                }
                else
                    scply.toldperm++;
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
                }
            }
            else
            {
                if (scply.toldperm == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    scply.toldperm = 0;
                }
                else
                    scply.toldperm++;
            }

            if (tplayer.Group.HasPermission("usesignmessage"))
            {
                if (Main.sign[id].text.ToLower().Contains("show motd"))
                {
                    if (scply.toldmsgmotd == 5)
                    {
                        TShock.Utils.ShowFileToUser(tplayer, "motd.txt");
                        scply.toldmsgmotd = 0;
                    }
                    else
                        scply.toldmsgmotd++;
                }
                else if (Main.sign[id].text.ToLower().Contains("show rules"))
                {
                    if (scply.toldmsgrules == 5)
                    {
                        TShock.Utils.ShowFileToUser(tplayer, "rules.txt");
                        scply.toldmsgrules = 0;
                    }
                    else
                        scply.toldmsgrules++;
                }
                else if (Main.sign[id].text.ToLower().Contains("show playing"))
                {
                    if (scply.toldmsgplaying == 5)
                    {
                        tplayer.SendMessage(string.Format("Current players: {0}.", TShock.Utils.GetPlayers()), 255, 240, 20);
                        scply.toldmsgplaying = 0;
                    }
                    else
                        scply.toldmsgplaying++;
                }
                else if (Main.sign[id].text.ToLower().Contains("show message") && File.Exists("scMeassge.txt"))
                    TShock.Utils.ShowFileToUser(tplayer, "scMessage.txt");
                else if (Main.sign[id].text.ToLower().Contains("show message") && !File.Exists("scMeassge.txt"))
                    tplayer.SendMessage("Could not find message", Color.IndianRed);
            }
            else
            {
                if (scply.toldperm == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    scply.toldperm = 0;
                }
                else
                    scply.toldperm++;
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
                if (scply.toldperm == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    scply.toldperm = 0;
                }
                else
                    scply.toldperm++;
            }

            if (tplayer.Group.HasPermission("usesignspawnboss"))
            {
                if (Main.sign[id].text.ToLower().Contains("boss eater") && (scply.bossspawns <= getConfig.MaxBosses || scply.TSPlayer.Group.HasPermission("unlimitedspawnboss")))
                {
                    NPC npc = TShock.Utils.GetNPCById(13);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scply.bossspawns++;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss eye") && (scply.bossspawns <= getConfig.MaxBosses || scply.TSPlayer.Group.HasPermission("unlimitedspawnboss")))
                {
                    NPC npc = TShock.Utils.GetNPCById(4);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scply.bossspawns++;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss king") && (scply.bossspawns <= getConfig.MaxBosses || scply.TSPlayer.Group.HasPermission("unlimitedspawnboss")))
                {
                    NPC npc = TShock.Utils.GetNPCById(50);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scply.bossspawns++;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss skeletron") && (scply.bossspawns <= getConfig.MaxBosses || scply.TSPlayer.Group.HasPermission("unlimitedspawnboss")) && !Main.sign[id].text.ToLower().Contains("boss skeletronprime"))
                {
                    NPC npc = TShock.Utils.GetNPCById(35);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scply.bossspawns++;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss wof") && (scply.bossspawns <= getConfig.MaxBosses || scply.TSPlayer.Group.HasPermission("unlimitedspawnboss")))
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
                    scply.bossspawns++;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss twins") && (scply.bossspawns <= getConfig.MaxBosses || scply.TSPlayer.Group.HasPermission("unlimitedspawnboss")))
                {
                    NPC retinazer = TShock.Utils.GetNPCById(125);
                    NPC spaz = TShock.Utils.GetNPCById(126);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, 1, tplayer.TileX, tplayer.TileY);
                    TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, 1, tplayer.TileX, tplayer.TileY);
                    scply.bossspawns++;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss destroyer") && (scply.bossspawns <= getConfig.MaxBosses || scply.TSPlayer.Group.HasPermission("unlimitedspawnboss")))
                {
                    NPC npc = TShock.Utils.GetNPCById(134);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scply.bossspawns++;
                }
                else if (Main.sign[id].text.ToLower().Contains("boss skeletronprime") && (scply.bossspawns <= getConfig.MaxBosses || scply.TSPlayer.Group.HasPermission("unlimitedspawnboss")))
                {
                    NPC npc = TShock.Utils.GetNPCById(127);
                    TSPlayer.Server.SetTime(false, 0.0);
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
                    scply.bossspawns++;
                }
            }
            else
            {
                if (scply.toldperm == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    scply.toldperm = 0;
                }
                else
                    scply.toldperm++;
            }

            if (tplayer.Group.HasPermission("usesignwarp"))
            {
                string[] linesplit = Main.sign[id].text.Split('\'', '\"');
                if (Main.sign[id].text.ToLower().Contains("warp"))
                {
                    var warpxy = TShock.Warps.FindWarp(linesplit[1]);

                    if (warpxy.WarpName == "" || warpxy.WarpPos.X == 0 || warpxy.WarpPos.Y == 0 || warpxy.WorldWarpID == "")
                    {
                        if (scply.toldwarp == 15)
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
            }
            else
            {
                if (scply.toldperm == 15)
                {
                    tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    scply.toldperm = 0;
                }
                else
                    scply.toldperm++;
            }

            #region sign shop test stuff
            //if (tplayer.Group.HasPermission("usesignshop"))
            //{
                //string[] shopsplit = Main.sign[id].text.Split('\'', '\"');
                //if (Main.sign[id].text.ToLower().Contains("shop"))
                //{
                    //string[] shopbuy = shopsplit[1].Split(' ');

                    //int buya = 0;
                    //bool buyanum = int.TryParse(shopbuy[0], out buya);
                    //if (!buyanum)
                        //buya = 1;

                    //string buyis = "";
                    //foreach (string itmpart in shopbuy)
                    //{
                        //buyis = buyis + itmpart + " ";
                    //}
                    
                    
                    //[Sign Shop]
                    //Buy: "25 Glass"
                    //For: "15 Shards"
                    
                //}
            //}
            //else
            //{
                //if (timestold == 15)
                //{
                    //tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
                    //timestold = 0;
                //}
                //else
                    //timestold++;
            //}
            #endregion sign shop test stuff
        }
        #endregion
    }

    public class scPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public bool destsign = false;
        public int tolddestsign = 10;
        public int toldwarp = 15;
        public int toldperm = 15;
        public int toldmsgmotd = 5;
        public int toldmsgrules = 5;
        public int toldmsgplaying = 5;
        public int bossspawns = 0;

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
        public int MaxBosses = 3;
        public int MaxBossesResetDuration = 30;

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