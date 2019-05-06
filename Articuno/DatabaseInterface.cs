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
        SQLiteConnection articunoDBConnection;

        internal String[] getTurbineList()
        {
            throw new NotImplementedException();
        }

        internal String getOPCServer()
        {
            throw new NotImplementedException();
        }

        internal String[] getmetTower()
        {
            throw new NotImplementedException();
        }

        //Returns true if the Articuno sqlite database is not found 
        public Boolean databaseNotFound()
        {
            try
            {
                openConnection();
                closeConnect();
            }
            catch (Exception e)
            {
                return true;
            }
            return false;
        }

        //Open connection to the SQLLite DB file
        private void openConnection()
        {
            this.articunoDBConnection = new SQLiteConnection("Data Source = Articuno.sqlite;Version=3;");
            articunoDBConnection.Open();
        }

        //Close connection to the SQLLite DB file
        private void closeConnect()
        {
            articunoDBConnection.Close();
        }

        //Used for executing read queries
        public SQLiteDataReader readCommand(string command)
        {
            SQLiteCommand cmd = new SQLiteCommand(command, articunoDBConnection);
            return cmd.ExecuteReader();
        }

        //Used for update queries. 
        public int updateCommand(string command)
        {
            SQLiteCommand cmd = new SQLiteCommand(command, articunoDBConnection);
            return cmd.ExecuteNonQuery();
        }

        public List<Turbine> getTurbines()
        {
            //TODO: Implement
            return null;
        }

        public string getOpcServer()
        {
            //TODO: Implement
            return "";
        }

        public string getMetTower()
        {
            //TODO: IMplement
            return "";
        }
    }
}
