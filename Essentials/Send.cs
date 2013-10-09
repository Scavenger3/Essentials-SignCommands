using System;
using TShockAPI;

namespace Essentials
{
	public class Send
	{
		public static void Error(TSPlayer to, string message, params object[] format)
		{
			Send.Error(to, string.Format(message, format));
		}
		public static void Error(TSPlayer to, string message)
		{
			if (to.Index < 0 || to.Index > 255)
			{
				to.SendErrorMessage(message);
			}
			to.SendMessage(message, Color.OrangeRed);
		}

		public static void Success(TSPlayer to, string message, params object[] format)
		{
			Send.Success(to, string.Format(message, format));
		}
		public static void Success(TSPlayer to, string message)
		{
			if (to.Index < 0 || to.Index > 255)
			{
				to.SendSuccessMessage(message);
			}
			to.SendMessage(message, Color.MediumSeaGreen);
		}

		public static void Info(TSPlayer to, string message, params object[] format)
		{
			Send.Info(to, string.Format(message, format));
		}
		public static void Info(TSPlayer to, string message)
		{
			if (to.Index < 0 || to.Index > 255)
			{
				to.SendInfoMessage(message);
				to.SendMessage(message, Color.Yellow);
			}
		}
	}
}
