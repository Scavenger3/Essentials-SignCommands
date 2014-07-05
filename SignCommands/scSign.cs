using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace SignCommands
{
    public class ScSign
    {
        public int cooldown;
        private int _cooldown;
        private string _cooldownGroup;
        public readonly Dictionary<List<string>, SignCommand> commands = new Dictionary<List<string>, SignCommand>(); 
        public bool freeAccess;
        private bool _confirm;

        public ScSign(string text, TSPlayer registrar)
        {
            cooldown = 0;
            _cooldownGroup = string.Empty;
            RegisterCommands(text, registrar);
        }

        #region ParseCommands

        private IEnumerable<List<string>> ParseCommands(string text)
        {
            //Remove the Sign Command definer. It's not required
            text = text.Remove(0, SignCommands.config.DefineSignCommands.Length);

            if (text.Contains("-no perm"))
            {
                freeAccess = true;
                text = text.Replace("-no perm", string.Empty);
            }
            if (text.Contains("-confirm"))
            {
                _confirm = true;
                text = text.Replace("-confirm", string.Empty);
            }

            if (text.Contains("-cd "))
            {
                var pos = text.IndexOf("-cd ", StringComparison.Ordinal) + 4;
                var cdStr = text.Substring(pos, text.Length - pos).Trim();
                text = text.Remove(pos - 4, text.Length - (pos - 4));
                int cd;
                if (!int.TryParse(cdStr, out cd))
                {
                    if (SignCommands.config.CooldownGroups.ContainsKey(cdStr))
                    {
                        cd = SignCommands.config.CooldownGroups[cdStr];
                        _cooldownGroup = cdStr;
                    }
                }
                _cooldown = cd;
            }

            text = text.Replace(SignCommands.config.CommandsStartWith, TShock.Config.CommandSpecifier);

            //Remove whitespace
            text = text.Trim();

            var ret = new List<List<string>>();

            //Remove the CommandStartsWith operator
            var cmdStrings = text.Split(Convert.ToChar(TShock.Config.CommandSpecifier));

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
            if (!freeAccess && !CheckPermissions(sPly.TsPlayer) && sPly.AlertPermissionCooldown == 0)
            {
                sPly.TsPlayer.SendErrorMessage("You do not have access to the commands on this sign");
                sPly.AlertPermissionCooldown = 3;
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
    }
}