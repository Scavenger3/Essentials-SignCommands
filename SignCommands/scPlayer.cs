using TShockAPI;

namespace SignCommands
{
	public class ScPlayer
	{
		public int Index { get; set; }
		public TSPlayer TsPlayer { get { return TShock.Players[Index]; } }
	    public ScSign confirmSign;
		public bool DestroyMode { get; set; }
		public int AlertCooldownCooldown { get; set; }
		public int AlertPermissionCooldown { get; set; }
		public int AlertDestroyCooldown { get; set; }

		public ScPlayer(int index)
		{
			Index = index;
			DestroyMode = false;
			AlertDestroyCooldown = 0;
			AlertPermissionCooldown = 0;
			AlertCooldownCooldown = 0;
		}
	}
}