using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Threading;
using System.IO;
using Newtonsoft.Json;
using Terraria;
using TShockAPI;
using Hooks;
using config;
using System.Text;

namespace SignCommands
{
	[APIVersion(1, 12)]
	public class SignCommands : TerrariaPlugin
	{
		public static scConfig getConfig { get; set; }
		internal static string TempConfigPath { get { return Path.Combine(TShock.SavePath, "PluginConfigs/SignCommandsConfig.json"); } }

		public static List<scPlayer> scPlayers = new List<scPlayer>();
		public static Dictionary<string, List<int>> svCool = new Dictionary<string, List<int>>();

		public static int GlobalBossCooldown = 0;
		public static int GlobalTimeCooldown = 0;
		public static int GlobalSpawnMobCooldown = 0;
		public static int GlobalCommandCooldown = 0;
		public static int GlobalKitCooldown = 0;

		public static DateTime Cooldown = DateTime.UtcNow;

		public override string Name
		{
			get { return "Sign Commands"; }
		}

		public override string Author
		{
			get { return "by Scavenger"; }
		}

		public override string Description
		{
			get { return "Put commands on signs!"; }
		}

		public override Version Version
		{
			get { return new Version("1.3.8.3"); }
		}

		public override void Initialize()
		{
			GameHooks.Initialize += OnInitialize;
			NetHooks.GetData += GetData;
			NetHooks.GreetPlayer += OnGreetPlayer;
			ServerHooks.Leave += OnLeave;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				GameHooks.Initialize -= OnInitialize;
				NetHooks.GetData -= GetData;
				NetHooks.GreetPlayer -= OnGreetPlayer;
				ServerHooks.Leave -= OnLeave;
			}
			base.Dispose(disposing);
		}

		public SignCommands(Main game)
			: base(game)
		{
			getConfig = new scConfig();

			Order = 4;
		}

		#region Commands
		public void OnInitialize()
		{
			Cooldown = DateTime.UtcNow;

			Commands.ChatCommands.Add(new Command("essentials.sign.destroy", destsign, "destsign"));
			Commands.ChatCommands.Add(new Command("essentials.sign.reload", screload, "screload"));

			#region Check Config
			if (!Directory.Exists(@"tshock/PluginConfigs/"))
			{
				Directory.CreateDirectory(@"tshock/PluginConfigs/");
			}
			SetupConfig();
			#endregion
		}

		public static void destsign(CommandArgs args)
		{
			scPlayer play = GetscPlayerByName(args.Player.Name);
			if (play == null) return;
			if (play.destsign)
			{
				play.destsign = false;
				args.Player.SendMessage("You are now in use mode, Type \"/destsign\" to return to destroy mode!", Color.LightGreen);
			}
			else
			{
				play.destsign = true;
				args.Player.SendMessage("You are now in destroy mode, Type \"/destsign\" to return to use mode!", Color.LightGreen);
			}
		}

		public static void screload(CommandArgs args)
		{
			try
			{
				if (File.Exists(TempConfigPath))
				{
					getConfig = scConfig.Read(TempConfigPath);
				}
				getConfig.Write(TempConfigPath);
				args.Player.SendMessage("Sign Command Config Reloaded Successfully.", Color.MediumSeaGreen);
				return;
			}
			catch (Exception ex)
			{
				args.Player.SendMessage("Error: Could not reload Sign Command config, Check log for more details.", Color.IndianRed);
				Log.Error("Config Exception in Sign Commands config file");
				Log.Error(ex.ToString());
			}
		}
		#endregion

		#region Config
		public static void SetupConfig()
		{
			try
			{
				if (File.Exists(TempConfigPath))
				{
					getConfig = scConfig.Read(TempConfigPath);
				}
				getConfig.Write(TempConfigPath);
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine("Error in Sign Commands config file");
				Console.ForegroundColor = ConsoleColor.Gray;
				Log.Error("Config Exception in Sign Commands config file");
				Log.Error(ex.ToString());
			}
		}
		#endregion

		public void OnGreetPlayer(int who, HandledEventArgs e)
		{
			lock (scPlayers)
				scPlayers.Add(new scPlayer(who));

			var ply = GetscPlayerByID(who);
			if (ply == null) return;
			lock (svCool)
			{
				if (svCool.ContainsKey(ply.plrName))
				{
					ply.CooldownBoss = svCool[ply.plrName][0];
					ply.CooldownBuff = svCool[ply.plrName][1];
					ply.CooldownCommand = svCool[ply.plrName][2];
					ply.CooldownDamage = svCool[ply.plrName][3];
					ply.CooldownHeal = svCool[ply.plrName][4];
					ply.CooldownItem = svCool[ply.plrName][5];
					ply.CooldownKit = svCool[ply.plrName][6];
					ply.CooldownMsg = svCool[ply.plrName][7];
					ply.CooldownSpawnMob = svCool[ply.plrName][8];
					ply.CooldownTime = svCool[ply.plrName][9];
					svCool.Remove(ply.plrName);
				}
			}
		}

		public void OnLeave(int who)
		{
			lock (scPlayers)
			{
				for (int i = 0; i < scPlayers.Count; i++)
				{
					if (scPlayers[i].Index == who)
					{
						//CHECK:
						var play = scPlayers[i];
						if (play != null && play.TSPlayer != null)
						{
							if (play.HasCooldown() && !play.TSPlayer.Group.HasPermission("essentials.sign.nocooldown"))
							{
								lock (svCool)
									svCool.Add(play.plrName, play.Cooldowns());
							}
						}

						//THEN:
						scPlayers.RemoveAt(i);
						break;
					}
				}
			}
		}

		#region Timer
		static void OnUpdate()
		{
			if ((DateTime.UtcNow - Cooldown).TotalMilliseconds >= 1000)
			{
				Cooldown = DateTime.UtcNow;
				//Global Cooldowns:
				if (getConfig.GlobalBossCooldown && GlobalBossCooldown > 0)
					GlobalBossCooldown--;

				if (getConfig.GlobalTimeCooldown && GlobalTimeCooldown > 0)
					GlobalTimeCooldown--;

				if (getConfig.GlobalSpawnMobCooldown && GlobalSpawnMobCooldown > 0)
					GlobalSpawnMobCooldown--;

				if (getConfig.GlobalDoCommandCooldown && GlobalCommandCooldown > 0)
					GlobalCommandCooldown--;

				if (getConfig.GlobalKitCooldown && GlobalKitCooldown > 0)
					GlobalKitCooldown--;

				lock (scPlayers)
				{
					foreach (scPlayer play in scPlayers)
					{
						//Player Cooldowns:
						if (play.CooldownBoss > 0)
							play.CooldownBoss--;

						if (play.CooldownTime > 0)
							play.CooldownTime--;

						if (play.CooldownSpawnMob > 0)
							play.CooldownSpawnMob--;

						if (play.CooldownDamage > 0)
							play.CooldownDamage--;

						if (play.CooldownHeal > 0)
							play.CooldownHeal--;

						if (play.CooldownMsg > 0)
							play.CooldownMsg--;

						if (play.CooldownItem > 0)
							play.CooldownItem--;

						if (play.CooldownBuff > 0)
							play.CooldownBuff--;

						if (play.CooldownKit > 0)
							play.CooldownKit--;

						if (play.CooldownCommand > 0)
							play.CooldownCommand--;

						//Message Cooldowns:
						if (play.toldcool > 0)
							play.toldcool--;

						if (play.toldperm > 0)
							play.toldperm--;

						if (play.toldalert > 0)
							play.toldalert--;

						if (play.tolddest > 0)
							play.tolddest--;

						if (play.handlecmd > 0)
							play.handlecmd--;
					}
				}
			}
		}
		#endregion

		public void GetData(GetDataEventArgs e)
		{
			try
			{
				switch (e.MsgID)
				{
					#region Tile Modify
					case PacketTypes.Tile:
						using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
						{
							var reader = new BinaryReader(data);
							if (e.MsgID == PacketTypes.Tile)
							{
								var type = reader.ReadByte();
								if (!(type == 0 || type == 4))
									return;
							}
							var x = reader.ReadInt32();
							var y = reader.ReadInt32();
							reader.Close();

							if (Main.tile[x, y].type != 55) return;

							var id = Terraria.Sign.ReadSign(x, y);
							var tplayer = TShock.Players[e.Msg.whoAmI];
							

							if (id != -1 && Main.sign[id].text.ToLower().StartsWith(getConfig.DefineSignCommands.ToLower()))
							{
								scPlayer hitplay = GetscPlayerByName(tplayer.Name);

								if (!hitplay.destsign)
								{
									if (tplayer.Group.HasPermission("essentials.sign.destroy") && hitplay.tolddest <= 0)
									{
										tplayer.SendMessage("To destroy this sign, Type /destsign", Color.IndianRed);
										hitplay.tolddest = 10;
									}
									tplayer.SendTileSquare(x, y);
									e.Handled = true;

									//string[] lines = Main.sign[id].text.Split(Encoding.UTF8.GetString(new byte[] { 10 }).ToCharArray()[0]);
									char tosplit = '>';
									if (getConfig.CommandsStartWith.Length == 1)
										tosplit = getConfig.CommandsStartWith.ToCharArray()[0];
									string text = Main.sign[id].text.Replace(Encoding.UTF8.GetString(new byte[] { 10 }), "");
									string[] commands = text.Split(tosplit);
									foreach (string cmd in commands)
									{
										DoCommand(id, cmd, tplayer);
									}
								}
							}
						}
						break;
					#endregion

					#region Edit Sign
					case PacketTypes.SignNew:
						if (!e.Handled)
						{
							using (var data = new MemoryStream(e.Msg.readBuffer, e.Index, e.Length))
							{
								var reader = new BinaryReader(data);
								var signId = reader.ReadInt16();
								var x = reader.ReadInt32();
								var y = reader.ReadInt32();
								string newtext = "";
								try
								{
									var Bytes = reader.ReadBytes(e.Length - 11);
									newtext = Encoding.UTF8.GetString(Bytes);
								}
								catch { }
								reader.Close();

								var id = Terraria.Sign.ReadSign(x, y);
								var tplayer = TShock.Players[e.Msg.whoAmI];
								scPlayer edplay = GetscPlayerByID(tplayer.Index);

								if (Main.sign[id] == null || tplayer == null || edplay == null) return;
								string oldtext = Main.sign[id].text;
								string dSignCmd = getConfig.DefineSignCommands;

								if ((oldtext.StartsWith(dSignCmd) || newtext.StartsWith(dSignCmd)) && !tplayer.Group.HasPermission("essentials.sign.create"))
								{
									tplayer.SendMessage("You do not have permission to create/edit sign commands!", Color.IndianRed);
									e.Handled = true;
									tplayer.SendData(PacketTypes.SignNew, "", id);
									return;
								}
							}
						}
						break;
					#endregion
				}
			}
			catch (Exception ex)
			{
				Log.Error("[Sign Commands] Exception while recieving data: ");
				Log.Error(ex.ToString());
			}
		}

		public static void DoCommand(int id, string cmd, TSPlayer tplayer)
		{
			scPlayer doplay = GetscPlayerByID(tplayer.Index);
			Sign sign = Main.sign[id];
			cmd = cmd.ToLower();
			if (tplayer == null || doplay == null || sign == null) return;

			#region Check permissions
			bool stime = tplayer.Group.HasPermission("essentials.sign.use.time");
			bool sheal = tplayer.Group.HasPermission("essentials.sign.use.heal");
			bool smsg = tplayer.Group.HasPermission("essentials.sign.use.message");
			bool sdmg = tplayer.Group.HasPermission("essentials.sign.use.damage");
			bool sboss = tplayer.Group.HasPermission("essentials.sign.use.spawnboss");
			bool smob = tplayer.Group.HasPermission("essentials.sign.use.spawnmob");
			bool swarp = tplayer.Group.HasPermission("essentials.sign.use.warp");
			bool sitem = tplayer.Group.HasPermission("essentials.sign.use.item");
			bool sbuff = tplayer.Group.HasPermission("essentials.sign.use.buff");
			bool skit = tplayer.Group.HasPermission("essentials.sign.use.kit");
			bool scmd = tplayer.Group.HasPermission("essentials.sign.use.command");
			bool nocool = tplayer.Group.HasPermission("essentials.sign.nocooldown");
			#endregion

			#region Time
			if (cmd.StartsWith("time") && stime && (nocool || ((getConfig.GlobalTimeCooldown && GlobalTimeCooldown <= 0) || (!getConfig.GlobalTimeCooldown && doplay.CooldownTime <= 0))))
			{
				bool done = false;
				if (cmd.StartsWith("time day"))
				{
					TSPlayer.Server.SetTime(true, 150.0);
					done = true;
				}
				else if (cmd.StartsWith("time night"))
				{
					TSPlayer.Server.SetTime(false, 0.0);
					done = true;
				}
				else if (cmd.StartsWith("time dusk"))
				{
					TSPlayer.Server.SetTime(false, 0.0);
					done = true;
				}
				else if (cmd.StartsWith("time noon"))
				{
					TSPlayer.Server.SetTime(true, 27000.0);
					done = true;
				}
				else if (cmd.StartsWith("time midnight"))
				{
					TSPlayer.Server.SetTime(false, 16200.0);
					done = true;
				}
				else if (cmd.StartsWith("time fullmoon"))
				{
					TSPlayer.Server.SetFullMoon(true);
					done = true;
				}
				else if (cmd.StartsWith("time bloodmoon"))
				{
					TSPlayer.Server.SetBloodMoon(true);
					done = true;
				}

				if (done)
				{
					if (!nocool && getConfig.GlobalTimeCooldown)
						GlobalTimeCooldown = getConfig.TimeCooldown;
					else
						doplay.CooldownTime = getConfig.TimeCooldown;
				}
			}
			else if (doplay.toldperm <= 0 && !stime && cmd.StartsWith("time "))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			else if (doplay.toldcool <= 0 && stime && (!nocool && (!getConfig.GlobalTimeCooldown && doplay.CooldownTime > 0)) && cmd.StartsWith("time "))
			{
				tplayer.SendMessage("You have to wait another " + doplay.CooldownTime + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			else if (doplay.toldcool <= 0 && stime && (!nocool && (getConfig.GlobalTimeCooldown && GlobalTimeCooldown > 0)) && cmd.StartsWith("time "))
			{
				tplayer.SendMessage("Everyone has to wait another " + GlobalTimeCooldown + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			#endregion

			#region Heal
			if (cmd.StartsWith("heal") && sheal && (nocool || doplay.CooldownHeal <= 0))
			{
				Item heart = TShock.Utils.GetItemById(58);
				Item star = TShock.Utils.GetItemById(184);
				for (int ic = 0; ic < 20; ic++)
					tplayer.GiveItem(heart.type, heart.name, heart.width, heart.height, heart.maxStack);
				for (int ic = 0; ic < 10; ic++)
					tplayer.GiveItem(star.type, star.name, star.width, star.height, star.maxStack);
				doplay.CooldownHeal = getConfig.HealCooldown;
			}
			else if (doplay.toldperm <= 0 && !sheal && cmd.StartsWith("heal"))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			else if (doplay.toldcool <= 0 && sheal && !nocool && doplay.CooldownHeal > 0 && cmd.StartsWith("heal"))
			{
				tplayer.SendMessage("You have to wait another " + doplay.CooldownHeal + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			#endregion

			#region Message
			if (cmd.StartsWith("show") && smsg && (nocool || doplay.CooldownMsg <= 0))
			{
				bool done = false;
				if (cmd.StartsWith("show motd"))
				{
					TShock.Utils.ShowFileToUser(tplayer, "motd.txt");
					done = true;
				}
				else if (cmd.StartsWith("show rules"))
				{
					TShock.Utils.ShowFileToUser(tplayer, "rules.txt");
					done = true;
				}
				else if (cmd.StartsWith("show playing"))
				{
					tplayer.SendMessage(string.Format("Current players: {0}.", TShock.Utils.GetPlayers()), 255, 240, 20);
					done = true;
				}
				else if (cmd.StartsWith("show message") && File.Exists("scMeassge.txt"))
				{
					TShock.Utils.ShowFileToUser(tplayer, "scMessage.txt");
					done = true;
				}
				else if (cmd.StartsWith("show message") && !File.Exists("scMeassge.txt"))
				{
					tplayer.SendMessage("Could not find message", Color.IndianRed);
					done = true;
				}

				if (done)
					doplay.CooldownMsg = getConfig.ShowMsgCooldown;
			}
			else if (doplay.toldperm <= 0 && !smsg && cmd.StartsWith("show "))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			else if (doplay.toldcool <= 0 && smsg && !nocool && doplay.CooldownMsg > 0 && cmd.StartsWith("show "))
			{
				tplayer.SendMessage("You have to wait another " + doplay.CooldownTime + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			#endregion

			#region Damage
			if (cmd.StartsWith("damage ") && sdmg && (nocool || doplay.CooldownDamage <= 0))
			{
				try
				{
					string[] linesplit = cmd.Split('\'', '\"');
					int amtdamage = 0;
					bool dmgisint = int.TryParse(linesplit[1], out amtdamage);
					if (dmgisint)
					{
						tplayer.DamagePlayer(amtdamage);
						doplay.CooldownDamage = getConfig.DamageCooldown;
					}
					else
					{
						if (doplay.toldalert <= 0)
						{
							doplay.toldalert = 3;
							tplayer.SendMessage("Invalid damage value!", Color.IndianRed);
						}
					}
				}
				catch (Exception)
				{
					if (doplay.toldalert <= 0)
					{
						doplay.toldalert = 3;
						tplayer.SendMessage("Could not parse Damage Amount - Correct Format: \"<Amount to Damage>\"", Color.IndianRed);
					}
				}
			}
			else if (doplay.toldperm <= 0 && !sdmg && cmd.StartsWith("damage "))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			else if (doplay.toldcool <= 0 && sdmg && !nocool && doplay.CooldownDamage > 0 && cmd.StartsWith("damage "))
			{
				tplayer.SendMessage("You have to wait another " + doplay.CooldownDamage + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			#endregion

			#region Boss
			if (cmd.StartsWith("boss") && sboss && (nocool || ((getConfig.GlobalBossCooldown && GlobalBossCooldown <= 0) || (!getConfig.GlobalBossCooldown && doplay.CooldownBoss <= 0))))
			{
				bool done = false;
				if (cmd.StartsWith("boss eater"))
				{
					NPC npc = TShock.Utils.GetNPCById(13);
					TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
					done = true;
				}
				else if (cmd.StartsWith("boss eye"))
				{
					NPC npc = TShock.Utils.GetNPCById(4);
					TSPlayer.Server.SetTime(false, 0.0);
					TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
					done = true;
				}
				else if (cmd.StartsWith("boss king"))
				{
					NPC npc = TShock.Utils.GetNPCById(50);
					TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
					done = true;
				}
				else if (cmd.StartsWith("boss skeletron") && !cmd.StartsWith("boss skeletronprime"))
				{
					NPC npc = TShock.Utils.GetNPCById(35);
					TSPlayer.Server.SetTime(false, 0.0);
					TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
					done = true;
				}
				else if (cmd.StartsWith("boss wof"))
				{
					if (Main.wof >= 0 || (tplayer.Y / 16f < (float)(Main.maxTilesY - 205)))
					{
						if (doplay.toldalert <= 0)
						{
							doplay.toldalert = 3;
							tplayer.SendMessage("Can't spawn Wall of Flesh!", Color.Red);
						}
						return;
					}
					NPC.SpawnWOF(new Vector2(tplayer.X, tplayer.Y));
					done = true;
				}
				else if (cmd.StartsWith("boss twins"))
				{
					NPC retinazer = TShock.Utils.GetNPCById(125);
					NPC spaz = TShock.Utils.GetNPCById(126);
					TSPlayer.Server.SetTime(false, 0.0);
					TSPlayer.Server.SpawnNPC(retinazer.type, retinazer.name, 1, tplayer.TileX, tplayer.TileY);
					TSPlayer.Server.SpawnNPC(spaz.type, spaz.name, 1, tplayer.TileX, tplayer.TileY);
					done = true;
				}
				else if (cmd.StartsWith("boss destroyer"))
				{
					NPC npc = TShock.Utils.GetNPCById(134);
					TSPlayer.Server.SetTime(false, 0.0);
					TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
					done = true;
				}
				else if (cmd.StartsWith("boss skeletronprime"))
				{
					NPC npc = TShock.Utils.GetNPCById(127);
					TSPlayer.Server.SetTime(false, 0.0);
					TSPlayer.Server.SpawnNPC(npc.type, npc.name, 1, tplayer.TileX, tplayer.TileY);
					done = true;
				}

				if (done)
				{
					if (!nocool && getConfig.GlobalBossCooldown)
						GlobalBossCooldown = getConfig.BossCooldown;
					else
						doplay.CooldownBoss = getConfig.BossCooldown;
				}
			}
			else if (doplay.toldperm <= 0 && !sboss && cmd.StartsWith("boss "))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			else if (doplay.toldcool <= 0 && sboss && (!nocool && (!getConfig.GlobalBossCooldown && doplay.CooldownBoss > 0)) && cmd.StartsWith("boss "))
			{
				tplayer.SendMessage("You have to wait another " + doplay.CooldownBoss + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			else if (doplay.toldcool <= 0 && sboss && (!nocool && (getConfig.GlobalBossCooldown && GlobalBossCooldown > 0)) && cmd.StartsWith("boss "))
			{
				tplayer.SendMessage("Everyone has to wait another " + GlobalBossCooldown + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			#endregion

			#region Spawn Mob
			if ((cmd.StartsWith("spawn mob ") || cmd.StartsWith("spawnmob ") || cmd.StartsWith("spawn "))
				&& smob && (nocool || ((getConfig.GlobalSpawnMobCooldown && GlobalSpawnMobCooldown <= 0) || (!getConfig.GlobalSpawnMobCooldown && doplay.CooldownSpawnMob <= 0))))
			{
				try
				{
					string[] linesplit = cmd.Split('\'', '\"');
					for (int i = 1; i < linesplit.Length; i++)
					{
					}
					bool containstime = false;
					string[] datasplit;

					if (linesplit[1].Contains(","))
					{
						datasplit = linesplit[1].Split(',');
						containstime = true;
					}
					else
						datasplit = null;

					var npcs = new List<NPC>();
					int amount = 1;
					amount = Math.Min(amount, Main.maxNPCs);

					if (!containstime)
					{
						amount = 1;
						npcs = TShock.Utils.GetNPCByIdOrName(linesplit[1]);
					}
					else
					{
						int.TryParse(datasplit[1], out amount);
						npcs = TShock.Utils.GetNPCByIdOrName(datasplit[0]);
					}

					if (npcs.Count == 0)
					{
						if (doplay.toldalert <= 0)
						{
							doplay.toldalert = 3;
							tplayer.SendMessage("Invalid mob type!", Color.Red);
						}
					}
					else if (npcs.Count > 1)
					{
						if (doplay.toldalert <= 0)
						{
							doplay.toldalert = 3;
							tplayer.SendMessage(string.Format("More than one ({0}) mob matched!", npcs.Count), Color.Red);
						}
					}
					else
					{
						var npc = npcs[0];
						if (npc.type >= 1 && npc.type < Main.maxNPCTypes && npc.type != 113)
						{
							TSPlayer.Server.SpawnNPC(npc.type, npc.name, amount, tplayer.TileX, tplayer.TileY, 50, 20);
							if (!nocool && getConfig.GlobalSpawnMobCooldown)
								GlobalSpawnMobCooldown = getConfig.SpawnMobCooldown;
							else
								doplay.CooldownSpawnMob = getConfig.SpawnMobCooldown;
						}
						else if (npc.type == 113)
						{
							if (doplay.toldalert <= 0)
							{
								doplay.toldalert = 3;
								tplayer.SendMessage("Sorry, you can't spawn Wall of Flesh! Try the sign command \"boss wof\"");
							}
						}
						else
						{
							if (doplay.toldalert <= 0)
							{
								doplay.toldalert = 3;
								tplayer.SendMessage("Invalid mob type!", Color.Red);
							}
						}
					}
				}
				catch (Exception)
				{
					if (doplay.toldalert <= 0)
					{
						doplay.toldalert = 3;
						tplayer.SendMessage("Could not parse Mob / Amount - Correct Format: \"<Mob Name>, <Amount>\"", Color.IndianRed);
					}
				}
			}
			else if (doplay.toldperm <= 0 && !smob && cmd.StartsWith("spawnmob"))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			else if (doplay.toldcool <= 0 && smob && (!nocool && (!getConfig.GlobalSpawnMobCooldown && doplay.CooldownSpawnMob > 0)) &&
				(cmd.StartsWith("spawn mob ") || cmd.StartsWith("spawnmob ") || cmd.StartsWith("spawn ")))
			{
				tplayer.SendMessage("You have to wait another " + doplay.CooldownSpawnMob + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			else if (doplay.toldcool <= 0 && smob && (!nocool && (getConfig.GlobalSpawnMobCooldown && GlobalSpawnMobCooldown > 0)) &&
				(cmd.StartsWith("spawn mob ") || cmd.StartsWith("spawnmob ") || cmd.StartsWith("spawn ")))
			{
				tplayer.SendMessage("Everyone has to wait another " + GlobalSpawnMobCooldown + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			#endregion

			#region Warp
			if (swarp && cmd.StartsWith("warp "))
			{
				try
				{
					string[] linesplit = cmd.Split('\'', '\"');
					var warpxy = TShock.Warps.FindWarp(linesplit[1]);

					if (warpxy.WarpName == "" || warpxy.WarpPos.X == 0 || warpxy.WarpPos.Y == 0 || warpxy.WorldWarpID == "")
					{
						if (doplay.toldalert <= 0)
						{
							doplay.toldalert = 3;
							tplayer.SendMessage("Could not find warp!", Color.IndianRed);
						}
					}
					else
						tplayer.Teleport((int)warpxy.WarpPos.X, (int)warpxy.WarpPos.Y);
				}
				catch (Exception)
				{
					if (doplay.toldalert <= 0)
					{
						doplay.toldalert = 3;
						tplayer.SendMessage("Could not parse Warp - Correct Format: \"<Warp Name>\"", Color.IndianRed);
					}
				}
			}
			else if (doplay.toldperm <= 0 && !swarp && cmd.StartsWith("warp "))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			#endregion

			#region Item
			if (cmd.StartsWith("item ") && sitem && (nocool || doplay.CooldownItem <= 0))
			{
				try
				{
					string[] linesplit = cmd.Split('\'', '\"');
					bool containsamount = false;
					string[] datasplit;

					if (linesplit[1].Contains(","))
					{
						datasplit = linesplit[1].Split(',');
						containsamount = true;
					}
					else
						datasplit = null;

					int itemAmount = 0;
					var items = new List<Item>();

					if (!containsamount)
					{
						itemAmount = 1;
						items = TShock.Utils.GetItemByIdOrName(linesplit[1]);
					}
					else
					{
						int.TryParse(datasplit[1], out itemAmount);
						items = TShock.Utils.GetItemByIdOrName(datasplit[0]);
					}

					if (items.Count <= 0)
					{
						if (doplay.toldalert <= 0)
						{
							doplay.toldalert = 3;
							tplayer.SendMessage("Invalid item type!", Color.Red);
						}
					}
					else if (items.Count > 1)
					{
						if (doplay.toldalert <= 0)
						{
							doplay.toldalert = 3;
							tplayer.SendMessage(string.Format("More than one ({0}) item matched!", items.Count), Color.Red);
						}
					}
					else
					{
						var item = items[0];
						if (item.type >= 1 && item.type < Main.maxItemTypes)
						{
							if (tplayer.InventorySlotAvailable || item.name.Contains("Coin"))
							{
								if (itemAmount <= 0 || itemAmount > item.maxStack)
									itemAmount = item.maxStack;
								tplayer.GiveItem(item.type, item.name, item.width, item.height, itemAmount, 0);
								if (doplay.toldalert <= 0)
								{
									doplay.toldalert = 3;
									tplayer.SendMessage(string.Format("Gave {0} {1}(s).", itemAmount, item.name));
								}
								doplay.CooldownItem = getConfig.ItemCooldown;
							}
							else
							{
								if (doplay.toldalert <= 0)
								{
									doplay.toldalert = 3;
									tplayer.SendMessage("You don't have free slots!", Color.Red);
								}
							}
						}
						else
						{
							if (doplay.toldalert <= 0)
							{
								doplay.toldalert = 3;
								tplayer.SendMessage("Invalid item type!", Color.Red);
							}
						}
					}
				}
				catch (Exception)
				{
					if (doplay.toldalert <= 0)
					{
						doplay.toldalert = 3;
						tplayer.SendMessage("Could not parse Item / Amount - Correct Format: \"<Item Name>, <Amount>\"", Color.IndianRed);
					}
				}
			}
			else if (doplay.toldperm <= 0 && !sitem && cmd.StartsWith("item "))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			else if (doplay.toldcool <= 0 && sitem && !nocool && doplay.CooldownItem > 0 && cmd.StartsWith("item "))
			{
				tplayer.SendMessage("You have to wait another " + doplay.CooldownItem + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			#endregion

			#region Buff
			if (cmd.StartsWith("buff ") && sbuff && (nocool || doplay.CooldownItem <= 0))
			{
				try
				{
					string[] linesplit = cmd.Split('\'', '\"');
					int count = 0;
					int c1bid = 1;
					int c1tme = 1;
					for (int i = 1; i < linesplit.Length - 1; i++)
					{
						int bid = 0;
						int time = 0;
						bool containstime = false;
						string[] datasplit;

						if (linesplit[i].Contains(","))
						{
							datasplit = linesplit[i].Split(',');
							containstime = true;
						}
						else
							datasplit = null;

						string buffvalue = "";

						if (!containstime)
						{
							time = 60;
							buffvalue = linesplit[i];
						}
						else
						{
							int.TryParse(datasplit[1], out time);
							buffvalue = datasplit[0];
						}

						if (!int.TryParse(buffvalue, out bid))
						{
							var found = TShock.Utils.GetBuffByName(buffvalue);
							if (found.Count == 0)
							{
								if (doplay.toldalert <= 0)
								{
									doplay.toldalert = 3;
									doplay.TSPlayer.SendMessage("Invalid buff name in buff " + i, Color.Red);
								}
								return;
							}
							else if (found.Count > 1)
							{
								if (doplay.toldalert <= 0)
								{
									doplay.toldalert = 3;
									doplay.TSPlayer.SendMessage(string.Format("More than one ({0}) buffs matched in buff " + i, found.Count), Color.Red);
								}
								return;
							}
							bid = found[0];
						}
						if (bid > 0 && bid < Main.maxBuffs)
						{
							if (time < 0 || time > short.MaxValue)
								time = 60;
							doplay.TSPlayer.SetBuff(bid, time * 60);
							count++;
							c1bid = bid;
							c1tme = time;
						}
						else
						{
							if (doplay.toldalert <= 0)
							{
								doplay.toldalert = 3;
								doplay.TSPlayer.SendMessage("Invalid buff ID in buff " + i, Color.Red);
							}
						}
					}
					if (doplay.toldalert <= 0)
					{
						doplay.toldalert = 3;
						if (count == 1)
							doplay.TSPlayer.SendMessage(string.Format("You have buffed yourself with {0}({1}) for {2} seconds!", TShock.Utils.GetBuffName(c1bid), TShock.Utils.GetBuffDescription(c1bid), (c1tme)), Color.Green);
						else
							doplay.TSPlayer.SendMessage("You have buffed yourself with multiple buffs!", Color.Green);

					}
					if (count > 0)
						doplay.CooldownBuff = getConfig.BuffCooldown;
				}
				catch (Exception)
				{
					tplayer.SendMessage("Could not parse Buff / Duration - Correct Format: \"<Buff Name>, <Duration>\"", Color.IndianRed);
					tplayer.SendMessage("Or for more than one buff: \"<Name>, <Duration>\"<Name>, <Duration>\"", Color.IndianRed);
					return;
				}
			}
			else if (doplay.toldperm <= 0 && !sbuff && cmd.StartsWith("buff "))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			else if (doplay.toldcool <= 0 && sbuff && !nocool && doplay.CooldownBuff > 0 && cmd.StartsWith("buff "))
			{
				tplayer.SendMessage("You have to wait another " + doplay.CooldownBuff + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			#endregion

			#region Kit
			if (cmd.StartsWith("kit ") && skit && (nocool || ((getConfig.GlobalKitCooldown && GlobalKitCooldown <= 0) || (!getConfig.GlobalKitCooldown && doplay.CooldownKit <= 0))))
			{
				string kitnme = "";
				try
				{
					string[] linesplit = cmd.Split('\'', '\"');

					kitnme = linesplit[1];
				}
				catch
				{
					if (doplay.toldalert <= 0)
					{
						doplay.toldalert = 3;
						tplayer.SendMessage("Could not parse Kit - Correct Format: \"<Kit Name>\"", Color.IndianRed);
					}
					return;
				}
				try
				{
					HandleKits(tplayer, doplay, kitnme);
				}
				catch
				{
					if (doplay.toldalert <= 0)
					{
						doplay.toldalert = 3;
						tplayer.SendMessage("Kits.dll must be present to use Kit Sign Commands!", Color.IndianRed);
					}
					return;
				}

			}
			else if (doplay.toldperm <= 0 && !skit && cmd.StartsWith("kit "))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			else if (doplay.toldcool <= 0 && skit && !nocool && doplay.CooldownKit > 0 && cmd.StartsWith("kit "))
			{
				tplayer.SendMessage("You have to wait another " + doplay.CooldownKit + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			#endregion

			#region Do Command
			if ((cmd.StartsWith("command ") || cmd.StartsWith("cmd ")) && scmd &&
				(nocool || ((getConfig.GlobalDoCommandCooldown && GlobalCommandCooldown <= 0) || (!getConfig.GlobalDoCommandCooldown && doplay.CooldownCommand <= 0))))
			{
				try
				{
					string[] linesplit = cmd.Split('\'', '\"');
					if (doplay.handlecmd == 0)
					{
						TShockAPI.Commands.HandleCommand(tplayer, "/" + linesplit[1]);
						doplay.handlecmd = 2;
					}

				}
				catch (Exception)
				{
					if (doplay.toldalert <= 0)
					{
						doplay.toldalert = 3;
						tplayer.SendMessage("Could not parse Command - Correct Format: \"<Command>\"", Color.IndianRed);
					}
				}
			}
			else if (doplay.toldperm <= 0 && !scmd && (cmd.StartsWith("Command ") || cmd.StartsWith("Cmd ")))
			{
				tplayer.SendMessage("You do not have permission to use this sign command!", Color.IndianRed);
				doplay.toldperm = 5;
			}
			else if (doplay.toldcool <= 0 && scmd && (!nocool && (!getConfig.GlobalDoCommandCooldown && doplay.CooldownCommand > 0)) && (cmd.StartsWith("command ") || cmd.StartsWith("cmd ")))
			{
				tplayer.SendMessage("You have to wait another " + doplay.CooldownCommand + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			else if (doplay.toldcool <= 0 && scmd && (!nocool && (getConfig.GlobalDoCommandCooldown && GlobalCommandCooldown > 0)) && (cmd.StartsWith("command ") || cmd.StartsWith("cmd ")))
			{
				tplayer.SendMessage("Everyone has to wait another " + GlobalCommandCooldown + " seconds before using this sign", Color.IndianRed);
				doplay.toldcool = 5;
			}
			#endregion
		}

		#region Handle Kits
		public static void HandleKits(TSPlayer tplayer, scPlayer doplay, string KitName)
		{
			try
			{
				Kits.Kit k = Kits.Kits.FindKit(KitName.ToLower());

				if (k == null)
				{
					if (doplay.toldalert <= 0)
					{
						doplay.toldalert = 3;
						tplayer.SendMessage("Error: The specified kit does not exist.", Color.Red);
					}
					return;
				}

				if (tplayer.Group.HasPermission(k.getPerm()))
				{
					k.giveItems(tplayer);
					if (getConfig.GlobalKitCooldown)
						GlobalKitCooldown = getConfig.KitCooldown;
					else
						doplay.CooldownKit = getConfig.KitCooldown;
				}
				else
				{
					if (doplay.toldalert <= 0)
					{
						doplay.toldalert = 3;
						tplayer.SendMessage(string.Format("You do not have permission to use the {0} kit.", KitName), Color.Red);
					}
				}
			}
			catch { }
		}
		#endregion

		#region Get scPlayer Methods
		public static scPlayer GetscPlayerByID(int id)
		{
			scPlayer player = null;
			foreach (scPlayer ply in scPlayers)
			{
				if (ply.Index == id)
					return ply;
			}
			return player;
		}
		public static scPlayer GetscPlayerByName(string name)
		{
			var player = TShock.Utils.FindPlayer(name)[0];
			if (player != null)
			{
				foreach (scPlayer ply in scPlayers)
				{
					if (ply.TSPlayer == player)
						return ply;
				}
			}
			return null;
		}
		#endregion
	}
}