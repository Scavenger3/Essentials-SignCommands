using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace SignCommands
{
    public class ScSign
    {
        public int cooldown;
        private int _cooldown;
        private string _cooldownGroup;
        private readonly List<string> _groups = new List<string>();
        private readonly List<string> _users = new List<string>();
        private readonly Dictionary<string, int> _bosses = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _mobs = new Dictionary<string, int>();
        public readonly Dictionary<List<string>, SignCommand> commands = new Dictionary<List<string>, SignCommand>(); 
        public bool freeAccess;
        private bool _confirm;
        private Point _point;

        public ScSign(string text, TSPlayer registrar, Point point)
        {
            _point = point;
            cooldown = 0;
            _cooldownGroup = string.Empty;
            RegisterCommands(text, registrar);
        }

        #region ParseCommands

        private IEnumerable<List<string>> ParseCommands(string text)
        {
            //Remove the Sign Command definer. It's not required
            text = text.Remove(0, SignCommands.config.DefineSignCommands.Length);
            
            //Replace the Sign Command command starter with the TShock one so that it gets handled properly later
            text = text.Replace(SignCommands.config.CommandsStartWith, TShock.Config.CommandSpecifier);

            //Remove whitespace
            text = text.Trim();

            //Create a local variable for our return value
            var ret = new List<List<string>>();

            //Split the text string at any TShock command character
            var cmdStrings = text.Split(Convert.ToChar(TShock.Config.CommandSpecifier));

            //Iterate through the strings
            foreach (var str in cmdStrings)
            {
                var sbList = new List<string>();
                var sb = new StringBuilder();
                var instr = false;
                for (var i = 0; i < str.Length; i++)
                {
                    var c = str[i];

                    if (c == '\\' && ++i < str.Length)
                    {
                        if (str[i] != '"' && str[i] != ' ' && str[i] != '\\')
                            sb.Append('\\');
                        sb.Append(str[i]);
                    }
                    else if (c == '"')
                    {
                        instr = !instr;
                        if (!instr)
                        {
                            sbList.Add(sb.ToString());
                            sb.Clear();
                        }
                        else if (sb.Length > 0)
                        {
                            sbList.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    else if (IsWhiteSpace(c) && !instr)
                    {
                        if (sb.Length > 0)
                        {
                            sbList.Add(sb.ToString());
                            sb.Clear();
                        }
                    }
                    else
                        sb.Append(c);
                }
                if (sb.Length > 0)
                    sbList.Add(sb.ToString());

                ret.Add(sbList);
            }
            return ret;
        }

        private static bool IsWhiteSpace(char c)
        {
            return c == ' ' || c == '\t' || c == '\n';
        }

        #endregion

        #region RegisterCommands

        private void RegisterCommands(string text, TSPlayer ply)
        {
            var cmdList = ParseCommands(text);

            foreach (var cmdArgs in cmdList)
            {
                var args = new List<string>(cmdArgs);
                if (args.Count < 1)
                    continue;

                var cmdName = args[0];

                if (cmdName == "no-perm")
                {
                    freeAccess = true;
                    continue;
                }

                if (cmdName == "confirm")
                {
                    _confirm = true;
                    continue;
                }

                if (cmdName == "cd" || cmdName == "cooldown")
                {
                    ParseSignCd(args);
                    continue;
                }



                if (cmdName == "allowg")
                {
                    ParseGroups(args);
                    continue;
                }

                if (cmdName == "allowu")
                {
                    ParseUsers(args);
                    continue;
                }

                if (cmdName == "spawnmob" || cmdName == "sm")
                {
                    ParseSpawnMob(args);
                    continue;
                }

                if (cmdName == "spawnboss" || cmdName == "sb")
                {
                    ParseSpawnBoss(args);
                    continue;
                }

                IEnumerable<Command> cmds = Commands.ChatCommands.Where(c => c.HasAlias(cmdName)).ToList();

                foreach (var cmd in cmds)
                {
                    if (!CheckPermissions(ply))
                        return;

                    var sCmd = new SignCommand(cooldown, cmd.Permissions, cmd.CommandDelegate, cmdName);
                    commands.Add(args, sCmd);
                }
            }
        }

        #endregion

        #region ExecuteCommands

        public void ExecuteCommands(ScPlayer sPly)
        {
            if (!freeAccess && !CheckPermissions(sPly.TsPlayer))
            {
                if (sPly.AlertPermissionCooldown == 0)
                {
                    sPly.TsPlayer.SendErrorMessage("You do not have access to the commands on this sign");
                    sPly.AlertPermissionCooldown = 3;
                }
                return;
            }

            if (cooldown > 0)
            {
                if (sPly.AlertCooldownCooldown == 0)
                    sPly.TsPlayer.SendErrorMessage("This sign is still cooling down. Please wait {0} more second{1}",
                        cooldown, cooldown.Suffix());

                return;
            }

            if (_confirm && sPly.confirmSign != this)
            {
                sPly.confirmSign = this;
                sPly.TsPlayer.SendWarningMessage("Are you sure you want to execute this sign command?");
                sPly.TsPlayer.SendWarningMessage("Hit the sign again to confirm.");
                cooldown = 2;
                return;
            }

            if (_groups.Count > 0 && !_groups.Contains(sPly.TsPlayer.Group.Name))
            {
                if (sPly.AlertPermissionCooldown == 0)
                {
                    sPly.TsPlayer.SendErrorMessage("Your group does not have access to this sign"); 
                    sPly.AlertPermissionCooldown = 3;
                }
                return;
            }

            if (_users.Count > 0 && !_users.Contains(sPly.TsPlayer.UserAccountName))
            {
                if (sPly.AlertPermissionCooldown == 0)
                {
                    sPly.TsPlayer.SendErrorMessage("You do not have access to this sign");
                    sPly.AlertPermissionCooldown = 3;
                }
                return;
            }

            if (_mobs.Count > 0)
            {
                if (!sPly.TsPlayer.Group.HasPermission(Permissions.spawnmob))
                {
                    if (sPly.AlertPermissionCooldown == 0)
                    {
                        sPly.TsPlayer.SendErrorMessage("You do not have access to the commands on this sign");
                        sPly.AlertPermissionCooldown = 3;
                    }
                    return;
                }

                SpawnMobs(_mobs, sPly);
            }

            if (_bosses.Count > 0)
            {
                if (!sPly.TsPlayer.Group.HasPermission(Permissions.spawnboss))
                {
                    if (sPly.AlertPermissionCooldown == 0)
                    {
                        sPly.TsPlayer.SendErrorMessage("You do not have access to the commands on this sign");
                        sPly.AlertPermissionCooldown = 3;
                    }
                    return;
                }
                SpawnBosses(_bosses, sPly);
            }

            foreach (var cmdPair in commands)
            {
                var args = new List<string>(cmdPair.Key);
                var cmd = cmdPair.Value;
                var cmdText = string.Join(" ", args);
                cmdText = cmdText.Replace("{player}", sPly.TsPlayer.Name);

                if (args.Any(s => s.Contains("{player}")))
                    args[args.IndexOf("{player}")] = sPly.TsPlayer.Name;

                if (cmd.DoLog)
                    TShock.Utils.SendLogs(
                        string.Format("{0} executed: {1}{2} [Via sign command].", sPly.TsPlayer.Name,
                            TShock.Config.CommandSpecifier,
                            cmdText), Color.PaleVioletRed, sPly.TsPlayer);

                args.RemoveAt(0);

                cmd.CommandDelegate.Invoke(new CommandArgs(cmdText, sPly.TsPlayer, args));
            }

            cooldown = _cooldown;
            sPly.AlertCooldownCooldown = 3;
            sPly.confirmSign = null;
        }

        #endregion
        
        private bool CheckPermissions(TSPlayer player)
        {
            return commands.Values.All(command => command.CanRun(player));
        }

        private void ParseSpawnMob(IEnumerable<string> args)
        {
            //>sm "blue slime":10 zombie:100
            foreach (var obj in args)
            {
                var mob = obj.Split(':')[0];
                var num = obj.Split(':')[1];

                int spawnCount;
                if (!int.TryParse(num, out spawnCount))
                    continue;

                _mobs.Add(mob, spawnCount);
            }
        }

        private void ParseSpawnBoss(IEnumerable<string> args)
        {
            foreach (var obj in args)
            {
                var boss = obj.Split(':')[0];
                var num = obj.Split(':')[1];

                int spawnCount;
                if (!int.TryParse(num, out spawnCount))
                    continue;

                _bosses.Add(boss, spawnCount);
            }
        }

        private void ParseSignCd(IList<string> args)
        {
            int cd;
            if (args.Count < 3)
            {
                //args[0] is command name
                if (!int.TryParse(args[1], out cd))
                {
                    if (SignCommands.config.CooldownGroups.ContainsKey(args[1]))
                    {
                        cd = SignCommands.config.CooldownGroups[args[1]];
                        _cooldownGroup = args[1];
                    }
                }
                _cooldown = cd;
            }
            else
            {
                //args[0] is command name. args[1] is cooldown specifier. args[2] is cooldown
                if (string.Equals(args[1], "global", StringComparison.CurrentCultureIgnoreCase))
                {
                    if (!int.TryParse(args[2], out cd))
                    {
                        if (SignCommands.config.CooldownGroups.ContainsKey(args[2]))
                        {
                            cd = SignCommands.config.CooldownGroups[args[2]];
                            _cooldownGroup = args[2];
                        }
                    }
                    _cooldown = cd;
                }
            }
        }

        private void ParseGroups(IEnumerable<string> args)
        {
            var groups = new List<string>(args);
            //Remove the command name- it's not a group
            groups.RemoveAt(0);

            foreach (var group in groups)
                _groups.Add(group);
        }

        private void ParseUsers(IEnumerable<string> args)
        {
            var users = new List<string>(args);
            //Remove the command name- it's not a user
            users.RemoveAt(0);

            foreach (var user in users)
                _users.Add(user);
        }

        private void SpawnMobs(Dictionary<string, int> mobs, ScPlayer sPly)
        {
            var mobList = new List<string>();
            foreach (var pair in mobs)
            {
                var amount = Math.Min(pair.Value, Main.maxNPCs);

                var npcs = TShock.Utils.GetNPCByIdOrName(pair.Key);
                if (npcs.Count == 0)
                    continue;

                if (npcs.Count > 1)
                    continue;

                var npc = npcs[0];
                if (npc.type >= 1 && npc.type < Main.maxNPCTypes && npc.type != 113)
                {
                    TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, _point.X, _point.Y, 50,
                        20);
                    mobList.Add(npc.name + " (" + amount + ")");
                }
                else if (npc.type == 113)
                {
                    if (Main.wof >= 0 || (_point.Y/16f < (Main.maxTilesY - 205)))
                        continue;
                    NPC.SpawnWOF(new Vector2(_point.X, _point.Y));
                    mobList.Add("the Wall of Flesh (" + amount + ")");
                }
            }

            TSPlayer.All.SendSuccessMessage("{0} has spawned {1}", sPly.TsPlayer.Name, string.Join(", ", mobList)); 
            
            TShock.Utils.SendLogs(
                         string.Format("{0} executed: {1}{2} [Via sign command].", sPly.TsPlayer.Name,
                             TShock.Config.CommandSpecifier,
                             "/spawnmob " + string.Join(", ", mobList)), Color.PaleVioletRed, sPly.TsPlayer);
        }

        private void SpawnBosses(Dictionary<string, int> bosses, ScPlayer sPly)
        {
            var bossList = new List<string>();
            foreach (var pair in bosses)
            {
                var npc = new NPC();
                switch (pair.Key.ToLower())
                {
                    case "*":
                    case "all":
                        int[] npcIds = {4, 13, 35, 50, 125, 126, 127, 134, 222, 245, 262, 266, 370};
                        TSPlayer.Server.SetTime(false, 0.0);
                        foreach (var i in npcIds)
                        {
                            npc.SetDefaults(i);
                            TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        }

                        bossList.Add("all bosses (" + pair.Value + ")");
                        return;
                    case "brain":
                    case "brain of cthulhu":
                        npc.SetDefaults(266);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "destroyer":
                        npc.SetDefaults(134);
                        TSPlayer.Server.SetTime(false, 0.0);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "duke":
                    case "duke fishron":
                    case "fishron":
                        npc.SetDefaults(370);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "eater":
                    case "eater of worlds":
                        npc.SetDefaults(13);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "eye":
                    case "eye of cthulhu":
                        npc.SetDefaults(4);
                        TSPlayer.Server.SetTime(false, 0.0);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "golem":
                        npc.SetDefaults(245);
                        TSPlayer.Server.SetTime(false, 0.0);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "king":
                    case "king slime":
                        npc.SetDefaults(50);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "plantera":
                        npc.SetDefaults(262);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "prime":
                    case "skeletron prime":
                        npc.SetDefaults(127);
                        TSPlayer.Server.SetTime(false, 0.0);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "queen":
                    case "queen bee":
                        npc.SetDefaults(222);
                        TSPlayer.Server.SetTime(false, 0.0);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "skeletron":
                        npc.SetDefaults(35);
                        TSPlayer.Server.SetTime(false, 0.0);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add(npc.name + " (" + pair.Value + ")");
                        return;
                    case "twins":
                        TSPlayer.Server.SetTime(false, 0.0);
                        npc.SetDefaults(125);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        npc.SetDefaults(126);
                        TSPlayer.Server.SpawnNPC(npc.type, npc.name, pair.Value, _point.X, _point.Y);
                        bossList.Add("the Twins " + " (" + pair.Value + ")");
                        return;
                    case "wof":
                    case "wall of flesh":
                        if (Main.wof >= 0)
                            return;
                        if (_point.Y/16f < Main.maxTilesY - 205)
                            return;
                        NPC.SpawnWOF(new Vector2(_point.X, _point.Y));
                        bossList.Add("the Wall of Flesh " + " (" + pair.Value + ")");
                        return;
                    default:
                        return;
                }
            }

            TSPlayer.All.SendSuccessMessage("{0} has spawned {1}", sPly.TsPlayer.Name, string.Join(", ", bossList)); 
            TShock.Utils.SendLogs(
                          string.Format("{0} executed: {1}{2} [Via sign command].", sPly.TsPlayer.Name,
                              TShock.Config.CommandSpecifier,
                              "/spawnboss " + string.Join(", ", bossList)), Color.PaleVioletRed, sPly.TsPlayer);
        }
    }
}