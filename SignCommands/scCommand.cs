using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using TShockAPI;

namespace SignCommands
{
    public class SignCommand : Command
    {
        public int coolDown;
        public SignCommand(int coolDown, List<string> permissions, CommandDelegate cmd, params string[] names)
            : base(permissions, cmd, names)
        {
            this.coolDown = coolDown;
        }
    }


    public class ScCommand
	{
		public string Command { get; private set; }
		public List<string> Args { get; private set; }

		public ScCommand(string command, List<string> args)
		{
			Command = command.ToLower();
			Args = args;
		}

		#region AmIValid
		public bool AmIValid()
		{
			switch (Command)
			{
				case "time":
				case "heal":
				case "show":
				case "damage":
				case "boss":
				case "spawnmob":
				case "warp":
				case "item":
				case "buff":
				case "command":
					return true;
				default:
					return false;
			}
		}
		#endregion

		#region ExecuteCommand
		public void ExecuteCommand(ScPlayer sPly)
		{
			switch (Command)
			{
				case "time":
					CmdTime(sPly, Args);
					break;
				case "heal":
					CmdHeal(sPly, Args);
					break;
				case "show":
					CmdShow(sPly, Args);
					break;
				case "damage":
					CmdDamage(sPly, Args);
					break;
				case "boss":
					CmdBoss(sPly, Args);
					break;
				case "spawnmob":
					CmdSpawnMob(sPly, Args);
					break;
				case "warp":
					CmdWarp(sPly, Args);
					break;
				case "item":
					CmdItem(sPly, Args);
					break;
				case "buff":
					CmdBuff(sPly, Args);
					break;
				case "command":
					CmdCommand(sPly, Args);
					break;
			}
		}
		#endregion

		#region CMDtime

	    private static void CmdTime(ScPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			var time = args[0].ToLower();
			switch (time)
			{
				case "day":
					TSPlayer.Server.SetTime(true, 150.0);
					break;
				case "night":
					TSPlayer.Server.SetTime(false, 0.0);
					break;
				case "dusk":
					TSPlayer.Server.SetTime(false, 0.0);
					break;
				case "noon":
					TSPlayer.Server.SetTime(true, 27000.0);
					break;
				case "midnight":
					TSPlayer.Server.SetTime(false, 16200.0);
					break;
				case "fullmoon":
					TSPlayer.Server.SetFullMoon(true);
					break;
				case "bloodmoon":
					TSPlayer.Server.SetBloodMoon(true);
					break;
			}
		}
		#endregion

		#region CMDheal

	    private static void CmdHeal(ScPlayer sPly, List<string> args)
	    {
	        sPly.TsPlayer.Heal(sPly.TsPlayer.TPlayer.statLifeMax);
	    }
		#endregion

		#region CMDshow

	    private static void CmdShow(ScPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			string file = args[0].ToLower();
			switch (file)
			{
				case "motd":
					TShock.Utils.ShowFileToUser(sPly.TsPlayer, "motd.txt");
					break;
				case "rules":
					TShock.Utils.ShowFileToUser(sPly.TsPlayer, "rules.txt");
					break;
				case "playing":
					Commands.HandleCommand(sPly.TsPlayer, "/playing");
					break;
				default:
					{
						if (File.Exists(args[0]))
							TShock.Utils.ShowFileToUser(sPly.TsPlayer, args[0]);
						else
							sPly.TsPlayer.SendErrorMessage("Could not find file.");
					}
					break;
			}
		}
		#endregion

		#region CMDdamage

	    private static void CmdDamage(ScPlayer sPly, List<string> args)
		{
			var amount = 10;
			if (args.Count > 0)
				int.TryParse(args[0], out amount);
			if (amount < 1)
				amount = 10;
			sPly.TsPlayer.DamagePlayer(amount);
		}
		#endregion

		#region CMDboss

	    private static void CmdBoss(ScPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			var boss = args[0].ToLower();

			var amount = 1;
			if (args.Count > 1)
				int.TryParse(args[1], out amount);

			amount = Math.Min(amount, Main.maxNPCs);
			if (amount < 1)
				amount = 1;

			NPC npc = null;
			switch (boss)
			{
				case "eater":
					npc = TShock.Utils.GetNPCById(13);
					break;
				case "eye":
					{
						npc = TShock.Utils.GetNPCById(4);
						TSPlayer.Server.SetTime(false, 0.0);
					}
					break;
				case "king":
					npc = TShock.Utils.GetNPCById(50);
					break;
				case "skeletron":
					{
						npc = TShock.Utils.GetNPCById(35);
						TSPlayer.Server.SetTime(false, 0.0);
					}
					break;
				case "wof":
					{
						for (var i = 0; i < amount; i++)
						{
							if (Main.wof >= 0 || (sPly.TsPlayer.Y / 16f < (Main.maxTilesY - 205)))
								sPly.TsPlayer.SendErrorMessage("Can't spawn Wall of Flesh.");
							NPC.SpawnWOF(new Vector2(sPly.TsPlayer.X, sPly.TsPlayer.Y));
						}
					}
					break;
				case "twins":
					{
						var retinazer = TShock.Utils.GetNPCById(125);
						var spaz = TShock.Utils.GetNPCById(126);
						TSPlayer.Server.SetTime(false, 0.0);
						for (var i = 0; i < amount; i++)
						{
							TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, 1, sPly.TsPlayer.TileX, sPly.TsPlayer.TileY);
							TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, 1, sPly.TsPlayer.TileX, sPly.TsPlayer.TileY);
						}
					}
					break;
				case "destroyer":
					{
						npc = TShock.Utils.GetNPCById(134);
						TSPlayer.Server.SetTime(false, 0.0);
					}
					break;
				case "skeletronprime":
					{
						npc = TShock.Utils.GetNPCById(127);
						TSPlayer.Server.SetTime(false, 0.0);
					}
					break;
			}

			if (npc != null)
				for (var i = 0; i < amount; i++)
					TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, sPly.TsPlayer.TileX, sPly.TsPlayer.TileY);
		}
		#endregion

		#region CMDspawnmob

	    private static void CmdSpawnMob(ScPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			var mob = args[0];

			var amount = 1;
			if (args.Count > 1)
				int.TryParse(args[1], out amount);
			if (amount < 1)
				amount = 1;

			amount = Math.Min(amount, Main.maxNPCs);

			var npc = TShock.Utils.GetNPCByIdOrName(mob);
			if (npc.Count == 1 && npc[0].type >= 1 && npc[0].type < Main.maxNPCTypes && npc[0].type != 113)
				TSPlayer.Server.SpawnNPC(npc[0].type, npc[0].name, amount, sPly.TsPlayer.TileX, sPly.TsPlayer.TileY, 50, 20);
		}
		#endregion

		#region CMDwarp

	    private static void CmdWarp(ScPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;

			var warpName = args[0];

            var warp = TShock.Warps.Find(warpName);

			if (warp != null && warp.Name != "" && warp.Position.X > 0 && warp.Position.Y > 0)
				sPly.TsPlayer.Teleport(warp.Position.X * 16F, warp.Position.Y * 16F);
		}
		#endregion

		#region CMDitem

	    private static void CmdItem(ScPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			var itemname = args[0];

			var amount = 1;
			if (args.Count > 1)
				int.TryParse(args[1], out amount);
			if (amount < 1)
				amount = 1;

			var prefix = 0;
			if (args.Count > 2)
				int.TryParse(args[2], out prefix);
			if (prefix < 0)
				prefix = 0;

			var items = TShock.Utils.GetItemByIdOrName(itemname);
			if (items.Count == 1 && items[0].type >= 1 && items[0].type < Main.maxItemTypes)
			{
				var item = items[0];
				if (sPly.TsPlayer.InventorySlotAvailable || item.name.Contains("Coin"))
				{
					if (amount == 0 || amount > item.maxStack)
						amount = item.maxStack;
					sPly.TsPlayer.GiveItemCheck(item.type, item.name, item.width, item.height, amount, prefix);
				}
			}
		}
		#endregion

		#region CMDbuff

	    private static void CmdBuff(ScPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
		    var buffname = args[0];

			var duration = 60;
			if (args.Count > 1)
				int.TryParse(args[1], out duration);

		    int buffid;
			if (!int.TryParse(buffname, out buffid))
			{
				var buffs = TShock.Utils.GetBuffByName(buffname);
				if (buffs.Count == 1)
					buffid = buffs[0];
				else
					return;
			}
			if (buffid > 0 && buffid < Main.maxBuffTypes)
			{
				if (duration < 1 || duration > short.MaxValue)
					duration = 60;
				sPly.TsPlayer.SetBuff(buffid, duration * 60);
			}
		}
		#endregion

		#region CMDcommand

	    private static void CmdCommand(ScPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			var command = args[0];
			Commands.HandleCommand(sPly.TsPlayer, command);
		}
		#endregion
	}
}
