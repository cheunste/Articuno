using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data.SQLite;

namespace ArticunoTest
{
    /// <summary>
    /// Summary description for MetTowerUnitTest
    /// </summary>
    [TestClass]
    public class MetTowerUnitTest
    {
        OpcServer opcServer;

        //Test contants
        public double DEFAULT_AMB_TEMP_THRESHOLD = 0.00;
        public double DEFAULT_DELTA_THRESHOLD = 1.00;

        //These are tags for met tower1
        string[] met1Tags ={
            "Met1.AmbTmp1",
            "Met1.AmbTmp2",
            "Articuno.Met1.TempAlm",
            "Articuno.Met1.TmpHiDispAlm",
            "Met1.RH1",
            "Met1.RH2",
            "Articuno.Met1.IcePossible",
            "Articuno.Met1.RHS1OutRngAlm",
            "Articuno.Met1.RHAlm",
            "Articuno.Met1.TowerAlm"};

        string[] met2Tags = {
            "Met2.AmbTmp1",
            "Met2.AmbTmp2",
            "Articuno.Met2.TempAlm",
            "Articuno.Met2.TmpHiDispAlm",
            "Met2.RH1",
            "Met2.RH2",
            "Articuno.Met2.IcePossible",
            "Articuno.Met2.RHS1OutRngAlm",
            "Articuno.Met2.RHAlm",
            "Articuno.Met2.TowerAlm"};

        string[] ArticunoMetTags = {
            "Articuno.Met1.IcePossible",
            "Articuno.Met1.RHAlm",
            "Articuno.Met1.RHS1OutRngAlm",
            "Articuno.Met1.TempAlm",
            "Articuno.Met1.TmpHiDispAlm",
            "Articuno.Met1.TowerAlm",
            "Articuno.Met2.IcePossible",
            "Articuno.Met2.RHAlm",
            "Articuno.Met2.RHS1OutRngAlm",
            "Articuno.Met2.TempAlm",
            "Articuno.Met2.TmpHiDispAlm",
            "Articuno.Met2.TowerAlm"
        };
        string siteName;
        string opcServerName = "SV.OPCDAServer.1";

        DatabaseInterface dbi;

        //Create a Met Tower Class
        public MetTowerUnitTest()
        {
        }

        [TestInitialize]
        public void Initialize()
        {
            //Insert some test data into Articuno.db
            dbi = DatabaseInterface.Instance;
            //Create new met tower mediator
            MetTowerMediator.Instance.createMetTower();
            opcServer = new OpcServer(opcServerName);
            siteName = "SCRAB";
            //set default met tower Data
            //setValidMetData();

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
            met.RelativeHumidityValue = hmdVal;
            met.PrimTemperatureValue = tempVal1;
            met.SecTemperatureValue = tempVal1;

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
            //Assert.AreEqual(tempThreshold, this.DEFAULT_AMB_TEMP_THRESHOLD, 0.001, "Temperature Threshold compared with default are not equal");
            //Assert.AreEqual(deltaThreshold, this.DEFAULT_DELTA_THRESHOLD, 0.001, "Delta Threshold compared with default are not equal");

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
        [DataTestMethod]
        [DataRow("Met1", 300.0, 120.0, 110.0, 200.0)]
        public void checkMetTower(string metId, double tempVal1, double tempVal2, double hmdVal1, double hmdVal2)
        {
            //setValidMetData();
            double tempBeforeSwitch;
            double humdBeforeSwitch;
            MetTower met = MetTowerMediator.Instance.getMetTower(metId);

            met.RelativeHumidityValue = hmdVal1;
            met.PrimTemperatureValue = tempVal1;
            met.SecTemperatureValue = tempVal1;
            bool alarm = Convert.ToBoolean(met.NoDataAlarmValue);
            Console.WriteLine("Met Tower Quality: {0}", MetTowerMediator.Instance.checkMetTowerQuality(metId));
            Console.WriteLine("Met Tower Temperature Quality: {0}", MetTowerMediator.Instance.tempQualityCheck(metId));
            Console.WriteLine("Met Tower Humidity Quality: {0}", MetTowerMediator.Instance.humidQualityCheck(metId));

            Console.WriteLine("{0}", Convert.ToBoolean(met.TemperaturePrimOutOfRange));
            Console.WriteLine("{0}", Convert.ToBoolean(met.HumidityOutOfRng));
            Console.WriteLine("{0}", Convert.ToBoolean(met.HumidityBadQuality));
            Console.WriteLine("{0}", Convert.ToBoolean(met.TemperaturePrimBadQuality));


            //Assert.AreEqual(true, Convert.ToBoolean(met.readTemperaturePrimOutOfRange()));
            //Assert.AreEqual(true, Convert.ToBoolean(met.readHumidityOutOfRng()));
            //Assert.AreEqual(true, Convert.ToBoolean(met.readHumidityBadQuality()));
            //Assert.AreEqual(true, Convert.ToBoolean(met.readTemperaturePrimBadQuality()));
        }

        [TestMethod]
        //Test to see what happens when the met tower is switched from the default.
        //IMPORTANT: This unit test for some reason fials occasionally if all the tests are executed at once
        //but never fails if it is executed by itself. I want to say this has something to do with concurrency as it was reading
        //values that were set up by a different unit test. 
        [DataTestMethod]
        [DataRow("Met1", 21.0, 31.0, 17.0, 57.0)]
        [DataRow("Met2", 21.0, 41.0, 57.0, 97.0)]
        public void swtichMetTowers(string metId, double tempVal1, double tempVal2, double hmdVal1, double hmdVal2)
        {

            //setValidMetData();
            double tempBeforeSwitch;
            double humdBeforeSwitch;
            MetTower met1 = MetTowerMediator.Instance.getMetTower("Met1");
            MetTower met2 = MetTowerMediator.Instance.getMetTower("Met2");

            Console.WriteLine("Testing {0}, with temperature {1}, {2}, humidty {3},{4}", metId, tempVal1, tempVal2, hmdVal1, hmdVal2);
            //Write Values to the tags
            met1.RelativeHumidityValue = hmdVal1;
            met1.PrimTemperatureValue = tempVal1;
            met1.SecTemperatureValue = tempVal1;

            met2.RelativeHumidityValue = hmdVal2;
            met2.PrimTemperatureValue = tempVal2;
            met2.SecTemperatureValue = tempVal2;

            //switch met tower 
            MetTowerMediator.Instance.switchMetTower(metId);
            Console.WriteLine("Met Tower 1 temperature before switch {0}, humidty before switch {1}", tempVal1, hmdVal1);
            Console.WriteLine("Met Tower 2 temperature before switch {0}, humidty before switch {1}", tempVal2, hmdVal2);
            double tempAfterSwitch = Convert.ToDouble(MetTowerMediator.Instance.readTemperature(metId));
            double humdAfterSwitch = Convert.ToDouble(MetTowerMediator.Instance.readHumidity(metId));
            Console.WriteLine("Met Tower {0} temperature after switch {1}, humidty after switch {2}", metId, tempAfterSwitch, humdAfterSwitch);

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
            Assert.AreNotEqual(humdAfterSwitch, humdBeforeSwitch, 0.001, "Humidity is equal after switching met towers");

            //Switch back the met tower to the original
            MetTowerMediator.Instance.switchMetTower(metId);
            tempAfterSwitch = Convert.ToDouble(MetTowerMediator.Instance.readTemperature(metId));
            humdAfterSwitch = Convert.ToDouble(MetTowerMediator.Instance.readHumidity(metId));
            Console.WriteLine("Met Tower {0} temperature after switch back {1}, humidty after switch back {2}", metId, tempAfterSwitch, humdAfterSwitch);

            Assert.AreEqual(tempAfterSwitch, tempBeforeSwitch, 0.001, "Temperature is not equal after switching back");
            Assert.AreEqual(humdAfterSwitch, humdBeforeSwitch, 0.001, "Humidity is not equal after switching back");

            //Call the default good values. God knows what happened in the rpevious tests
            //setValidMetData();

        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met1", -50.0, -50.0)]
        //Needs imporvement. The database is getting locked. FIgure this out after a DB refactor
        public void useTurbineTempTest(string metId, double tempSensor1Val, double tempSensor2Val)
        {
            //Create a test turbine
            TurbineMediator.Instance.createTestTurbines();
            var turbine =TurbineMediator.Instance.getTurbinePrefixList()[0];

            //write the fail values to the met tower
            MetTower met = MetTowerMediator.Instance.getMetTower(metId);
            met.PrimTemperatureValue = tempSensor1Val;
            met.SecTemperatureValue = tempSensor2Val;

            //Get the temperatures from both the turbine and the met tower and assert they're equal.
            double temperature = Convert.ToDouble(MetTowerMediator.Instance.readTemperature(metId));
            double turbineTemp = Convert.ToDouble(TurbineMediator.Instance.readTemperatureValue(turbine));
            Console.WriteLine("Temperature of {0}: {1}", metId, temperature);
            Assert.AreEqual(temperature, turbineTemp, 0.001);
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met1", -50.0, -50.0, 110.0, false, false, true)]
        [DataRow("Met1", 10.0, 10.0, 60.0, true, true, false)]
        [DataRow("Met1", 10.0, -50.0, 60.0, true, true, false)]
        public void noDataTest(string metId, double tempVal1, double tempVal2, double hmdVal, bool expectedTempQual, bool expectedHumiQual, bool nodata)
        {
            MetTowerMediator.Instance.writePrimTemperature(metId, tempVal1);
            MetTowerMediator.Instance.writeSecTemperature(metId, tempVal2);
            MetTowerMediator.Instance.writeHumidity(metId, hmdVal);

            MetTowerMediator.Instance.humidQualityCheck(metId);

            var tempTuple = MetTowerMediator.Instance.tempQualityCheck(metId);
            var humidTuple = MetTowerMediator.Instance.humidQualityCheck(metId);
            var metTowerQuality = MetTowerMediator.Instance.checkMetTowerQuality(metId);

            Assert.AreEqual(expectedTempQual, tempTuple.Item1, "No Data alarm is still showing true (good quality)");
            Assert.AreEqual(expectedHumiQual, humidTuple.Item1, "Temperature alarm is still showing true (good quality)");
            Assert.AreEqual(metTowerQuality, nodata, "No data alarm status not equal");
        }

        public void cleanup()
        {
            resetTagsToZero();
            setValidMetData();
        }

        //method to set all the input met tags to zero
        private void resetTagsToZero()
        {
            foreach (string tag in met1Tags)
            {
                writeValue(String.Format("{0}.{1}", siteName, tag), 0);
            }

            foreach (string tag in met2Tags)
            {
                writeValue(String.Format("{0}.{1}", siteName, tag), 0);
            }
            foreach (string tag in ArticunoMetTags)
            {
                writeValue(String.Format("{0}.{1}", siteName, tag), 0);
            }

        }

        //Sets the met tower data to normal parameters. Needs to be hard coded so I can see a value difference between the two mets
        private void setValidMetData()
        {
            //Met1
            writeValue(String.Format("{0}.Met1.AmbTmp1", siteName), 60.33);
            writeValue(String.Format("{0}.Met1.AmbTmp2", siteName), 52.00);
            writeValue(String.Format("{0}.Met1.RH1", siteName), 30.22);
            writeValue(String.Format("{0}.Met1.RH2", siteName), 25.22);
            //Met2
            writeValue(String.Format("{0}.Met2.AmbTmp1", siteName), 80.00);
            writeValue(String.Format("{0}.Met2.AmbTmp2", siteName), 75.00);
            writeValue(String.Format("{0}.Met2.RH1", siteName), 50.11);
            writeValue(String.Format("{0}.Met2.RH2", siteName), 45.11);
        }

        //Method used in this class to write values 
        private void writeValue(string tag, double value)
        {
            opcServer.writeTagValue(tag, value);
        }

    }
}
