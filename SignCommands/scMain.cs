using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Timers;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace SignCommands
{
    //TODO: Add infinite signs support & Global cooldowns
    [ApiVersion(1, 16)]
    public class SignCommands : TerrariaPlugin
    {
        public override string Name
        {
            get { return "Sign Commands"; }
        }

        public override string Author
        {
            get { return "Scavenger"; }
        }

        public override string Description
        {
            get { return "Put commands on signs!"; }
        }

        public override Version Version
        {
            get { return new Version(1, 6, 0); }
        }

        public static ScConfig config = new ScConfig();
        private static readonly ScPlayer[] ScPlayers = new ScPlayer[256];
        
        public static readonly List<Cooldown> Cooldowns = new List<Cooldown>(); 

        private static readonly Dictionary<Point, ScSign> ScSigns = new Dictionary<Point, ScSign>(); 

        private static bool UsingInfiniteSigns { get; set; }
        //private DateTime _lastPurge = DateTime.UtcNow;

        private readonly Timer _updateTimer = new Timer {Enabled = true, Interval = 1000d};

        public SignCommands(Main game)
            : base(game)
        {
            UsingInfiniteSigns = File.Exists(Path.Combine("ServerPlugins", "InfiniteSigns.dll"));
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            if (!UsingInfiniteSigns)
                ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                if (!UsingInfiniteSigns)
                    ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.ServerJoin.Deregister(this, OnJoin);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
            }
            base.Dispose(disposing);
        }

        private void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("essentials.signs.break", CmdDestroySign, "destsign"));
            Commands.ChatCommands.Add(new Command("essentials.signs.reload", CmdReloadSigns, "screload"));

            var savePath = Path.Combine(TShock.SavePath, "Essentials");
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            var configPath = Path.Combine(savePath, "scConfig.json");
            (config = ScConfig.Read(configPath)).Write(configPath);

            _updateTimer.Elapsed += UpdateTimerOnElapsed;
        }


        #region Commands

        private void CmdDestroySign(CommandArgs args)
        {
            var sPly = ScPlayers[args.Player.Index];

            sPly.DestroyMode = true;
            args.Player.SendSuccessMessage("You can now destroy a sign.");
        }

        private void CmdReloadSigns(CommandArgs args)
        {
            var savePath = Path.Combine(TShock.SavePath, "Essentials");
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            var configPath = Path.Combine(savePath, "esConfig.json");
            (config = ScConfig.Read(configPath)).Write(configPath);

            args.Player.SendSuccessMessage("Config reloaded");
        }

        #endregion

        #region scPlayers

        private static void OnJoin(JoinEventArgs args)
        {
            ScPlayers[args.Who] = new ScPlayer(args.Who);

            //if (OfflineCooldowns.ContainsKey(TShock.Players[args.Who].Name))
            //{
            //ScPlayers[args.Who].Cooldowns = OfflineCooldowns[TShock.Players[args.Who].Name];
            //OfflineCooldowns.Remove(TShock.Players[args.Who].Name);
            //}
        }

        private static void OnLeave(LeaveEventArgs args)
        {
            //if (ScPlayers[args.Who] != null && ScPlayers[args.Who].Cooldowns.Count > 0)
            //    OfflineCooldowns.Add(TShock.Players[args.Who].Name, ScPlayers[args.Who].Cooldowns);
            ScPlayers[args.Who] = null;
        }

        #endregion

        #region Timer

        private void UpdateTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            foreach (var sPly in ScPlayers.Where(sPly => sPly != null))
            {
                if (sPly.AlertCooldownCooldown > 0)
                    sPly.AlertCooldownCooldown--;
                if (sPly.AlertPermissionCooldown > 0)
                    sPly.AlertPermissionCooldown--;
                if (sPly.AlertDestroyCooldown > 0)
                    sPly.AlertDestroyCooldown--;
            }
            
            foreach (var signPair in ScSigns)
            {
                var sign = signPair.Value;
                if (sign.cooldown > 0)
                    sign.cooldown--;
            }
            //if ((DateTime.UtcNow - _lastPurge).TotalMinutes >= 5)
            //{
            //    _lastPurge = DateTime.UtcNow;

            //    var cooldownGroups = new List<string>(GlobalCooldowns.Keys);
            //    foreach (var g in cooldownGroups.Where(g => DateTime.UtcNow > GlobalCooldowns[g]))
            //        GlobalCooldowns.Remove(g);

            //    var offlinePlayers = new List<string>(OfflineCooldowns.Keys);
            //    foreach (var p in offlinePlayers)
            //    {
            //        var offlinePlayerCooldowns = new List<string>(OfflineCooldowns[p].Keys);
            //        var localp = p;
            //        foreach (var g in offlinePlayerCooldowns.Where(g => DateTime.UtcNow > OfflineCooldowns[localp][g]))
            //            OfflineCooldowns[p].Remove(g);

            //        if (OfflineCooldowns[p].Count == 0)
            //            OfflineCooldowns.Remove(p);
            //    }

            //    foreach (var sPly in ScPlayers)
            //    {
            //        if (sPly == null) continue;
            //        var cooldownIds = new List<string>(sPly.Cooldowns.Keys);

            //        var ply = sPly;
            //        foreach (var id in cooldownIds.Where(id => DateTime.UtcNow > ply.Cooldowns[id]))
            //            sPly.Cooldowns.Remove(id);
            //    }
            //}
        }

        #endregion

        #region OnSignNew

        private static bool OnSignNew(int x, int y, string text, int who)
        {
            if (!text.ToLower().StartsWith(config.DefineSignCommands.ToLower())) return false;

            var tPly = TShock.Players[who];
            var point = new Point(x, y);
            var sign = new ScSign(text, tPly, point);
            if (tPly == null)
                return false;

            if (sign.noEdit && !tPly.Group.HasPermission("essentials.signs.openall"))
                return true;

            if (ScUtils.CanCreate(tPly, sign))
            {
                ScSigns.AddItem(point, sign);
                return false;
            }

            tPly.SendErrorMessage("You do not have permission to create that sign command.");
            return true;
        }

        #endregion

        #region OnSignHit

        private static bool OnSignHit(int x, int y, string text, int who)
        {
            if (!text.ToLower().StartsWith(config.DefineSignCommands.ToLower())) return false;
            var tPly = TShock.Players[who];
            var sPly = ScPlayers[who];
            var sign = ScSigns.Check(x, y, text, tPly);

            if (tPly == null || sPly == null)
                return false;

            var canBreak = ScUtils.CanBreak(tPly, sign);
            if (sPly.DestroyMode && canBreak) return false;

            if (config.ShowDestroyMessage && canBreak && sPly.AlertDestroyCooldown == 0)
            {
                tPly.SendInfoMessage("To destroy this sign, Type \"/destsign\".");
                sPly.AlertDestroyCooldown = 10;
            }
            
            sign.ExecuteCommands(sPly);

            return true;
        }

        #endregion

        #region OnSignKill

        private static bool OnSignKill(int x, int y, string text, int who)
        {
            if (!text.ToLower().StartsWith(config.DefineSignCommands.ToLower())) return false;

            var sPly = ScPlayers[who];
            var sign = ScSigns.Check(x, y, text, sPly.TsPlayer);

            if (sPly == null)
                return false;

            if (sPly.DestroyMode && ScUtils.CanBreak(sPly.TsPlayer, sign))
            {
                sPly.DestroyMode = false;
                //Cooldown removal
                return false;
            }
            sign.ExecuteCommands(sPly);
            return true;
        }

        #endregion

        #region OnSignOpen

        private static bool OnSignOpen(int x, int y, string text, int who)
        {
            if (!text.ToLower().StartsWith(config.DefineSignCommands.ToLower())) return false;

            var tPly = TShock.Players[who];
            var sign = ScSigns.Check(x, y, text, tPly);

            if (tPly.Group.HasPermission("essentials.signs.openall"))
                return false;

            if (sign.noRead)
                return true;

            if (!sign.freeAccess)
                return true;

            return false;
        }

        #endregion

        #region OnGetData

        private static void OnGetData(GetDataEventArgs e)
        {
            if (e.Handled)
                return;

            using (var reader = new BinaryReader(new MemoryStream(e.Msg.readBuffer, e.Index, e.Length)))
                switch (e.MsgID)
                {
                        #region Sign Edit

                    case PacketTypes.SignNew:
                    {
                        reader.ReadInt16();
                        var x = reader.ReadInt16();
                        var y = reader.ReadInt16();
                        var text = reader.ReadString();
                        var id = Sign.ReadSign(x, y);
                        if (id < 0 || Main.sign[id] == null) return;
                        x = (short) Main.sign[id].x;
                        y = (short) Main.sign[id].y;
                        if (OnSignNew(x, y, text, e.Msg.whoAmI))
                        {
                            e.Handled = true;
                            TShock.Players[e.Msg.whoAmI].SendData(PacketTypes.SignNew, "", id);
                            TShock.Players[e.Msg.whoAmI].SendErrorMessage("This sign is protected from modifications");
                        }
                    }
                        break;

                        #endregion

                        #region Sign Read

                    case PacketTypes.SignRead:
                    {
                        var x = reader.ReadInt16();
                        var y = reader.ReadInt16();
                        var id = Sign.ReadSign(x, y);
                        if (id < 0 || Main.sign[id] == null) return;
                        x = (short) Main.sign[id].x;
                        y = (short) Main.sign[id].y;
                        var text = Main.sign[id].text;
                        if (OnSignOpen(x, y, text, e.Msg.whoAmI))
                        {
                            e.Handled = true;
                            TShock.Players[e.Msg.whoAmI].SendErrorMessage("This sign is protected from viewing");
                        }
                    }
                        break;

                        #endregion

                        #region Tile Modify

                    case PacketTypes.Tile:
                    {
                        var action = reader.ReadByte();
                        var x = reader.ReadInt16();
                        var y = reader.ReadInt16();
                        var type = reader.ReadUInt16();

                        if (Main.tile[x, y].type != 55) return;

                        var id = Sign.ReadSign(x, y);
                        if (id < 0 || Main.sign[id] == null) return;
                        x = (short) Main.sign[id].x;
                        y = (short) Main.sign[id].y;
                        var text = Main.sign[id].text;

                        bool handle;
                        if (action == 0 && type == 0)
                            handle = OnSignKill(x, y, text, e.Msg.whoAmI);
                        else
                            handle = OnSignHit(x, y, text, e.Msg.whoAmI);

                        if (handle)
                        {
                            e.Handled = true;
                            TShock.Players[e.Msg.whoAmI].SendTileSquare(x, y);
                        }
                    }
                        break;

                        #endregion
                }
        }

        #endregion
    }

    public class Cooldown
    {
        public int time;
        public ScSign sign;
        public string name;
        public string group;

        public Cooldown(int time, ScSign sign, string name, string group = null)
        {
            this.time = time;
            this.sign = sign;
            this.name = name;
            this.group = group;
        }
    }
}