using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;

namespace Essentials
{
    public class esPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public string plrName { get { return TShock.Players[Index].Name; } }
        public Group grpData { get { return TShock.Players[Index].Group; } }
        public int lastXtp = 0;
        public int lastYtp = 0;
        public int lastXondeath = 0;
        public int lastYondeath = 0;
        public string lastaction = "none";
        public bool ondeath = false;
        public string lastsearch = "";
        public string lastseachtype = "none";
        public string redpass = "";
        public string greenpass = "";
        public string bluepass = "";
        public string yellowpass = "";
        public string lastcmd = "";
        public bool disabled = false;
        public int disX = 0;
        public int disY = 0;
        public DateTime lastdis = DateTime.UtcNow;

        public esPlayer(int index)
        {
            Index = index;
        }

        public void SendMessage(string message, Color color)
        {
            NetMessage.SendData((int)PacketTypes.ChatText, Index, -1, message, 255, color.R, color.G, color.B);
        }

        public void Kick(string reason)
        {
            TShock.Players[Index].Disconnect(reason);
        }

        public void Teleport(int xtile, int ytile)
        {
            TShock.Players[Index].Teleport(xtile, ytile);
        }
    }
}
