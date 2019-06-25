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
        MetTowerMediator metMediator;
        OpcServer opcServer;

        //Test contants
        public double DEFAULT_AMB_TEMP_THRESHOLD = 0.00;
        public double DEFAULT_DELTA_THRESHOLD = 1.00;

        //These are tags for met tower1
        string[] met1Tags ={
            "Met1.AmbTmp1",
            "Met1.AmbTmp2",
            "Met1.TempAlm",
            "Met1.TmpHiDispAlm",
            "Met1.RH1",
            "Met1.RH2",
            "Met1.IcePossible",
            "Met1.RHS1OutRngAlm",
            "Met1.RHAlm",
            "Met1.TowerAlm"};

        string[] met2Tags = {
            "Met2.AmbTmp1",
            "Met2.AmbTmp2",
            "Met2.TempAlm",
            "Met2.TmpHiDispAlm",
            "Met2.RH1",
            "Met2.RH2",
            "Met2.IcePossible",
            "Met2.RHS1OutRngAlm",
            "Met2.RHAlm",
            "Met2.TowerAlm"};

        string siteName;


        //Create a Met Tower Class
        public MetTowerUnitTest()
        {

            //Insert some test data into Articuno.db
            DatabaseInterface dbi = new DatabaseInterface();
            //Create new met tower mediator
            metMediator = new MetTowerMediator();
            MetTowerMediatorSingleton.Instance.createMetTower();
            opcServer = new OpcServer("SV.OPCDAServer.1");
            siteName = "SCRAB";
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

        [TestMethod]
        public void createNewMetTower()
        {
            //var derp = MetTowerMediatorSingleton.Instance.getAllMeasurements("Met1");
            MetTower met1 = MetTowerMediatorSingleton.Instance.getMetTower("Met1");
            MetTower met2 = MetTowerMediatorSingleton.Instance.getMetTower("Met2");
            Assert.IsNotNull(met1);
            Assert.IsNotNull(met2);


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
            var met1Values = MetTowerMediatorSingleton.Instance.getAllMeasurements("Met1");
            var met2Values = MetTowerMediatorSingleton.Instance.getAllMeasurements("Met2");

            Console.WriteLine(met1Values.Item1);
            Console.WriteLine(met1Values.Item2);
            Console.WriteLine(met1Values.Item3);
            Console.WriteLine(met1Values.Item4);
        }

        [TestMethod]
        //Test the threshold values
        public void testThresholds()
        {

            MetTower met = MetTowerMediatorSingleton.Instance.getMetTower("Met1");
            //Get the thresholds
            double tempThreshold = met.AmbTempThreshold;
            double deltaThreshold = met.DeltaTempThreshold;

            //Compare
            Assert.AreEqual(tempThreshold, this.DEFAULT_AMB_TEMP_THRESHOLD, 0.001, "Temperature Threshold compared with default are not equal");
            Assert.AreEqual(deltaThreshold, this.DEFAULT_DELTA_THRESHOLD, 0.001, "Delta Threshold compared with default are not equal");

            //Set the Threshold 
            double testValue = 2.999;
            met.AmbTempThreshold = testValue;
            met.DeltaTempThreshold = testValue;
            tempThreshold = met.AmbTempThreshold;
            deltaThreshold = met.DeltaTempThreshold;

            //Compare new values with test values
            Assert.AreEqual(tempThreshold, testValue, 0.001, "Temperature Threshold compared with default are not equal");
            Assert.AreEqual(deltaThreshold, testValue, 0.001, "Delta Threshold compared with default are not equal");

        }

        [TestMethod]
        //Set bad quality to assert met tower throws an alarm
        public void checkMetTower()
        {

        }

        [TestMethod]
        //Test to see what happens when the met tower is switched from the default.
        public void swtichMetTowers()
        {

        }

        public void resetMetTowerValues()
        {
            foreach(string tag in met1Tags)
            {
                writeValue(siteName + tag, 0);
            }

            foreach(string tag in met2Tags)
            {
                writeValue(siteName + tag, 0);
            }

        }

        public void writeValue(string tag, double value)
        {
            opcServer.writeTagValue(tag, value);
        }
    }
}
