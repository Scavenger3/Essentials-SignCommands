using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignCommands
{
	public class scSign
	{
		public Point Position { get; set; }
		public int Cooldown { get; set; }
		public string CooldownGroup { get; set; }
		public int Cost { get; set; }
		public List<scCommand> Commands { get; set; }
		public scSign(string text, Point position)
		{
			this.Position = position;
			this.Cooldown = 0;
			this.CooldownGroup = string.Empty;
			this.Cost = 0;
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
				/* Parse Parameters */
				var args = scUtils.ParseParameters(cmd);

				var name = args[0];
				args.RemoveAt(0);

				this.Commands.Add(new scCommand(this, name, args));
			}

			List<scCommand> Commands = this.Commands.ToList();
			foreach (scCommand cmd in Commands)
			{
				/* Parse Cooldown */
				if (cmd.command == "cooldown" && cmd.args.Count > 0)
				{
					int seconds;
					if (!int.TryParse(cmd.args[0], out seconds))
					{
						this.CooldownGroup = cmd.args[0].ToLower();
						if (SignCommands.getConfig.CooldownGroups.ContainsKey(this.CooldownGroup))
							this.Cooldown = SignCommands.getConfig.CooldownGroups[this.CooldownGroup];
						else
							this.CooldownGroup = string.Empty;
					}
					else
						this.Cooldown = seconds;
					this.Commands.Remove(cmd);
				}
				/* Parse Cost */
				else if (SignCommands.UsingVault && cmd.command == "cost" && cmd.args.Count > 0)
				{
					int amount;
					if (int.TryParse(cmd.args[0], out amount))
						this.Cost = amount;
					this.Commands.Remove(cmd);
				}
				/* Check if Valid */
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
			#region Check Cooldown
			if (!sPly.TSPlayer.Group.HasPermission("essentials.signs.nocooldown") && this.Cooldown > 0)
			{
				if (this.CooldownGroup.StartsWith("global-"))
				{
					lock (SignCommands.GlobalCooldowns)
					{
						if (!SignCommands.GlobalCooldowns.ContainsKey(this.CooldownGroup))
							SignCommands.GlobalCooldowns.Add(this.CooldownGroup, DateTime.UtcNow.AddSeconds(this.Cooldown));
						else
						{
							if (SignCommands.GlobalCooldowns[this.CooldownGroup] > DateTime.UtcNow)
							{
								if (sPly.AlertCooldownCooldown == 0)
								{
									sPly.TSPlayer.SendMessage(string.Format("Everyone must wait another {0} seconds before using this sign!", (int)(SignCommands.GlobalCooldowns[this.CooldownGroup] - DateTime.UtcNow).TotalSeconds), Color.OrangeRed);
									sPly.AlertCooldownCooldown = 3;
								}
								return;
							}
							else
								SignCommands.GlobalCooldowns[this.CooldownGroup] = DateTime.UtcNow.AddSeconds(this.Cooldown);
						}
					}
				}
				else
				{
					lock (sPly.Cooldowns)
					{
						string CooldownID = string.Concat(this.Position.ToString());
						if (this.CooldownGroup != string.Empty)
							CooldownID = this.CooldownGroup;

						if (!sPly.Cooldowns.ContainsKey(CooldownID))
							sPly.Cooldowns.Add(CooldownID, DateTime.UtcNow.AddSeconds(this.Cooldown));
						else
						{
							if (sPly.Cooldowns[CooldownID] > DateTime.UtcNow)
							{
								if (sPly.AlertCooldownCooldown == 0)
								{
									sPly.TSPlayer.SendMessage(string.Format("You must wait another {0} seconds before using this sign!", (int)(sPly.Cooldowns[CooldownID] - DateTime.UtcNow).TotalSeconds), Color.OrangeRed);
									sPly.AlertCooldownCooldown = 3;
								}
								return;
							}
							else
								sPly.Cooldowns[CooldownID] = DateTime.UtcNow.AddSeconds(this.Cooldown);
						}
					}
				}
			}
			#endregion

			#region Check Cost
			if (SignCommands.UsingVault && this.Cost > 0 && !chargeSign(sPly))
				return;
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

			if (DoesntHavePermission > 0 && sPly.AlertPermissionCooldown == 0)
			{
				sPly.TSPlayer.SendMessage(string.Format("You do not have permission to use {0} command(s) on that sign!", DoesntHavePermission), Color.OrangeRed);
				sPly.AlertPermissionCooldown = 5;
			}
		}
		#endregion

		#region Vault
		bool chargeSign(scPlayer sPly)
		{
			try
			{
				int NewBalance = Vault.Vault.GetBalance(sPly.TSPlayer.Name) - this.Cost;
				if (NewBalance < 0)
				{
					if (sPly.AlertCooldownCooldown == 0)
					{
						sPly.TSPlayer.SendMessage(string.Format("You must have at least {0} to execute this sign!", Vault.Vault.MoneyToString(this.Cost)), Color.OrangeRed);
						sPly.AlertCooldownCooldown = 3;
					}
					return false;
				}
				if (!Vault.Vault.SetBalance(sPly.TSPlayer.Name, NewBalance, false))
				{
					if (sPly.AlertCooldownCooldown == 0)
					{
						sPly.TSPlayer.SendMessage("Changing your balance failed!", Color.OrangeRed);
						sPly.AlertCooldownCooldown = 3;
					}
					return false;
				}
				sPly.TSPlayer.SendMessage(string.Format("Charged {0}, Your balance is now {1}!", Vault.Vault.MoneyToString(this.Cost), Vault.Vault.MoneyToString(NewBalance)), Color.OrangeRed);
				return true;
			}
			catch { return true; }
		}
		#endregion
	}
}
