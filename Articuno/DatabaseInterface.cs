﻿using log4net;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    class DatabaseInterface
    {

        private static readonly string SYSTEM_TABLE = "SystemParameters";
        private static readonly string MET_TOWER_TABLE = "MetTower";
        private static readonly string TURBINE_INPUT_TABLE = "TurbineInputTags";
        private static readonly string PERFORMANCE_FILTER_TABLE = "PerformanceTable";

        static string dataSource = ".\\articuno.db";
        static string ConnectionString = String.Format("Data Source ={0};Version=3;", dataSource);

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(ArticunoMain));

        private DatabaseInterface()
        {

        }

        public static DatabaseInterface Instance { get { return Nested.instance; } }

        private class Nested
        {
            static Nested()
            {

            }
            internal static readonly DatabaseInterface instance = new DatabaseInterface();
        }



        //Returns true if the Articuno sqlite database is not found 
        public Boolean databaseNotFound()
        {
            try
            {
            }
            catch (Exception e)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Used for executing read queries. Doesn't check to see if artiunoDBConnection is null or not 
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        //public SQLiteDataReader readCommand(string command)
        //{
        //    var content = new Dictionary<string, string>();
        //    using (SQLiteConnection c = new SQLiteConnection(ConnectionString))
        //    {
        //        c.Open();
        //        using (SQLiteCommand cmd = new SQLiteCommand(command, c))
        //        {
        //            // return cmd.ExecuteReader();
        //            using (SQLiteDataReader reader = cmd.ExecuteReader())
        //            {
        //                int i = 0;
        //                while (reader.Read())
        //                {
        //                    content.Add(reader.GetOriginalName(i), reader.GetString(i));
        //                    i++;
        //                }
        //            }

        //        }
        //    }
        //    return null;
        //    //SQLiteCommand cmd = new SQLiteCommand(command, articunoDBConnection);
        //}
        public DataTable readCommand2(string command)
        {
            List<List<object>> content = new List<List<object>>();
            List<object> sublist = new List<object>();
            DataTable dt = new DataTable();
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(command, connection))
                {
                    using (SQLiteDataReader reader = cmd.ExecuteReader())
                    {
                        //You do things here
                        dt.Load(reader);
                    }
                }
            }
            return dt;
            //SQLiteCommand cmd = new SQLiteCommand(command, articunoDBConnection);
        }

        /// <summary>
        /// Used for update queries. Doesn't check to see if artiunoDBConnection is null or no
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public int updateCommand(string command)
        {
            using (SQLiteConnection connection = new SQLiteConnection(ConnectionString))
            {
                connection.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(command, connection))
                {
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Turbine> getTurbineList()
        {
            //TODO: Implement
            DataTable result = readCommand2("SELECT TurbineId from " + TURBINE_INPUT_TABLE);

            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the name of the OPC Server
        /// </summary>
        /// <returns></returns>
        public string getOpcServer()
        {
            //TODO: Implement
            DataTable result = readCommand2("SELECT Description from " + SYSTEM_TABLE);
            throw new NotImplementedException();
            return "";
        }

        public string getMetTower()
        {
            //TODO: check query
            DataTable result = readCommand2("SELECT * FROM" + MET_TOWER_TABLE);
            //throw new NotImplementedException();
            return "";
        }
    }
}
