using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Hooks;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;
using Terraria;

namespace Essentials
{
	public class esSQL
	{
		private static IDbConnection db;

		public static Dictionary<string, string> nicknames = new Dictionary<string, string>();

		#region Setup Database
		public static void SetupDB()
		{
			if (File.Exists(Path.Combine(TShock.SavePath, "EssentialsHomes.sqlite")))
			{
				File.Move(Path.Combine(TShock.SavePath, "EssentialsHomes.sqlite"), Path.Combine(TShock.SavePath, "Deprecated-EssentialsHomes.sqlite"));
			}
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					string[] host = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection()
					{
						ConnectionString = string.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
						host[0],
						host.Length == 1 ? "3306" : host[1],
						TShock.Config.MySqlDbName,
						TShock.Config.MySqlUsername,
						TShock.Config.MySqlPassword)
					};
					break;
				case "sqlite":
					string sql = Path.Combine(TShock.SavePath, "Essentials", "Essentials.db");
					db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;
			}
			SqlTableCreator sqlcreator = new SqlTableCreator(db,
			db.GetSqlType() == SqlType.Sqlite ? (IQueryBuilder)new SqliteQueryCreator() : new MysqlQueryCreator());
			sqlcreator.EnsureExists(new SqlTable("Homes",
				new SqlColumn("UserID", MySqlDbType.Int32),
				new SqlColumn("HomeX", MySqlDbType.Int32),
				new SqlColumn("HomeY", MySqlDbType.Int32),
				new SqlColumn("Name", MySqlDbType.Text),
				new SqlColumn("WorldID", MySqlDbType.Int32)));
			sqlcreator.EnsureExists(new SqlTable("Nicknames",
				new SqlColumn("Name", MySqlDbType.Text),
				new SqlColumn("Nickname", MySqlDbType.Text)));

			nicknames = ListNicknames();
		}
		#endregion

		#region List home names
		public static List<string> GetNames(int UserID, int WorldID)
		{
			String query = "SELECT Name FROM Homes WHERE UserID=@0 AND WorldID=@1;";
			List<string> names = new List<string>();

			lock (db)
			{
				try
				{
					var reader = db.QueryReader(query, UserID, WorldID);
					while (reader.Read())
					{
						names.Add(reader.Get<string>("Name"));
					}
					reader.Connection.Close();
				}
				catch (Exception e)
				{
					Log.ConsoleError(e.Message);
					return names;
				}
			}
			return names;
		}
		#endregion

		#region Add Home
		public static bool AddHome(int UserID, int HomeX, int HomeY, string Name, int WorldID)
		{
			String query = "INSERT INTO Homes (UserID, HomeX, HomeY, Name, WorldID) VALUES (@0, @1, @2, @3, @4);";

			lock (db)
			{
				try
				{
					if (db.Query(query, UserID, HomeX, HomeY, Name, WorldID) != 1)
					{
						Log.ConsoleError("[EssentialsDB] Creating a user's MyHome has faild");
						return false;
					}
				}
				catch (Exception e)
				{
					Log.ConsoleError(e.Message);
					return false;
				}
			}
			return true;
		}
		#endregion

		#region Update Home
		public static bool UpdateHome(int HomeX, int HomeY, int UserID, string Name, int WorldID)
		{
			String query = "UPDATE Homes SET HomeX=@0, HomeY=@1 WHERE UserID=@2 AND Name=@3 AND WorldID=@4;";

			lock (db)
			{
				try
				{
					if (db.Query(query, HomeX, HomeY, UserID, Name, WorldID) != 1)
					{
						Log.ConsoleError("[EssentialsDB] Updating a user's MyHome has failed!");
						return false;
					}
				}
				catch (Exception e)
				{
					Log.ConsoleError(e.Message);
					return false;
				}
			}
			return true;
		}
		#endregion

		#region Remove Home
		public static bool RemoveHome(int UserID, string Name, int WorldID)
		{
			String query = "DELETE FROM Homes WHERE UserID=@0 AND Name=@1 AND WorldID=@2;";

			lock (db)
			{
				try
				{
					if (db.Query(query, UserID, Name, WorldID) != 1)
					{
						Log.ConsoleError("[EssentialsDB] Removing a user's MyHome has faild");
						return false;
					}
				}
				catch (Exception e)
				{
					Log.ConsoleError(e.Message);
					return false;
				}
			}
			return true;
		}
		#endregion

		#region Get Home Position
		public static void GetHome(int UserID, string Name, int WorldID, out int HomeX, out int HomeY)
		{
			HomeX = -1; HomeY = -1;
			String query = "SELECT HomeX, HomeY FROM Homes WHERE UserID=@0 AND Name=@1 AND WorldID=@2;";

			lock (db)
			{
				var reader = db.QueryReader(query, UserID, Name, WorldID);
				try
				{
					if (reader.Read())
					{
						HomeX = reader.Get<int>("HomeX");
						HomeY = reader.Get<int>("HomeY");
					}
					reader.Connection.Close();
				}
				catch (Exception e)
				{
					Log.ConsoleError(e.Message);
					return;
				}
			}
		}
		#endregion

		#region List Nicknames
		public static Dictionary<string,string> ListNicknames()
		{
			String query = "SELECT * FROM Nicknames;";
			Dictionary<string, string> names = new Dictionary<string, string>();

			lock (db)
			{
				var reader = db.QueryReader(query);
				while (reader.Read())
				{
					names.Add(reader.Get<string>("Name"), reader.Get<string>("Nickname"));
				}
				reader.Connection.Close();
			}
			return names;
		}
		#endregion

		#region Add Nickname
		public static bool AddNickname(string Name, string Nickname)
		{
			nicknames.Add(Name, Nickname);

			String query = "INSERT INTO Nicknames (Name, Nickname) VALUES (@0, @1);";

			lock (db)
			{
				try
				{
					if (db.Query(query, Name, Nickname) != 1)
					{
						Log.ConsoleError("[EssentialsDB] Creating a user's Nickname has faild");
						return false;
					}
				}
				catch (Exception e)
				{
					Log.ConsoleError(e.Message);
					return false;
				}
			}
			return true;
		}
		#endregion

		#region Update Nickname
		public static bool UpdateNickname(string Name, string Nickname)
		{
			nicknames[Name] = Nickname;

			String query = "UPDATE Nicknames SET Nickname=@0 WHERE Name=@1;";

			lock (db)
			{
				try
				{
					if (db.Query(query, Nickname, Name) != 1)
					{
						Log.ConsoleError("[EssentialsDB] Updating a user's Nickname has failed!");
						return false;
					}
				}
				catch (Exception e)
				{
					Log.ConsoleError(e.Message);
					return false;
				}
			}
			return true;
		}
		#endregion

		#region Remove Nickname
		public static bool RemoveNickname(string Name)
		{
			nicknames.Remove(Name);

			String query = "DELETE FROM Nicknames WHERE Name=@0;";

			lock (db)
			{
				try
				{
					if (db.Query(query, Name) != 1)
					{
						Log.ConsoleError("[EssentialsDB] Removing a user's Nickname has faild");
						return false;
					}
				}
				catch (Exception e)
				{
					Log.ConsoleError(e.Message);
					return false;
				}
			}
			return true;
		}
		#endregion
	}
}
