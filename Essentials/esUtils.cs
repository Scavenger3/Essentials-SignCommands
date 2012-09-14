using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Terraria;
using TShockAPI;

namespace Essentials
{
	public static class esUtils
	{
		/* GET esPlayer! */
		public static esPlayer GetesPlayerByID(int id)
		{
			esPlayer player = null;
			foreach (esPlayer ply in Essentials.esPlayers)
				if (ply.Index == id)
					return ply;
			return player;
		}

		/* Search IDs */
		public static List<Item> GetItemByName(string name)
		{
			var found = new List<Item>();
			for (int i = -24; i < Main.maxItemTypes; i++)
			{
				try
				{
					Item item = new Item();
					item.netDefaults(i);
					if (item.name.ToLower().Contains(name.ToLower()))
						found.Add(item);
				}
				catch { }
			}
			return found;
		}

		public static List<NPC> GetNPCByName(string name)
		{
			var found = new List<NPC>();
			for (int i = 1; i < Main.maxNPCTypes; i++)
			{
				NPC npc = new NPC();
				npc.netDefaults(i);
				if (npc.name.ToLower().Contains(name.ToLower()))
					found.Add(npc);
			}
			return found;
		}
		public static void BCsearchitem(CommandArgs args, List<Item> list, int page)
		{
			args.Player.SendMessage("Item Search:", Color.Yellow);
			var sb = new StringBuilder();
			if (list.Count > (8 * (page - 1)))
			{
				for (int j = (8 * (page - 1)); j < (8 * page); j++)
				{
					if (sb.Length != 0)
						sb.Append(" | ");
					sb.Append(list[j].netID).Append(": ").Append(list[j].name);
					if (j == list.Count - 1)
					{
						args.Player.SendMessage(sb.ToString(), Color.MediumSeaGreen);
						break;
					}
					if ((j + 1) % 2 == 0)
					{
						args.Player.SendMessage(sb.ToString(), Color.MediumSeaGreen);
						sb.Clear();
					}
				}
			}
			if (list.Count > (8 * page))
			{
				args.Player.SendMessage(string.Format("Type /spage {0} for more Results.", (page + 1)), Color.Yellow);
			}
		}

		public static void BCsearchnpc(CommandArgs args, List<NPC> list, int page)
		{
			args.Player.SendMessage("NPC Search:", Color.Yellow);
			var sb = new StringBuilder();
			if (list.Count > (8 * (page - 1)))
			{
				for (int j = (8 * (page - 1)); j < (8 * page); j++)
				{
					if (sb.Length != 0)
						sb.Append(" | ");
					sb.Append(list[j].netID).Append(": ").Append(list[j].name);
					if (j == list.Count - 1)
					{
						args.Player.SendMessage(sb.ToString(), Color.MediumSeaGreen);
						break;
					}
					if ((j + 1) % 2 == 0)
					{
						args.Player.SendMessage(sb.ToString(), Color.MediumSeaGreen);
						sb.Clear();
					}
				}
			}
			if (list.Count > (8 * page))
			{
				args.Player.SendMessage(string.Format("Type /spage {0} for more Results.", (page + 1)), Color.Yellow);
			}
		}

		/* Sethome - how many homes can a user set. */
		public static int NoOfHomesCanSet(TSPlayer ply)
		{
			List<string> permissions = ply.Group.TotalPermissions;
			List<string> negated = ply.Group.negatedpermissions;
			List<int> count = new List<int>();
			if (ply.Group.HasPermission("essentials.home.*") || ply.Group.HasPermission("essentials.*") || ply.Group.HasPermission("*"))
				return -1;
			foreach (string p in permissions)
			{
				if (p.StartsWith("essentials.home.") && p != "essentials.home." && !negated.Contains(p))
				{
					int x = 0;
					if (int.TryParse(p.Remove(0, 16), out x))
						count.Add(x);
				}
			}

			if (count.Count == 1)
				return count[0];
			else if (count.Count > 1)
			{
				int top = 1;
				foreach (int v in count)
					if (v > top)
						top = v;
				return top;
			}
			return 1;
		}

		/* Sethome - get next home */
		public static string NextHome(List<string> homes)
		{
			List<int> intHomes = new List<int>();
			foreach (string h in homes)
			{
				int x = -1;
				if (int.TryParse(h, out x))
					intHomes.Add(x);
			}
			if (intHomes.Count < 1)
				return "1";
			int top = 1;
			foreach (int v in intHomes)
				if (v >= top)
					top = v + 1;
			return top.ToString();
		}

		/* Top, Up and Down Methods */
		public static int GetTop(int TileX)
		{
			for (int y = 0; y < Main.maxTilesY; y++)
			{
				if (WorldGen.SolidTile(TileX, y))
					return y - 1;
			}
			return -1;
		}

		public static int GetUp(int TileX, int TileY)
		{
			for (int y = TileY - 2; y > 0; y--)
			{
				if (!WorldGen.SolidTile(TileX, y))
				{
					if (WorldGen.SolidTile(TileX, y + 1) && !WorldGen.SolidTile(TileX, y - 1) && !WorldGen.SolidTile(TileX, y - 2))
					{
						return y;
					}
				}
			}
			return -1;
		}

		public static int GetDown(int TileX, int TileY)
		{
			for (int y = TileY + 4; y < Main.maxTilesY; y++)
			{
				if (!WorldGen.SolidTile(TileX, y))
				{
					if (WorldGen.SolidTile(TileX, y + 1) && !WorldGen.SolidTile(TileX, y - 1) && !WorldGen.SolidTile(TileX, y - 2))
					{
						return y;
					}
				}
			}
			return -1;
		}
	}
}
