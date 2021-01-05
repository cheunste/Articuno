using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data;
using System.Threading;

namespace ArticunoTest {
    /// <summary>
    /// Summary description for ArticunoTest
    /// </summary>
    [TestClass]
    public class ArticunoTest {
        Articuno.Articuno articuno;
        MetTowerMediator mm;
        TurbineMediator tm;
        DatabaseInterface di;
        MetTower testMetTower;
        Turbine testTurbine;
        string opcServerName = "SV.OPCDAServer.1";
        string sitePrefix;

        [TestInitialize]
        public void initialize() {
            articuno = new Articuno.Articuno(true);
            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;
            di = DatabaseInterface.Instance;

            mm.CreateMetTowerObject();
            tm.createTurbines();
            sitePrefix = di.getSitePrefixValue();

            testMetTower = mm.GetMetTowerList()[0];
            testTurbine = tm.GetAllTurbineList()[0];
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met", -15, -15, .97)]
        public void CalculateFrozenMetTowerConditionTest(string metId, double temp1, double temp2, double humidity) {
            int time = 1;

            setNormalCondition();
            mm.writePrimTemperature(metId, temp1);
            mm.writeSecTemperature(metId, temp2);
            mm.writeHumidity(metId, humidity);

            mm.CalculateFrozenMetTowerCondition(testMetTower, temp1, humidity);
            Assert.IsTrue(mm.IsMetTowerFrozen(testMetTower), "Met Tower is not frozen as expected");
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(false)]
        [DataRow(true)]
        public void FullIcingTest(bool isIced) {

            //articuno = new Articuno.Articuno(true);
            string turbineId = testTurbine.GetTurbinePrefixValue();
            tm.createTurbines();
            mm.CreateMetTowerObject();
            setNormalCondition();
            tm.startTurbineFromTurbineMediator(turbineId);

            tm.setTemperatureCondition(turbineId, isIced);
            tm.setOperatingStateCondition(turbineId, isIced);
            tm.setNrsActive(turbineId, isIced);
            tm.setTurbinePerformanceCondition(turbineId, isIced);

            tm.checkIcingConditions(turbineId);

            var blockedValue = testTurbine.readAgcBlockValue();
            Console.WriteLine("Turbine {0} blockec value {1}", turbineId, blockedValue);
            var opState = Convert.ToInt32(testTurbine.readTurbineOperatingStateValue());
            if (isIced) {
                Assert.AreEqual(Convert.ToBoolean(blockedValue), Convert.ToBoolean(0.00), "Block statuses do not match");
                Assert.IsTrue(opState == 50, "Turbine Operation state is :{0} and not 50", opState);
            }
            else
                Assert.AreEqual(Convert.ToBoolean(blockedValue), Convert.ToBoolean(1.00), "UnBlock statuses do not match");

            var shutdownStatus = OpcServer.readBooleanTag(opcServerName, testTurbine.StoppedAlarmTag);
            Console.WriteLine("Turbine {0} stopped by Articuno: {1}", turbineId, shutdownStatus);
            Assert.AreEqual(Convert.ToBoolean(shutdownStatus), isIced, "Turbine isn't paused by Articuno");

        }

        [TestMethod]
        public void SetAndClearTest() {
            //articuno = new Articuno.Articuno(true);
            string turbineId = "T001";
            bool state = true;
            IcedConditions(turbineId, true);
            IcedConditions(turbineId, false);
        }

        [TestMethod]
        public void SystemInputEventChangeTest() {
            //articuno = new Articuno.Articuno(true);
            OpcServer server = new OpcServer("SV.OPCDAServer.1");
            List<String> systemInputTags = new List<String>();
            DataTable reader = di.readQuery("SELECT * from SystemInputTags WHERE Description!='SitePrefix' AND Description!='OpcServerName' order by Description ASC");

            string tag;
            string tempThresholdTag;
            string enableArticunoTag;
            string articunoCtrTag;
            string deltaThresholdTag;
            string dewThresholdTag;

            for (int i = 0; i < reader.Rows.Count; i++) {
                tag = reader.Rows[i]["OpcTag"].ToString();
                switch (i) {
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
        public void CTRChangeTest(int ctrTime) {
            //articuno = new Articuno.Articuno(true);
            OpcServer server = new OpcServer("SV.OPCDAServer.1");

            string ctrTimeTag = sitePrefix + di.getArticunoCtrTag();
            //This mocks the ctrTime tag being written in the OPC server
            server.writeTagValue(ctrTimeTag, 3);
            Thread.Sleep(3000);
            server.writeTagValue(ctrTimeTag, ctrTime);
            Thread.Sleep(3000);

            var turbineId = testTurbine.GetTurbinePrefixValue();
            string readCtrTime = server.readTagValue(ctrTimeTag).ToString();
            Assert.IsTrue(ctrTime.ToString().Equals(readCtrTime), "The written and read time read from the CtrTag is not equal");
            string turbineCtrTime = tm.getTurbineCtrTimeRemaining(turbineId).ToString();

            Console.WriteLine("ctrTime Input: {0} turbineCtrTime: {1}", ctrTime.ToString(), turbineCtrTime);
            Assert.AreEqual(ctrTime.ToString(), turbineCtrTime, "The written and turbine read time are not equal");

        }

        [TestMethod]
        public void SiteClearTest() {
            //Assert.Fail("Test needs revist");
            OpcServer server = new OpcServer("SV.OPCDAServer.1");
            string turbineId = testTurbine.GetTurbinePrefixValue();
            IcedConditions(turbineId, true);

            //Set Command to false and turbine state to PAUSE (75)
            string shutdownTag = tm.getLoadShutdownTag(turbineId);
            string operatingStateTag = tm.getOperatingStateTag(turbineId);
            server.writeTagValue(operatingStateTag, 75);

            Thread.Sleep(100);

            //Site start it back up
            server.writeTagValue(testTurbine.StartCommandTag, true);
            server.writeTagValue(operatingStateTag, 100);
            var stoppedStatus = Convert.ToBoolean(testTurbine.readStoppedByArticunoAlarmValue());
            Assert.IsFalse(stoppedStatus, "Turbine's {0} tag is showing {1} when it is supposed to be stopped", testTurbine.StoppedAlarmTag, stoppedStatus);
            server.writeTagValue(testTurbine.StartCommandTag, false);
        }

        [TestMethod]
        [DataTestMethod]
        public void MetTowerCtrCountdownTest() {
            //Assert.Fail("Test not implemented");
            setNormalCondition();
            setTime(3);

            //Wait one minute
            mm.UpdateCtrCountdown(2);
            var ctrTag = sitePrefix + di.getMetTowerCtrCountdownTag();
            var metTowerCtr = Convert.ToInt32(OpcServer.readAnalogTag(opcServerName, ctrTag));

            Assert.IsTrue(metTowerCtr == 2, "MetTower CTR is supposed to be 2, but it is {0}", metTowerCtr);

        }

        private void clearAlarm() {
            Assert.Fail("Test not implemented");
        }

        private void IcedConditions(string turbineId, bool state) {
            tm.setTemperatureCondition(turbineId, state);
            tm.setOperatingStateCondition(turbineId, state);
            tm.setNrsActive(turbineId, state);
            tm.setTurbinePerformanceCondition(turbineId, state);


        }

        private void setNormalCondition() {
            var time = 1;
            var ctrTag = sitePrefix + di.getArticunoCtrTag();
            var temperatureTag = sitePrefix + di.getTemperatureThresholdTag();
            var enableTag = sitePrefix + di.getArticunoEnableTag();
            var uccActive = di.getActiveUccOpcTag();

            OpcServer.writeOpcTag(opcServerName, ctrTag, time);
            OpcServer.writeOpcTag(opcServerName, temperatureTag, 5);
            OpcServer.writeOpcTag(opcServerName, enableTag, true);
            OpcServer.writeOpcTag(opcServerName, uccActive, true);
            mm.writeDeltaThreshold(1);
        }

        private void setTime(int time) {
            var ctrTag = sitePrefix + di.getArticunoCtrTag();
            OpcServer.writeOpcTag(opcServerName, ctrTag, time);
        }
    }
}
