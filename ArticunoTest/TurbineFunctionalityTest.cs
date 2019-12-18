﻿using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;

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
        public TurbineFunctionalityTest()
        {
            am = new Articuno.Articuno(true);

            //Must create the MetTowersingleton first
            MetTowerMediator.Instance.CreateMetTowerObject();
            List<string> newList = new List<string>();
            tm = TurbineMediator.Instance;
            tm.createTestTurbines();
        }

        [TestCleanup]
        public void clearCommands()
        {

        }

        [TestMethod]
        [DataTestMethod]
        [DataRow(8.12)]
        [DataRow(0)]
        [DataRow(100)]
        //This tests to see if Articuno can read and write the status state of the turbine
        //However, as you can't write to all tags, just write to operating state
        public void rwOperatingState(double testValue)
        {
            //Write some random values to known tags in the test server. Hard coding is fine in this case 
            // AS LONG AS YOU HAVE THE NAME OF THE OPC TAG RIGHT
            // Note that OPC Tag is case sensative...apparently.
            //Read 
            foreach (Turbine turb in TurbineMediator.Instance.getTurbineList())
            {
                turb.writeOperatingState(testValue);
                double readValue = Convert.ToDouble(TurbineMediator.Instance.readTurbineOperatingStateValue(turb.GetTurbinePrefixValue()));

                Assert.AreEqual(testValue, readValue, 0.001, "Written value does not equal test value");
            }

        }

        [TestMethod]
        //Get the tag names of all the turbine associated Opc Tags and prints them out.
        public void getTagNameFromTurbine()
        {
            foreach (string prefix in TurbineMediator.Instance.getTurbinePrefixList())
            {
                string temp = TurbineMediator.Instance.getTurbineWindSpeedTag(prefix);
                temp = TurbineMediator.Instance.getOperatingStateTag(prefix);
                temp = TurbineMediator.Instance.getNrsStateTag(prefix);
                //WARNING: NRS can be empty or null

                //Turbine humidty tag is outside of requirement
                //temp = TurbineMediator.Instance.getHumidityTag();
                //printOutTags("humidity tag", temp);

                temp = TurbineMediator.Instance.getTemperatureTag(prefix);
                temp = TurbineMediator.Instance.getLoadShutdownTag(prefix);

                //No CTR tag provided. I think I'm going to make the turbine CTR independent of an Opc Tag
                //temp = TurbineMediator.Instance.getTurbineCtrTag();
                //printOutTags("CTR Tag", temp);

                temp = TurbineMediator.Instance.getRotorSpeedTag(prefix);

            }
        }

        [TestMethod]
        public void writeLoadShutDown()
        {
            List<Turbine> turbineList = (List<Turbine>)TurbineMediator.Instance.getTurbineList();

            foreach (Turbine turbine in turbineList)
            {
                double temp = turbine.writeTurbineLoadShutdownCommand();
                //Console.WriteLine(turbine.writeLoadShutdownCmd());
                Assert.AreEqual(temp, 1.00, 1.001);
                Assert.AreEqual(Convert.ToBoolean(turbine.readAgcBlockValue()), false);

            }
        }

        [TestMethod]
        //Test to see if I can raise (and clear) a turbine alarm based on serveral conditions
        //IMPORTANT: This test does will NOT cover comm loss. That would be the main Articuno class's job
        public void testAlarm()
        {
            List<Turbine> turbineList = (List<Turbine>)TurbineMediator.Instance.getTurbineList();

            foreach (Turbine turbine in turbineList)
            {
                turbine.SetPausedByArticunoAlarmValue(true);
                Assert.AreEqual(Convert.ToBoolean(turbine.readStoppedByArticunoAlarmValue()), true);
            }

        }

        private void printOutTags(string testName, List<string> printOutList)
        {
            Console.WriteLine("Test {0}", testName);
            foreach (var item in printOutList)
            {
                if (item.Equals("") && !testName.Equals("nrs"))
                    Assert.Fail("List for {1} is empty {0}", printOutList, testName);
                Console.WriteLine("tag: {0}", item);
            }
        }

        [TestMethod]
        //Prints out a list of turbine prefixes and prints them out
        public void prefixListTest()
        {
            TurbineMediator.Instance.createPrefixList();

            List<string> prefixList = TurbineMediator.Instance.getTurbinePrefixList();

            foreach (string prefix in prefixList)
            {
                Console.WriteLine(prefix);
            }
        }

        [TestMethod]
        [DataTestMethod]
        //[DataRow("T001", false)]
        [DataRow("T001", true)]
        public void AlgorithmTest(string turbineId, bool state)
        {
            Articuno.Articuno am = new Articuno.Articuno(true);

            //Reset the CTR time and start the turbine. Set the CTR for one minute
            tm.setTurbineCtrTime(turbineId, 1);

            //Manually start the turbine. You must do this as Articuno is not designed to start turbines by design
            OpcServer.writeOpcTag(dbi.getOpcServerName(), dbi.getSitePrefixValue()+".T001.WTUR.SetTurOp.ActSt.Str",1);
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
            Assert.AreEqual(state,TurbineMediator.Instance.isTurbinePausedByArticuno(turbineId));
            Assert.AreEqual(true, turbine.readTurbineParticipationValue(),"Turbine is not showing particiating state");
            Assert.IsTrue(Convert.ToBoolean(turbine.readTurbineLowRotorSpeedFlagValue()),"Low Rotor Speed flag not triggered");
            //Assert.AreEqual(1,Convert.ToBoolean(turbine.readAgcBlockValue()),"AGC for turbine isn't being blocked");
            Assert.AreEqual(0,Convert.ToInt32(turbine.readAgcBlockValue()));
        }

        [TestMethod]
        //There is no tag for internal CTR. Instead you'll be tracking this by writing a value to a member variable and decrementing it/
        [DataTestMethod]
        [DataRow(5)]
        public void setCtrPeriod(int value)
        {
            foreach (string prefix in TurbineMediator.Instance.getTurbinePrefixList())
            {
                //TurbineMediator.Instance.setCtrTime(prefix, value);
                TurbineMediator.Instance.writeCtrTime(value);
            }
            Assert.AreEqual(value, TurbineMediator.Instance.getTurbineCtrTimeRemaining("T001"));


        }
    }
}