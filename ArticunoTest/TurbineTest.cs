using System;
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
    public class TurbineTest
    {
        TurbineMediator tm;
        public TurbineTest()
        {

            //Must create the MetTowersingleton first
            MetTowerMediator.Instance.createMetTower();
            List<string> newList = new List<string>();
            tm = TurbineMediator.Instance;
            tm.createTestTurbines();
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
                double readValue = Convert.ToDouble(TurbineMediator.Instance.readOperatingStateTag(turb.getTurbinePrefixValue()));

                Assert.AreEqual(testValue, readValue, 0.001, "Written value does not equal test value");
            }

        }

        [TestMethod]
        //Get the tag names of all the turbine associated Opc Tags and prints them out.
        public void getTagNameFromTurbine()
        {
            List<string> temp;
            temp = TurbineMediator.Instance.getTurbineWindSpeedTag();
            printOutTags("turbine wind speed tag", temp);
            temp = TurbineMediator.Instance.getOperatingStateTag();
            printOutTags("operating state tag", temp);
            temp = TurbineMediator.Instance.getNrsStateTag();
            //WARNING: NRS can be empty or null
            printOutTags("nrs", temp);

            //Turbine humidty tag is outside of requirement
            //temp = TurbineMediator.Instance.getHumidityTag();
            //printOutTags("humidity tag", temp);

            temp = TurbineMediator.Instance.getTemperatureTag();
            printOutTags("temperature tag", temp);
            temp = TurbineMediator.Instance.getLoadShutdownTag();
            printOutTags("Load shutdown tag", temp);

            //No CTR tag provided. I think I'm going to make the turbine CTR independent of an Opc Tag
            //temp = TurbineMediator.Instance.getTurbineCtrTag();
            //printOutTags("CTR Tag", temp);

            temp = TurbineMediator.Instance.getRotorSpeedTag();
            printOutTags("Rotor speed tag", temp);
        }

        [TestMethod]
        public void writeLoadShutDown()
        {
            List<Turbine> turbineList = (List<Turbine>)TurbineMediator.Instance.getTurbineList();

            foreach (Turbine turbine in turbineList)
            {
                double temp = turbine.writeLoadShutdownCmd();
                //Console.WriteLine(turbine.writeLoadShutdownCmd());
                Assert.AreEqual(temp, 1.00, 1.001);
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
                turbine.writeAlarmTagValue(5);
                Assert.AreEqual(turbine.readAlarmValue(), 5.00);
            }

        }

        private void printOutTags(string testName, List<string> printOutList)
        {
            Console.WriteLine("Test {0}", testName);
            foreach (var item in printOutList)
            {
                if(item.Equals("") && !testName.Equals("nrs"))
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
        [DataRow("T001", false)]
        [DataRow("T001", true)]
        public void AlgorithmTest(string turbineId, bool state)
        {
            //Note complete. Do this later once you get the delegates figured out
            Assert.Fail();

            tm.setTemperatureCondition(turbineId, state);
            tm.setOperatingStateCondition(turbineId, state);
            tm.setNrscondition(turbineId, state);
            tm.setTurbinePerformanceCondition(turbineId, state);
            tm.setDeRateCondition(turbineId, state);
            //If all five are true, then this turbine should be paused due to Ice

        }
    }
}
