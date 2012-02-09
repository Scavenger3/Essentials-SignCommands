using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TShockAPI;
using System.IO;

namespace Essentials
{
    class EConfig
    {
        public static bool useteamperms = false;
        public static string redpassword = "";
        public static string greenpassword = "";
        public static string bluepassword = "";
        public static string yellowpassword = "";

        #region Load Config
        public static void LoadConfig()
        {
            if (!File.Exists(@"tshock/EssentialsConfig.txt"))
            {
                File.WriteAllText(@"tshock/EssentialsConfig.txt", "#> < is a comment" + Environment.NewLine +
                    "#> To Completely disable locking of teams, set LockTeamsWithPermissions to false and set the passwords to nothing" + Environment.NewLine +
                    "#> Lock Teams with Permissions or passwords (Boolean):" + Environment.NewLine +
                    "LockTeamsWithPermissions:false" + Environment.NewLine +
                    "#> Passwords for teams (leave blank for no password) Make sure the passowrd is in quotes \"<password>\" (String):" + Environment.NewLine +
                    "RedPassword:\"\"" + Environment.NewLine +
                    "GreenPassword:\"\"" + Environment.NewLine +
                    "BluePassword:\"\"" + Environment.NewLine +
                    "YellowPassword:\"\"" + Environment.NewLine);
                useteamperms = false;
                redpassword = "";
                greenpassword = "";
                bluepassword = "";
                yellowpassword = "";
            }
            else
            {
                using (StreamReader file = new StreamReader(@"tshock/EssentialsConfig.txt", true))
                {
                    string[] rFile = (file.ReadToEnd()).Split('\n');
                    foreach (string currentLine in rFile)
                    {
                        try
                        {
                            if (currentLine.StartsWith("LockTeamsWithPermissions:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 25);
                                if (tempLine.StartsWith("false"))
                                    useteamperms = false;
                                else if (tempLine.StartsWith("true"))
                                    useteamperms = true;
                                else
                                {
                                    Console.ForegroundColor = ConsoleColor.Red;
                                    Console.WriteLine("Error in Essentials config file - LockTeamsWithPermissions");
                                    Console.ForegroundColor = ConsoleColor.Gray;
                                }
                            }
                            else if (currentLine.StartsWith("RedPassword:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 12);
                                tempLine = tempLine.Split('\"', '\'')[1];
                                redpassword = tempLine;
                            }
                            else if (currentLine.StartsWith("GreenPassword:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 14);
                                tempLine = tempLine.Split('\"', '\'')[1];
                                greenpassword = tempLine;
                            }
                            else if (currentLine.StartsWith("BluePassword:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 13);
                                tempLine = tempLine.Split('\"', '\'')[1];
                                bluepassword = tempLine;
                            }
                            else if (currentLine.StartsWith("YellowPassword:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 15);
                                tempLine = tempLine.Split('\"', '\'')[1];
                                yellowpassword = tempLine;
                            }
                        }
                        catch (Exception)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                            Console.WriteLine("Error in Essentials config file - TeamPasswords", Color.IndianRed);
                            Console.ForegroundColor = ConsoleColor.Gray;
                            return;
                        }
                    }
                }
            }
        }
        #endregion

        #region reload Config
        public static void ReloadConfig(CommandArgs args)
        {
            bool err = false;
            if (!File.Exists(@"tshock/EssentialsConfig.txt"))
            {
                File.WriteAllText(@"tshock/EssentialsConfig.txt", "#> < is a comment" + Environment.NewLine +
                    "#> To Completely disable locking of teams, set LockTeamsWithPermissions to false and set the passwords to nothing" + Environment.NewLine +
                    "#> Lock Teams with Permissions or passwords (Boolean):" + Environment.NewLine +
                    "LockTeamsWithPermissions:false" + Environment.NewLine +
                    "#> Passwords for teams (leave blank for no password) Make sure the passowrd is in quotes \"<password>\" (String):" + Environment.NewLine +
                    "RedPassword:\"\"" + Environment.NewLine +
                    "GreenPassword:\"\"" + Environment.NewLine +
                    "BluePassword:\"\"" + Environment.NewLine +
                    "YellowPassword:\"\"" + Environment.NewLine);
                useteamperms = false;
                redpassword = "";
                greenpassword = "";
                bluepassword = "";
                yellowpassword = "";
            }
            else
            {
                using (StreamReader file = new StreamReader(@"tshock/EssentialsConfig.txt", true))
                {
                    string[] rFile = (file.ReadToEnd()).Split('\n');
                    foreach (string currentLine in rFile)
                    {
                        try
                        {
                            if (currentLine.StartsWith("LockTeamsWithPermissions:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 25);
                                if (tempLine.StartsWith("false"))
                                    useteamperms = false;
                                else if (tempLine.StartsWith("true"))
                                    useteamperms = true;
                                else
                                {
                                    args.Player.SendMessage("Error in Essentials config file - LockTeamsWithPermissions", Color.IndianRed);
                                    err = true;
                                }
                            }
                            else if (currentLine.StartsWith("RedPassword:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 12);
                                tempLine = tempLine.Split('\"', '\'')[1];
                                redpassword = tempLine;
                            }
                            else if (currentLine.StartsWith("GreenPassword:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 14);
                                tempLine = tempLine.Split('\"', '\'')[1];
                                greenpassword = tempLine;
                            }
                            else if (currentLine.StartsWith("BluePassword:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 13);
                                tempLine = tempLine.Split('\"', '\'')[1];
                                bluepassword = tempLine;
                            }
                            else if (currentLine.StartsWith("YellowPassword:"))
                            {
                                string tempLine = currentLine;
                                tempLine = tempLine.Remove(0, 15);
                                tempLine = tempLine.Split('\"', '\'')[1];
                                yellowpassword = tempLine;
                            }
                        }
                        catch (Exception)
                        {
                            args.Player.SendMessage("Error in Essentials config file - TeamPasswords", Color.IndianRed);
                            err = true;
                            return;
                        }
                    }
                }
            }
            if (!err)
                args.Player.SendMessage("Config Reloaded Successfully!", Color.MediumSeaGreen);
        }
        #endregion
    }
}
