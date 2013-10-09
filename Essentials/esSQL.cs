using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace Essentials
{
	public class esSQL
	{
		private static IDbConnection db;

		#region Setup Database
		public static void SetupDB()
		{
			if (File.Exists(Path.Combine(TShock.SavePath, "Essentials", "Essentials.db")) && !File.Exists(Path.Combine(TShock.SavePath, "Essentials", "Essentials.sqlite")))
			{
				File.Move(Path.Combine(TShock.SavePath, "Essentials", "Essentials.db"), Path.Combine(TShock.SavePath, "Essentials", "Essentials.sqlite"));
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
					string sql = Path.Combine(TShock.SavePath, "Essentials", "Essentials.sqlite");
					db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;
			}
			SqlTableCreator sqlcreator = new SqlTableCreator(db,
				db.GetSqlType() == SqlType.Sqlite
				? (IQueryBuilder)new SqliteQueryCreator()
				: new MysqlQueryCreator());
			sqlcreator.EnsureExists(new SqlTable("Homes",
				new SqlColumn("UserID", MySqlDbType.Int32),
				new SqlColumn("HomeX", MySqlDbType.Int32),
				new SqlColumn("HomeY", MySqlDbType.Int32),
				new SqlColumn("Name", MySqlDbType.Text),
				new SqlColumn("WorldID", MySqlDbType.Int32)));
			sqlcreator.EnsureExists(new SqlTable("Nicknames",
				new SqlColumn("Name", MySqlDbType.Text),
				new SqlColumn("Nickname", MySqlDbType.Text)));
		}
		#endregion

		#region List home names
		public static List<string> GetNames(int UserID, int WorldID)
		{
			String query = "SELECT Name FROM Homes WHERE UserID=@0 AND WorldID=@1;";
			List<string> names = new List<string>();

			using (var reader = db.QueryReader(query, UserID, WorldID))
			{
				while (reader.Read())
				{
					names.Add(reader.Get<string>("Name"));
				}
			}
			return names;
		}
		#endregion

		#region Add Home
		public static bool AddHome(int UserID, int HomeX, int HomeY, string Name, int WorldID)
		{
			String query = "INSERT INTO Homes (UserID, HomeX, HomeY, Name, WorldID) VALUES (@0, @1, @2, @3, @4);";

			if (db.Query(query, UserID, HomeX, HomeY, Name, WorldID) != 1)
			{
				Log.ConsoleError("[EssentialsDB] Creating a user's MyHome has faild");
				return false;
			}
			return true;
		}
		#endregion

		#region Update Home
		public static bool UpdateHome(int HomeX, int HomeY, int UserID, string Name, int WorldID)
		{
			String query = "UPDATE Homes SET HomeX=@0, HomeY=@1 WHERE UserID=@2 AND Name=@3 AND WorldID=@4;";

			if (db.Query(query, HomeX, HomeY, UserID, Name, WorldID) != 1)
			{
				Log.ConsoleError("[EssentialsDB] Updating a user's MyHome has failed!");
				return false;
			}
			return true;
		}
		#endregion

		#region Remove Home
		public static bool RemoveHome(int UserID, string Name, int WorldID)
		{
			String query = "DELETE FROM Homes WHERE UserID=@0 AND Name=@1 AND WorldID=@2;";

			if (db.Query(query, UserID, Name, WorldID) != 1)
			{
				Log.ConsoleError("[EssentialsDB] Removing a user's MyHome has faild");
				return false;
			}
			return true;
		}
		#endregion

		#region Get Home Position
		public static Point GetHome(int UserID, string Name, int WorldID)
		{
			String query = "SELECT HomeX, HomeY FROM Homes WHERE UserID=@0 AND Name=@1 AND WorldID=@2;";

			using (var reader = db.QueryReader(query, UserID, Name, WorldID))
			{
				if (reader.Read())
				{
					return new Point(reader.Get<int>("HomeX"), reader.Get<int>("HomeY"));
				}
			}
			return Point.Zero;
		}
		#endregion

		#region Get Nickname
		public static bool GetNickname(string Name, out string Nickname)
		{
			String query = "SELECT Nickname FROM Nicknames WHERE Name=@0;";
			Dictionary<string, string> names = new Dictionary<string, string>();

			using (var reader = db.QueryReader(query, Name))
			{
				if (reader.Read())
				{
					Nickname = reader.Get<string>("Nickname");
					return true;
				}
			}
			Nickname = string.Empty;
			return false;
		}
		#endregion

		#region Add Nickname
		public static bool AddNickname(string Name, string Nickname)
		{
			String query = "INSERT INTO Nicknames (Name, Nickname) VALUES (@0, @1);";

			if (db.Query(query, Name, Nickname) != 1)
			{
				Log.ConsoleError("[EssentialsDB] Creating a user's Nickname has faild");
				return false;
			}
			return true;
		}
		#endregion

		#region Update Nickname
		public static bool UpdateNickname(string Name, string Nickname)
		{
			String query = "UPDATE Nicknames SET Nickname=@0 WHERE Name=@1;";

			if (db.Query(query, Nickname, Name) != 1)
			{
				Log.ConsoleError("[EssentialsDB] Updating a user's Nickname has failed!");
				return false;
			}
			return true;
		}
		#endregion

		#region Remove Nickname
		public static bool RemoveNickname(string Name)
		{
			String query = "DELETE FROM Nicknames WHERE Name=@0;";

			if (db.Query(query, Name) != 1)
			{
				Log.ConsoleError("[EssentialsDB] Removing a user's Nickname has faild");
				return false;
			}
			return true;
		}
		#endregion
	}
}
