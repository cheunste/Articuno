using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data.SQLite;
using System.Data;
using System.Collections.Generic;

namespace ArticunoTest {
    [TestClass]
    public class DatabaseInterfaceTest {
        DatabaseInterface dbi;
        public DatabaseInterfaceTest() {
            dbi = DatabaseInterface.Instance;
        }

        [TestMethod]
        //Test to get the description column from various tables
        [DataTestMethod]
        [DataRow("SELECT * from SystemInputTags WHERE Description!='SitePrefix' AND Description!='OpcServerName'")]
        [DataRow("SELECT * from SystemInputTags WHERE Description='OpcServerName'")]
        public void readFromSystemInputTableTest(string sqlcmd) {
            //Get all the columns from the SystemParemeters
            //NOTE: To see the output, click the output in 'Test Explorer' after the test is executed.
            DataTable reader = dbi.readQuery(sqlcmd);
            Console.WriteLine(reader.Rows.Count);

            for (int i = 0; i < reader.Rows.Count; i++) {
                Console.WriteLine(reader.Rows[i]["OpcTag"].ToString());
            }

            for (int i = 0; i < reader.Rows.Count; i++) {
                Console.WriteLine(reader.Rows[i]["DefaultValue"].ToString());
            }
        }

        [TestMethod]
        [DataTestMethod]
        //[DataRow("SELECT NrsMode,OperatingState, Participation from TurbineInputTags")]
        [DataRow("SELECT TurbineId,OperatingState, Participation, NrsMode from TurbineInputTags")]
        //[DataRow("SELECT TurbineId from TurbineOutputTags")]
        //[DataRow("SELECT * from MetTowerInputTags")]
        //[DataRow("SELECT * from MetTowerOutputTags")]
        //[DataRow("SELECT TurbineId from TurbineInputTags")]
        //[DataRow("SELECT TurbineId from TurbineOutputTags")]
        public void readFromTurbine(string sqlcmd) {
            DataTable reader = dbi.readQuery(sqlcmd);
            Console.WriteLine(reader.Rows.Count);

            for (int i = 0; i < reader.Rows.Count; i++) {
                Console.WriteLine(reader.Rows[i]["TurbineId"].ToString());
            }

            for (int i = 0; i < reader.Rows.Count; i++) {
                Console.WriteLine(reader.Rows[i]["Participation"].ToString());
            }

        }



        [TestMethod]
        //Test to see if you are able to write to the database. 
        //You'll only be writing to the XXXXXOuputTags tables
        public void updateTest() {
            //For simplicity, write to the SystemOutputTgas table
            //Open the DB

            //generate a random int
            Random rnd = new Random();
            int randomNumber = rnd.Next();
            //testConnection.Update
            string sqlcmd = String.Format("UPDATE SystemOutputTags SET DefaultValue ='{0}' WHERE Description = 'Heartbeat'", randomNumber);
            dbi.updateDatabaseWithQuery(sqlcmd);

            sqlcmd = "SELECT DefaultValue from SystemOutputTags where Description='Heartbeat'";
            DataTable reader = dbi.readQuery(sqlcmd);
            int readHeartbeat = Convert.ToInt32(reader.Rows[0]["DefaultValue"]);
            Assert.AreEqual(readHeartbeat, randomNumber);
            Console.WriteLine("Random Number: {0}", randomNumber);
            Console.WriteLine("Value in DB: {0}", readHeartbeat);
        }

        [TestMethod]
        //Test to see if it is able to get items from the turbineInput tables
        [DataTestMethod]
        //[DataRow("SELECT Pause from TurbineInputTags WHERE TurbineId=", "'T001'")]
        [DataRow("SELECT NrsMode from TurbineInputTags WHERE TurbineId=", "'T001'")]
        [DataRow("SELECT OperatingState from TurbineInputTags WHERE TurbineId=", "'T001'")]
        [DataRow("SELECT RotorSpeed from TurbineInputTags WHERE TurbineId=", "'T001'")]
        [DataRow("SELECT Temperature from TurbineInputTags WHERE TurbineId=", "'T001'")]
        [DataRow("SELECT WindSpeed from TurbineInputTags WHERE TurbineId=", "'T001'")]

        public void turbineTableTest(string cmd, string turbinePrefix) {
            string sqlcmd = cmd + turbinePrefix;
            DataTable reader = dbi.readQuery(sqlcmd);
            Assert.IsNotNull(reader.Rows[0][0]);
            Console.WriteLine(reader.Rows[0][0]);

        }

        [TestMethod]
        public void GetTurbineAverageRotorSpeedTagTest() {
            var tag = dbi.GetTurbineAvgRotorSpeedTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }
        [TestMethod]
        public void GetTurbineAverageWindSpeedTagTest() {
            var tag = dbi.GetTurbineAvgWindSpeedTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }
        [TestMethod]
        public void GetTurbineCtrTagTest() {
            var tag = dbi.GetTurbineCtrCountdownTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        public void GetTurbineLowRotorSpeedTagTest() {
            var tag = dbi.GetTurbineLowRotorSpeedFlagTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        public void GetTurbineAgcBlockingTag() {
            var tag = dbi.GetTurbineAgcBlockingTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        public void GetTurbineMetTowerReferenceTest() {
            var tag = dbi.GetMetTowerReference("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        public void GetTurbineWindSpeedTagTest() {
            var tag = dbi.GetTurbineWindSpeedTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        public void GetTurbineTemperatureTagTest() {
            var tag = dbi.GetTurbineTemperatureTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        public void GetTurbinePauseTagTest() {
            var tag = dbi.GetTurbinePauseCommandTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        public void GetTurbineStartCmdTag() {
            var tag = dbi.GetTurbineStartCommandTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        public void GetTurbineRotorSpeedTagTest() {
            var tag = dbi.GetTurbineRotorSpeedTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        public void GetTurbineParticipationTag() {
            var tag = dbi.GetTurbineParticiaptionTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        //This field can be empty in the database
        public void GetTurbineNrsTagTest() {
            var tag = dbi.GetTurbineNrsModeTag("T001");
            Assert.IsNotNull(tag);
        }

        [TestMethod]
        public void GetOperatingStateTagTest() {
            var tag = dbi.GetTurbineOperatingStateTag("T001");
            Assert.IsTrue(tag != "", "Tag contains no value");
            Assert.IsNotNull(tag);
        }
    }
}
