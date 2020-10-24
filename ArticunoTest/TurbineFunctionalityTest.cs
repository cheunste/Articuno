using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data;
using System.Data.SQLite;
using System.Threading;
using NUnit.Framework;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;
using System.Threading.Tasks;

namespace ArticunoTest {
    /// <summary>
    /// Summary description for TurbineTest
    /// </summary>
    [TestClass]
    public class TurbineFunctionalityTest {
        TurbineMediator turbineMediator;
        TurbineMediator tm = TurbineMediator.Instance;
        MetTowerMediator metTowerMediator;
        Articuno.Articuno am;
        DatabaseInterface dbi;
        string opcServerName;
        Turbine testTurbine;

        [TestInitialize]
        public void initialize() {
            am = new Articuno.Articuno(true);
            metTowerMediator = MetTowerMediator.Instance;
            metTowerMediator.CreateMetTowerObject();
            turbineMediator = TurbineMediator.Instance;
            turbineMediator.createTurbines();
            dbi = DatabaseInterface.Instance;
            opcServerName = dbi.getOpcServerName();
            testTurbine = tm.GetAllTurbineList()[0];
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(8.12)]
        [DataRow(0)]
        [DataRow(100)]
        public void ReadTurbineOperatingStateTest(double testValue) {
            testTurbine.writeOperatingState(testValue);
            double readValue = Convert.ToDouble(turbineMediator.readTurbineOperatingStateValue(testTurbine.GetTurbinePrefixValue()));
            Assert.AreEqual(testValue, readValue, 0.001, "Written value does not equal test value");
        }

        [TestMethod]
        public void writeLoadShutDownTest() {
            double temp = testTurbine.writeTurbineLoadShutdownCommand();
            Assert.AreEqual(temp, 1.00, 1.001);
            Assert.AreEqual(Convert.ToBoolean(testTurbine.readAgcBlockValue()), false);
        }

        [TestMethod]
        public void raiseArticunoPauseAlarmTest() {
            testTurbine.SetPausedByArticunoAlarmValue(true);
            Assert.AreEqual(Convert.ToBoolean(testTurbine.readStoppedByArticunoAlarmValue()), true);
        }

        [TestMethod]
        public void turbineGetIdTest() {
            testTurbine.TurbinePrefix = "T001";
            Assert.IsTrue(testTurbine.TurbinePrefix == "T001");
        }

        [TestMethod]
        public void turbineIdListTest() {
            List<string> prefixList = turbineMediator.getTurbinePrefixList();
            List<String> turbineListFromDatabase = getTurbineIdFromDatabase();
            foreach (string prefix in prefixList) {
                if (!turbineListFromDatabase.Contains(prefix))
                    Assert.Fail("The config file does not contain turbine id: {0}. Where this {0} come from?", prefix);
            }
        }

        [TestMethod]
        [DataTestMethod]
        //[DataRow("T001", false)]
        [DataRow("T001", true)]
        public void AlgorithmTest(string turbineId, bool state) {
            //Articuno.Articuno am = new Articuno.Articuno(true);

            //Reset the CTR time and start the turbine. Set the CTR for one minute
            turbineMediator.setTurbineCtrTime(turbineId, 1);

            //Manually start the turbine. You must do this as Articuno is not designed to start turbines by design
            OpcServer.writeOpcTag(dbi.getOpcServerName(), dbi.getSitePrefixValue() + dbi.GetTurbineStartCommandTag(turbineId), 1);

            turbineMediator.startTurbineFromTurbineMediator(turbineId);

            //Set the NRS condition to true, or else the turbine will never ice up.
            turbineMediator.getNrsStateTag(turbineId);
            if (state) turbineMediator.writeToTurbineNrsStateTag(turbineId, 5);
            else turbineMediator.writeToTurbineNrsStateTag(turbineId, 1);

            turbineMediator.setTemperatureCondition(turbineId, state);
            turbineMediator.setOperatingStateCondition(turbineId, state);
            turbineMediator.setNrsActive(turbineId, state);
            turbineMediator.setTurbinePerformanceCondition(turbineId, state);

            //If all five are true, then this turbine should be paused due to Ice
            //Force the mediator to check icing conditions. I don't want to wait.
            turbineMediator.checkIcingConditions(turbineId);
            System.Threading.Thread.Sleep(5000);

            //The following asserts are for feedback tags 
            Turbine turbine = tm.GetTurbine(turbineId);
            Assert.AreEqual(state, turbineMediator.isTurbinePausedByArticuno(turbineId));
            Assert.AreEqual(true, turbine.isTurbineParticipating(), "Turbine is not showing particiating state");
            Assert.IsTrue(Convert.ToBoolean(turbine.readTurbineLowRotorSpeedFlagValue()), "Low Rotor Speed flag not triggered");
            Assert.AreEqual(0, Convert.ToInt32(turbine.readAgcBlockValue()));
        }

        [TestMethod]
        public void GetAllTurbineTest() {
            var turbList = tm.GetAllTurbineList();
            Assert.IsTrue(turbList.Count > 0);
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(5)]
        //There is no tag for internal CTR. Instead you'll be tracking this by writing a value to a member variable and decrementing it/
        public void setCtrPeriod(int value) {
            turbineMediator.writeCtrTime(value);
            tm.GetAllTurbineList().ForEach(t => {
                Assert.AreEqual(value, t.readTurbineCtrTimeRemaining(), "The value in the turbine is {0} while {1} is expected",
                    t.readTurbineCtrTimeRemaining(), value);
            });
        }

        [TestMethod]
        public void lowRotorSpeedAlarmTest() {
            OpcServer.writeOpcTag(opcServerName, testTurbine.ParticipationTag, generateRandomBoolean());
            testTurbine.SetTurbineUnderPerformanceCondition(generateRandomBoolean());
            bool lowRotorSpeedValue = testTurbine.readTurbineLowRotorSpeedFlagValue();
            bool testTurbineParticipation = testTurbine.isTurbineParticipating();

            if (!testTurbineParticipation && lowRotorSpeedValue)
                Assert.Fail(String.Format("Turbine {0} should not be raising low rotor speed flag as it isn't participating", testTurbine.GetTurbinePrefixValue()));
            else if (testTurbineParticipation && lowRotorSpeedValue) {
                Assert.IsTrue(testTurbine.readTurbineLowRotorSpeedFlagValue());
            }
        }

        [TestMethod]
        public void lowRotorSpeedQualityCheck() {
            lowRotorSpeedQualityHelper(0.00,0.00);
            lowRotorSpeedQualityHelper(0.0101, 0.010);
            lowRotorSpeedQualityHelper(-0.10, 0.00);
            lowRotorSpeedQualityHelper(20.00, 20.00);
            lowRotorSpeedQualityHelper(20.50, 20.00);
            lowRotorSpeedQualityHelper(21.00, 20.00);
            lowRotorSpeedQualityHelper(15.00, 15.00);

        }
        private void lowRotorSpeedQualityHelper(double rotorSpeed, double expectedRotorSpeed) {
            var rtsQueue = testTurbine.getRotorSpeedQueue();
            rtsQueue.Clear();
            rtsQueue.Enqueue(rotorSpeed);
            try {
                double storedRotorSpeed = testTurbine.getRotorSpeedQueue().Peek();
                Assert.AreEqual(storedRotorSpeed, expectedRotorSpeed, 3);
            }
            catch (Exception e) {
                Assert.Fail("Getting an unexpected error: " + e);
                throw e;
            }
        }
        //public void storeMinuteAverages(string turbineId)
        //{
        //    Turbine turbine = GetTurbinePrefixFromMediator(turbineId);
        //    double windSpeedAvg = WindSpeedAverageCheck(Convert.ToDouble(turbine.readTurbineWindSpeedValue()));
        //    double rotorSpeedAvg = RotorSpeedQualityCheck(Convert.ToDouble(turbine.readTurbineRotorSpeedValue()));

        //    turbine.addWindSpeedToQueue(windSpeedAvg);
        //    turbine.addRotorSpeedToQueue(rotorSpeedAvg);
        //}


        private bool generateRandomBoolean() {
            Random rand = new Random();
            return rand.Next(2) > 0 ? true : false;
        }

        private List<String> getTurbineIdFromDatabase() {

            DataTable table = dbi.readQuery("SELECT turbineId from TurbineInputTags Order By TurbineId asc");
            List<String> turbineIdList = new List<String>();
            foreach (DataRow row in table.Rows) {
                turbineIdList.Add(row["TurbineId"].ToString());
            }
            return turbineIdList;
        }

        [TestCleanup]
        public void cleanup() {
            testTurbine.SetTemperatureCondition(false);
            testTurbine.SetOperatingStateCondition(false);
            testTurbine.TurbineNrsModeChanged(false);
            testTurbine.SetTurbineUnderPerformanceCondition(false);
            OpcServer.writeOpcTag(opcServerName, testTurbine.LowRotorSpeedFlagTag, false);
            OpcServer.writeOpcTag(opcServerName, testTurbine.ParticipationTag, true);
        }
    }
}
