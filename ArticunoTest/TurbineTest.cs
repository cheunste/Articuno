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
        public void getTagValueFromTurbine(double testValue)
        {
            //Write some random values to known tags in the test server. Hard coding is fine in this case 
            // AS LONG AS YOU HAVE THE NAME OF THE OPC TAG RIGHT
            // Note that OPC Tag is case sensative...apparently.
            //Read 
            List<Object> windSpeedValues = (List<Object>)TurbineMediator.Instance.readTurbineWindSpeedTag();
            foreach (object value in windSpeedValues)
            {
                Console.WriteLine(Convert.ToDouble(value));
                Assert.AreEqual(Convert.ToDouble(value), testValue, 0.002);
            }
            windSpeedValues.Clear();
        }

        [TestMethod]
        public void getTagNameFromTurbine()
        {
            List<string> temp;
            temp = TurbineMediator.Instance.getTurbineWindSpeedTag();
            printOutTags(temp);
            temp = TurbineMediator.Instance.getOperatingStateTag();
            printOutTags(temp);
            temp = TurbineMediator.Instance.getNrsStateTag();
            printOutTags(temp);
            temp = TurbineMediator.Instance.getHumidityTag();
            printOutTags(temp);
            temp = TurbineMediator.Instance.getTemperatureTag();
            printOutTags(temp);
            temp = TurbineMediator.Instance.getLoadShutdownTag();
            printOutTags(temp);
            temp = TurbineMediator.Instance.getTurbineCtrTag();
            printOutTags(temp);
            temp = TurbineMediator.Instance.getRotorSpeedTag();
            printOutTags(temp);
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
        public void testAlarm()
        {
            List<Turbine> turbineList = (List<Turbine>)TurbineMediator.Instance.getTurbineList();

            foreach (Turbine turbine in turbineList)
            {
                turbine.writeAlarmTagValue(5);
                Assert.AreEqual(turbine.readAlarmValue(),5.00);
            }

        }

        private void printOutTags(List<string> printOutList)
        {
            foreach(var item in printOutList)
            {
                Console.WriteLine(item);
                //Assert.IsNotNull(item);
            }
        }

        [TestMethod]
        //Prints out a list of turbine prefixes and prints them out
        public void prefixListTest()
        {
            TurbineMediator.Instance.createPrefixList();

            List<string> prefixList = TurbineMediator.Instance.getTurbinePrefixList();

            foreach(string prefix in prefixList)
            {
                Console.WriteLine(prefix);
            }
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("T001",false,false,false,false,false)]
        [DataRow("T001",true,true,true,true,true)]
        public void AlgorithmTest(string turbineId,bool state)
        {
            tm.setTemperatureCondition(turbineId, state);
            tm.setTemperatureCondition(turbineId, state);
            tm.setTemperatureCondition(turbineId, state);
            tm.setTemperatureCondition(turbineId, state);
            tm.setTemperatureCondition(turbineId, state);

            //If all five are true, then this turbine should be paused due to Ice

        }
    }
}
