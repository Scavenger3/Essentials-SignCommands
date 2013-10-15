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
		public long Cost { get; set; }
		public List<scCommand> Commands { get; set; }
		public scSign(string text, Point position)
		{
			this.Position = position;
			this.Cooldown = 0;
			this.CooldownGroup = string.Empty;
			this.Cost = 0L;
			this.Commands = new List<scCommand>();
			ParseCommands(text);
		}

		#region ParseCommands
		public void ParseCommands(string text)
		{
			char LF = Encoding.UTF8.GetString(new byte[] { 10 })[0];
			text = string.Join(" ", text.Split(LF));

			char split = '>';
			if (SignCommands.Config.CommandsStartWith.Length == 1)
				split = SignCommands.Config.CommandsStartWith[0];

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
						if (SignCommands.Config.CooldownGroups.ContainsKey(this.CooldownGroup))
							this.Cooldown = SignCommands.Config.CooldownGroups[this.CooldownGroup];
						else
							this.CooldownGroup = string.Empty;
					}
					else
						this.Cooldown = seconds;
					this.Commands.Remove(cmd);
				}
				/* Parse Cost */
				else if (SignCommands.UsingSEConomy && cmd.command == "cost" && cmd.args.Count > 0)
				{
					this.Cost = parseCost(cmd.args[0]);
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
									Send.Error(sPly.TSPlayer, "Everyone must wait another {0} seconds before using this sign.", (int)(SignCommands.GlobalCooldowns[this.CooldownGroup] - DateTime.UtcNow).TotalSeconds);
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
									Send.Error(sPly.TSPlayer, "You must wait another {0} seconds before using this sign.", (int)(sPly.Cooldowns[CooldownID] - DateTime.UtcNow).TotalSeconds);
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
			if (SignCommands.UsingSEConomy && this.Cost > 0)
				chargeSign(sPly);
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
				Send.Error(sPly.TSPlayer, "You do not have permission to use {0} command(s) on that sign.", DoesntHavePermission);
				sPly.AlertPermissionCooldown = 5;
			}
		}
		#endregion

		#region Economy
		long parseCost(string arg)
		{
			try
			{
				Wolfje.Plugins.SEconomy.Money cost;
				if (!Wolfje.Plugins.SEconomy.Money.TryParse(arg, out cost))
					return 0L;
				return cost;
			}
			catch { return 0L; }
		}
		void chargeSign(scPlayer sPly)
		{
			try
			{
				var economyPlayerSafe = Wolfje.Plugins.SEconomy.SEconomyPlugin.GetEconomyPlayerSafe(sPly.Index);
				var money = new Wolfje.Plugins.SEconomy.Money(-this.Cost);
				Wolfje.Plugins.SEconomy.SEconomyPlugin.WorldAccount.TransferTo(economyPlayerSafe.BankAccount, money, Wolfje.Plugins.SEconomy.Journal.BankAccountTransferOptions.AnnounceToReceiver, "", string.Format("Sign Command charge: {0} to {1} ", money.ToString(), economyPlayerSafe.TSPlayer.Name));
			}
			catch { }
		}
		#endregion
	}
}
