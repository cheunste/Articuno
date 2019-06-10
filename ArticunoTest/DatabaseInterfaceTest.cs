using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data.SQLite;
using System.Data;

namespace ArticunoTest
{
    [TestClass]
    public class DatabaseInterfaceTest
    {
        DatabaseInterface dbi;
        public DatabaseInterfaceTest()
        {
            dbi = new DatabaseInterface();
        }

        [TestMethod]
        public void connectionTest()
        {
            SQLiteConnection testConnection = dbi.openConnection();
            ConnectionState test = testConnection.State;
            Assert.AreEqual(test.ToString().ToLower(), "open");

            dbi.closeConnection(testConnection);
            test = testConnection.State;
            Assert.AreEqual(test.ToString().ToLower(), "closed");
        }

        [TestMethod]
        //Test to get the description column from various tables
        public void readFromDBTest()
        {
            //Open the DB
            SQLiteConnection testConnection = dbi.openConnection();

            //Get all the columns from the SystemParemeters
            //NOTE: To see the output, click the output in 'Test Explorer' after the test is executed.
            string sqlcmd = "Select * from SystemInputTags";
            SQLiteDataReader reader = dbi.readCommand(sqlcmd);

            while (reader.Read())
            {
                Assert.IsNotNull(reader["Description"]);
                Console.WriteLine(
                    reader["Description"]
                    );
            }

            sqlcmd = "Select * from PerformanceTable limit 5";
            reader = dbi.readCommand(sqlcmd);

            while (reader.Read())
            {
                Assert.IsNotNull(reader["id"]);
                Assert.IsNotNull(reader["WindSpeed"]);
                Assert.IsNotNull(reader["RotorSpeed"]);
                Assert.IsNotNull(reader["RotorSpeedNrs"]);
                Assert.IsNotNull(reader["StdDevNrs"]);
                Console.WriteLine(
                    reader["id"] + " " + reader["WindSpeed"] + " " + reader["RotorSpeed"] + " " + reader["RotorSpeedNrs"] + " " + reader["StdDevNrs"]
                );
            }

            //Get everything from the MetTower tables
            sqlcmd = "Select * from MetTowerInputTags";
            reader = dbi.readCommand(sqlcmd);

            while (reader.Read())
            {
                Assert.IsNotNull(reader["MetId"]);
                Console.WriteLine(
                    reader["MetId"]
                );
            }

            sqlcmd = "Select * from MetTowerOutputTags";
            reader = dbi.readCommand(sqlcmd);

            while (reader.Read())
            {
                Assert.IsNotNull(reader["MetId"]);
                Console.WriteLine(
                    reader["MetId"]
                );
            }

            //Get the Turbine Ids from the TurbineINputTags and TurbineOutputTable table
            sqlcmd = "Select TurbineId from TurbineInputTags";
            reader = dbi.readCommand(sqlcmd);

            while (reader.Read())
            {
                Assert.IsNotNull(reader["TurbineId"]);
                Console.WriteLine(
                    reader["TurbineId"]
                );
            }

            sqlcmd = "Select TurbineId from TurbineOutputTags";
            reader = dbi.readCommand(sqlcmd);

            while (reader.Read())
            {
                Assert.IsNotNull(reader["TurbineId"]);
                Console.WriteLine(
                    reader["TurbineId"]
                );
            }

            //Close the DB
            dbi.closeConnection(testConnection);
        }

        [TestMethod]
        //Test to see if you are able to write to the database. 
        //You'll only be writing to the XXXXXOuputTags tables
        public void updateTest()
        {
            //For simplicity, write to the SystemOutputTgas table
            //Open the DB
            SQLiteConnection testConnection = dbi.openConnection();

            //generate a random int
            Random rnd = new Random();
            int randomNumber = rnd.Next();
            //testConnection.Update
            string sqlcmd = "UPDATE SystemOutputTags SET 'Default' ='" + randomNumber + "' WHERE Description = 'Heartbeat'";
            dbi.updateCommand(sqlcmd);

            SQLiteDataReader reader = dbi.readCommand(sqlcmd);
            sqlcmd = "Select Default from SystemOutputTags where Description='Heartbeat'";
            while (reader.Read())
            {
                Assert.AreEqual(reader["Default"], randomNumber);
                Console.WriteLine("Random Number: " + randomNumber);
                Console.WriteLine("Value in DB: " + reader["Default"]);
            }
            //Close the DB
            dbi.closeConnection(testConnection);
        }

        [TestMethod]
        //Test to see if it is able to get items from the turbineInput tables
        public void turbineTableTest()
        {
            SQLiteConnection testConnection = dbi.openConnection();
            string sqlcmd = "";
            string turbinePrefix = "'T001'";

            sqlcmd = "SELECT Pause from TurbineInputTags WHERE TurbineId=" + turbinePrefix;
            SQLiteDataReader reader = dbi.readCommand(sqlcmd);
            while (reader.Read())
            {
                Assert.IsNotNull(reader[0]);
                Console.WriteLine(reader[0]);

            }

            sqlcmd = "SELECT NrsMode from TurbineInputTags WHERE TurbineId=" + turbinePrefix;
            reader = dbi.readCommand(sqlcmd);
            while (reader.Read())
            {
                Assert.IsNotNull(reader[0]);
                Console.WriteLine(reader[0]);
            }

            sqlcmd = "SELECT OperatingState from TurbineInputTags WHERE TurbineId=" + turbinePrefix;
            reader = dbi.readCommand(sqlcmd);
            while (reader.Read())
            {
                Assert.IsNotNull(reader[0]);
                Console.WriteLine(reader[0]);
            }

            sqlcmd = "SELECT RotorSpeed from TurbineInputTags WHERE TurbineId=" + turbinePrefix;
            reader = dbi.readCommand(sqlcmd);
            while (reader.Read())
            {
                Assert.IsNotNull(reader[0]);
                Console.WriteLine(reader[0]);
            }

            sqlcmd = "SELECT Temperature from TurbineInputTags WHERE TurbineId=" + turbinePrefix;
            reader = dbi.readCommand(sqlcmd);
            while (reader.Read())
            {
                Assert.IsNotNull(reader[0]);
                Console.WriteLine(reader[0]);
            }

            sqlcmd = "SELECT WindSpeed from TurbineInputTags WHERE TurbineId=" + turbinePrefix;
            reader = dbi.readCommand(sqlcmd);
            while (reader.Read())
            {
                Assert.IsNotNull(reader[0]);
                Console.WriteLine(reader[0]);
            }

            //Close the DB
            dbi.closeConnection(testConnection);
        }
    }
}
