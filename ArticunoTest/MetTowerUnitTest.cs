using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;

namespace ArticunoTest 
{
    /// <summary>
    /// Summary description for MetTowerUnitTest
    /// </summary>
    [TestClass]
    public class MetTowerUnitTest
    {
        MetTower metTowerTest;
        OpcServer opcServer;

        //Test contants
        public double DEFAULT_AMB_TEMP_THRESHOLD = 0.00;
        public double DEFAULT_DELTA_THRESHOLD = 1.00;

        //Create a Met Tower Class
        public MetTowerUnitTest()
        {

            //Insert some test data into Articuno.db
            DatabaseInterface dbi = new DatabaseInterface();
            //Create a new OPC Server instance
            opcServer = new OpcServer("");

            //Create new met tower
            metTowerTest = new MetTower("99", DEFAULT_AMB_TEMP_THRESHOLD, DEFAULT_DELTA_THRESHOLD,opcServer);
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
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
        //Test the temperature values and see if they match the database
        public void tempGetValueTest()
        {
            metTowerTest.readPrimTemperatureValue();
            metTowerTest.readSecTemperatureValue();

            //Manually set the tags

            metTowerTest.setPrimTemperatureTag("");
            metTowerTest.setSecTemperatureTag("");

            
        }

        [TestMethod]
        //Test the threshold values
        public void testThresholds()
        {
            //Get the thresholds
            double tempThreshold = metTowerTest.AmbTempThreshold;
            double deltaThreshold = metTowerTest.DeltaTempThreshold;

            //Compare
            Assert.AreEqual(tempThreshold, this.DEFAULT_AMB_TEMP_THRESHOLD,0.001,"Temperature Threshold compared with default are not equal");
            Assert.AreEqual(deltaThreshold, this.DEFAULT_DELTA_THRESHOLD,0.001,"Delta Threshold compared with default are not equal");

            //Set the Threshold 
            double testValue = 2.999;
            metTowerTest.AmbTempThreshold = testValue;
            metTowerTest.DeltaTempThreshold = testValue;
            tempThreshold = metTowerTest.AmbTempThreshold;
            deltaThreshold = metTowerTest.DeltaTempThreshold;

            //Compare new values with test values
            Assert.AreEqual(tempThreshold, testValue,0.001,"Temperature Threshold compared with default are not equal");
            Assert.AreEqual(deltaThreshold, testValue,0.001,"Delta Threshold compared with default are not equal");

        }

        [TestMethod]
        //Set bad quality to assert met tower throws an alarm
        public void checkMetTower()
        {

        }
    }
}
