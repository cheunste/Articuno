using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data.SQLite;
using System.Threading;

namespace ArticunoTest {
    /// <summary>
    /// Summary description for MetTowerUnitTest
    /// </summary>
    [TestClass]
    public class MetTowerFunctionalityTest {
        OpcServer opcServer;
        MetTowerMediator mm;
        TurbineMediator tm;
        DatabaseInterface dbi;

        public double DEFAULT_AMB_TEMP_THRESHOLD = 0.00;
        public double DEFAULT_DELTA_THRESHOLD = 1.00;

        List<String> met1Tags;
        List<String> met2Tags;
        List<String> ArticunoMetAlarmTags;

        string siteName;
        string opcServerName = "SV.OPCDAServer.1";


        [TestInitialize]
        public void initialize() {
            met1Tags = new List<String>();
            met2Tags = new List<String>();
            ArticunoMetAlarmTags = new List<String>();
            dbi = DatabaseInterface.Instance;

            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;
            //Create new met tower mediator
            mm.CreateMetTowerObject();
            opcServer = new OpcServer(opcServerName);
            siteName = dbi.getSitePrefixValue();
            //set default met tower Data
            cleanup();

            met1Tags.AddRange(new string[] {
                dbi.GetMetTowerPrimTempValueTag("Met"),
                dbi.GetMetTowerSecTempValueTag("Met"),
                dbi.GetMetTowerPrimHumidityTag("Met"),
                dbi.GetMetTowerSwitchCommandTag("Met"),
                dbi.GetBackupTurbineForMet("Met") }
            );

            met2Tags.AddRange(new string[] {
                dbi.GetMetTowerPrimTempValueTag("Met2"),
                dbi.GetMetTowerSecTempValueTag("Met2"),
                dbi.GetMetTowerPrimHumidityTag("Met2"),
                dbi.GetMetTowerSwitchCommandTag("Met2"),
                dbi.GetBackupTurbineForMet("Met2")
            });

            ArticunoMetAlarmTags.AddRange(new string[]
            {
                dbi.GetMetBadPrimaryTempSensorAlarmTag("Met"),
                dbi.GetMetBadSecondaryTempSensorAlarmTag("Met"),
                dbi.GetMetPrimaryTempOutOfRangeAlarmTag("Met"),
                dbi.GetMetSecondaryTempOutOfRangeAlarmTag("Met"),
                dbi.GetMetHumidityOutOfRangeAlarmTag("Met"),
                dbi.GetMetIceIndicationAlarmTag("Met"),
                dbi.GetMetNoDataAlarmTag("Met"),

                dbi.GetMetBadPrimaryTempSensorAlarmTag("Met2"),
                dbi.GetMetBadSecondaryTempSensorAlarmTag("Met2"),
                dbi.GetMetPrimaryTempOutOfRangeAlarmTag("Met2"),
                dbi.GetMetSecondaryTempOutOfRangeAlarmTag("Met2"),
                dbi.GetMetHumidityOutOfRangeAlarmTag("Met2"),
                dbi.GetMetIceIndicationAlarmTag("Met2"),
                dbi.GetMetNoDataAlarmTag("Met2"),

            });
        }
        
        [TestMethod]
        public void createNewMetTower() {
            //var derp = MetTowerMediatorSingleton.Instance.getAllMeasurements("Met");
            MetTower met1 = mm.GetMetTowerFromId("Met");
            MetTower met2 = mm.GetMetTowerFromId("Met2");
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
        [DataRow("Met", 20.0, 60.0, .100)]
        [DataRow("Met2", 40.0, 60.0, .500)]

        public void GetValueTest(string metId, double tempVal1, double tempVal2, double hmdVal) {
            MetTower met = mm.GetMetTowerFromId(metId);
            met.RelativeHumidityValue = hmdVal;
            met.PrimTemperatureValue = tempVal1;
            met.SecTemperatureValue = tempVal1;

            var met1Temp1 = Convert.ToDouble(mm.GetMetTowerFromId(metId).PrimTemperatureValue);
            var met1Temp2 = Convert.ToDouble(mm.GetMetTowerFromId(metId).SecTemperatureValue);
            var met1Humd1 = Convert.ToDouble(mm.GetMetTowerFromId(metId).RelativeHumidityValue);

            Assert.AreEqual(met1Temp1, tempVal1, 0.001, "temperature on sensor 1 value not equal");
            Assert.AreEqual(met1Temp2, tempVal1, 0.001, "temperature on sensor 2 value not equal");
            Assert.AreEqual(met1Humd1, hmdVal / 100.0, 0.001, "Humidty values are not equal");
        }

        [TestMethod]
        //Test the threshold values
        public void testThresholds() {

            MetTower met = mm.GetMetTowerFromId("Met");
            //Get the thresholds
            double tempThreshold = met.AmbTempThreshold;
            double deltaThreshold = met.DeltaTempThreshold;

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
        [DataRow(300.0, 120.0, 1.100, 200.0)]
        public void checkMetTower(double tempVal1, double tempVal2, double hmdVal1, double hmdVal2) {
            //Assert.Fail();
            //For this test, you need both met towers because you do not want to fall back on redundancy
            string metId1 = "Met";
            string metId2 = "Met2";
            MetTower met = mm.GetMetTowerFromId(metId1);
            MetTower met2 = mm.GetMetTowerFromId(metId2);

            met.RelativeHumidityValue = hmdVal1;
            met.PrimTemperatureValue = tempVal1;
            met.SecTemperatureValue = tempVal1;

            createStaleData(met);

            bool alarm = met.isAllSensorGood();

            Assert.IsTrue(Convert.ToBoolean(met.NoDataAlarmValue),
                String.Format("The NoDataAlarm Value is not raised for {0} after stale data", metId1)
                );
        }


        [TestMethod]
        //Test to see what happens when the met tower is switched from the default.
        [DataTestMethod]
        [DataRow("Met", 21.0, 31.0, 17.0, 57.0)]
        [DataRow("Met2", 21.0, 41.0, 57.0, 97.0)]
        public void swtichMetTowers(string metId, double tempVal1, double tempVal2, double hmdVal1, double hmdVal2) {
            tm.createTurbines();
            var turbine = tm.getTurbinePrefixList()[0];

            //setValidMetData();
            double tempBeforeSwitch;
            double humdBeforeSwitch;
            MetTower met1 = mm.GetMetTowerFromId("Met");
            MetTower met2 = mm.GetMetTowerFromId("Met2");

            Console.WriteLine("Testing {0}, with temperature {1}, {2}, humidty {3},{4}", metId, tempVal1, tempVal2, hmdVal1, hmdVal2);
            //Write Values to the tags
            met1.RelativeHumidityValue = hmdVal1;
            met1.PrimTemperatureValue = tempVal1;
            met1.SecTemperatureValue = tempVal1;

            met2.RelativeHumidityValue = hmdVal2;
            met2.PrimTemperatureValue = tempVal2;
            met2.SecTemperatureValue = tempVal2;

            //switch met tower 
            mm.switchMetTower(metId);
            Thread.Sleep(500);
            Console.WriteLine("Met Tower 1 temperature before switch {0}, humidty before switch {1}", tempVal1, hmdVal1);
            Console.WriteLine("Met Tower 2 temperature before switch {0}, humidty before switch {1}", tempVal2, hmdVal2);
            double tempAfterSwitch = Convert.ToDouble(mm.ReadTemperatureFromMetTower(metId));
            double humdAfterSwitch = Convert.ToDouble(mm.readHumidity(metId));
            Console.WriteLine("Met Tower {0} temperature after switch {1}, humidty after switch {2}", metId, tempAfterSwitch, humdAfterSwitch);

            switch (metId) {
                case "Met":
                    tempBeforeSwitch = tempVal1;
                    humdBeforeSwitch = hmdVal1 / 100;
                    break;
                default:
                    tempBeforeSwitch = tempVal2;
                    humdBeforeSwitch = hmdVal2 / 100;
                    break;
            }
            Assert.AreNotEqual(tempAfterSwitch, tempBeforeSwitch, 0.001, "Temperature is equal after switching met towers");
            Assert.AreNotEqual(humdAfterSwitch, humdBeforeSwitch, 0.001, "Humidity is equal after switching met towers");

            //Switch back the met tower to the original
            mm.switchMetTower(metId);
            Thread.Sleep(500);
            tempAfterSwitch = Convert.ToDouble(mm.ReadTemperatureFromMetTower(metId));
            humdAfterSwitch = Convert.ToDouble(mm.readHumidity(metId));
            Console.WriteLine("Met Tower {0} temperature after switch back {1}, humidty after switch back {2}", metId, tempAfterSwitch, humdAfterSwitch);

            Assert.AreEqual(tempAfterSwitch, tempBeforeSwitch, 0.001, "Temperature is not equal after switching back");
            Assert.AreEqual(humdAfterSwitch, humdBeforeSwitch, 0.001, "Humidity is not equal after switching back");
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met", -50.0)]
        public void useTurbineTempTest(string metId, double badTemperatureValue) {
            tm.createTurbines();
            string turbine = dbi.GetBackupTurbineForMet(metId);

            Turbine turb = tm.GetTurbine(turbine);
            OpcServer.writeOpcTag(opcServerName, turb.TemperatureTag, 30); ;

            //write the fail values to the met tower
            mm.CreateMetTowerObject();
            MetTower met = mm.GetMetTowerFromId(metId);
            met.PrimTemperatureValue = badTemperatureValue;
            met.SecTemperatureValue = badTemperatureValue;

            //Get measurements about 50 tiems or so to see if it is flatline. 
            createStaleData(met);

            double temperature = Convert.ToDouble(mm.ReadTemperatureFromMetTower(metId));
            double turbineTemp = Convert.ToDouble(tm.readTurbineTemperatureValue(turbine));
            Console.WriteLine("Temperature of {0}: {1}", metId, temperature);
            Console.WriteLine("Temperature from Turbine: {0}", turbineTemp);
            //verify  temperature from both the met tower and its backup turbine are not the same.
            Assert.AreNotEqual(badTemperatureValue, turbineTemp, 0.001, "The Temperatures are equal. Turbine: {0}, Met Temp {1}", turbineTemp, badTemperatureValue);
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met", -50.0, -50.0, 110.0, true)]
        [DataRow("Met", 10.0, 10.0, 60.0, false)]
        [DataRow("Met", 10.0, -50.0, 60.0, true)]

        public void noDataTest(string metId, double tempVal1, double tempVal2, double hmdVal, bool expectedState) {
            mm.writePrimTemperature(metId, tempVal1);
            mm.writeSecTemperature(metId, tempVal2);
            mm.writeHumidity(metId, hmdVal);
            Thread.Sleep(500);

            if (expectedState)
                createStaleData(mm.GetMetTowerFromId(metId));

            var humidQuality = mm.GetMetTowerFromId(metId).getPrimaryHumiditySensor().isSensorBadQuality();
            var primTempQuality = mm.GetMetTowerFromId(metId).getPrimaryTemperatureSensor().isSensorBadQuality();
            var secTempQuality = mm.GetMetTowerFromId(metId).getSecondaryTemperatureSensor().isSensorBadQuality();

            var humidOutOfRange = mm.GetMetTowerFromId(metId).getPrimaryHumiditySensor().isSensorOutofRange();
            var primTempOutOfRange = mm.GetMetTowerFromId(metId).getPrimaryTemperatureSensor().isSensorOutofRange();
            var secTempOutOfRange = mm.GetMetTowerFromId(metId).getSecondaryTemperatureSensor().isSensorOutofRange();

            var metTowerQuality = mm.isAllMetTowerSensorBadQuality(metId);

            Thread.Sleep(500);

            Assert.AreEqual(metTowerQuality, expectedState);
        }

        public void cleanup() {
            resetTagsToZero();
            setValidMetData();
        }

        //method to set all the input met tags to zero
        private void resetTagsToZero() {
            foreach (string tag in met1Tags) {
                writeValue(String.Format("{0}{1}", siteName, tag), 0);
            }

            foreach (string tag in met2Tags) {
                writeValue(String.Format("{0}{1}", siteName, tag), 0);
            }
            foreach (string tag in ArticunoMetAlarmTags) {
                writeValue(String.Format("{0}{1}", siteName, tag), 0);
            }

        }

        //Sets the met tower data to normal parameters. Needs to be hard coded so I can see a value difference between the two mets
        private void setValidMetData() {
            writeValue(String.Format("{0}Met.AmbTmp1", siteName), 50.33);
            writeValue(String.Format("{0}Met.AmbTmp2", siteName), 52.00);
            writeValue(String.Format("{0}Met.RH1", siteName), 30.22);
            writeValue(String.Format("{0}Met.RH2", siteName), 25.22);
            writeValue(String.Format("{0}Met2.AmbTmp1", siteName), 40.00);
            writeValue(String.Format("{0}Met2.AmbTmp2", siteName), 35.00);
            writeValue(String.Format("{0}Met2.RH1", siteName), 50.11);
            writeValue(String.Format("{0}Met2.RH2", siteName), 45.11);
        }

        //Method used in this class to write values 
        private void writeValue(string tag, double value) {
            opcServer.writeTagValue(tag, value);
        }

        [TestMethod]
        [DataRow("Met", -50, -50, MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY)]
        [DataRow("Met", 0, 0, MetTowerMediator.MetQualityEnum.MET_GOOD_QUALITY)]
        [DataRow("Met", 61, 61, MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY)]
        public void tempOutOfRangeTest(string metId, double temp1, double temp2, Object failureExpected) {
            tm.createTurbines();
            var turbine = tm.getTurbinePrefixList()[0];

            mm.writePrimTemperature(metId, temp1);
            mm.writeSecTemperature(metId, temp2);

            MetTower met = mm.GetMetTowerFromId(metId);
            Thread.Sleep(500);
            var readTemperature = mm.ReadTemperatureFromMetTower(metId);

            //Assert.AreEqual(Convert.ToDouble(readTemperature), temp1, 0.001, "Temperature are not equal. Read Temperature: {0}, Set Temperature {1}",readTemperature,temp1);
            Console.WriteLine("Read Temperature: {0}, Set Temperature {1}", (object)readTemperature, temp1);

            var readTemp1 = met.PrimTemperatureValue;
            var readTemp2 = met.SecTemperatureValue;

            bool primOutOfRange = Convert.ToBoolean(met.TemperaturePrimOutOfRange.ToString());
            bool secOutOfRange = Convert.ToBoolean(met.TemperatureSecOutOfRange.ToString());

            Console.WriteLine("Primary Temperature sensor value: {0}", readTemp1);
            Console.WriteLine("Secondary Temperature sensor value: {0}", readTemp2);

            bool inputQualityStatus = !Convert.ToBoolean(failureExpected);

            Thread.Sleep(500);
            Assert.AreEqual(inputQualityStatus, primOutOfRange, "input quality {0}, prim out of range {1}", inputQualityStatus, primOutOfRange);
            Assert.AreEqual(inputQualityStatus, secOutOfRange, "input quality {0}, sec out of range {1}", inputQualityStatus, secOutOfRange);

        }


        [TestMethod]
        [DataTestMethod]
        [DataRow("Met", -1, -1, MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY)]
        [DataRow("Met", 20, 20, MetTowerMediator.MetQualityEnum.MET_GOOD_QUALITY)]
        [DataRow("Met", 101, 101, MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY)]
        public void humidityOutOfRangeTest(string metId, double humidity, double temp2, Object failureExpected) {
            tm.createTurbines();
            var turbine = tm.getTurbinePrefixList()[0];

            mm.writeHumidity(metId, humidity);
            MetTower met = mm.GetMetTowerFromId(metId);

            opcServer.writeTagValue(met.HumidityPrimValueTag, humidity);
            var readValue = mm.readHumidity(metId);
            Thread.Sleep(500);
            bool primOutOfRange = Convert.ToBoolean(met.HumidityOutOfRng.ToString());

            Console.WriteLine("Humidity {0}, OutOfRange {1}",
                readValue, primOutOfRange);

            Thread.Sleep(500);
            Assert.AreEqual(primOutOfRange, !Convert.ToBoolean(failureExpected), "MetTowerMediator's inputStatus and primOutOfRange should be equal");
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met", -1, 0)]
        [DataRow("Met", 101, -3)]
        public void BadHumitiyUseTemperatureOnlyTest(string metId, double humidity, double temperature) {

            tm.createTurbines();
            var turbine = tm.getTurbinePrefixList()[0];

            mm.writeHumidity(metId, humidity);
            mm.writePrimTemperature(metId, temperature);
            mm.writeSecTemperature(metId, temperature);

            var threshold = mm.GetMetTowerFromId(metId).AmbTempThreshold;

            //Get measurements about 50 tiems or so to see if it is flatline. 50 is the default in the config db file
            for (int i = 0; i <= 50; i++)
                mm.getAllMeasurements(metId);

            Console.WriteLine("Threshold: {0} Temp {1} Humid {2}", threshold, temperature, humidity);

            MetTower met = mm.GetMetTowerFromId(metId);
            mm.CalculateFrozenMetTowerCondition(metId, temperature, humidity);
            bool isFrozen = mm.IsMetTowerFrozen(metId);


            Assert.IsTrue(isFrozen, "Met Tower isn't declaring freezing even though humidity is bad.");
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met", 20, .15, false)]
        [DataRow("Met", 90, .15, false)]
        [DataRow("Met", 00, .99, true)]
        [DataRow("Met", -20, .99, true)]
        public void FrozenMetTowerTest(string metId, double temperature, double humidity, bool expectedFrozenState) {
            MetTower met = mm.GetMetTowerFromId(metId);
            mm.writeDeltaThreshold(1);
            mm.UpdateTemperatureThresholdForAllMetTowers(0);

            mm.CalculateFrozenMetTowerCondition(metId, temperature, humidity);
            Console.WriteLine("Ice Indication Value: {0}", met.IceIndicationValue);
            Assert.AreEqual(expectedFrozenState, Convert.ToBoolean(met.IceIndicationValue));
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met", 20, .15, true)]
        [DataRow("Met", 20, .15, false)]
        public void StaleDataDetectionTest(string metId, double temperature, double humidity, bool Flatline) {
            MetTower met = mm.GetMetTowerFromId(metId);
            mm.writeDeltaThreshold(1);
            mm.UpdateTemperatureThresholdForAllMetTowers(0);

            met.PrimTemperatureValue = temperature;

            for (int i = 0; i <= 50; i++) {
                if (!Flatline) {
                    Random rnd = new Random();
                    met.PrimTemperatureValue = rnd.Next((int)temperature - 5, (int)temperature);
                }
                try {
                    var temp = mm.GetMetTowerFromId(metId).PrimTemperatureValue;
                }
                //Reaching here is good as an exception will be thrown because Aritcuno will attempt to read from turbine data...which I havne't set up for in this test.
                catch (NullReferenceException) {
                    Assert.IsTrue(true);
                }
            }

            if (Flatline)
                Assert.IsTrue(met.getPrimaryTemperatureSensor().BadQualityCheck(), "Frozen increment should be past 50, but it is {0}");
            else
                Assert.AreEqual(Flatline, met.TemperaturePrimBadQuality);

            //Check Humidity
            met.RelativeHumidityValue = humidity;

            for (int i = 0; i <= 50; i++) {
                if (!Flatline) {
                    Random rnd = new Random();
                    met.RelativeHumidityValue = rnd.Next((int)humidity - 5, (int)humidity);
                }
                try {
                    var temp = mm.GetMetTowerFromId(metId).RelativeHumidityValue;
                }
                //Reaching here is good as an exception will be thrown because Aritcuno will attempt to read from turbine data...which I havne't set up for in this test.
                catch (NullReferenceException) {
                    Assert.IsTrue(true);
                }
            }

            if (Flatline)
                Assert.IsTrue(met.getPrimaryHumiditySensor().BadQualityCheck(), "Frozen increment should be past 50, but it is {0}");
            else
                Assert.AreEqual(Flatline, met.HumidityBadQuality);

        }
        private void createStaleData(MetTower met) {
            for (int i = 0; i <= 100; i++) {
                var humid = met.RelativeHumidityValue;
                var temp1 = met.PrimTemperatureValue;
                var temp2 = met.SecTemperatureValue;
            }

        }

    }
}
