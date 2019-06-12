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
    public class TurbineTest
    {
        private TurbineFactory tf;

        public TurbineTest()
        {
            List<string> newList = new List<string>();
            newList.Add("T001");
            tf = new TurbineFactory(newList, "SV.OPCDAServer.1");
            tf.createTurbines();
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
        public void getValueFromTurbine()
        {
            //List<string> newList = new List<string>();
            //newList.Add("T001");
            //string serverName = "SV.OPCDAServer.1";
            //tf = new TurbineFactory(newList,serverName);
            //tf.createTurbines();

            //Write some random values to known tags in the test server. Hard coding is fine in this case 
            // AS LONG AS YOU HAVE THE NAME OF THE OPC TAG RIGHT
            // Note that OPC Tag is case sensative...apparently.
            var testValue = 8.12;

            //Read 
            List<Object> derp = (List<Object>)tf.readTurbineWindSpeedTag();
            foreach (object foo in derp)
            {
                Console.WriteLine(Convert.ToDouble(foo));
                Assert.AreEqual(Convert.ToDouble(foo), testValue, 0.002);
            }

            derp.Clear();
            testValue = 0;
            derp = (List<Object>)tf.readRotorSpeedTag();
            foreach (object foo in derp)
            {
                Console.WriteLine(Convert.ToDouble(foo));
                Assert.AreEqual(Convert.ToDouble(foo), testValue, 0.002);
            }

            derp.Clear();
            testValue = 100;
            derp = (List<Object>)tf.readOperatingStateTag();
            foreach (object foo in derp)
            {
                Console.WriteLine(Convert.ToDouble(foo));
                Assert.AreEqual(Convert.ToDouble(foo), testValue, 0.002);
            }

        }

        [TestMethod]
        public  void getTagNameFromTurbine()
        {
            List<string> temp;
            temp = tf.getTurbineWindSpeedTag();
            printOutTags(temp);
            temp= tf.getOperatingStateTag();
            printOutTags(temp);
            temp= tf.getNrsStateTag();
            printOutTags(temp);
            temp= tf.getHumidityTag();
            printOutTags(temp);
            temp= tf.getTemperatureTag();
            printOutTags(temp);
            temp= tf.getLoadShutdownTag();
            printOutTags(temp);
            temp= tf.getTurbineCtrTag();
            printOutTags(temp);
            temp= tf.getRotorSpeedTag();
            printOutTags(temp);


        }

        [TestMethod]
        public void writeLoadShutDown()
        {
            List<Turbine> turbineList =(List<Turbine>)tf.getTurbineList();

            foreach(Turbine turbine in turbineList)
            {
                double temp = turbine.writeLoadShutdownCmd();
                //Console.WriteLine(turbine.writeLoadShutdownCmd());
                Assert.AreEqual(temp, 1.00, 1.001);
            }


        }

        private void printOutTags(List<string> printOutList)
        {
            foreach(var item in printOutList)
            {
                Console.WriteLine(item);
            }

        }
    }
}
