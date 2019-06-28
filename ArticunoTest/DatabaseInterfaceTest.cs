using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data.SQLite;
using System.Data;
using System.Collections.Generic;

namespace ArticunoTest
{
    [TestClass]
    public class DatabaseInterfaceTest
    {
        DatabaseInterface dbi;
        public DatabaseInterfaceTest()
        {
            dbi = DatabaseInterface.Instance;
        }

        [TestMethod]
        //Test to get the description column from various tables
        [DataTestMethod]
        [DataRow("Select * from SystemInputTags")]
        //[DataRow("Select * from PerformanceTable limit 5")]
        //[DataRow("Select TurbineId from TurbineOutputTags")]
        //[DataRow("Select * from MetTowerInputTags")]
        //[DataRow("Select * from MetTowerOutputTags")]
        //[DataRow("Select TurbineId from TurbineInputTags")]
        //[DataRow("Select TurbineId from TurbineOutputTags")]
        public void readFromDBTest(string sqlcmd)
        {
            //Get all the columns from the SystemParemeters
            //NOTE: To see the output, click the output in 'Test Explorer' after the test is executed.
            DataTable reader = dbi.readCommand2(sqlcmd);
            var derp = reader.Columns["OpcTag"];
            Console.WriteLine(reader.Rows[0]["OpcTag"].ToString());

            //foreach(DataColumn col  in reader.Columns)
            //{
            //    Console.WriteLine("Header: {0}",col);
            //}
            //foreach(DataRow row in reader.Rows)
            //{
            //    Console.WriteLine(row[0]);
            //}
        }

        [TestMethod]
        //Test to see if you are able to write to the database. 
        //You'll only be writing to the XXXXXOuputTags tables
        public void updateTest()
        {
            //For simplicity, write to the SystemOutputTgas table
            //Open the DB

            //generate a random int
            Random rnd = new Random();
            int randomNumber = rnd.Next();
            //testConnection.Update
            string sqlcmd = "UPDATE SystemOutputTags SET 'Default' ='" + randomNumber + "' WHERE Description = 'Heartbeat'";
            dbi.updateCommand(sqlcmd);

            DataTable reader = dbi.readCommand2(sqlcmd);
            sqlcmd = "Select 'Default' from SystemOutputTags where Description='Heartbeat'";
            int readHeartbeat = Convert.ToInt16(reader.Rows[0]["'Default'"]);
            Assert.AreEqual(reader.Rows[0]["Default"], randomNumber);
            Console.WriteLine("Random Number: " + randomNumber);
            Console.WriteLine("Value in DB: " + reader.Rows[0]["Default"]);
        }

        [TestMethod]
        //Test to see if it is able to get items from the turbineInput tables
        [DataTestMethod]
        [DataRow("SELECT Pause from TurbineInputTags WHERE TurbineId=", "'T001'")]
        [DataRow("SELECT NrsMode from TurbineInputTags WHERE TurbineId=", "'T001'")]
        [DataRow("SELECT OperatingState from TurbineInputTags WHERE TurbineId=", "'T001'")]
        [DataRow("SELECT RotorSpeed from TurbineInputTags WHERE TurbineId=", "'T001'")]
        [DataRow("SELECT Temperature from TurbineInputTags WHERE TurbineId=", "'T001'")]
        [DataRow("SELECT WindSpeed from TurbineInputTags WHERE TurbineId=", "'T001'")]

        public void turbineTableTest(string cmd, string turbinePrefix)
        {
            string sqlcmd = cmd + turbinePrefix;
            DataTable reader = dbi.readCommand2(sqlcmd);
            Assert.IsNotNull(reader.Rows[0][0]);
            Console.WriteLine(reader.Rows[0][0]);

        }
    }
}
