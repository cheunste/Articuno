using log4net;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    class DatabaseInterface
    {

        private string SYSTEM_TABLE             = "SystemParameters";
        private string MET_TOWER_TABLE          = "MetTower";
        private string TURBINE_OPC_TABLE        = "TurbineOpcTag";
        private string PERFORMANCE_FILTER_TABLE = "PerformanceTable";
        
        SQLiteConnection articunoDBConnection;

        //Log
        private static readonly ILog log = LogManager.GetLogger(typeof(Articuno));

        //Returns true if the Articuno sqlite database is not found 
        public Boolean databaseNotFound()
        {
            try
            {
                openConnection();
                closeConnection();
            }
            catch (Exception e)
            {
                return true;
            }
            return false;
        }

        //Open connection to the SQLLite DB file
        public SQLiteConnection openConnection()
        {
            string dataSource = "C:\\Users\\Stephen\\Desktop\\articuno.db";
            dataSource = ".\\articuno.db";
            
            //this.articunoDBConnection = new SQLiteConnection("Data Source = Articuno.db;Version=3;");
            this.articunoDBConnection = new SQLiteConnection("Data Source ="+dataSource+";Version=3;");

            articunoDBConnection.Open();

            return articunoDBConnection;
        }

        //Close connection to the SQLLite DB file
        public void closeConnection()
        {
            articunoDBConnection.Close();
        }

        public void closeConnection(SQLiteConnection connection)
        {
            connection.Close();
        }



        //Used for executing read queries. Doesn't check to see if artiunoDBConnection is null or not
        public SQLiteDataReader readCommand(string command)
        {
            SQLiteCommand cmd = new SQLiteCommand(command, articunoDBConnection);
            return cmd.ExecuteReader();
        }

        //Used for update queries. Doesn't check to see if artiunoDBConnection is null or no
        public int updateCommand(string command)
        {
            SQLiteCommand cmd = new SQLiteCommand(command, articunoDBConnection);
            return cmd.ExecuteNonQuery();
        }

        public List<Turbine> getTurbineList()
        {
            //TODO: Implement
            throw new NotImplementedException();
        }

        /// <summary>
        /// Gets the name of the OPC Server
        /// </summary>
        /// <returns></returns>
        public string getOpcServer()
        {
            //TODO: Implement
            openConnection();
            SQLiteDataReader result =readCommand("SELECT Description from "+SYSTEM_TABLE);
            closeConnection();
            throw new NotImplementedException();
            return "";
        }

        public string getMetTower()
        {
            //TODO: check query
            openConnection();
            SQLiteDataReader result =readCommand("SELECT * FROM"+ MET_TOWER_TABLE);
            closeConnection();
            //throw new NotImplementedException();
            return "";
        }
    }
}
