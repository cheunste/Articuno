﻿using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data;
using System.Threading;

namespace ArticunoTest
{
    /// <summary>
    /// Summary description for ArticunoTest
    /// </summary>
    [TestClass]
    public class ArticunoTest
    {
        ArticunoMain articuno;
        MetTowerMediator mm;
        TurbineMediator tm;
        DatabaseInterface di;
        public ArticunoTest()
        {
            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;
            di = DatabaseInterface.Instance;

            mm.createMetTower();
            tm.createTestTurbines();
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met1", -20, -20, 90.0)]
        public void IcedTowerTest(string metId, double temp1, double temp2, double humidity)
        {
            Assert.Fail();
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("T001", false)]
        [DataRow("T001", true)]
        public void FullIcingTest(string turbineId, bool state)
        {

            articuno = new ArticunoMain();

            //Note complete. Do this later once you get the delegates figured out
            tm.setTemperatureCondition(turbineId, state);
            tm.setOperatingStateCondition(turbineId, state);
            tm.setNrscondition(turbineId, state);
            tm.setTurbinePerformanceCondition(turbineId, state);
            tm.setDeRateCondition(turbineId, state);
        }

        [TestMethod]
        public void SetAndClearTest()
        {

            articuno = new ArticunoMain();
            string turbineId = "T001";
            bool state = true;
            IcedConditions(turbineId, true);
            IcedConditions(turbineId, false);

            //tm.setTemperatureCondition(turbineId, state);
            //tm.setOperatingStateCondition(turbineId, state);
            //tm.setNrscondition(turbineId, state);
            //tm.setTurbinePerformanceCondition(turbineId, state);
            //tm.setDeRateCondition(turbineId, state);

            //state = false;
            //tm.setTemperatureCondition(turbineId, state);
            //tm.setOperatingStateCondition(turbineId, state);
            //tm.setNrscondition(turbineId, state);
            //tm.setTurbinePerformanceCondition(turbineId, state);
            //tm.setDeRateCondition(turbineId, state);

        }

        [TestMethod]
        public void PariticipationTest()
        {

        }

        [TestMethod]
        public void SystemInputEventChangeTest()
        {
            articuno = new ArticunoMain();
            OpcServer server = new OpcServer("SV.OPCDAServer.1");
            List<String> systemInputTags = new List<String>();
            DataTable reader = di.readCommand("SELECT * from SystemInputTags WHERE Description!='SitePrefix' AND Description!='OpcServerName' order by Description ASC");

            string tag;
            string tempThresholdTag;
            string enableArticunoTag;
            string articunoCtrTag;
            string deltaThresholdTag;
            string dewThresholdTag;

            for (int i = 0; i < reader.Rows.Count; i++)
            {
                tag = reader.Rows[i]["OpcTag"].ToString();
                switch (i)
                {
                    case 0: tempThresholdTag = tag; break;
                    case 1: enableArticunoTag = tag; break;
                    case 2: articunoCtrTag = tag; break;
                    case 3: deltaThresholdTag = tag; break;
                    case 4: dewThresholdTag = tag; break;
                }

            }
        }

        [TestMethod]
        [DataRow(5)]
        [DataRow(1)]
        public void CTRChangeTest(int ctrTime)
        {
            articuno = new ArticunoMain();
            string turbineId = "T001";

            Thread.Sleep(500);
            DataTable reader = di.readCommand("Select OpcTag from SystemInputTags WHERE Description='CTRPeriod'");
            string ctrTimeTag = reader.Rows[0]["OpcTag"].ToString();

            OpcServer server = new OpcServer("SV.OPCDAServer.1");

            server.writeTagValue(ctrTimeTag, ctrTime);

            Thread.Sleep(500);

            string readCtrTime = server.readTagValue(ctrTimeTag).ToString();
            string turbineCtrTime = tm.getTurbineCtrTime(turbineId).ToString();

            Console.WriteLine("ctrTimeTag: {0}\nturbineCtrTime tag: {1}", ctrTimeTag, turbineCtrTime);

            Console.WriteLine("ctrTime Input: {0} readCtrTime: {1}", ctrTime.ToString(), readCtrTime);
            Assert.IsTrue(ctrTime.ToString().Equals(readCtrTime), "The written and read time are not equal");

            Console.WriteLine("ctrTime Input: {0} turbineCtrTime: {1}", ctrTime.ToString(), turbineCtrTime);
            Assert.IsTrue(ctrTime.ToString().Equals(turbineCtrTime), "The written and turbine read time are not equal");
        }

        [TestMethod]
        public void OPCServerAliveTest() { }

        [TestMethod]
        public void SiteClearTest()
        {
            OpcServer server = new OpcServer("SV.OPCDAServer.1");
            string turbineId = "T001";
            articuno = new ArticunoMain();
            IcedConditions(turbineId, true);

            //Set Command to false and turbine state to PAUSE (75)
            string shutdownTag = tm.getLoadShutdownTag(turbineId);
            //server.writeTagValue(shutdownTag,0);

            string operatingStateTag = tm.getOperatingStateTag(turbineId);
            server.writeTagValue(operatingStateTag, 75);

            Thread.Sleep(500);

            //Site start it back up
            server.writeTagValue(operatingStateTag, 100);



        }

        private void clearAlarm()
        {

        }

        private void IcedConditions(string turbineId, bool state)
        {
            tm.setTemperatureCondition(turbineId, state);
            tm.setOperatingStateCondition(turbineId, state);
            tm.setNrscondition(turbineId, state);
            tm.setTurbinePerformanceCondition(turbineId, state);
            tm.setDeRateCondition(turbineId, state);

        }
    }
}
