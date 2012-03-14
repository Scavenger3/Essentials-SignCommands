using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;

namespace SignCommands
{
    public class scPlayer
    {
        public int Index { get; set; }
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public string plrName { get { return TShock.Players[Index].Name; } }
        public bool destsign = false;
        public int toldperm = 0;
        public int toldcool = 0;
        public int tolddest = 0;
        public int toldalert = 0;
        public int CooldownTime = 0;
        public int CooldownHeal = 0;
        public int CooldownMsg = 0;
        public int CooldownDamage = 0;
        public int CooldownBoss = 0;
        public int CooldownItem = 0;
        public int CooldownBuff = 0;
        public int CooldownSpawnMob = 0;
        public int CooldownKit = 0;
        public int CooldownCommand = 0;
        public int handlecmd = 0;
        public bool checkforsignedit = false;
        public bool DestroyingSign = false;
        public string originalsigntext = "";
        public int signeditid = 0;

        public scPlayer(int index)
        {
            Index = index;
        }

        public bool HasCooldown()
        {
            if (CooldownTime > 0 || CooldownHeal > 0 || CooldownMsg > 0 || CooldownDamage > 0 || CooldownBoss > 0 ||
                CooldownItem > 0 || CooldownBuff > 0 || CooldownSpawnMob > 0 || CooldownKit > 0 || CooldownCommand > 0)
            {
                return true;
            }

            return false;
        }

        public List<int> Cooldowns()
        {
            List<int> r = new List<int>();
            r.Add(CooldownBoss);
            r.Add(CooldownBuff);
            r.Add(CooldownCommand);
            r.Add(CooldownDamage);
            r.Add(CooldownHeal);
            r.Add(CooldownItem);
            r.Add(CooldownKit);
            r.Add(CooldownMsg);
            r.Add(CooldownSpawnMob);
            r.Add(CooldownTime);
            return r;
        }
    }
}
