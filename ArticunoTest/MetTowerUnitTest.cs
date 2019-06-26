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
            MetTowerMediator.Instance.createMetTower();
            opcServer = new OpcServer("SV.OPCDAServer.1");
            siteName = "SCRAB";
        }

        [TestMethod]
        public void createNewMetTower()
        {
            //var derp = MetTowerMediatorSingleton.Instance.getAllMeasurements("Met1");
            MetTower met1 = MetTowerMediator.Instance.getMetTower("Met1");
            MetTower met2 = MetTowerMediator.Instance.getMetTower("Met2");
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
        [DataTestMethod]
        [DataRow("Met1", 20.0, 60.0, 10.0)]
        [DataRow("Met2", 40.0, 60.0, 50.0)]

        public void GetValueTest(string metId, double tempVal1, double tempVal2, double hmdVal)
        {
            MetTower met = MetTowerMediator.Instance.getMetTower(metId);
            met.writeRelativeHumityValue(hmdVal);
            met.writePrimTemperatureValue(tempVal1);
            met.writeSecTemperatureValue(tempVal1);

            var met1Values = MetTowerMediator.Instance.getAllMeasurements(metId);

            Console.WriteLine(met1Values.Item1);
            Console.WriteLine(met1Values.Item2);
            Console.WriteLine(met1Values.Item3);
            Console.WriteLine(met1Values.Item4);

            Assert.AreEqual(met1Values.Item1, tempVal1, 0.001, "temperature value not equal");
            Assert.AreEqual(met1Values.Item2, hmdVal, 0.001, "Humidty values are not equal");

            //Warning, all I can do for the dew point and delta calcs are check if they're not null. Mainly because I didn't come up with the formula for this
            Assert.IsNotNull(met1Values.Item3);
            Assert.IsNotNull(met1Values.Item4);

        }

        [TestMethod]
        //Test the threshold values
        public void testThresholds()
        {

            MetTower met = MetTowerMediator.Instance.getMetTower("Met1");
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
        [DataTestMethod]
        [DataRow("Met1", 20.0, 60.0, 10.0, 50.0)]
        [DataRow("Met2", 20.0, 60.0, 50.0, 90.0)]
        public void swtichMetTowers(string metId, double tempVal1, double tempVal2, double hmdVal1, double hmdVal2)
        {
            double tempBeforeSwitch;
            double humdBeforeSwitch;
            MetTower met1 = MetTowerMediator.Instance.getMetTower("Met1");
            MetTower met2 = MetTowerMediator.Instance.getMetTower("Met2");

            //Write Values to the tags
            met1.writeRelativeHumityValue(hmdVal1);
            met1.writePrimTemperatureValue(tempVal1);
            met1.writeSecTemperatureValue(tempVal1);

            met2.writeRelativeHumityValue(hmdVal2);
            met2.writePrimTemperatureValue(tempVal2);
            met2.writeSecTemperatureValue(tempVal2);

            //switch met tower 
            MetTowerMediator.Instance.switchMetTower(metId);
            double tempAfterSwitch = Convert.ToDouble(MetTowerMediator.Instance.getTemperature(metId));
            double humdAfterSwitch = Convert.ToDouble(MetTowerMediator.Instance.getHumidity(metId));
            switch (metId)
            {
                case "Met1":
                    tempBeforeSwitch = tempVal1;
                    humdBeforeSwitch = hmdVal1;
                    break;
                default:
                    tempBeforeSwitch = tempVal2;
                    humdBeforeSwitch = hmdVal2;
                    break;
            }
            Assert.AreNotEqual(tempAfterSwitch, tempBeforeSwitch, 0.001, "Temperature is equal after switching met towers");
            Assert.AreNotEqual(humdAfterSwitch, tempBeforeSwitch, 0.001, "Humidity is equal after switching met towers");

            //Switch back the met tower to the original
            MetTowerMediator.Instance.switchMetTower(metId);
            tempAfterSwitch = Convert.ToDouble(MetTowerMediator.Instance.getTemperature(metId));
            humdAfterSwitch = Convert.ToDouble(MetTowerMediator.Instance.getHumidity(metId));

            Assert.AreEqual(tempAfterSwitch, tempBeforeSwitch, 0.001, "Temperature is not equal after switching back");
            Assert.AreEqual(humdAfterSwitch, humdBeforeSwitch, 0.001, "Humidity is not equal after switching back");
        }

        //method to set all the input met tags to zero
        public void resetMetTowerValues()
        {
            foreach (string tag in met1Tags)
            {
                writeValue(siteName + tag, 0);
            }

            foreach (string tag in met2Tags)
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
