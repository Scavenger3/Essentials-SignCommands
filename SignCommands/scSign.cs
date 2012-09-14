using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace SignCommands
{
	public class scSign
	{
		public int ID { get; set; }
		public int MyCooldown { get; set; }
		public string CooldownGroup { get; set; }
		public bool GlobalCooldown { get; set; }
		public List<scCommand> Commands { get; set; }

		public scSign(string text, int id)
		{
			this.ID = id;
			this.Commands = new List<scCommand>();

			ParseCommands(text);
		}

		#region ParseCommands
		public void ParseCommands(string text)
		{
			char LF = Encoding.UTF8.GetString(new byte[] { 10 })[0];
			text = string.Join(" ", text.Split(LF));

			char split = '>';
			if (SignCommands.getConfig.CommandsStartWith.Length == 1)
				split = SignCommands.getConfig.CommandsStartWith[0];

			List<string> commands = new List<string> { text };
			if (text.Contains(split))
				commands = text.Split(split).ToList();

			/* Define Sign Command string should be at [0], we dont need it. */
			if (commands.Count > 0)
				commands.RemoveAt(0);

			foreach (string cmd in commands)
			{
				List<string> args = new List<string> { cmd };
				if (cmd.Contains(' '))
					args = cmd.Split(' ').ToList();

				string main = args[0];
				args.RemoveAt(0);

				/* Parse Parameters */
				args = scUtils.ParseParameters(string.Join(" ", args));

				this.Commands.Add(new scCommand(this, main, args));
			}

			/* Parse Cooldown */
			List<scCommand> Commands = this.Commands;
			foreach (scCommand cmd in Commands)
			{
				if (cmd.command == "cooldown")
				{
					if (cmd.args.Count < 1) continue;
					int seconds = 0;
					string group = string.Empty;
					if (!int.TryParse(cmd.args[0], out seconds))
					{
						group = cmd.args[0].ToLower();
						if (SignCommands.getConfig.CooldownGroups.ContainsKey(group))
							seconds = SignCommands.getConfig.CooldownGroups[group];
						else
							group = string.Empty;
					}
					this.MyCooldown = seconds;
					this.CooldownGroup = group;
					this.GlobalCooldown = group.StartsWith("global");
					this.Commands.Remove(cmd);
					break;
				}
				else if (!cmd.AmIValid())
				{
					this.Commands.Remove(cmd);
				}
			}
		}
		#endregion

		#region ExecuteCommands
		public void ExecuteCommands(scPlayer sPly)
		{
			#region Check Cooldowns
			if (!sPly.TSPlayer.Group.HasPermission("essentials.signs.nocooldown"))
			{
				if (this.GlobalCooldown)
				{
					lock (SignCommands.GlobalCooldowns)
					{
						if (!SignCommands.GlobalCooldowns.ContainsKey(this.CooldownGroup))
							SignCommands.GlobalCooldowns.Add(this.CooldownGroup, this.MyCooldown);
						else
						{
							if (SignCommands.GlobalCooldowns[this.CooldownGroup] > 0)
							{
								if (sPly.AlertCooldownCooldown == 0)
								{
									sPly.TSPlayer.SendMessage(string.Format("Everyone must wait another {0} seconds before using this sign!", SignCommands.GlobalCooldowns[this.CooldownGroup]), Color.OrangeRed);
									sPly.AlertCooldownCooldown = 3;
								}
								return;
							}
							else
								SignCommands.GlobalCooldowns[this.CooldownGroup] = this.MyCooldown;
						}
					}
				}
				else
				{
					lock (sPly.Cooldowns)
					{
						if (!sPly.Cooldowns.ContainsKey(this.ID))
							sPly.Cooldowns.Add(this.ID, this.MyCooldown);
						else
						{
							if (sPly.Cooldowns[this.ID] > 0)
							{
								if (sPly.AlertCooldownCooldown == 0)
								{
									sPly.TSPlayer.SendMessage(string.Format("You must wait another {0} seconds before using this sign!", sPly.Cooldowns[this.ID]), Color.OrangeRed);
									sPly.AlertCooldownCooldown = 3;
								}
								return;
							}
							else
							{
								sPly.Cooldowns[this.ID] = this.MyCooldown;
							}
						}
					}
				}
			}
			#endregion

			int DoesntHavePermission = 0;
			foreach (scCommand cmd in this.Commands)
			{
				if (!sPly.TSPlayer.Group.HasPermission(string.Format("essentials.signs.use.{0}", cmd.command)))
				{
					DoesntHavePermission++;
					continue;
				}

				cmd.ExecuteCommand(sPly);
			}

			if (DoesntHavePermission != 0 && sPly.AlertPermissionCooldown == 0)
			{
				sPly.TSPlayer.SendMessage(string.Format("You do not have permission to use {0} command(s) on that sign!", DoesntHavePermission), Color.OrangeRed);
				sPly.AlertPermissionCooldown = 5;
			}
		}
		#endregion
	}
}
