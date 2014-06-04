using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SignCommands
{
	public class ScSign
	{
	    private Point Position { get; set; }
	    private int Cooldown { get; set; }
	    private string CooldownGroup { get; set; }
	    private long Cost { get; set; }
		public List<ScCommand> Commands { get; private set; }
		public ScSign(string text, Point position)
		{
			Position = position;
			Cooldown = 0;
			CooldownGroup = string.Empty;
			Cost = 0L;
			Commands = new List<ScCommand>();
			ParseCommands(text);
		}

		#region ParseCommands

	    private void ParseCommands(string text)
		{
			var lf = Encoding.UTF8.GetString(new byte[] { 10 })[0];
			text = string.Join(" ", text.Split(lf));

			var split = '>';
			if (SignCommands.config.CommandsStartWith.Length == 1)
				split = SignCommands.config.CommandsStartWith[0];

			var commands = new List<string> { text };
			if (text.Contains(split))
				commands = text.Split(split).ToList();

			/* Define Sign Command string should be at [0], we dont need it. */
			if (commands.Count > 0)
				commands.RemoveAt(0);

			foreach (var cmd in commands)
			{
				/* Parse Parameters */
				var args = scUtils.ParseParameters(cmd);

				var name = args[0];
				args.RemoveAt(0);

				Commands.Add(new ScCommand(this, name, args));
			}

			var cmds = Commands.ToList();
			foreach (var cmd in cmds)
			{
				/* Parse Cooldown */
				if (cmd.Command == "cooldown" && cmd.Args.Count > 0)
				{
					int seconds;
					if (!int.TryParse(cmd.Args[0], out seconds))
					{
						CooldownGroup = cmd.Args[0].ToLower();
						if (SignCommands.config.CooldownGroups.ContainsKey(CooldownGroup))
							Cooldown = SignCommands.config.CooldownGroups[CooldownGroup];
						else
							CooldownGroup = string.Empty;
					}
					else
						Cooldown = seconds;
					Commands.Remove(cmd);
				}
				/* Parse Cost */
				else if (SignCommands.UsingSEConomy && cmd.Command == "cost" && cmd.Args.Count > 0)
				{
					Cost = ParseCost(cmd.Args[0]);
					Commands.Remove(cmd);
				}
				/* Check if Valid */
				else if (!cmd.AmIValid())
					Commands.Remove(cmd);
			}
		}
		#endregion

		#region ExecuteCommands
		public void ExecuteCommands(scPlayer sPly)
		{
			#region Check Cooldown
			if (!sPly.TSPlayer.Group.HasPermission("essentials.signs.nocooldown") && Cooldown > 0)
			{
				if (CooldownGroup.StartsWith("global-"))
				{
					lock (SignCommands.GlobalCooldowns)
					{
						if (!SignCommands.GlobalCooldowns.ContainsKey(CooldownGroup))
							SignCommands.GlobalCooldowns.Add(CooldownGroup, DateTime.UtcNow.AddSeconds(Cooldown));
						else
						{
						    if (SignCommands.GlobalCooldowns[CooldownGroup] > DateTime.UtcNow)
							{
								if (sPly.AlertCooldownCooldown == 0)
								{
									sPly.TSPlayer.SendErrorMessage("Everyone must wait another {0} seconds before using this sign.", (int)(SignCommands.GlobalCooldowns[this.CooldownGroup] - DateTime.UtcNow).TotalSeconds);
									sPly.AlertCooldownCooldown = 3;
								}
								return;
							}
						    SignCommands.GlobalCooldowns[CooldownGroup] = DateTime.UtcNow.AddSeconds(Cooldown);
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
									sPly.TSPlayer.SendErrorMessage("You must wait another {0} seconds before using this sign.", (int)(sPly.Cooldowns[CooldownID] - DateTime.UtcNow).TotalSeconds);
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
			if (SignCommands.UsingSEConomy && Cost > 0 && !ChargeSign(sPly))
				return;
			#endregion

			var doesntHavePermission = 0;
			foreach (var cmd in Commands)
			{
				if (!sPly.TSPlayer.Group.HasPermission(string.Format("essentials.signs.use.{0}", cmd.Command)))
				{
					doesntHavePermission++;
					continue;
				}

				cmd.ExecuteCommand(sPly);
			}

			if (doesntHavePermission > 0 && sPly.AlertPermissionCooldown == 0)
			{
				sPly.TSPlayer.SendErrorMessage("You do not have permission to use {0} command(s) on that sign.", doesntHavePermission);
				sPly.AlertPermissionCooldown = 5;
			}
		}
		#endregion

		#region Economy

	    static long ParseCost(string arg)
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
		bool ChargeSign(scPlayer sPly)
		{
			try
			{
				var economyPlayer = Wolfje.Plugins.SEconomy.SEconomyPlugin.GetEconomyPlayerSafe(sPly.Index);
				var commandCost = new Wolfje.Plugins.SEconomy.Money(Cost);

				if (economyPlayer.BankAccount != null)
				{
					if (!economyPlayer.BankAccount.IsAccountEnabled)
						sPly.TSPlayer.SendErrorMessage("You cannot use this command because your account is disabled.");

                    else if (economyPlayer.BankAccount.Balance >= Cost)
                    {
                        var trans = economyPlayer.BankAccount.TransferTo(
                            Wolfje.Plugins.SEconomy.SEconomyPlugin.WorldAccount,
                            commandCost,
                            Wolfje.Plugins.SEconomy.Journal.BankAccountTransferOptions.AnnounceToSender |
                            Wolfje.Plugins.SEconomy.Journal.BankAccountTransferOptions.IsPayment,
                            "",
                            string.Format("Sign Command charge to {0}", sPly.TSPlayer.Name)
                            );
                        if (trans.TransferSucceeded)
                            return true;

                        sPly.TSPlayer.SendErrorMessage("Your payment failed.");
                    }
                    else
                    {
                        sPly.TSPlayer.SendErrorMessage(
                            "This Sign Command costs {0}. You need {1} more to be able to use it.",
                            commandCost.ToLongString(),
                            ((Wolfje.Plugins.SEconomy.Money) (economyPlayer.BankAccount.Balance - commandCost))
                                .ToLongString()
                            );
                    }
				}
				else
					sPly.TSPlayer.SendErrorMessage("This command costs money and you don't have a bank account. Please log in first.");
			}
			catch
			{ }
			return false;
		}
		#endregion
	}
}
