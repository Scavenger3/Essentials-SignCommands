using System.Collections.Generic;
using System.Linq;
using TShockAPI;

namespace SignCommands
{
    public static class ScUtils
    {
		/// <summary>
		/// Check if the player can create a sign.
		/// The player must have the ability to use every command they put on the sign.
		/// </summary>
		/// <param name="player">Player to check permissions with</param>
		/// <param name="sign">Sign</param>
		/// <returns></returns>
        public static bool CanCreate(TSPlayer player, ScSign sign)
        {
			if (sign.commands.Count == 0) return true;

            var fails = sign.commands.Count(cmd => !cmd.Value.CanRun(player));

            return fails != sign.commands.Values.Count;
        }

		public static bool CanEdit(TSPlayer player, ScSign sign)
		{
			return !sign.noEdit || player.Group.HasPermission("essentials.signs.editall");
		}

		public static bool CanRead(TSPlayer player, ScSign sign)
		{
			return !sign.noRead || player.Group.HasPermission("essentials.signs.readall");
		}

		/// <summary>
		/// Check if the player can break a sign.
		/// If the player has the sign's override permission they can break it.
		/// If the player has the permission "essentials.signs.break" they can break it
		/// </summary>
		/// <param name="player"></param>
		/// <param name="sign"></param>
		/// <returns></returns>
        public static bool CanBreak(TSPlayer player, ScSign sign)
        {
            if (player.Group.HasPermission(sign.requiredPermission))
                return true;
            if (!player.Group.HasPermission("essentials.signs.break"))
                return false;
            return true;
        }
    }

    public static class Extensions
    {
		/// <summary>
		/// Adds or modifies a dictionary value
		/// </summary>
		/// <param name="dictionary">Dictionary to edit</param>
		/// <param name="point">Sign location</param>
		/// <param name="sign">Sign</param>
        public static void AddItem(this Dictionary<Point, ScSign> dictionary, Point point, ScSign sign)
        {
            if (!dictionary.ContainsKey(point))
            {
                dictionary.Add(point, sign);
                return;
            }

            dictionary[point] = sign;
        }

		/// <summary>
		/// Returns or adds an ScSign from a dictionary.
		/// </summary>
		/// <param name="dictionary">Dictionary to get results from</param>
		/// <param name="x">x position of sign</param>
		/// <param name="y">y position of sign</param>
		/// <param name="text">text on the sign</param>
		/// <param name="tPly">player who initiated the check</param>
		/// <returns></returns>
        public static ScSign Check(this Dictionary<Point, ScSign> dictionary, int x, int y, string text, TSPlayer tPly)
        {
            var point = new Point(x, y);
            if (!dictionary.ContainsKey(point))
            {
                var sign = new ScSign(text, tPly, point);
                dictionary.Add(point, sign);
                return sign;
            }
            return dictionary[point];
        }

        public static string Suffix(this int number)
        {
            return number == 0 || number > 1 ? "s" : "";
        }
    }
}
