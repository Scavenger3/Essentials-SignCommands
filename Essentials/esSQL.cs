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

		#region Setup Database
		internal static void SetupDB()
		{
			if (TShock.Config.StorageType.ToLower() == "sqlite")
			{
				string sql = Path.Combine(TShock.SavePath, "EssentialsHomes.sqlite");
				db = new SqliteConnection(string.Format("uri=file://{0},Version=3", sql));
			}
			else if (TShock.Config.StorageType.ToLower() == "mysql")
			{
				try
				{
					var hostport = TShock.Config.MySqlHost.Split(':');
					db = new MySqlConnection();
					db.ConnectionString =
						String.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4};",
									  hostport[0],
									  hostport.Length > 1 ? hostport[1] : "3306",
									  TShock.Config.MySqlDbName,
									  TShock.Config.MySqlUsername,
									  TShock.Config.MySqlPassword
							);
				}
				catch (MySqlException ex)
				{
					Log.Error(ex.ToString());
					throw new Exception("MySql not setup correctly");
				}
			}
			else
			{
				throw new Exception("Invalid storage type");
			}

			var table2 = new SqlTable("EssentialsHomes",
				new SqlColumn("UserID", MySqlDbType.Int32),
				new SqlColumn("HomeX", MySqlDbType.Int32),
				new SqlColumn("HomeY", MySqlDbType.Int32),
				new SqlColumn("WorldID", MySqlDbType.Int32)
				);

			var creator2 = new SqlTableCreator(db,
											  db.GetSqlType() == SqlType.Sqlite
												? (IQueryBuilder)new SqliteQueryCreator()
												: new MysqlQueryCreator());
			creator2.EnsureExists(table2);
		}
		#endregion

		#region Has Home
		public static bool HasHome(int UserID, int WorldID)
		{
			String query = "SELECT UserID FROM EssentialsHomes WHERE WorldID=@0";

			lock (db)
			{
				var reader = db.QueryReader(query, WorldID);

				while (reader.Read())
				{
					var UID = reader.Get<int>("UserID");
					if (UID == UserID)
					{
						reader.Connection.Close();
						return true;
					}
				}
				reader.Connection.Close();
			}
			return false;
		}
		#endregion

		#region Add Home
		public static void AddHome(int UserID, int HomeX, int HomeY, int WorldID)
		{
			String query = "INSERT INTO EssentialsHomes (UserID, HomeX, HomeY, WorldID) VALUES (@0, @1, @2, @3);";

			lock (db)
			{
				if (db.Query(query, UserID, HomeX, HomeY, WorldID) != 1)
				{
					Log.ConsoleError("[Essentials] Creating a user's MyHome has faild");
				}
			}
		}
		#endregion

		#region Update Home
		public static void UpdateHome(int HomeX, int HomeY, int UserID, int WorldID)
		{
			String query = "UPDATE EssentialsHomes SET HomeX=@0, HomeY=@1 WHERE UserID=@2 AND WorldID=@3;";

			lock (db)
			{
				try
				{
					if (db.Query(query, HomeX, HomeY, UserID, WorldID) != 1)
					{
						Log.ConsoleError("[Essentials] Updating a user's MyHome has failed!");
					}
				}
				catch (Exception e)
				{
					Log.ConsoleError(e.Message);
				}
			}
		}
		#endregion

		#region Get Home
		public static void GetHome(int UserID, int WorldID, out int HomeX, out int HomeY)
		{
			HomeX = Main.spawnTileX; HomeY = Main.spawnTileY;
			String query = "SELECT HomeX, HomeY FROM EssentialsHomes WHERE UserID=@0 AND WorldID=@1;";

			lock (db)
			{
				var reader = db.QueryReader(query, UserID, WorldID);

				if (reader.Read())
				{
					HomeX = reader.Get<int>("HomeX");
					HomeY = reader.Get<int>("HomeY");
				}
				reader.Connection.Close();
			}
		}
		#endregion
	}
}
