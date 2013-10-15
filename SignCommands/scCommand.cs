using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using TShockAPI;

namespace SignCommands
{
	public class scCommand
	{
		public string command { get; set; }
		public List<string> args { get; set; }

		public scCommand(scSign parent, string command, List<string> args)
		{
			this.command = command.ToLower();
			this.args = args;
		}

		#region AmIValid
		public bool AmIValid()
		{
			switch (command)
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
		public void ExecuteCommand(scPlayer sPly)
		{
			switch (command)
			{
				case "time":
					CMDtime(sPly, args);
					break;
				case "heal":
					CMDheal(sPly, args);
					break;
				case "show":
					CMDshow(sPly, args);
					break;
				case "damage":
					CMDdamage(sPly, args);
					break;
				case "boss":
					CMDboss(sPly, args);
					break;
				case "spawnmob":
					CMDspawnmob(sPly, args);
					break;
				case "warp":
					CMDwarp(sPly, args);
					break;
				case "item":
					CMDitem(sPly, args);
					break;
				case "buff":
					CMDbuff(sPly, args);
					break;
				case "command":
					CMDcommand(sPly, args);
					break;
			}
		}
		#endregion

		#region CMDtime
		public static void CMDtime(scPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			string time = args[0].ToLower();
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
		public static void CMDheal(scPlayer sPly, List<string> args)
		{
			Item heart = TShock.Utils.GetItemById(58);
			Item star = TShock.Utils.GetItemById(184);
			for (int ic = 0; ic < 20; ic++)
				sPly.TSPlayer.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
			for (int ic = 0; ic < 10; ic++)
				sPly.TSPlayer.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
		}
		#endregion

		#region CMDshow
		public static void CMDshow(scPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			string file = args[0].ToLower();
			switch (file)
			{
				case "motd":
					TShock.Utils.ShowFileToUser(sPly.TSPlayer, "motd.txt");
					break;
				case "rules":
					TShock.Utils.ShowFileToUser(sPly.TSPlayer, "rules.txt");
					break;
				case "playing":
					Commands.HandleCommand(sPly.TSPlayer, "/playing");
					break;
				default:
					{
						if (File.Exists(args[0]))
							TShock.Utils.ShowFileToUser(sPly.TSPlayer, args[0]);
						else
							sPly.TSPlayer.SendErrorMessage("Could not find file.");
					}
					break;
			}
		}
		#endregion

		#region CMDdamage
		public static void CMDdamage(scPlayer sPly, List<string> args)
		{
			int amount = 10;
			if (args.Count > 0)
				int.TryParse(args[0], out amount);
			if (amount < 1)
				amount = 10;
			sPly.TSPlayer.DamagePlayer(amount);
		}
		#endregion

		#region CMDboss
		public static void CMDboss(scPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			string boss = args[0].ToLower();

			int amount = 1;
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
						for (int i = 0; i < amount; i++)
						{
							if (Main.wof >= 0 || (sPly.TSPlayer.Y / 16f < (float)(Main.maxTilesY - 205)))
							{
								sPly.TSPlayer.SendErrorMessage("Can't spawn Wall of Flesh.");
							}
							NPC.SpawnWOF(new Vector2(sPly.TSPlayer.X, sPly.TSPlayer.Y));
						}
					}
					break;
				case "twins":
					{
						NPC retinazer = TShock.Utils.GetNPCById(125);
						NPC spaz = TShock.Utils.GetNPCById(126);
						TSPlayer.Server.SetTime(false, 0.0);
						for (int i = 0; i < amount; i++)
						{
							TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, 1, sPly.TSPlayer.TileX, sPly.TSPlayer.TileY);
							TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, 1, sPly.TSPlayer.TileX, sPly.TSPlayer.TileY);
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
				for (int i = 0; i < amount; i++)
					TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, sPly.TSPlayer.TileX, sPly.TSPlayer.TileY);
		}
		#endregion

		#region CMDspawnmob
		public static void CMDspawnmob(scPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			string mob = args[0];

			int amount = 1;
			if (args.Count > 1)
				int.TryParse(args[1], out amount);
			if (amount < 1)
				amount = 1;

			amount = Math.Min(amount, Main.maxNPCs);

			var npc = TShock.Utils.GetNPCByIdOrName(mob);
			if (npc.Count == 1 && npc[0].type >= 1 && npc[0].type < Main.maxNPCTypes && npc[0].type != 113)
				TSPlayer.Server.SpawnNPC(npc[0].type, npc[0].name, amount, sPly.TSPlayer.TileX, sPly.TSPlayer.TileY, 50, 20);
		}
		#endregion

		#region CMDwarp
		public static void CMDwarp(scPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;

			string WarpName = args[0];

			var Warp = TShock.Warps.FindWarp(WarpName);

			if (Warp != null && Warp.WarpName != "" && Warp.WarpPos.X > 0 && Warp.WarpPos.Y > 0 && Warp.WorldWarpID != "")
				sPly.TSPlayer.Teleport(Warp.WarpPos.X * 16F, Warp.WarpPos.Y * 16F);
		}
		#endregion

		#region CMDitem
		public static void CMDitem(scPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			string itemname = args[0];

			int amount = 1;
			if (args.Count > 1)
				int.TryParse(args[1], out amount);
			if (amount < 1)
				amount = 1;

			int prefix = 0;
			if (args.Count > 2)
				int.TryParse(args[2], out prefix);
			if (prefix < 0)
				prefix = 0;

			List<Item> items = TShock.Utils.GetItemByIdOrName(itemname);
			if (items.Count == 1 && items[0].type >= 1 && items[0].type < Main.maxItemTypes)
			{
				Item item = items[0];
				if (sPly.TSPlayer.InventorySlotAvailable || item.name.Contains("Coin"))
				{
					if (amount == 0 || amount > item.maxStack)
						amount = item.maxStack;
					sPly.TSPlayer.GiveItemCheck(item.type, item.name, item.width, item.height, amount, prefix);
				}
			}
		}
		#endregion

		#region CMDbuff
		public static void CMDbuff(scPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			string buffname = args[0];

			int duration = 60;
			if (args.Count > 1)
				int.TryParse(args[1], out duration);

			int buffid = -1;
			if (!int.TryParse(buffname, out buffid))
			{
				List<int> buffs = TShock.Utils.GetBuffByName(buffname);
				if (buffs.Count == 1)
					buffid = buffs[0];
				else
					return;
			}
			if (buffid > 0 && buffid < Main.maxBuffs)
			{
				if (duration < 1 || duration > short.MaxValue)
					duration = 60;
				sPly.TSPlayer.SetBuff(buffid, duration * 60);
			}
		}
		#endregion

		#region CMDcommand
		public static void CMDcommand(scPlayer sPly, List<string> args)
		{
			if (args.Count < 1) return;
			string command = args[0];
			Commands.HandleCommand(sPly.TSPlayer, command);
		}
		#endregion
	}
}
