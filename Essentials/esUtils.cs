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
		/* Search IDs */
		public static List<object> ItemIdSearch(string search)
		{
			try
			{
				var found = new List<object>();
				for (int i = -24; i < Main.maxItemTypes; i++)
				{
					Item item = new Item();
					item.netDefaults(i);
					if (item.name.ToLower().Contains(search.ToLower()))
						found.Add(item);
				}
				return found;
			}
			catch { return new List<object>(); }
		}
		public static List<object> NPCIdSearch(string search)
		{
			try
			{
				var found = new List<object>();
				for (int i = -17; i < Main.maxNPCTypes; i++)
				{
					NPC npc = new NPC();
					npc.netDefaults(i);
					if (npc.name.ToLower().Contains(search.ToLower()))
						found.Add(npc);
				}
				return found;
			}
			catch { return new List<object>(); }
		}
		public static void DisplaySearchResults(TSPlayer Player, List<object> Results, int Page)
		{
			if (Results[0] is Item)
				Player.SendInfoMessage("Item Search:");
			else if (Results[0] is NPC)
				Player.SendInfoMessage("NPC Search:");
			var sb = new StringBuilder();
			if (Results.Count > (8 * (Page - 1)))
			{
				for (int j = (8 * (Page - 1)); j < (8 * Page); j++)
				{
					if (sb.Length != 0)
						sb.Append(" | ");
					if (Results[j] is Item)
						sb.Append(((Item)Results[j]).netID).Append(": ").Append(((Item)Results[j]).name);
					else if (Results[j] is NPC)
						sb.Append(((NPC)Results[j]).netID).Append(": ").Append(((NPC)Results[j]).name);
					if (j == Results.Count - 1)
					{
						Player.SendMessage(sb.ToString(), Color.MediumSeaGreen);
						break;
					}
					if ((j + 1) % 2 == 0)
					{
						Player.SendMessage(sb.ToString(), Color.MediumSeaGreen);
						sb.Clear();
					}
				}
			}
			if (Results.Count > (8 * Page))
			{
				Player.SendMessage(string.Format("Type /spage {0} for more Results.", (Page + 1)), Color.Yellow);
			}
		}

		/* Sethome - how many homes can a user set. */
		public static int NoOfHomesCanSet(TSPlayer ply)
		{
			if (ply.Group.HasPermission("essentials.home.set.*"))
				return -1;

			List<int> maxHomes = new List<int>();
			foreach (string p in ply.Group.TotalPermissions)
			{
				if (p.StartsWith("essentials.home.set.") && p != "essentials.home.set." && !ply.Group.negatedpermissions.Contains(p))
				{
					int m = 0;
					if (int.TryParse(p.Remove(0, 20), out m))
						maxHomes.Add(m);
				}
			}

			if (maxHomes.Count < 1)
				return 1;
			else
				return maxHomes.Max();
		}

		/* Sethome - get next home */
		public static string NextHome(List<string> homes)
		{
			List<int> intHomes = new List<int>();
			foreach (string h in homes)
			{
				int m = 0;
				if (int.TryParse(h, out m))
					intHomes.Add(m);
			}
			if (intHomes.Count < 1)
				return "1";
			else
				return (intHomes.Max() + 1).ToString();
		}

		/* Top, Up and Down Methods */
		public static int GetTop(int TileX)
		{
			for (int y = 0; y < Main.maxTilesY; y++)
			{
				if (WorldGen.SolidTile(TileX, y))
					return y - 3;
			}
			return -1;
		}
		public static int GetUp(int TileX, int TileY)
		{
			for (int y = TileY - 1; y > 0; y--)
			{
				if (WorldGen.SolidTile(TileX, y + 1) && !WorldGen.SolidTile(TileX, y) && !WorldGen.SolidTile(TileX, y - 1) && !WorldGen.SolidTile(TileX, y - 2))
					return y - 2;
			}
			return -1;
		}
		public static int GetDown(int TileX, int TileY)
		{
			for (int y = TileY + 3; y < Main.maxTilesY; y++)
			{
				if (WorldGen.SolidTile(TileX, y + 1) && !WorldGen.SolidTile(TileX, y) && !WorldGen.SolidTile(TileX, y - 1) && !WorldGen.SolidTile(TileX, y - 2))
					return y - 2;
			}
			return -1;
		}

		/* Left and Right */
		public static int GetLeft(int TileX, int TileY)
		{
			bool FoundSolid = false;
			for (int x = TileX; x > 0; x--)
			{
				if (!FoundSolid)
				{
					if (!WorldGen.SolidTile(x, TileY))
						continue;
					else
						FoundSolid = true;
				}
				else if (!WorldGen.SolidTile(x - 1, TileY) &&
					!WorldGen.SolidTile(x, TileY + 1) && !WorldGen.SolidTile(x - 1, TileY + 1) &&
					!WorldGen.SolidTile(x, TileY + 2) && !WorldGen.SolidTile(x - 1, TileY + 2))
					return x - 1;
				else
					FoundSolid = true;
			}
			return -1;
		}
		public static int GetRight(int TileX, int TileY)
		{
			bool FoundSolid = false;
			for (int x = TileX + 1; x < Main.maxTilesX; x++)
			{
				if (!FoundSolid)
				{
					if (!WorldGen.SolidTile(x, TileY))
						continue;
					else
						FoundSolid = true;
				}
				else if (!WorldGen.SolidTile(x + 1, TileY) &&
					!WorldGen.SolidTile(x, TileY + 1) && !WorldGen.SolidTile(x + 1, TileY + 1) &&
					!WorldGen.SolidTile(x, TileY + 2) && !WorldGen.SolidTile(x + 1, TileY + 2))
					return x + 1;
				
					
			}
			return -1;
		}

		#region ParseParameters
		public static List<String> ParseParameters(string str)
		{
			var ret = new List<string>();
			var sb = new StringBuilder();
			bool instr = false;
			for (int i = 0; i < str.Length; i++)
			{
				char c = str[i];

				if (instr)
				{
					if (c == '\\')
					{
						if (i + 1 >= str.Length)
							break;
						c = GetEscape(str[++i]);
					}
					else if (c == '"')
					{
						ret.Add(sb.ToString());
						sb.Clear();
						instr = false;
						continue;
					}
					sb.Append(c);
				}
				else
				{
					if (IsWhiteSpace(c))
					{
						if (sb.Length > 0)
						{
							ret.Add(sb.ToString());
							sb.Clear();
						}
					}
					else if (c == '"')
					{
						if (sb.Length > 0)
						{
							ret.Add(sb.ToString());
							sb.Clear();
						}
						instr = true;
					}
					else
					{
						sb.Append(c);
					}
				}
			}
			if (sb.Length > 0)
				ret.Add(sb.ToString());

			return ret;
		}
		private static char GetEscape(char c)
		{
			switch (c)
			{
				case '\\':
					return '\\';
				case '"':
					return '"';
				case 't':
					return '\t';
				default:
					return c;
			}
		}
		private static bool IsWhiteSpace(char c)
		{
			return c == ' ' || c == '\t' || c == '\n';
		}
		#endregion
	}
}
