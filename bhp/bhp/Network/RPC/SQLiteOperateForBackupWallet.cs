using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;

namespace Bhp.Network.RPC
{
    public static class SQLiteOperateForBackupWallet
    {
        public static SQLiteConnection m_dbConnection;
        public static SQLiteCommand command;
        public static string BackupWalletName;

        public static string BackupWalletToSQLite(List<string> wifs, string WalletName)
        {
            CreateNewDatabase(WalletName);
            ConnectToDatabase(BackupWalletName);
            CreateTable();
            InsertTable(wifs);
            return BackupWalletName;
        }

        public static List<string> RecoverWalletFromSQLite(string datebasename)
        {
            if (!File.Exists(datebasename))
            {
                return null;
            }
            ConnectToDatabase(datebasename);
            string sql1 = "SELECT count(*) FROM sqlite_master WHERE type = 'table' AND name = 'Wallet'";
            command.CommandText = sql1;
            int i = Convert.ToInt32(command.ExecuteScalar());
            //判断数据表是否存在 
            if (i > 0)
            {
                //存在
                string sql2 = "SELECT count(*) FROM Wallet WHERE 1=1";
                command.CommandText = sql2;
                int j = Convert.ToInt32(command.ExecuteScalar());
                if (j > 0) //表中存在数据
                {
                    return QueryTable();
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        //创建一个空的数据库
        static void CreateNewDatabase(string WalletName)
        {
            BackupWalletName = $"{WalletName}-backup-{System.DateTime.Now.ToString("yyyymmddHHmmss")}.sqlite";
            if (!File.Exists(BackupWalletName))
            {
                SQLiteConnection.CreateFile(BackupWalletName);
            }
        }

        //创建一个连接到指定数据库
        static void ConnectToDatabase(string BackupWalletName)
        {
            m_dbConnection = new SQLiteConnection($"Data Source={BackupWalletName};Version=3;");
            m_dbConnection.Open();
            command = new SQLiteCommand(m_dbConnection);
        }

        //在指定数据库中创建一个table
        static void CreateTable()
        {
            string sql1 = "SELECT count(*) as CountTable FROM sqlite_master WHERE type = 'table' AND name = 'Wallet'";
            command.CommandText = sql1;
            int i = Convert.ToInt32(command.ExecuteScalar());
            //判断数据表是否存在 
            if (i == 1)
            {
                //
            }
            else
            {
                string sql = "create table Wallet (wif varchar(100))";
                command.CommandText = sql;
                command.ExecuteNonQuery();
            }
        }

        static void InsertTable(List<string> wifs)
        {
            DbTransaction trans = m_dbConnection.BeginTransaction();//开始事务
            try
            {
                command.CommandText = "insert into Wallet values(@a)";
                foreach (string wif in wifs)
                {
                    command.Parameters.Add(new SQLiteParameter("@a", DbType.String));
                    command.Parameters["@a"].Value = wif;
                    command.ExecuteNonQuery();
                }
                trans.Commit();
            }
            catch (Exception ex)
            {
                trans.Rollback();
                throw new Exception(ex.Message.ToString());
            }
            finally
            {
                m_dbConnection.Close();
            }
        }

        static List<string> QueryTable()
        {
            List<string> wifs = new List<string>();
            string sql = "select wif From Wallet where 1=1";
            command.CommandText = sql;
            SQLiteDataReader reader = command.ExecuteReader();

            while (reader.Read())
            {
                wifs.Add(reader["wif"].ToString().Trim());
            }
            m_dbConnection.Close();
            return wifs;
        }
    }
}