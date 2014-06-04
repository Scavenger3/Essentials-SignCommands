using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace SignCommands
{
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
        private static readonly scPlayer[] ScPlayers = new scPlayer[256];
        public static readonly Dictionary<string, DateTime> GlobalCooldowns = new Dictionary<string, DateTime>();

        private static readonly Dictionary<string, Dictionary<string, DateTime>> OfflineCooldowns =
            new Dictionary<string, Dictionary<string, DateTime>>();

        private static bool UsingInfiniteSigns { get; set; }
        public static bool UsingSEConomy { get; private set; }
        private DateTime _lastCooldown = DateTime.UtcNow;
        private DateTime _lastPurge = DateTime.UtcNow;

        public SignCommands(Main game)
            : base(game)
        {
            UsingInfiniteSigns = File.Exists(Path.Combine("ServerPlugins", "InfiniteSigns.dll"));
            UsingSEConomy = File.Exists(Path.Combine("ServerPlugins", "Wolfje.Plugins.SEconomy.dll"));
        }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            if (!UsingInfiniteSigns)
            {
                ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            }
            ServerApi.Hooks.ServerJoin.Register(this, OnJoin);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
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
                ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
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
            var configPath = Path.Combine(savePath, "esConfig.json");
            (config = ScConfig.Read(configPath)).Write(configPath);
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
            //Reload config
        }

        #endregion

        #region scPlayers

        private static void OnJoin(JoinEventArgs args)
        {
            ScPlayers[args.Who] = new scPlayer(args.Who);

            if (OfflineCooldowns.ContainsKey(TShock.Players[args.Who].Name))
            {
                ScPlayers[args.Who].Cooldowns = OfflineCooldowns[TShock.Players[args.Who].Name];
                OfflineCooldowns.Remove(TShock.Players[args.Who].Name);
            }
        }

        private static void OnLeave(LeaveEventArgs args)
        {
            if (ScPlayers[args.Who] != null && ScPlayers[args.Who].Cooldowns.Count > 0)
                OfflineCooldowns.Add(TShock.Players[args.Who].Name, ScPlayers[args.Who].Cooldowns);
            ScPlayers[args.Who] = null;
        }

        #endregion

        #region Timer

        private void OnUpdate(EventArgs args)
        {
            if ((DateTime.UtcNow - _lastCooldown).TotalMilliseconds >= 1000)
            {
                _lastCooldown = DateTime.UtcNow;
                foreach (var sPly in ScPlayers.Where(sPly => sPly != null))
                {
                    if (sPly.AlertCooldownCooldown > 0)
                        sPly.AlertCooldownCooldown--;
                    if (sPly.AlertPermissionCooldown > 0)
                        sPly.AlertPermissionCooldown--;
                    if (sPly.AlertDestroyCooldown > 0)
                        sPly.AlertDestroyCooldown--;
                }

                if ((DateTime.UtcNow - _lastPurge).TotalMinutes >= 5)
                {
                    _lastPurge = DateTime.UtcNow;

                    var cooldownGroups = new List<string>(GlobalCooldowns.Keys);
                    foreach (var g in cooldownGroups.Where(g => DateTime.UtcNow > GlobalCooldowns[g]))
                    {
                        GlobalCooldowns.Remove(g);
                    }

                    var offlinePlayers = new List<string>(OfflineCooldowns.Keys);
                    foreach (var p in offlinePlayers)
                    {
                        var offlinePlayerCooldowns = new List<string>(OfflineCooldowns[p].Keys);
                        foreach (var g in offlinePlayerCooldowns)
                        {
                            if (DateTime.UtcNow > OfflineCooldowns[p][g])
                                OfflineCooldowns[p].Remove(g);
                        }
                        if (OfflineCooldowns[p].Count == 0)
                            OfflineCooldowns.Remove(p);
                    }

                    foreach (var sPly in ScPlayers)
                    {
                        if (sPly == null) continue;
                        var cooldownIds = new List<string>(sPly.Cooldowns.Keys);
                        foreach (var id in cooldownIds)
                        {
                            if (DateTime.UtcNow > sPly.Cooldowns[id])
                                sPly.Cooldowns.Remove(id);
                        }
                    }
                }
            }
        }

        #endregion

        #region OnSignEdit

        private static bool OnSignEdit(int x, int y, string text, int who)
        {
            if (!text.ToLower().StartsWith(config.DefineSignCommands.ToLower())) return false;

            var tPly = TShock.Players[who];
            var sign = new ScSign(text, new Point(x, y));
            if (tPly == null)
                return false;

            if (scUtils.CanCreate(tPly, sign)) return false;

            tPly.SendErrorMessage("You do not have permission to create that sign command.");
            return true;
        }

        #endregion

        #region OnSignHit

        private static bool OnSignHit(int X, int Y, string text, int who)
        {
            if (!text.ToLower().StartsWith(config.DefineSignCommands.ToLower())) return false;
            var tPly = TShock.Players[who];
            var sPly = ScPlayers[who];
            var sign = new ScSign(text, new Point(X, Y));

            if (tPly == null || sPly == null)
                return false;

            var canBreak = scUtils.CanBreak(tPly, sign);
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
            var sign = new ScSign(text, new Point(x, y));

            if (sPly == null)
                return false;

            if (sPly.DestroyMode && scUtils.CanBreak(sPly.TSPlayer, sign))
            {
                sPly.DestroyMode = false;
                var id = new Point(x, y).ToString();
                var offlinePlayers = new List<string>(OfflineCooldowns.Keys);
                foreach (var p in offlinePlayers.Where(p => OfflineCooldowns[p].ContainsKey(id)))
                    OfflineCooldowns[p].Remove(id);

                foreach (var ply in ScPlayers.Where(ply => ply != null && ply.Cooldowns.ContainsKey(id)))
                    ply.Cooldowns.Remove(id);
                return false;
            }
            sign.ExecuteCommands(sPly);
            return true;
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
                        int x = reader.ReadInt16();
                        int y = reader.ReadInt16();
                        var text = reader.ReadString();
                        var id = Sign.ReadSign(x, y);
                        if (id < 0 || Main.sign[id] == null) return;
                        x = Main.sign[id].x;
                        y = Main.sign[id].y;
                        if (OnSignEdit(x, y, text, e.Msg.whoAmI))
                        {
                            e.Handled = true;
                            TShock.Players[e.Msg.whoAmI].SendData(PacketTypes.SignNew, "", id);
                        }
                    }
                        break;

                        #endregion

                    #region Tile Modify

                    case PacketTypes.Tile:
                    {
                        var action = reader.ReadByte();
                        int x = reader.ReadInt16();
                        int y = reader.ReadInt16();
                        var type = reader.ReadUInt16();

                        if (Main.tile[x, y].type != 55) return;

                        var id = Sign.ReadSign((Int32) x, (Int32) y);
                        if (id < 0 || Main.sign[id] == null) return;
                        x = Main.sign[id].x;
                        y = Main.sign[id].y;
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
}