/*
 * Full credit goes to Olink's Kit Plugin!
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using Terraria;
using System.IO;
using Newtonsoft.Json;

namespace Kits
{
    #region Kit
    public class Kit
    {
        public String name;
        public String perm;
        public List<KitItem> items;

        public Kit(String name, String perm, List<KitItem> items)
        {
            this.name = name.ToLower();
            this.perm = perm;
            this.items = items;
        }

        public String getName()
        {
            return name;
        }

        public String getPerm()
        {
            return perm;
        }

        public void giveItems(TSPlayer ply)
        {
            foreach (KitItem i in items)
            {
                Item item = TShock.Utils.GetItemById(i.id);
                int amount = Math.Min(item.maxStack, i.amt);
                if (item != null)
                    ply.GiveItem(item.type, item.name, item.width, item.height, amount);
            }

            ply.SendMessage(String.Format("{0} kit given. Enjoy!", name), Color.Green);
        }
    }
    #endregion

    #region KitItem
    public class KitItem
    {
        public int id;
        public int amt;

        public KitItem(int i, int a)
        {
            id = i;
            amt = a;
        }
    }
    #endregion

    #region KitList
    public class KitList
    {
        public List<Kit> kits;

        public KitList()
        {
            kits = new List<Kit>();
        }

        public Kit findKit(String name)
        {
            foreach (Kit k in kits)
            {
                if (k.getName() == name)
                {
                    return k;
                }
            }

            return null;
        }
    }
    #endregion

    #region KitReader
    class KitReader
    {
        public KitList writeFile(String file)
        {
            TextWriter tw = new StreamWriter(file);

            KitList kits = new KitList();
            List<KitItem> testItems = new List<KitItem>();

            testItems.Add(new KitItem(76, 1));
            testItems.Add(new KitItem(80, 1));
            testItems.Add(new KitItem(89, 1));
            kits.kits.Add(new Kit("basics", "default-kit", testItems));

            tw.Write(JsonConvert.SerializeObject(kits, Formatting.Indented));
            tw.Close();

            return kits;
        }

        public KitList readFile(String file)
        {
            TextReader tr = new StreamReader(file);
            String raw = tr.ReadToEnd();
            tr.Close();

            KitList kitList = JsonConvert.DeserializeObject<KitList>(raw);
            return kitList;
        }
    }
    #endregion
}
