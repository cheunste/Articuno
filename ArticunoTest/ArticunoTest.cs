using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data;

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
        public void AlgorithmTest(string turbineId, bool state)
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

            tm.setTemperatureCondition(turbineId, state);
            tm.setOperatingStateCondition(turbineId, state);
            tm.setNrscondition(turbineId, state);
            tm.setTurbinePerformanceCondition(turbineId, state);
            tm.setDeRateCondition(turbineId, state);

            state = false;
            tm.setTemperatureCondition(turbineId, state);
            tm.setOperatingStateCondition(turbineId, state);
            tm.setNrscondition(turbineId, state);
            tm.setTurbinePerformanceCondition(turbineId, state);
            tm.setDeRateCondition(turbineId, state);


        }

        [TestMethod]
        public void PariticipationTest()
        {

        }

        [TestMethod]
        public void EventChangeTest()
        {

        }

        [TestMethod]
        [DataRow(5)]
        [DataRow(1)]
        public void CTRChangeTest(int ctrTime)
        {

            DataTable reader = di.readCommand("Select OpcTag from SystemInputTags WHERE Description='CTRPeriod'");
            string ctrTimeTag = reader.Rows[0]["OpcTag"].ToString();

            OpcServer server = new OpcServer("SV.OPCDAServer.1");

            server.writeTagValue(ctrTimeTag, ctrTime);

            string readCtrTime = server.readTagValue(ctrTimeTag).ToString();

            Assert.IsTrue(ctrTime.ToString().Equals(readCtrTime), "The written and read time are not equal");
            Console.WriteLine("ctrTime Input: {0} readCtrTime: {1}", ctrTime.ToString(), readCtrTime);

            string turbineCtrTime = tm.getCtrTime("T001").ToString();
            Console.WriteLine("ctrTime Input: {0} turbineCtrTime: {1}", ctrTime.ToString(), turbineCtrTime);
            Assert.IsTrue(ctrTime.ToString().Equals(turbineCtrTime), "The written and turbine read time are not equal");
        }

        [TestMethod]
        public void FullIcingTest() { }

        [TestMethod]
        public void OPCServerAliveTest() { }

        [TestMethod]
        public void SiteClearTest() { }
    }
}
