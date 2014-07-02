using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using TShockAPI.Hooks;

namespace Essentials
{
	[ApiVersion(1, 16)]
	public class Essentials : TerrariaPlugin
	{
		public override string Name { get { return "Essentials"; } }
		public override string Author { get { return "Scavenger"; } }
		public override string Description { get { return "Some Essential commands for TShock!"; } }
		public override Version Version { get { return new Version(1, 6, 0); } }

	    private readonly Dictionary<string, int[]> _disabled = new Dictionary<string, int[]>();
	    private readonly List<EsPlayer> _players = new List<EsPlayer>();
	    private DateTime _lastCheck = DateTime.UtcNow;
	    private static EsConfig _config = new EsConfig();
	    private static string _savePath = string.Empty;

		public Essentials(Main game)
			: base(game)
		{
			Order = 4;
		}

		public override void Initialize()
		{
			ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
			ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
			ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
			ServerApi.Hooks.ServerChat.Register(this, OnChat);
			PlayerHooks.PlayerCommand += OnPlayerCommand;
			ServerApi.Hooks.NetGetData.Register(this, OnGetData);
			ServerApi.Hooks.NetSendBytes.Register(this, OnSendBytes);
			ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
				ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
				ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
				ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
				PlayerHooks.PlayerCommand -= OnPlayerCommand;
				ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
				ServerApi.Hooks.NetSendBytes.Deregister(this, OnSendBytes);
				ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
			}
			base.Dispose(disposing);
		}

		private void OnInitialize(EventArgs args)
		{
			#region Add Commands
			Commands.ChatCommands.Add(new Command("essentials.more", CmdMore, "more"));

			Commands.ChatCommands.Add(new Command(
                new List<string> { "essentials.position.get", "essentials.position.getother" },
                CmdPos, "pos", "getpos"));

			Commands.ChatCommands.Add(new Command("essentials.position.tp", CmdTpPos, "tppos"));
			Commands.ChatCommands.Add(new Command("essentials.position.ruler", CmdRuler, "ruler"));
			Commands.ChatCommands.Add(new Command("essentials.helpop.ask", CmdHelpOp, "helpop"));
			Commands.ChatCommands.Add(new Command("essentials.suicide", CmdSuicide, "suicide", "die"));
			Commands.ChatCommands.Add(new Command("essentials.pvp.burn", CmdBurn, "burn"));
			Commands.ChatCommands.Add(new Command("essentials.kickall.kick", CmdKickAll, "kickall"));
			Commands.ChatCommands.Add(new Command("essentials.moon", CmdMoon, "moon"));

			Commands.ChatCommands.Add(new Command(
                new List<string> { "essentials.back.tp", "essentials.back.death" }, CmdBack, "b"));

			Commands.ChatCommands.Add(new Command("essentials.ids.search", CmdItemSearch, "sitem", "si", "searchitem"));
			Commands.ChatCommands.Add(new Command("essentials.ids.search", CmdPageSearch, "spage", "sp"));
			Commands.ChatCommands.Add(new Command("essentials.ids.search", CmdNpcSearch, "snpc", "sn", "searchnpc"));
			Commands.ChatCommands.Add(new Command("essentials.home", CmdSetHome, "sethome"));
			Commands.ChatCommands.Add(new Command("essentials.home", CmdMyHome, "myhome"));
			Commands.ChatCommands.Add(new Command("essentials.home", CmdDeleteHome, "delhome"));
			Commands.ChatCommands.Add(new Command("essentials.essentials", CmdReload, "essentials"));
			Commands.ChatCommands.Add(new Command(CmdTeamUnlock, "teamunlock"));
			Commands.ChatCommands.Add(new Command("essentials.lastcommand", CmdRepeatCommand, "=") { DoLog = false });
			Commands.ChatCommands.Add(new Command("essentials.pvp.killr", CmdKillReason, "killr"));
			Commands.ChatCommands.Add(new Command("essentials.disable", CmdDisable, "disable"));
			Commands.ChatCommands.Add(new Command("essentials.level.top", CmdTop, "top"));
			Commands.ChatCommands.Add(new Command("essentials.level.up", CmdUp, "up"));
			Commands.ChatCommands.Add(new Command("essentials.level.down", CmdDown, "down"));
			Commands.ChatCommands.Add(new Command("essentials.level.side", CmdLeft, "left"));
			Commands.ChatCommands.Add(new Command("essentials.level.side", CmdRight, "right"));

			Commands.ChatCommands.Add(new Command(
                new List<string> { "essentials.playertime.set", "essentials.playertime.setother" },
                CmdPTime, "ptime"));

			Commands.ChatCommands.Add(new Command("essentials.ping", CmdPing, "ping", "pong", "echo"));
			Commands.ChatCommands.Add(new Command("essentials.sudo", CmdSudo, "sudo"));
			Commands.ChatCommands.Add(new Command("essentials.socialspy", CmdSocialSpy, "socialspy"));
			Commands.ChatCommands.Add(new Command("essentials.near", CmdNear, "near"));

			Commands.ChatCommands.Add(new Command(
                new List<string> { "essentials.nick.set", "essentials.nick.setother" }, CmdNick, "nick"));

			Commands.ChatCommands.Add(new Command("essentials.realname", CmdRealName, "realname"));
			Commands.ChatCommands.Add(new Command("essentials.exacttime", CmdExactTime, "etime", "exacttime"));
			Commands.ChatCommands.Add(new Command("essentials.forcelogin", CmdForceLogin, "forcelogin"));
			Commands.ChatCommands.Add(new Command("essentials.inventory.see", CmdInvSee, "invsee"));
			Commands.ChatCommands.Add(new Command("essentials.whois", CmdWhoIs, "whois"));
			#endregion

			_savePath = Path.Combine(TShock.SavePath, "Essentials");
			if (!Directory.Exists(_savePath))
				Directory.CreateDirectory(_savePath);

            EsSql.SetupDb();
            var configPath = Path.Combine(_savePath, "esConfig.json");
            (_config = EsConfig.Read(configPath)).Write(configPath);
		}

		#region esPlayer Join / Leave

	    private void OnGreet(GreetPlayerEventArgs args)
		{
            var ePly = _players.AddObj(new EsPlayer(args.Who));

            if (ePly == null || TShock.Players[args.Who] == null) return;


            if (_disabled.ContainsKey(TShock.Players[args.Who].Name))
            {
                if (_disabled[TShock.Players[args.Who].Name].Length != 2)  return;

                ePly.DisabledX = _disabled[TShock.Players[args.Who].Name][0];
                ePly.DisabledY = _disabled[TShock.Players[args.Who].Name][1];
                ePly.TSPlayer.Teleport(ePly.DisabledX * 16F, ePly.DisabledY * 16F);
                ePly.Disabled = true;
                ePly.Disable();
                ePly.LastDisabledCheck = DateTime.UtcNow;
                ePly.TSPlayer.SendErrorMessage("You are still disabled.");
            }

            string nickname;
            if (!EsSql.GetNickname(TShock.Players[args.Who].Name, out nickname)) return;
            ePly.HasNickName = true;
            ePly.OriginalName = ePly.TSPlayer.Name;
            ePly.Nickname = nickname;
		}

	    private void OnLeave(LeaveEventArgs args)
        {
            var ply = FindPlayer(args.Who);

	        if (TShock.Players[args.Who] == null || ply == null)
	            return;

	        if (ply.InvSee == null) return;
	        ply.InvSee.RestoreCharacter(TShock.Players[args.Who]);
            ply.InvSee = null;

            _players.RemoveAll(p => p.Index == args.Who);
	    }

	    #endregion

		#region Chat / Command

	    private void OnChat(ServerChatEventArgs e)
		{
			if (e.Handled)
			{
				return;
			}

	        var ePly = FindPlayer(e.Who);
			var tPly = TShock.Players[e.Who];

            if (ePly == null || tPly == null)
                return;

		    if (!ePly.TSPlayer.Active || !tPly.Active) return;

		    if (ePly.HasNickName && !e.Text.StartsWith("/") && !tPly.mute)
		    {
		        e.Handled = true;
		        var nick = _config.PrefixNicknamesWith + ePly.Nickname;
		        TSPlayer.All.SendMessage(String.Format(TShock.Config.ChatFormat, tPly.Group.Name, tPly.Group.Prefix, nick, tPly.Group.Suffix, e.Text),
		            tPly.Group.R, tPly.Group.G, tPly.Group.B);
		    }
		    else if (ePly.HasNickName && e.Text.StartsWith("/me ") && !tPly.mute)
		    {
		        e.Handled = true;
		        var nick = _config.PrefixNicknamesWith + ePly.Nickname;
		        TSPlayer.All.SendMessage(string.Format("*{0} {1}", nick, e.Text.Remove(0, 4)), 205, 133, 63);
		    }
		}

	    private void OnPlayerCommand(PlayerCommandEventArgs e)
		{
			if (e.Handled)
				return;

	        var ply = FindPlayer(e.Player.Index);

	        if (ply == null)
	            return;

				if (e.CommandName != "=")
					ply.LastCmd = string.Concat("/", e.CommandText);

			    if ((e.CommandName != "tp" || !e.Player.Group.HasPermission(Permissions.tp)) &&
			        (e.CommandName != "home" || !e.Player.Group.HasPermission(Permissions.home)) &&
			        (e.CommandName != "spawn" || !e.Player.Group.HasPermission(Permissions.spawn)) &&
			        (e.CommandName != "warp" || !e.Player.Group.HasPermission(Permissions.warp))) return;

			    ply.LastBackX = e.Player.TileX;
			    ply.LastBackY = e.Player.TileY;
			    ply.LastBackAction = BackAction.Tp;

	        if (e.CommandName != "whisper" && e.CommandName != "w" && e.CommandName != "tell"
                && e.CommandName != "reply" && e.CommandName != "r") return;

	        if (!e.Player.Group.HasPermission(Permissions.whisper))
	            return;

	        foreach (var player in _players)
	        {
	            if (player == null || !player.SocialSpy || player.Index == e.Player.Index)
	                continue;
	            if ((e.CommandName == "reply" || e.CommandName == "r") && e.Player.LastWhisper != null)
	                player.TSPlayer.SendMessage(string.Format("[SocialSpy] from {0} to {1}: /{2}",
	                    e.Player.Name, e.Player.LastWhisper.Name, e.CommandText), Color.Gray);
	            else
	                player.TSPlayer.SendMessage(string.Format("[SocialSpy] {0}: /{1}", 
	                    e.Player.Name, e.CommandText), Color.Gray);
	        }
		}
		#endregion

		#region Get Data
		private void OnGetData(GetDataEventArgs e)
		{
			if (e.MsgID != PacketTypes.PlayerTeam || !(_config.LockRedTeam || _config.LockGreenTeam || _config.LockBlueTeam || _config.LockYellowTeam))
				return;

			var who = e.Msg.readBuffer[e.Index];
			var team = e.Msg.readBuffer[e.Index + 1];

		    var ePly = FindPlayer(who);
			var tPly = TShock.Players[who];
		    if (ePly == null || tPly == null)
		        return;
			switch (team)
			{
				#region Red
				case 1:
					if (_config.LockRedTeam && !tPly.Group.HasPermission(_config.RedTeamPermission) && (ePly.RedPassword != _config.RedTeamPassword || ePly.RedPassword == ""))
					{
						e.Handled = true;
						tPly.SetTeam(tPly.Team);
						if (_config.RedTeamPassword == "")
							tPly.SendErrorMessage("You do not have permission to join that team.");
						else
							tPly.SendErrorMessage("That team is locked, use \'/teamunlock red <password>\' to access it.");

					}
					break;
				#endregion

				#region Green
				case 2:
					if (_config.LockGreenTeam && !tPly.Group.HasPermission(_config.GreenTeamPermission) && (ePly.GreenPassword != _config.GreenTeamPassword || ePly.GreenPassword == ""))
					{
						e.Handled = true;
						tPly.SetTeam(tPly.Team);
						if (_config.GreenTeamPassword == "")
							tPly.SendErrorMessage("You do not have permission to join that team.");
						else
							tPly.SendErrorMessage("That team is locked, use \'/teamunlock green <password>\' to access it.");

					}
					break;
				#endregion

				#region Blue
				case 3:
					if (_config.LockBlueTeam && !tPly.Group.HasPermission(_config.BlueTeamPermission) && (ePly.BluePassword != _config.BlueTeamPassword || ePly.BluePassword == ""))
					{
						e.Handled = true;
						tPly.SetTeam(tPly.Team);
						if (_config.BlueTeamPassword == "")
							tPly.SendErrorMessage("You do not have permission to join that team.");
						else
							tPly.SendErrorMessage("That team is locked, use \'/teamunlock blue <password>\' to access it.");

					}
					break;
				#endregion

				#region Yellow
				case 4:
					if (_config.LockYellowTeam && !tPly.Group.HasPermission(_config.YellowTeamPermission) && (ePly.YellowPassword != _config.YellowTeamPassword || ePly.YellowPassword == ""))
					{
						e.Handled = true;
						tPly.SetTeam(tPly.Team);
						if (_config.YellowTeamPassword == "")
							tPly.SendErrorMessage("You do not have permission to join that team.");
						else
							tPly.SendErrorMessage("That team is locked, use \'/teamunlock yellow <password>\' to access it.");

					}
					break;
				#endregion
			}
		}
		#endregion

		#region Send Bytes
		void OnSendBytes(SendBytesEventArgs args)
		{
		    var ply = FindPlayer(args.Socket.whoAmI);
		    if (ply == null)
		        return;

			if ((args.Buffer[4] != 7 && args.Buffer[4] != 18) || args.Socket.whoAmI < 0 || args.Socket.whoAmI > 255 || ply.PtTime < 0.0)
				return;


			switch (args.Buffer[4])
			{
				case 7:
					Buffer.BlockCopy(BitConverter.GetBytes((int)ply.PtTime), 0, args.Buffer, 5, 4);
					args.Buffer[9] = (byte)(ply.PtDay ? 1 : 0);
					break;
				case 18:
					args.Buffer[5] = (byte)(ply.PtDay ? 1 : 0);
					Buffer.BlockCopy(BitConverter.GetBytes((int)ply.PtTime), 0, args.Buffer, 6, 4);
					break;
			}
		}
		#endregion

		#region On Update
		private void OnUpdate(EventArgs args)
		{
			if ((DateTime.UtcNow - _lastCheck).TotalMilliseconds >= 1000)
			{
				_lastCheck = DateTime.UtcNow;
				foreach (var ePly in _players)
				{
				    if (ePly == null || ePly.TSPlayer == null)
				        continue;

					if (!ePly.SavedBackAction && ePly.TSPlayer.Dead)
					{
						if (ePly.TSPlayer.Group.HasPermission("essentials.back.death"))
						{
							ePly.LastBackX = ePly.TSPlayer.TileX;
							ePly.LastBackY = ePly.TSPlayer.TileY;
							ePly.LastBackAction = BackAction.Death;
							ePly.SavedBackAction = true;
							if (_config.ShowBackMessageOnDeath)
								ePly.TSPlayer.SendSuccessMessage("Type \"/b\" to return to your position before you died.");
						}
					}
					else if (ePly.SavedBackAction && !ePly.TSPlayer.Dead)
						ePly.SavedBackAction = false;

					if (ePly.BackCooldown > 0)
						ePly.BackCooldown--;

					if (ePly.PtTime > -1.0)
					{
						ePly.PtTime += 60.0;
						if (!ePly.PtDay && ePly.PtTime > 32400.0)
							ePly.PtDay = true; ePly.PtTime = 0.0;

						if (ePly.PtDay && ePly.PtTime > 54000.0)
							ePly.PtDay = false; ePly.PtTime = 0.0;
					}

					if (ePly.Disabled && ((DateTime.UtcNow - ePly.LastDisabledCheck).TotalMilliseconds) > 3000)
					{
						ePly.LastDisabledCheck = DateTime.UtcNow;
						ePly.Disable();
						if ((ePly.TSPlayer.TileX > ePly.DisabledX + 5 || ePly.TSPlayer.TileX < ePly.DisabledX - 5) || (ePly.TSPlayer.TileY > ePly.DisabledY + 5 || ePly.TSPlayer.TileY < ePly.DisabledY - 5))
							ePly.TSPlayer.Teleport(ePly.DisabledX * 16F, ePly.DisabledY * 16F);
					}
				}
			}
		}
		#endregion

		/* Commands: */

		#region More
		private static void CmdMore(CommandArgs args)
		{
			if (args.Parameters.Count > 0 && args.Parameters[0].ToLower() == "all")
			{
				var full = true;
				foreach (var item in args.TPlayer.inventory)
				{
					if (item == null || item.stack == 0) continue;
					var amtToAdd = item.maxStack - item.stack;
					if (item.stack > 0 && amtToAdd > 0 && !item.name.ToLower().Contains("coin"))
					{
						full = false;
						args.Player.GiveItem(item.type, item.name, item.width, item.height, amtToAdd);
					}
				}
				if (!full)
					args.Player.SendSuccessMessage("Filled all your items.");
				else
					args.Player.SendErrorMessage("Your inventory is already full.");
			}
			else
			{
				var holding = args.Player.TPlayer.inventory[args.TPlayer.selectedItem];
				var amtToAdd = holding.maxStack - holding.stack;
				if (holding.stack > 0 && amtToAdd > 0)
					args.Player.GiveItem(holding.type, holding.name, holding.width, holding.height, amtToAdd);
				if (amtToAdd == 0)
					args.Player.SendErrorMessage("Your {0} is already full.", holding.name);
				else
					args.Player.SendSuccessMessage("Filled up your {0}.", holding.name);
			}
		}
		#endregion

		#region Position Commands
		private static void CmdPos(CommandArgs args)
		{
			if (args.Player.Group.HasPermission("essentials.position.getother") && args.Parameters.Count > 0)
			{
				var found = TShock.Utils.FindPlayer(string.Join(" ", args.Parameters));
				if (found.Count != 1)
				{
				    TShock.Utils.SendMultipleMatchError(args.Player, found.Select(p => p.Name));
                    return;
				}
				args.Player.SendSuccessMessage("Position for {0}: X Tile: {1} - Y Tile: {2}", found[0].Name, found[0].TileX, found[0].TileY);
				return;
			}
			args.Player.SendSuccessMessage("X Tile: {0} - Y Tile: {1}", args.Player.TileX, args.Player.TileY);
		}

		private void CmdTpPos(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendErrorMessage("Usage: /tppos <X> [Y]");
				return;
			}

			int x, y = 0;
			if (!int.TryParse(args.Parameters[0], out x)
                || (args.Parameters.Count == 2 && !int.TryParse(args.Parameters[1], out y)))
			{
				args.Player.SendErrorMessage("Usage: /tppos <X> [Y]");
				return;
			}

			if (args.Parameters.Count == 1)
				y = esUtils.GetTop(x);

		    var ePly = FindPlayer(args.Player.Index);
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.Tp;
			}

			args.Player.Teleport(x * 16F, y * 16F);
			args.Player.SendSuccessMessage("Teleported you to X: {0} - Y: {1}", x, y);
		}

		private static void CmdRuler(CommandArgs args)
		{
		    int choice;
			if (args.Parameters.Count == 1 &&
				int.TryParse(args.Parameters[0], out choice) &&
				choice >= 1 && choice <= 2)
			{
				args.Player.SendInfoMessage("Hit a block to Set Point {0}.", choice);
				args.Player.AwaitingTempPoint = choice;
			}
			else
			{
				if (args.Player.TempPoints[0] == Point.Zero || args.Player.TempPoints[1] == Point.Zero)
					args.Player.SendErrorMessage("Invalid Points. To set points use: /ruler [1/2]");
				else
				{
					var width = Math.Abs(args.Player.TempPoints[0].X - args.Player.TempPoints[1].X);
					var height = Math.Abs(args.Player.TempPoints[0].Y - args.Player.TempPoints[1].Y);
					args.Player.SendSuccessMessage("Area Height: {0} Width: {1}", height, width);
					args.Player.TempPoints[0] = Point.Zero; args.Player.TempPoints[1] = Point.Zero;
				}
			}
		}
		#endregion

		#region HelpOp
		private void CmdHelpOp(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Usage: /helpop <message>");
				return;
			}

			var text = string.Join(" ", args.Parameters);

			var online = new List<string>();

			foreach (var ePly in _players.Where(ePly => ePly != null 
                && ePly.TSPlayer.Group.HasPermission("essentials.helpop.receive")))
			{
			    online.Add(ePly.TSPlayer.Name);
			    ePly.TSPlayer.SendMessage(string.Format("[HelpOp] {0}: {1}", args.Player.Name, text),
                    Color.MediumPurple);
			}
			if (online.Count == 0)
				args.Player.SendMessage("[HelpOp] There are no operators online to receive your message.", 
                    Color.MediumPurple);
			else
			{
				args.Player.SendMessage(string.Format("[HelpOp] Your message has been received by {0} operator{1}", 
                    online.Count, online.Count == 1 ? "" : "s"), Color.MediumPurple);
			}
		}
		#endregion

		#region Suicide
		private static void CmdSuicide(CommandArgs args)
		{
			if (!args.Player.RealPlayer)
				return;
			NetMessage.SendData(26, -1, -1, " decided it wasnt worth living.", args.Player.Index, 0, 15000);
		}
		#endregion

		#region Burn
		private static void CmdBurn(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendErrorMessage("Usage: /burn <player> [seconds]");
				return;
			}

			var duration = 30;
			if (args.Parameters.Count == 2 && !int.TryParse(args.Parameters[1], out duration))
				duration = 30;
            //duration *= 60;
			var found = TShock.Utils.FindPlayer(args.Parameters[0]);
		    if (found.Count == 0)
		    {
		        args.Player.SendErrorMessage("Invalid player");
		        return;
		    }
			if (found.Count > 1)
			{
			    TShock.Utils.SendMultipleMatchError(args.Player, found.Select(p => p.Name));
                return;
			}
			found[0].SetBuff(24, duration);
			args.Player.SendSuccessMessage("{0} Has been set on fire for {1} second(s).", found[0].Name, duration);
		}
		#endregion

		#region KickAll
		private void CmdKickAll(CommandArgs args)
		{
			var reason = string.Empty;
			if (args.Parameters.Count > 0)
				reason = " (" + string.Join(" ", args.Parameters) + ")";

			foreach (var ePly in _players.Where(ePly => ePly != null 
                && !ePly.TSPlayer.Group.HasPermission("essentials.kickall.immune")))
			{
			    ePly.TSPlayer.Disconnect(string.Format("Everyone has been kicked{0}", reason));
			}
			TSPlayer.All.SendMessage("Everyone has been kicked from the server.", Color.MediumSeaGreen);
		}
		#endregion

		#region Moon
		private static void CmdMoon(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Usage: /moon [ new | 1/4 | half | 3/4 | full ]");
				return;
			}

			var subcmd = args.Parameters[0].ToLower();

			switch (subcmd)
			{
				case "new":
					Main.moonPhase = 4;
					break;
				case "1/4":
					Main.moonPhase = 3;
					break;
				case "half":
					Main.moonPhase = 2;
					break;
				case "3/4":
					Main.moonPhase = 1;
					break;
				case "full":
					Main.moonPhase = 0;
					break;
				default:
					args.Player.SendErrorMessage("Usage: /moon [ new | 1/4 | half | 3/4 | full ]");
					break;
			}
			NetMessage.SendData(7);
			args.Player.SendSuccessMessage("Moon Phase set to {0} Moon.", subcmd);
		}
		#endregion

		#region Back
		private void CmdBack(CommandArgs args)
		{
		    var ePly = FindPlayer(args.Player.Index);
			if (ePly.TSPlayer.Dead)
			{
				args.Player.SendErrorMessage("Please wait until you respawn.");
				return;
			}
			if (ePly.BackCooldown > 0)
			{
				args.Player.SendErrorMessage("You must wait another {0} seconds before you can use /b again.", ePly.BackCooldown);
				return;
			}

			switch (ePly.LastBackAction)
			{
			    case BackAction.None:
			        args.Player.SendErrorMessage("You do not have a /b position stored.");
			        break;
			    case BackAction.Tp:
			        if (_config.BackCooldown > 0 && !args.Player.Group.HasPermission("essentials.back.nocooldown"))
			        {
			            ePly.BackCooldown = _config.BackCooldown;
			        }
			        args.Player.Teleport(ePly.LastBackX * 16F, ePly.LastBackY * 16F);
			        args.Player.SendSuccessMessage("Moved you to your position before you last teleported.");
			        break;
                case BackAction.Death:
			        if (args.Player.Group.HasPermission("essentials.back.death"))
			        {
			            if (_config.BackCooldown > 0 && !args.Player.Group.HasPermission("essentials.back.nocooldown"))
			                ePly.BackCooldown = _config.BackCooldown;

			            args.Player.Teleport(ePly.LastBackX * 16F, ePly.LastBackY * 16F);
			            args.Player.SendSuccessMessage("Moved you to your position before you last died.");
			        }
			        else
			            args.Player.SendErrorMessage("You do not have permission to /b after death.");
			        break;
			}
		}
		#endregion

		#region Seach IDs
		private void CmdPageSearch(CommandArgs args)
		{
			if (args.Parameters.Count != 1)
			{
				args.Player.SendErrorMessage("Usage: /spage <page>");
				return;
			}

		    int page;
			if (!int.TryParse(args.Parameters[0], out page))
			{
				args.Player.SendErrorMessage(string.Format("Specified page ({0}) invalid.", args.Parameters[0]));
				return;
			}

		    var ePly = FindPlayer(args.Player.Index);

			esUtils.DisplaySearchResults(args.Player, ePly.LastSearchResults, page);
		}

		private void CmdItemSearch(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Usage: /sitem <search term>");
				return;
			}

			var search = string.Join(" ", args.Parameters);
			var results = esUtils.ItemIdSearch(search);

            if (results.Count < 1)
			{
				args.Player.SendErrorMessage("Could not find any matching Items.");
				return;
			}

		    var ePly = FindPlayer(args.Player.Index);

            ePly.LastSearchResults = results;
            esUtils.DisplaySearchResults(args.Player, results, 1);
		}

		private void CmdNpcSearch(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Usage: /snpc <search term>");
				return;
			}

			var search = string.Join(" ", args.Parameters);
			var results = esUtils.NPCIdSearch(search);

			if (results.Count < 1)
			{
				args.Player.SendErrorMessage("Could not find any matching NPCs.");
				return;
			}

            var ePly = FindPlayer(args.Player.Index);

			ePly.LastSearchResults = results;
			esUtils.DisplaySearchResults(args.Player, results, 1);
		}
		#endregion

		#region MyHome
		private void CmdSetHome(CommandArgs args)
		{
			/* Chek if the player is a real player */
			if (!args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("You must be a real player.");
				return;
			}
			/* Make sure the player is logged in */
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}
			/* Make sure the player isn't in a SetHome Disabled region */
			if (!args.Player.Group.HasPermission("essentials.home.bypassdisabled"))
			{
				if (_config.DisableSetHomeInRegions.Select(r =>
                    TShock.Regions.GetRegionByName(r)).Where(region => 
                        region != null).Any(region => 
                            region.InArea(args.Player.TileX, args.Player.TileY)))
				{
				    args.Player.SendErrorMessage("You cannot set your home in this region.");
				    return;
				}
			}

			/* get a list of the player's homes */
			var homes = EsSql.GetNames(args.Player.UserID, Main.worldID);
			/* how many homes is the user allowed to set */
			var canSet = esUtils.NoOfHomesCanSet(args.Player);

			if (homes.Count < 1)
			{
				/* the player doesn't have a home, Create one! */
				if (args.Parameters.Count < 1 || (args.Parameters.Count > 0 && canSet == 1))
				{
					/* they dont specify a name OR they specify a name but they only have permission to set 1, use a default name */
					if (EsSql.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, "1", Main.worldID))
						args.Player.SendSuccessMessage("Set home.");
					else
						args.Player.SendErrorMessage("An error occurred while setting your home.");
				}
				else if (args.Parameters.Count == 1 && !args.Parameters[0].Contains(" ") && (canSet == -1 || canSet > 1))
				{
					/* they specify a name And have permission to specify more than 1 */
					var name = args.Parameters[0].ToLower();
					if (EsSql.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, name, Main.worldID))
						args.Player.SendSuccessMessage("Set home {0}.", name);
					else
						args.Player.SendErrorMessage("An error occurred while setting your home.");
				}
				else
				{
					/* homes cant have more than 1 word */
					args.Player.SendErrorMessage("Error: Homes cannot contain spaces.");
				}
			}
			else if (homes.Count == 1)
			{
				/* If they only have 1 home */
				if (args.Parameters.Count == 1 && !args.Parameters[0].Contains(" ") && (1 < canSet || canSet == -1))
				{
					/* They Specify a name and can set more than 1  */
					var name = args.Parameters[0].ToLower();
					if (homes.Contains(name))
					{
						/* They want to update a home */
						if (EsSql.UpdateHome(args.Player.TileX, args.Player.TileY, args.Player.UserID, name, Main.worldID))
							args.Player.SendSuccessMessage("Updated home {0}.", name);
						else
							args.Player.SendErrorMessage("An error occurred while updating your home.");
					}
					else
					{
						/* They want to add a new home */
						if (EsSql.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, name, Main.worldID))
							args.Player.SendSuccessMessage("Set home {0}.", name);
						else
							args.Player.SendErrorMessage("An error occurred while setting your home.");
					}
				}
				else if (args.Parameters.Count < 1 && (1 < canSet || canSet == -1))
				{
					/* if they dont specify a name & can set more than 1  - add a new home*/
					if (EsSql.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, esUtils.NextHome(homes), Main.worldID))
						args.Player.SendSuccessMessage("Set home.");
					else
						args.Player.SendErrorMessage("An error occurred while setting your home.");
				}
				else if (args.Parameters.Count > 0 && canSet == 1)
				{
					/* They specify a name but can only set 1 home, update their current home */
					if (EsSql.UpdateHome(args.Player.TileX, args.Player.TileY, args.Player.UserID, homes[0], Main.worldID))
						args.Player.SendSuccessMessage("Updated home.");
					else
						args.Player.SendErrorMessage("An error occurred while updating your home.");
				}
				else
				{
					/* homes cant have more than 1 word */
					args.Player.SendErrorMessage("Error: Homes cannot contain spaces.");
				}
			}
			else
			{
				/* If they have more than 1 home */
				if (args.Parameters.Count < 1)
				{
					/* they didnt specify a name */
					if (homes.Count < canSet || canSet == -1)
					{
						/* They can set more homes */
						if (EsSql.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, esUtils.NextHome(homes), Main.worldID))
							args.Player.SendSuccessMessage("Set home.");
						else
							args.Player.SendErrorMessage("An error occurred while setting your home.");
					}
					else
					{
						/* they cant set any more homes */
						args.Player.SendErrorMessage("You are only allowed to set {0} homes.", canSet.ToString());
						args.Player.SendErrorMessage("Homes: {0}", string.Join(", ", homes));
					}
				}
				else if (args.Parameters.Count == 1 && !args.Parameters[0].Contains(" "))
				{
					/* they want to set / update another home and specified a name */
					var name = args.Parameters[0].ToLower();
					if (homes.Contains(name))
					{
						/* they want to update a home */
						if (EsSql.UpdateHome(args.Player.TileX, args.Player.TileY, args.Player.UserID, name, Main.worldID))
							args.Player.SendSuccessMessage("Updated home.");
						else
							args.Player.SendErrorMessage("An error occurred while updating your home.");
					}
					else
					{
						/* they want to add a new home */
						if (homes.Count < canSet || canSet == -1)
						{
							/* they can set more homes */
							if (EsSql.AddHome(args.Player.UserID, args.Player.TileX, args.Player.TileY, name, Main.worldID))
								args.Player.SendSuccessMessage("Set home {0}.", name);
							else
								args.Player.SendErrorMessage("An error occurred while setting your home.");
						}
						else
						{
							/* they cant set any more homes */
							args.Player.SendErrorMessage("You are only allowed to set {0} homes.", canSet.ToString());
							args.Player.SendErrorMessage("Homes: {0}", string.Join(", ", homes));
						}
					}
				}
				else
				{
					/* homes cant have more than 1 word */
					args.Player.SendErrorMessage("Error: Homes cannot contain spaces.");
				}
			}
		}

		private void CmdMyHome(CommandArgs args)
		{
			/* Chek if the player is a real player */
			if (!args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("You must be a real player.");
				return;
			}
			/* Make sure the player is logged in */
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			/* get a list of the player's homes */
			var homes = EsSql.GetNames(args.Player.UserID, Main.worldID);
			/* Set home position variable */
			var homePos = Point.Zero;

			if (homes.Count < 1)
			{
				/* they do not have a home */
				args.Player.SendErrorMessage("You have not set a home. type /sethome to set one.");
				return;
			}
			if (homes.Count == 1)
			{
				/* they have 1 home */
				homePos = EsSql.GetHome(args.Player.UserID, homes[0], Main.worldID);
			}
			else if (homes.Count > 1)
			{
				/* they have more than 1 home */
				if (args.Parameters.Count < 1)
				{
					/* they didnt specify the name */
					args.Player.SendErrorMessage("Usage: /myhome <home>");
					args.Player.SendErrorMessage("Homes: {0}", string.Join(", ", homes));
					return;
				}
				if (args.Parameters.Count == 1 && !args.Parameters[0].Contains(" "))
				{
					var name = args.Parameters[0].ToLower();
					if (homes.Contains(name))
					{
						homePos = EsSql.GetHome(args.Player.UserID, name, Main.worldID);
					}
					else
					{
						/* could not find the name */
						args.Player.SendErrorMessage("Usage: /myhome <home>");
						args.Player.SendErrorMessage("Homes: {0}", string.Join(", ", homes));
						return;
					}
				}
				else
				{
					/* could not find the name */
					args.Player.SendErrorMessage("Usage: /myhome <home>");
					args.Player.SendErrorMessage("Homes: {0}", string.Join(", ", homes));
					return;
				}
			}

			/* teleport home */
			if (homePos == Point.Zero)
			{
				args.Player.SendErrorMessage("There is an error with your home.");
				return;
			}

			var ePly = FindPlayer(args.Player.Index);
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.Tp;
			}

			args.Player.Teleport(homePos.X * 16F, homePos.Y * 16F);
			args.Player.SendSuccessMessage("Teleported home.");
		}

		private void CmdDeleteHome(CommandArgs args)
		{
			/* Chek if the player is a real player */
			if (!args.Player.RealPlayer)
			{
				args.Player.SendErrorMessage("You must be a real player.");
				return;
			}
			/* Make sure the player is logged in */
			if (!args.Player.IsLoggedIn)
			{
				args.Player.SendErrorMessage("You must be logged in to do that.");
				return;
			}

			/* get a list of the player's homes */
			var homes = EsSql.GetNames(args.Player.UserID, Main.worldID);

			if (homes.Count < 1)
			{
				/* they do not have a home */
				args.Player.SendErrorMessage("You have not set a home. type /sethome to set one.");
			}
			else if (homes.Count == 1)
			{
				/* they have 1 home */
				if (EsSql.RemoveHome(args.Player.UserID, homes[0], Main.worldID))
					args.Player.SendSuccessMessage("Removed home.");
				else
					args.Player.SendErrorMessage("An error occurred while removing your home.");
			}
			else if (homes.Count > 1)
			{
				/* they have more than 1 home */
				if (args.Parameters.Count < 1)
				{
					/* they didnt specify the name */
					args.Player.SendErrorMessage("Usage: /delhome <home>");
					args.Player.SendErrorMessage("Homes: {0}", string.Join(", ", homes));
				}
				else if (args.Parameters.Count == 1 && !args.Parameters[0].Contains(" "))
				{
					var name = args.Parameters[0].ToLower();
					if (homes.Contains(name))
					{
						if (EsSql.RemoveHome(args.Player.UserID, name, Main.worldID))
							args.Player.SendSuccessMessage("Removed home {0}.", name);
						else
							args.Player.SendErrorMessage("An error occurred while removing your home.");
					}
					else
					{
						/* could not find the name */
						args.Player.SendErrorMessage("Usage: /delhome <home>");
						args.Player.SendErrorMessage("Homes: {0}", string.Join(", ", homes));
					}
				}
				else
				{
					/* could not find the name */
					args.Player.SendErrorMessage("Usage: /delhome <home>");
					args.Player.SendErrorMessage("Homes: {0}", string.Join(", ", homes));
				}
			}
		}
		#endregion

		#region Reload
		private void CmdReload(CommandArgs args)
		{
            //reload config
		}
		#endregion

		#region Team Unlock
		private void CmdTeamUnlock(CommandArgs args)
		{
			if (!_config.LockRedTeam && !_config.LockGreenTeam && !_config.LockBlueTeam && !_config.LockYellowTeam)
			{
				args.Player.SendErrorMessage("Teams are not locked.");
				return;
			}

			if (args.Parameters.Count < 2)
			{
				args.Player.SendErrorMessage("Usage: /teamunlock <color> <password>");
				return;
			}

			var team = args.Parameters[0].ToLower();

			args.Parameters.RemoveAt(0);
			var password = string.Join(" ", args.Parameters);

			var ePly = FindPlayer(args.Player.Index);

			switch (team)
			{
				case "red":
					{
						if (_config.LockRedTeam)
						{
							if (password == _config.RedTeamPassword && _config.RedTeamPassword != "")
							{
								args.Player.SendSuccessMessage("You can now join red team.");
								ePly.RedPassword = password;
							}
							else
								args.Player.SendErrorMessage("Incorrect Password.");
						}
						else
							args.Player.SendErrorMessage("The red team isn't locked.");
					}
					break;
				case "green":
					{
						if (_config.LockGreenTeam)
						{
							if (password == _config.GreenTeamPassword && _config.GreenTeamPassword != "")
							{
								args.Player.SendSuccessMessage("You can now join green team.");
								ePly.GreenPassword = password;
							}
							else
								args.Player.SendErrorMessage("Incorrect Password.");
						}
						else
							args.Player.SendErrorMessage("The green team isn't locked.");
					}
					break;
				case "blue":
					{
						if (_config.LockBlueTeam)
						{
							if (password == _config.BlueTeamPassword && _config.BlueTeamPassword != "")
							{
								args.Player.SendSuccessMessage("You can now join blue team.");
								ePly.BluePassword = password;
							}
							else
								args.Player.SendErrorMessage("Incorrect Password.");
						}
						else
							args.Player.SendErrorMessage("The blue team isn't lock.");
					}
					break;
				case "yellow":
					{
						if (_config.LockYellowTeam)
						{
							if (password == _config.YellowTeamPassword && _config.YellowTeamPassword != "")
							{
								args.Player.SendSuccessMessage("You can now join yellow team.");
								ePly.YellowPassword = password;
							}
							else
								args.Player.SendErrorMessage("Incorrect Password.");
						}
						else
							args.Player.SendErrorMessage("The yellow team isn't locked.");
					}
					break;
				default:
					args.Player.SendErrorMessage("Usage: /teamunlock <red/green/blue/yellow> <password>");
					break;
			}
		}
		#endregion

		#region Last Command
		private void CmdRepeatCommand(CommandArgs args)
		{
		    var ePly = FindPlayer(args.Player.Index);

            if (ePly.LastCmd == "/=" || ePly.LastCmd.StartsWith("/= "))
			{
				args.Player.SendErrorMessage("Error with last command.");
				return;
			}

            if (ePly.LastCmd == string.Empty)
				args.Player.SendErrorMessage("You have not entered a command yet.");
			else
                Commands.HandleCommand(args.Player, ePly.LastCmd);
		}
		#endregion

		#region Kill Reason
		private static void CmdKillReason(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Usage: /killr <player> <reason>");
				return;
			}

			var found = TShock.Utils.FindPlayer(args.Parameters[0]);
		    if (found.Count > 1)
		    {
		        TShock.Utils.SendMultipleMatchError(args.Player, found.Select(p => p.Name));
		        return;
		    }
		    if (found.Count == 0)
		    {
		        args.Player.SendErrorMessage("No matches found");
		    }

		    var ply = found[0];
			args.Parameters.RemoveAt(0); //remove player name
			string reason = " " + string.Join(" ", args.Parameters);

			NetMessage.SendData(26, -1, -1, reason, ply.Index, 0f, 15000);
			args.Player.SendSuccessMessage("You just killed {0}.", ply.Name);
		}
		#endregion

		#region Disable
		private void CmdDisable(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Usage: /disable <player/-list> [reason]");
				return;
			}

			if (args.Parameters[0].ToLower() == "-list")
			{
				var names = new List<string>(_disabled.Keys);
				if (_disabled.Count < 1)
					args.Player.SendSuccessMessage("There are currently no players disabled.", Color.MediumSeaGreen);
				else
					args.Player.SendSuccessMessage("Disabled Players: {0}", string.Join(", ", names));
				return;
			}

			var found = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (found.Count < 1)
			{
			    foreach (var pair in _disabled.Where(pair => 
                    pair.Key.ToLower().Contains(args.Parameters[0].ToLower())))
			    {
			        _disabled.Remove(pair.Key);
			        args.Player.SendSuccessMessage("{0} is no longer disabled (even though they aren't online).",
                        pair.Key);
			        return;
			    }
			    args.Player.SendErrorMessage("No players matched.");
			    return;
			}
			if (found.Count > 1)
			{
			    TShock.Utils.SendMultipleMatchError(args.Player, found.Select(p => p.Name));
                return;
			}

			var tPly = found[0];
		    var ePly = FindPlayer(tPly.Index);

			if (!_disabled.ContainsKey(tPly.Name))
			{
				var reason = string.Empty;
				if (args.Parameters.Count > 1)
				{
					args.Parameters.RemoveAt(0);
                    reason = string.Join(" ", args.Parameters);
				}
				ePly.DisabledX = tPly.TileX;
				ePly.DisabledY = tPly.TileY;
				ePly.Disabled = true;
				ePly.Disable();
				ePly.LastDisabledCheck = DateTime.UtcNow;
				var pos = new int[2];
				pos[0] = ePly.DisabledX;
				pos[1] = ePly.DisabledY;
				_disabled.Add(tPly.Name, pos);
				args.Player.SendSuccessMessage("You disabled {0}, They can not be enabled until you type \"/disable {0}\".", tPly.Name);
                if (reason == string.Empty)
					tPly.SendSuccessMessage("You have been disabled by {0}.", args.Player.Name);
				else
                    tPly.SendSuccessMessage("You have been disabled by {0} for {1}.", args.Player.Name, reason);
			}
			else
			{
				ePly.Disabled = false;
				ePly.DisabledX = -1;
				ePly.DisabledY = -1;

				_disabled.Remove(tPly.Name);

				args.Player.SendSuccessMessage("{0} is no longer disabled.", tPly.Name);
				tPly.SendSuccessMessage("You are no longer disabled.");
			}
		}
		#endregion

		#region Top, Up and Down

		private void CmdTop(CommandArgs args)
		{
			var y = esUtils.GetTop(args.Player.TileX);
			if (y == -1)
			{
				args.Player.SendErrorMessage("You are already on the top.");
				return;
			}
			var ePly = FindPlayer(args.Player.Index);
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.Tp;
			}
			args.Player.Teleport(args.Player.TileX * 16F, y * 16F);
			args.Player.SendSuccessMessage("Teleported you to the top.");
		}
		private void CmdUp(CommandArgs args)
		{
			var levels = 1;
			if (args.Parameters.Count > 0 && !int.TryParse(args.Parameters[0], out levels))
			{
				args.Player.SendErrorMessage("Usage: /up [No. levels]");
				return;
			}

			var y = esUtils.GetUp(args.Player.TileX, args.Player.TileY);
			if (y == -1)
			{
				args.Player.SendErrorMessage("You are already on the top.");
				return;
			}
			var limit = false;
			for (var i = 1; i < levels; i++)
			{
				var newY = esUtils.GetUp(args.Player.TileX, y);
				if (newY == -1)
				{
					levels = i;
					limit = true;
					break;
				}
				y = newY;
			}

			var ePly = FindPlayer(args.Player.Index);
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.Tp;
			}
			args.Player.Teleport(args.Player.TileX * 16F, y * 16F);
			args.Player.SendSuccessMessage("Teleported you up {0} level(s).{1}", levels, limit ? " You cant go up any further." : string.Empty);
		}

		private void CmdDown(CommandArgs args)
		{
			var levels = 1;
			if (args.Parameters.Count > 0 && !int.TryParse(args.Parameters[0], out levels))
			{
				args.Player.SendErrorMessage("Usage: /down [No. levels]");
				return;
			}

			var y = esUtils.GetDown(args.Player.TileX, args.Player.TileY);
			if (y == -1)
			{
				args.Player.SendErrorMessage("You are already on the bottom.");
				return;
			}
			var limit = false;
			for (var i = 1; i < levels; i++)
			{
				var newY = esUtils.GetDown(args.Player.TileX, y);
				if (newY == -1)
				{
					levels = i;
					limit = true;
					break;
				}
				y = newY;
			}

			var ePly = FindPlayer(args.Player.Index);
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.Tp;
			}
			args.Player.Teleport(args.Player.TileX * 16F, y * 16F);
			args.Player.SendSuccessMessage("Teleported you down {0} level(s).{1}", levels, limit ? " You can't go down any further." : string.Empty);
		}
		#endregion

		#region Left & Right
		private void CmdLeft(CommandArgs args)
		{
		    var levels = 1;
			if (args.Parameters.Count > 0 && !int.TryParse(args.Parameters[0], out levels))
			{
				args.Player.SendErrorMessage("Usage: /left [No. times]");
				return;
			}

			var x = esUtils.GetLeft(args.Player.TileX, args.Player.TileY);
			if (x == -1)
			{
				args.Player.SendErrorMessage("You cannot go any further left.");
				return;
			}
			var limit = false;
			for (var i = 1; i < levels; i++)
			{
				var newX = esUtils.GetLeft(x, args.Player.TileY);
				if (newX == -1)
				{
					levels = i;
					limit = true;
					break;
				}
				x = newX;
			}

		    var ePly = FindPlayer(args.Player.Index);
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.Tp;
			}
			args.Player.Teleport(x * 16F, args.Player.TileY * 16F);
			args.Player.SendSuccessMessage("Teleported you to the left {0} time(s).{1}", levels, limit ? " You can't go any further." : string.Empty);
		}
		private void CmdRight(CommandArgs args)
		{
			var levels = 1;
			if (args.Parameters.Count > 0 && !int.TryParse(args.Parameters[0], out levels))
			{
				args.Player.SendErrorMessage("Usage: /right [No. times]");
				return;
			}

			var x = esUtils.GetRight(args.Player.TileX, args.Player.TileY);
			if (x == -1)
			{
				args.Player.SendErrorMessage("You cannot go any further right.");
				return;
			}
			var limit = false;
			for (var i = 1; i < levels; i++)
			{
				var newX = esUtils.GetRight(x, args.Player.TileY);
				if (newX == -1)
				{
					levels = i;
					limit = true;
					break;
				}
				x = newX;
			}

		    var ePly = FindPlayer(args.Player.Index);
			if (ePly != null)
			{
				ePly.LastBackX = args.Player.TileX;
				ePly.LastBackY = args.Player.TileY;
				ePly.LastBackAction = BackAction.Tp;
			}
			args.Player.Teleport(x * 16F, args.Player.TileY * 16F);
			args.Player.SendSuccessMessage("Teleported you to the right {0} time(s).{1}", levels, limit ? " You can't go any further." : string.Empty);
		}
		#endregion

		#region ptime
		private void CmdPTime(CommandArgs args)
		{
			if (!args.Player.Group.HasPermission("essentials.playertime.setother") && args.Parameters.Count != 1)
			{
				args.Player.SendErrorMessage("Usage: /ptime <day/night/noon/midnight/reset>");
				return;
			}
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendErrorMessage("Usage: /ptime <day/night/noon/midnight/reset> [player]");
				return;
			}

			var ply = args.Player;
			if (args.Player.Group.HasPermission("essentials.playertime.setother") && args.Parameters.Count == 2)
			{
				var found = TShock.Utils.FindPlayer(args.Parameters[1]);

                if (found.Count > 1)
                {
                    TShock.Utils.SendMultipleMatchError(args.Player, found.Select(p => p.Name));
                    return;
                }
			    if (found.Count == 0)
			    {
			        args.Player.SendErrorMessage("No players matched");
			    }

                ply = found[0];
			}

		    var ePly = FindPlayer(args.Player.Index);
			var time = args.Parameters[0].ToLower();

			switch (time)
			{
				case "day":
					{
						ePly.PtDay = true;
						ePly.PtTime = 150.0;
						ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
					}
					break;
				case "night":
					{
						ePly.PtDay = false;
						ePly.PtTime = 0.0;
						ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
					}
					break;
				case "noon":
					{
						ePly.PtDay = true;
						ePly.PtTime = 27000.0;
						ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
					}
					break;
				case "midnight":
					{
						ePly.PtDay = false;
						ePly.PtTime = 16200.0;
						ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
					}
					break;
				case "reset":
					{
						ePly.PtTime = -1.0;
						ply.SendData(PacketTypes.TimeSet, "", 0, Main.sunModY, Main.moonModY);
						args.Player.SendSuccessMessage("{0} time is the same as the server.", 
                            ply == args.Player ? "Your" : string.Concat(ply.Name, "'s"));
						if (ply != args.Player)
							ply.SendSuccessMessage("{0} Set your time to the server time.", args.Player.Name);
					}
					return;
				default:
					args.Player.SendErrorMessage("Usage: /ptime <day/night/dusk/noon/midnight/reset> [player]");
					return;
			}
			args.Player.SendSuccessMessage("Set {0} time to {1}.", 
                args.Player == ply ? "your" : string.Concat(ply.Name, "'s"), time);
			if (ply != args.Player)
				ply.SendSuccessMessage("{0} set your time to {1}.", args.Player.Name, time);
		}
		#endregion

		#region Ping
		private static void CmdPing(CommandArgs args)
		{
			args.Player.SendMessage("pong.", Color.White);
		}
		#endregion

		#region Sudo
		private void CmdSudo(CommandArgs args)
		{
			if (args.Parameters.Count < 2)
			{
				args.Player.SendErrorMessage("Usage: /sudo <player> <command>");
				return;
			}

			var found = TShock.Utils.FindPlayer(args.Parameters[0]);
			if (found.Count == 0)
			{
			    args.Player.SendErrorMessage("No players found");
				return;
			}
		    if (found.Count > 1)
		    {
		        TShock.Utils.SendMultipleMatchError(args.Player, found.Select(p => p.Name));
		        return;
		    }

			var ply = found[0];
			if (ply.Group.HasPermission("essentials.sudo.immune"))
			{
				args.Player.SendErrorMessage("You cannot force {0} to do a command.", ply.Name);
				return;
			}
			Group oldGroup = null;
			if (args.Player.Group.HasPermission("essentials.sudo.super"))
			{
				oldGroup = ply.Group;
				ply.Group = new SuperAdminGroup();
			}

			args.Parameters.RemoveAt(0);
			var command = string.Join(" ", args.Parameters);
			if (!command.StartsWith("/"))
				command = string.Concat("/", command);

			Commands.HandleCommand(ply, command);
			args.Player.SendSuccessMessage("Forced {0} to execute: {1}", ply.Name, command);

			if (oldGroup != null)
				ply.Group = oldGroup;
		}
		#endregion

		#region SocialSpy
		private void CmdSocialSpy(CommandArgs args)
		{
			var ePly = FindPlayer(args.Player.Index);

			ePly.SocialSpy = !ePly.SocialSpy;
			args.Player.SendSuccessMessage("Socialspy {0}abled.", ePly.SocialSpy ? "En" : "Dis");
		}
		#endregion

		#region Near
		private void CmdNear(CommandArgs args)
		{
			var plys = new Dictionary<string, int>();
			foreach (var ePly in _players)
			{
				if (ePly == null || ePly.Index == args.Player.Index) continue;
				var x = Math.Abs(args.Player.TileX - ePly.TSPlayer.TileX);
				var y = Math.Abs(args.Player.TileY - ePly.TSPlayer.TileY);
				var h = (int)Math.Sqrt((Math.Pow(x, 2) + Math.Pow(y, 2)));
				plys.Add(ePly.TSPlayer.Name, h);
			}
			if (plys.Count == 0)
			{
				args.Player.SendSuccessMessage("No players found.");
				return;
			}
			var names = new List<string>();
			plys.OrderBy(pair => pair.Value).ForEach(pair => names.Add(pair.Key));
			var results = new List<string>();
			var line = new StringBuilder();
			var added = 0;
			foreach (var n in names)
			{
			    line.Append(line.Length == 0 
                    ? string.Format("{0}({1}m)", n, plys[n]) 
                    : string.Format(", {0}({1}m)", n, plys[n]));
			    added++;
			    if (added != 5) continue;
			    results.Add(line.ToString());
			    line.Clear();
			    added = 0;
			}
            if (results.Count <= 6)
			{
				args.Player.SendInfoMessage("Nearby Players:");
                foreach (var res in results)
				{
					args.Player.SendSuccessMessage(res);
				}
			}
			else
			{
				var page = 1;
				if (args.Parameters.Count > 0 && !int.TryParse(args.Parameters[0], out page))
					page = 1;
				page--;
				const int pagelimit = 6;

                var pagecount = results.Count / pagelimit;
				if (page > pagecount)
				{
					args.Player.SendErrorMessage("Page number exceeds pages ({0}/{1}).", page + 1, pagecount + 1);
					return;
				}

				args.Player.SendInfoMessage("Nearby Players - Page {0} of {1} | /near [page]", page + 1, pagecount + 1);
                for (var i = (page * pagelimit); (i < ((page * pagelimit) + pagelimit)) && i < results.Count; i++)
                    args.Player.SendSuccessMessage(results[i]);
			}
		}
		#endregion

		#region Nick
		private void CmdNick(CommandArgs args)
		{
			if (args.Parameters.Count != 1 && !args.Player.Group.HasPermission("essentials.nick.setother"))
			{
				args.Player.SendErrorMessage("Usage: /nick <nickname / off>");
				return;
			}
			if ((args.Parameters.Count < 1 || args.Parameters.Count > 2) && args.Player.Group.HasPermission("essentials.nick.setother"))
			{
				args.Player.SendErrorMessage("Usage: /nick [player] <nickname / off>");
				return;
			}

			var ply = args.Player;
			if (args.Parameters.Count > 1 && args.Player.Group.HasPermission("essentials.nick.setother"))
			{
				var found = TShock.Utils.FindPlayer(args.Parameters[0]);
				if (found.Count == 0)
				{
					args.Player.SendErrorMessage("No players matched.");
					return;
				}
			    if (found.Count > 1)
			    {
			        TShock.Utils.SendMultipleMatchError(args.Player, found.Select(p => p.Name));
			    }
				ply = found[0];
			}

			var eNickPly = FindPlayer(args.Player.Index);

			var self = ply == args.Player;

			var nickname = args.Parameters[args.Parameters.Count - 1];

			if (nickname.ToLower() == "off")
			{
				if (eNickPly.HasNickName)
				{
					EsSql.RemoveNickname(ply.Name);

					eNickPly.HasNickName = false;
					eNickPly.Nickname = string.Empty;

					args.Player.SendSuccessMessage("Removed {0} nickname.",
                        self ? "your" : string.Concat(ply.Name, "'s"));
					if (!self)
						ply.SendSuccessMessage("Your nickname was removed by {0}.", args.Player.Name);
				}
				else
				{
					args.Player.SendErrorMessage("{0} not have a nickname to remove.", 
                        self ? "You do" : string.Concat(ply.Name, " does"));
				}
				return;
			}

			if (!eNickPly.HasNickName)
			{
				eNickPly.OriginalName = ply.Name;
				eNickPly.HasNickName = true;
			}

			eNickPly.Nickname = nickname;

			string curNickname;
			if (EsSql.GetNickname(ply.Name, out curNickname))
				EsSql.UpdateNickname(ply.Name, nickname);
			else
				EsSql.AddNickname(ply.Name, nickname);

			args.Player.SendSuccessMessage("Set {0} nickname to '{1}'.",
                self ? "your" : eNickPly.OriginalName, nickname);
			if (!self)
				ply.SendSuccessMessage("{0} set your nickname to '{1}'.", args.Player.Name, nickname);
		}
		#endregion

		#region RealName
		private void CmdRealName(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Usage: /realname <player/-all>");
				return;
			}
			var search = args.Parameters[0].ToLower();
			if (search == "-all")
			{
				var nicks = (from player in _players where player != null 
                                 && player.HasNickName 
                             select string.Concat(_config.PrefixNicknamesWith, player.Nickname, 
                             "(", player.OriginalName, ")")).ToList();

			    if (nicks.Count < 1)
					args.Player.SendErrorMessage("No players online have nicknames.");
				else
                    args.Player.SendSuccessMessage(string.Join(", ", nicks));
				return;
			}
			if (search.StartsWith(_config.PrefixNicknamesWith))
				search = search.Remove(0, _config.PrefixNicknamesWith.Length);

			var found = new List<EsPlayer>();
			foreach (var player in _players.Where(player => player != null && player.HasNickName))
			{
			    if (player.Nickname.ToLower() == search)
			    {
			        found = new List<EsPlayer> { player };
			        break;
			    }
			    if (player.Nickname.ToLower().Contains(search))
			        found.Add(player);
			}
			if (found.Count > 1)
			{
			    TShock.Utils.SendMultipleMatchError(args.Player, found.Select(p => p.TSPlayer.Name));
                return;
			}

			var ply = found[0];

			args.Player.SendSuccessMessage("The user '{0}' has the nickname '{1}'.", ply.OriginalName, ply.Nickname);

		}
		#endregion

		#region Exact Time
		private void CmdExactTime(CommandArgs args)
		{
			if (args.Parameters.Count != 1 || !args.Parameters[0].Contains(':'))
			{
				args.Player.SendErrorMessage("Usage: /etime <hours>:<minutes>");
				return;
			}

			var split = args.Parameters[0].Split(':');
			var sHours = split[0];
			var sMinutes = split[1];

			var pm = false;
		    int hours;
		    int minutes;
			if (!int.TryParse(sHours, out hours) || !int.TryParse(sMinutes, out minutes))
			{
				args.Player.SendErrorMessage("Usage: /etime <hours>:<minutes>");
				return;
			}
			if (hours < 0 || hours > 24)
			{
				args.Player.SendErrorMessage("Hours is out of range.");
				return;
			}
			if (minutes < 0 || minutes > 59)
			{
				args.Player.SendErrorMessage("Minutes is out of range.");
				return;
			}

			var tfHour = hours;

			if (tfHour == 24 || tfHour == 0)
			{
				hours = 12;
			}
			if (tfHour >= 12 && tfHour < 24)
			{
				pm = true;
				if (hours > 12)
					hours -= 12;
			}

			var hour = hours;

			hours = tfHour;
			if (hours == 24)
				hours = 0;

			var timeMinutes = minutes / 60.0;
			var mainTime = timeMinutes + hours;

			if (mainTime >= 4.5 && mainTime < 24)
				mainTime -= 24.0;

			mainTime = mainTime + 19.5;
			mainTime = mainTime / 24.0 * 86400.0;

		    var day = 
                (!pm && ((hour > 4 || (hour == 4 && minutes >= 30))) && hour < 12) ||
                (pm && ((hour < 7 || (hour == 7 && minutes < 30)) || hour == 12));

		    if (!day)
				mainTime -= 54000.0;

			TSPlayer.Server.SetTime(day, mainTime);

			var min = minutes.ToString(CultureInfo.InvariantCulture);
			if (minutes < 10)
				min = "0" + minutes;
			TSPlayer.All.SendSuccessMessage("{0} set time to {1}:{2} {3}.", args.Player.Name, hour, min, pm ? "PM" : "AM");
		}
		#endregion

		#region Force Login
		private void CmdForceLogin(CommandArgs args)
		{
			if (args.Parameters.Count < 1 || args.Parameters.Count > 2)
			{
				args.Player.SendErrorMessage("Usage: /forcelogin <account> [player]");
				return;
			}
			var user = TShock.Users.GetUserByName(args.Parameters[0]);
			if (user == null)
			{
				args.Player.SendErrorMessage("User {0} does not exist.", args.Parameters[0]);
				return;
			}
			var group = TShock.Utils.GetGroup(user.Group);

			var found = new List<TSPlayer> { args.Player };
			if (args.Parameters.Count == 2)
			{
                found = TShock.Utils.FindPlayer(args.Parameters[1]);
			    if (found.Count > 1)
			    {
			        TShock.Utils.SendMultipleMatchError(args.Player, found.Select(p => p.Name));
			        return;
			    }
			    if (found.Count == 0)
			    {
			        args.Player.SendErrorMessage("No players found");
			        return;
			    }
			}

            var player = found[0];
			player.Group = group;
			player.UserAccountName = user.Name;
			player.UserID = TShock.Users.GetUserID(player.UserAccountName);
			player.IsLoggedIn = true;
			player.IgnoreActionsForInventory = "none";

			player.SendSuccessMessage("{0} in as {1}.", player != args.Player ? string.Concat(args.Player.Name, " Logged you") : "Logged", user.Name);
			if (player != args.Player)
				args.Player.SendSuccessMessage("Logged {0} in as {1}.", player.Name, user.Name);
			Log.ConsoleInfo(string.Format("{0} forced logged in {1}as user: {2}.", args.Player.Name, args.Player != player ? string.Concat(player.Name, " ") : string.Empty, user.Name));
		}
		#endregion

		#region Invsee
		private void CmdInvSee(CommandArgs args)
		{
			if (!TShock.Config.ServerSideCharacter)
			{
				args.Player.SendErrorMessage("Server Side Character must be enabled.");
				return;
			}
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Usage: /invsee <player name / -restore>");
				return;
			}
		    var ePly = FindPlayer(args.Player.Index);
			if (args.Parameters.Count == 1 && args.Parameters[0].ToLower() == "-restore")
			{
				if (ePly.InvSee == null)
				{
					args.Player.SendErrorMessage("You are not viewing another player's inventory.");
					return;
				}
				ePly.InvSee.RestoreCharacter(args.Player);
				ePly.InvSee = null;
				args.Player.SendSuccessMessage("Restored your inventory.");
				return;
			}
			var found = TShock.Utils.FindPlayer(string.Join(" ", args.Parameters));
			if (found.Count > 1)
			{
			    TShock.Utils.SendMultipleMatchError(args.Player, found.Select(p => p.Name));
				return;
			}
		    if (found.Count == 0)
		    {
		        args.Player.SendErrorMessage("No players matched");
		        return;
		    }

			var playerChar = new PlayerData(args.Player);
			playerChar.CopyCharacter(args.Player);
			if (ePly.InvSee == null)
				ePly.InvSee = playerChar;

			var copyChar = new PlayerData(found[0]);
			copyChar.CopyCharacter(found[0]);
			copyChar.health = playerChar.health;
			copyChar.maxHealth = playerChar.maxHealth;
			copyChar.mana = playerChar.mana;
			copyChar.maxMana = playerChar.maxMana;
			copyChar.spawnX = playerChar.spawnX;
			copyChar.spawnY = playerChar.spawnY;
			copyChar.RestoreCharacter(args.Player);

			args.Player.SendSuccessMessage("Copied {0}'s inventory", found[0].Name);
		}
		#endregion

		#region Whois
		private void CmdWhoIs(CommandArgs args)
		{
			if (args.Parameters.Count < 1)
			{
				args.Player.SendErrorMessage("Usage: /whois [-eui] player name");
				args.Player.SendErrorMessage("Flags: ");
				args.Player.SendErrorMessage("-e Exact name search");
				args.Player.SendErrorMessage("-u User ID search");
				args.Player.SendErrorMessage("-i IP address search");
				return;
			}
			var type = "\b";
			string query;
			var results = new List<UserObj>();
			switch (args.Parameters[0].ToUpper())
			{
				case "-E":
					{
						type = "Exact name";
						args.Parameters.RemoveAt(0);
						query = string.Join(" ", args.Parameters);
                        var user = TShock.Users.GetUserByName(query);
                        results.Add(new UserObj(-1, user.Name, user.KnownIps.Split(',')[0]));
					}
					break;
				case "-U":
					{
						type = "User ID";
						query = args.Parameters[1];
						int id;
						if (!int.TryParse(query, out id))
						{
							args.Player.SendWarningMessage("ID must be an integer.");
							return;
						}
					    results.AddRange(from tPly in TShock.Players 
                                         where tPly.UserID == id select new UserObj(id, tPly.Name, tPly.IP));
					}
					break;
				case "-I":
					{
						type = "IP";
						query = args.Parameters[1];
                        var ipUser = TShock.Users.GetUsers().Find(user => user.KnownIps.Contains(query));
                        results.Add(new UserObj(-1, ipUser.Name, ipUser.KnownIps.Split(',')[0]));
					}
					break;
				default:
					{
						query = string.Join(" ", args.Parameters);
                        //Results = TShock.Utils.FindPlayer(Query);
					}
					break;
			}
			if (results.Count < 1)
			{
				args.Player.SendErrorMessage("No matches for {0}", query);
			    return;
			}
			if (results.Count == 1)
			{
				args.Player.SendInfoMessage("Details of {0} search: {0}", type, query);

                if (TShock.Players.Contains(TShock.Utils.FindPlayer(results[0].name)[0]))
                {
                    var ply = TShock.Utils.FindPlayer(results[0].name)[0];
                    var ePly = FindPlayer(ply.Index);
                    args.Player.SendSuccessMessage("UserID: {0}, Registered Name: {1}",
                    results[0].userId, results[0].name);

                    if (ePly.HasNickName)
                        args.Player.SendSuccessMessage("Nickname: {0}{1}", _config.PrefixNicknamesWith,
                            ePly.Nickname);

                    args.Player.SendSuccessMessage("IP: {0}", results[0].ip);
                }

                else
                {
                    args.Player.SendSuccessMessage("Registered Name: {0}", results[0].name);
                    args.Player.SendSuccessMessage("IP: {0}", results[0].ip);                    
                }
				
			}
			else
			{
				args.Player.SendWarningMessage("Matches: ({0}):", results.Count);

                results.ForEach(r =>
                {
                    TShock.Players.ForEach(pl =>
                    {
                        if (r.name != pl.Name) return;
                        args.Player.SendInfoMessage("Online matches:");
                        args.Player.SendSuccessMessage("({0}){1} (IP:{3})", pl.UserID, pl.Name, pl.IP);
                        results.RemoveAll(result => result == r);
                    });

                    args.Player.SendInfoMessage("Offline matches:");
                    args.Player.SendSuccessMessage("{1} (IP:{3})", r.name, r.ip);
                });
			}
		}
		#endregion

	    private EsPlayer FindPlayer(int index)
	    {
	        return _players.Find(p => p.Index == index);
	    }
	}

    public static class Extensions
    {
        public static T AddObj<T>(this List<T> list, T generic)
        {
            list.Add(generic);
            return generic;
        }
    }

    public class UserObj
    {
        public readonly int userId;
        public readonly string name;
        public readonly string ip;

        public UserObj(int u, string n, string i)
        {
            userId = u;
            name = n;
            ip = i;
        }
    }
}
