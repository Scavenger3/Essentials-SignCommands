using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace SignCommands
{
	public static class scUtils
	{
		public static scPlayer GetscPlayerByID(int id)
		{
			scPlayer player = null;
			foreach (scPlayer ply in SignCommands.scPlayers)
			{
				if (ply.Index == id)
					return ply;
			}
			return player;
		}

		public static bool CanCreate(TSPlayer player, scSign sign)
		{
			foreach (scCommand cmd in sign.Commands)
				if (!player.Group.HasPermission(string.Format("essentials.signs.create.{0}", cmd.command)))
					return false;
			return true;
		}

		public static bool CanBreak(TSPlayer player, scSign sign)
		{
			foreach (scCommand cmd in sign.Commands)
				if (!player.Group.HasPermission(string.Format("essentials.signs.break.{0}", cmd.command)))
					return false;
			return true;
		}

		#region ParseParameters
		/*
		 * This code has been used under the terms of the GNU General Public License. Copyright (C) 2011-2012 The TShock Team.
		 */
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
