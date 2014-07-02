using System.Collections.Generic;
using System.Linq;
using TShockAPI;

namespace SignCommands
{
    public static class ScUtils
    {
        public static bool CanCreate(TSPlayer player, ScSign sign)
        {
            var fails = sign.commands.Count(cmd => !cmd.Value.CanRun(player));

            return fails != sign.commands.Values.Count;
        }

        public static bool CanBreak(TSPlayer player)
        {
            if (!player.Group.HasPermission("essentials.signs.break"))
                return false;
            return true;
        }
    }

    public static class Extensions
    {
        public static void AddItem(this Dictionary<Point, ScSign> dictionary, Point point, ScSign sign)
        {
            if (!dictionary.ContainsKey(point))
            {
                dictionary.Add(point, sign);
                return;
            }

            dictionary[point] = sign;
        }

        public static ScSign Check(this Dictionary<Point, ScSign> dictionary, int x, int y, string text, TSPlayer tPly)
        {
            var point = new Point(x, y);
            if (!dictionary.ContainsKey(point))
            {
                var sign = new ScSign(text, tPly);
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
