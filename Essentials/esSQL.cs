using System.Collections.Generic;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;
using TShockAPI;
using TShockAPI.DB;

namespace Essentials
{
	public class EsSql
	{
		private static IDbConnection _db;

		#region Setup Database
		public static void SetupDb()
		{
			if (File.Exists(Path.Combine(TShock.SavePath, "Essentials", "Essentials.db")) && !File.Exists(Path.Combine(TShock.SavePath, "Essentials", "Essentials.sqlite")))
			{
				File.Move(Path.Combine(TShock.SavePath, "Essentials", "Essentials.db"), Path.Combine(TShock.SavePath, "Essentials", "Essentials.sqlite"));
			}
			switch (TShock.Config.StorageType.ToLower())
			{
				case "mysql":
					var host = TShock.Config.MySqlHost.Split(':');
					_db = new MySqlConnection
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
					var sql = Path.Combine(TShock.SavePath, "Essentials", "Essentials.sqlite");
					_db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
					break;
			}
			var sqlcreator = new SqlTableCreator(_db,
				_db.GetSqlType() == SqlType.Sqlite
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
		public static List<string> GetNames(int userId, int worldId)
		{
			const string query = "SELECT Name FROM Homes WHERE UserID=@0 AND WorldID=@1;";
			var names = new List<string>();

			using (var reader = _db.QueryReader(query, userId, worldId))
			{
				while (reader.Read())
					names.Add(reader.Get<string>("Name"));
			}
			return names;
		}
		#endregion

		#region Add Home
		public static bool AddHome(int userId, int homeX, int homeY, string name, int worldId)
		{
			const string query = "INSERT INTO Homes (UserID, HomeX, HomeY, Name, WorldID) VALUES (@0, @1, @2, @3, @4);";

		    if (_db.Query(query, userId, homeX, homeY, name, worldId) == 1) return true;
		    Log.ConsoleError("[EssentialsDB] Creating a user's MyHome has failed");
		    return false;
		}
		#endregion

		#region Update Home
		public static bool UpdateHome(int homeX, int homeY, int userId, string name, int worldId)
		{
			const string query = "UPDATE Homes SET HomeX=@0, HomeY=@1 WHERE UserID=@2 AND Name=@3 AND WorldID=@4;";

		    if (_db.Query(query, homeX, homeY, userId, name, worldId) == 1) return true;
		    Log.ConsoleError("[EssentialsDB] Updating a user's MyHome has failed!");
		    return false;
		}
		#endregion

		#region Remove Home
		public static bool RemoveHome(int userId, string name, int worldId)
		{
			const string query = "DELETE FROM Homes WHERE UserID=@0 AND Name=@1 AND WorldID=@2;";

		    if (_db.Query(query, userId, name, worldId) == 1) return true;
		    Log.ConsoleError("[EssentialsDB] Removing a user's MyHome has faild");
		    return false;
		}
		#endregion

		#region Get Home Position
		public static Point GetHome(int userId, string name, int worldId)
		{
			const string query = "SELECT HomeX, HomeY FROM Homes WHERE UserID=@0 AND Name=@1 AND WorldID=@2;";

			using (var reader = _db.QueryReader(query, userId, name, worldId))
			{
				if (reader.Read())
					return new Point(reader.Get<int>("HomeX"), reader.Get<int>("HomeY"));
			}
			return Point.Zero;
		}
		#endregion

		#region Get Nickname
		public static bool GetNickname(string name, out string nickname)
		{
			const string query = "SELECT Nickname FROM Nicknames WHERE Name=@0;";

			using (var reader = _db.QueryReader(query, name))
			{
				if (reader.Read())
				{
					nickname = reader.Get<string>("Nickname");
					return true;
				}
			}
			nickname = string.Empty;
			return false;
		}
		#endregion

		#region Add Nickname
		public static bool AddNickname(string name, string nickname)
		{
	        const string query = "INSERT INTO Nicknames (Name, Nickname) VALUES (@0, @1);";

		    if (_db.Query(query, name, nickname) == 1) return true;
		    Log.ConsoleError("[EssentialsDB] Creating a user's Nickname has faild");
		    return false;
		}
		#endregion

		#region Update Nickname
		public static bool UpdateNickname(string name, string nickname)
		{
			const string query = "UPDATE Nicknames SET Nickname=@0 WHERE Name=@1;";

		    if (_db.Query(query, nickname, name) == 1) return true;
		    Log.ConsoleError("[EssentialsDB] Updating a user's Nickname has failed!");
		    return false;
		}
		#endregion

		#region Remove Nickname
		public static bool RemoveNickname(string name)
		{
			const string query = "DELETE FROM Nicknames WHERE Name=@0;";

		    if (_db.Query(query, name) == 1) return true;
		    Log.ConsoleError("[EssentialsDB] Removing a user's Nickname has faild");
		    return false;
		}
		#endregion
	}
}
