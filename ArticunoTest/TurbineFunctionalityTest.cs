using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data;

namespace ArticunoTest
{
    /// <summary>
    /// Summary description for TurbineTest
    /// </summary>
    [TestClass]
    public class TurbineFunctionalityTest
    {
        TurbineMediator tm;
        Articuno.Articuno am;
        DatabaseInterface dbi;
        string opcServerName;
        public TurbineFunctionalityTest()
        {
            am = new Articuno.Articuno(true);
            //Must create the MetTowersingleton first
            MetTowerMediator.Instance.CreateMetTowerObject();
            List<string> newList = new List<string>();
            tm = TurbineMediator.Instance;
            tm.createTestTurbines();
            dbi = DatabaseInterface.Instance;
            opcServerName = dbi.getOpcServerName();
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(8.12)]
        [DataRow(0)]
        [DataRow(100)]
        public void ReadTurbineOperatingStateTest(double testValue)
        {
            foreach (Turbine turb in TurbineMediator.Instance.getTurbineList())
            {
                turb.writeOperatingState(testValue);
                double readValue = Convert.ToDouble(TurbineMediator.Instance.readTurbineOperatingStateValue(turb.GetTurbinePrefixValue()));
                Assert.AreEqual(testValue, readValue, 0.001, "Written value does not equal test value");
            }
        }

        [TestMethod]
        public void writeLoadShutDownTest()
        {
            List<Turbine> turbineList = (List<Turbine>)TurbineMediator.Instance.getTurbineList();

            foreach (Turbine turbine in turbineList)
            {
                double temp = turbine.writeTurbineLoadShutdownCommand();
                Assert.AreEqual(temp, 1.00, 1.001);
                Assert.AreEqual(Convert.ToBoolean(turbine.readAgcBlockValue()), false);
            }
        }

        [TestMethod]
        public void raiseArticunoPauseAlarmTest()
        {
            List<Turbine> turbineList = (List<Turbine>)TurbineMediator.Instance.getTurbineList();

            foreach (Turbine turbine in turbineList)
            {
                turbine.SetPausedByArticunoAlarmValue(true);
                Assert.AreEqual(Convert.ToBoolean(turbine.readStoppedByArticunoAlarmValue()), true);
            }

        }

        [TestMethod]
        public void turbineIdListTest()
        {
            TurbineMediator.Instance.createPrefixList();
            List<string> prefixList = TurbineMediator.Instance.getTurbinePrefixList();
            List<String> turbineListFromDatabase = getTurbineIdFromDatabase();
            foreach (string prefix in prefixList)
            {
                if (!turbineListFromDatabase.Contains(prefix))
                    Assert.Fail("The config file does not contain turbine id: {0}. Where this {0} come from?",prefix);
            }
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("T001", false)]
        [DataRow("T001", true)]
        public void AlgorithmTest(string turbineId, bool state)
        {
            Articuno.Articuno am = new Articuno.Articuno(true);

            //Reset the CTR time and start the turbine. Set the CTR for one minute
            tm.setTurbineCtrTime(turbineId, 1);

            //Manually start the turbine. You must do this as Articuno is not designed to start turbines by design
            OpcServer.writeOpcTag(dbi.getOpcServerName(), dbi.getSitePrefixValue() + ".T001.WTUR.SetTurOp.ActSt.Str", 1);
            tm.startTurbineFromTurbineMediator(turbineId);

            //Set the NRS condition to true, or else the turbine will never ice up.
            if (state) tm.writeToTurbineNrsStateTag(turbineId, 5);
            else tm.writeToTurbineNrsStateTag(turbineId, 1);


            tm.setTemperatureCondition(turbineId, state);
            tm.setOperatingStateCondition(turbineId, state);
            tm.setNrsActive(turbineId, state);
            tm.setTurbinePerformanceCondition(turbineId, state);

            //If all five are true, then this turbine should be paused due to Ice
            //Force the mediator to check icing conditions. I don't want to wait.
            tm.checkIcingConditions(turbineId);
            System.Threading.Thread.Sleep(500);

            //The following asserts are for feedback tags 
            Turbine turbine = TurbineMediator.GetTurbinePrefixFromMediator(turbineId);
            Assert.AreEqual(state, TurbineMediator.Instance.isTurbinePausedByArticuno(turbineId));
            Assert.AreEqual(true, turbine.readTurbineParticipationValue(), "Turbine is not showing particiating state");
            Assert.IsTrue(Convert.ToBoolean(turbine.readTurbineLowRotorSpeedFlagValue()), "Low Rotor Speed flag not triggered");
            Assert.AreEqual(0, Convert.ToInt32(turbine.readAgcBlockValue()));
        }

        [TestMethod]
        //There is no tag for internal CTR. Instead you'll be tracking this by writing a value to a member variable and decrementing it/
        [DataTestMethod]
        [DataRow(5)]
        public void setCtrPeriod(int value)
        {
            foreach (string prefix in TurbineMediator.Instance.getTurbinePrefixList())
            {
                TurbineMediator.Instance.writeCtrTime(value);
            }
            Assert.AreEqual(value, TurbineMediator.Instance.getTurbineCtrTimeRemaining("T001"));
        }

        [TestMethod]
        public void lowRotorSpeedAlarmTest()
        {
            foreach (Turbine turbine in tm.getTurbineList())
            {
                OpcServer.writeOpcTag(opcServerName, turbine.LowRotorSpeedFlagTag, generateRandomBoolean());
                OpcServer.writeOpcTag(opcServerName, turbine.ParticipationTag, generateRandomBoolean());
                bool lowRotorSpeedValue = Convert.ToBoolean(OpcServer.readBooleanTag(opcServerName, turbine.LowRotorSpeedFlagTag));
                bool turbineParticipation = Convert.ToBoolean(turbine.readTurbineParticipationValue());

                if (!turbineParticipation && lowRotorSpeedValue)
                    Assert.Fail("Turbine should not be raising low rotor speed flag if it isn't participating");
            }
        }

        private bool generateRandomBoolean()
        {
            Random rand = new Random();
            return rand.Next(2) > 0 ? true : false;
        }

        private List<String> getTurbineIdFromDatabase()
        {

            DataTable table = dbi.readQuery("SELECT turbineId from TurbineInputTags Order By TurbineId asc");
            List<String> turbineIdList = new List<String>();
            foreach(DataRow row in table.Rows)
            {
                turbineIdList.Add( row["TurbineId"].ToString() );
            }
            return turbineIdList;
        }

        [TestCleanup]
        public void cleanup()
        {
            foreach (Turbine turbine in tm.getTurbineList())
            {
                OpcServer.writeOpcTag(opcServerName, turbine.LowRotorSpeedFlagTag, false);
                OpcServer.writeOpcTag(opcServerName, turbine.ParticipationTag, true);
                turbine.SetTemperatureCondition(false);
                turbine.SetOperatingStateCondition(false);
                turbine.SetTurbineNrsMode(false);
                turbine.SetTurbineUnderPerformanceCondition(false);
            }

        }
    }
}
