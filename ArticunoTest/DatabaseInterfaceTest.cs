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
        public void readTest()
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
            string sqlcmd = "Select * from SystemParameters";
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
                    reader["id"] +" " + reader["WindSpeed"]+  " " + reader["RotorSpeed"]+  " " + reader["RotorSpeedNrs"]+  " " + reader["StdDevNrs"]
                );
            }

            //Get everything from the MetTower table
            sqlcmd = "Select * from MetTower";
            reader = dbi.readCommand(sqlcmd);

            while (reader.Read())
            {
                Assert.IsNotNull(reader["MetId"]);
                Console.WriteLine(
                    reader["MetId"]
                );
            }

            //Get the Turbine Ids from the TurbineOpcTag table
            sqlcmd = "Select TurbineId from TurbineOpcTag";
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
        public void updateTest()
        {
            Assert.Fail(); 
        }

        [TestMethod]
        public void getTurbineListTest()
        {
            Assert.Fail(); 
        }

        [TestMethod]
        public void getOpcServerTest()
        {
            Assert.Fail(); 
        }
        [TestMethod]
        public void getMetTowerTest()
        {

            Assert.Fail(); 
        }

    }
}
