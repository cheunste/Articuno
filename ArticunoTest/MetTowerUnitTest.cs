﻿using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data.SQLite;
using System.Threading;

namespace ArticunoTest
{
    /// <summary>
    /// Summary description for MetTowerUnitTest
    /// </summary>
    [TestClass]
    public class MetTowerUnitTest
    {
        OpcServer opcServer;
        MetTowerMediator mm;
        TurbineMediator tm;
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
            "Articuno.Met1.TmpS1OutOfRangeAlm",
            "Articuno.Met1.TmpS2OutOfRangeAlm",
            "Articuno.Met2.RHAlm",
            "Articuno.Met2.RHS1OutRngAlm",
            "Articuno.Met2.TempAlm",
            "Articuno.Met2.TmpHiDispAlm",
            "Articuno.Met2.TowerAlm",
            "Articuno.Met2.TmpS1OutOfRangeAlm",
            "Articuno.Met2.TmpS2OutOfRangeAlm"
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
            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;
            //Create new met tower mediator
            mm.createMetTower();
            opcServer = new OpcServer(opcServerName);
            siteName = "SCRAB";
            //set default met tower Data
            cleanup();

        }

        [TestMethod]
        public void createNewMetTower()
        {
            //var derp = MetTowerMediatorSingleton.Instance.getAllMeasurements("Met1");
            MetTower met1 = mm.getMetTower("Met1");
            MetTower met2 = mm.getMetTower("Met2");
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
            Assert.Fail();
            MetTower met = mm.getMetTower(metId);
            met.RelativeHumidityValue = hmdVal;
            met.PrimTemperatureValue = tempVal1;
            met.SecTemperatureValue = tempVal1;

            var met1Values = mm.getAllMeasurements(metId);

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

            MetTower met = mm.getMetTower("Met1");
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
        [DataRow("Met1", 300.0, 120.0, 110.0, 200.0)]
        public void checkMetTower(string metId, double tempVal1, double tempVal2, double hmdVal1, double hmdVal2)
        {
            Assert.Fail();
            MetTower met = mm.getMetTower(metId);

            met.RelativeHumidityValue = hmdVal1;
            met.PrimTemperatureValue = tempVal1;
            met.SecTemperatureValue = tempVal1;
            bool alarm = Convert.ToBoolean(met.NoDataAlarmValue);
            Console.WriteLine("Met Tower Quality: {0}", mm.checkMetTowerQuality(metId));
            Console.WriteLine("Met Tower Temperature Quality: {0}", mm.tempQualityCheck(metId));
            Console.WriteLine("Met Tower Humidity Quality: {0}", mm.humidQualityCheck(metId));

            Console.WriteLine("{0}", Convert.ToBoolean(met.TemperaturePrimOutOfRange));
            Console.WriteLine("{0}", Convert.ToBoolean(met.HumidityOutOfRng));
            Console.WriteLine("{0}", Convert.ToBoolean(met.HumidityBadQuality));
            Console.WriteLine("{0}", Convert.ToBoolean(met.TemperaturePrimBadQuality));
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
            MetTower met1 = mm.getMetTower("Met1");
            MetTower met2 = mm.getMetTower("Met2");

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
            double tempAfterSwitch = Convert.ToDouble(mm.readTemperature(metId));
            double humdAfterSwitch = Convert.ToDouble(mm.readHumidity(metId));
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
            mm.switchMetTower(metId);
            Thread.Sleep(500);
            tempAfterSwitch = Convert.ToDouble(mm.readTemperature(metId));
            humdAfterSwitch = Convert.ToDouble(mm.readHumidity(metId));
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
            tm.createTestTurbines();
            var turbine = tm.getTurbinePrefixList()[0];

            //write the fail values to the met tower
            MetTower met = mm.getMetTower(metId);
            met.PrimTemperatureValue = tempSensor1Val;
            met.SecTemperatureValue = tempSensor2Val;

            //Get the temperatures from both the turbine and the met tower and assert they're equal.
            double temperature = Convert.ToDouble(mm.readTemperature(metId));
            double turbineTemp = Convert.ToDouble(tm.readTemperatureValue(turbine));
            Console.WriteLine("Temperature of {0}: {1}", metId, temperature);
            Console.WriteLine("Temperature from Turbine: {0}", turbineTemp);
            //Make sure the temperature from both the met tower and its backup turbine are not the same.
            Assert.AreEqual(temperature, turbineTemp, 0.001,"Not euqal. Turbine: {0}, Met Temp {1}",turbineTemp,temperature);
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met1", -50.0, -50.0, 110.0,MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY,MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY,MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY )]
        [DataRow("Met1", 10.0, 10.0, 60.0, MetTowerMediator.MetQualityEnum.MET_GOOD_QUALITY,MetTowerMediator.MetQualityEnum.MET_GOOD_QUALITY,MetTowerMediator.MetQualityEnum.MET_GOOD_QUALITY )]
        [DataRow("Met1", 10.0, -50.0, 60.0, MetTowerMediator.MetQualityEnum.MET_GOOD_QUALITY,MetTowerMediator.MetQualityEnum.MET_GOOD_QUALITY,MetTowerMediator.MetQualityEnum.MET_GOOD_QUALITY )]
        public void noDataTest(string metId, double tempVal1, double tempVal2, double hmdVal, Object expectedTempQual, Object expectedHumiQual, Object nodata)
        {
            mm.writePrimTemperature(metId, tempVal1);
            mm.writeSecTemperature(metId, tempVal2);
            mm.writeHumidity(metId, hmdVal);
            Thread.Sleep(500);

            var tempTuple = mm.tempQualityCheck(metId);
            var primTempQuality = tempTuple.Item1;
            var secTempQuality = tempTuple.Item2;
            var humidTuple = mm.humidQualityCheck(metId);
            var metTowerQuality = mm.checkMetTowerQuality(metId);

            Thread.Sleep(500);
            Assert.AreEqual(expectedTempQual, primTempQuality, "Primary Temperature alarm is {0}, Expected: {1}",primTempQuality, expectedTempQual);
            Assert.AreEqual(expectedHumiQual, humidTuple.Item1, "Primary Humidity alarm is {0}, Expected: {1}",humidTuple.Item1,expectedHumiQual);
            Assert.AreEqual(metTowerQuality, nodata, "No data alarm status not equal. Seeing {0}. Expected {1}, ",metTowerQuality,nodata);
        }

        public void cleanup()
        {
            //resetTagsToZero();
            //setValidMetData();
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
            writeValue(String.Format("{0}.Met1.AmbTmp1", siteName), 50.33);
            writeValue(String.Format("{0}.Met1.AmbTmp2", siteName), 52.00);
            writeValue(String.Format("{0}.Met1.RH1", siteName), 30.22);
            writeValue(String.Format("{0}.Met1.RH2", siteName), 25.22);
            //Met2
            writeValue(String.Format("{0}.Met2.AmbTmp1", siteName), 40.00);
            writeValue(String.Format("{0}.Met2.AmbTmp2", siteName), 35.00);
            writeValue(String.Format("{0}.Met2.RH1", siteName), 50.11);
            writeValue(String.Format("{0}.Met2.RH2", siteName), 45.11);
        }

        //Method used in this class to write values 
        private void writeValue(string tag, double value)
        {
            opcServer.writeTagValue(tag, value);
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met1", -50, -50, MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY)]
        [DataRow("Met1", 0, 0, MetTowerMediator.MetQualityEnum.MET_GOOD_QUALITY)]
        [DataRow("Met1", 61, 61,MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY)] 
        public void tempOutOfRangeTest(string metId, double temp1, double temp2, Object failureExpected)
        {
            tm.createTestTurbines();
            var turbine = tm.getTurbinePrefixList()[0];

            mm.writePrimTemperature(metId, temp1);
            mm.writeSecTemperature(metId, temp2);

            MetTower met = mm.getMetTower(metId);
            Thread.Sleep(500);
            var readTemperature = mm.readTemperature(metId);

            //Assert.AreEqual(Convert.ToDouble(readTemperature), temp1, 0.001, "Temperature are not equal. Read Temperature: {0}, Set Temperature {1}",readTemperature,temp1);
            Console.WriteLine("Read Temperature: {0}, Set Temperature {1}",readTemperature,temp1);


            bool primOutOfRange = Convert.ToBoolean(met.TemperaturePrimOutOfRange.ToString());
            bool secOutOfRange = Convert.ToBoolean(met.TemperatureSecOutOfRange.ToString());

            bool primQuaality = Convert.ToBoolean(met.TemperaturePrimBadQuality.ToString());
            bool secQuaality = Convert.ToBoolean(met.TemperatureSecBadQuality.ToString());


            bool inputQualityStatus = !Convert.ToBoolean(failureExpected);


            Thread.Sleep(500);
            Assert.AreEqual(inputQualityStatus, primOutOfRange,"input quality {0}, prim out of range {1}",inputQualityStatus,primOutOfRange);
            Assert.AreEqual(inputQualityStatus, secOutOfRange,"input quality {0}, sec out of range {1}",inputQualityStatus,secOutOfRange);
            Assert.AreEqual(inputQualityStatus, primQuaality,"input quality {0}, prim Quality {1}",inputQualityStatus,primQuaality);
            Assert.AreEqual(inputQualityStatus, secQuaality,"input quality {0}, sec Quality {1}",inputQualityStatus,secQuaality);

        }


        [TestMethod]
        [DataTestMethod]
        [DataRow("Met1", -1, -1, MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY)]
        [DataRow("Met1", 20, 20, MetTowerMediator.MetQualityEnum.MET_GOOD_QUALITY)]
        [DataRow("Met1", 101, 101,MetTowerMediator.MetQualityEnum.MET_BAD_QUALITY)] 
        public void humidityOutOfRangeTest(string metId, double temp1, double temp2, Object failureExpected)
        {
            tm.createTestTurbines();
            var turbine = tm.getTurbinePrefixList()[0];

            mm.writeHumidity(metId, temp1);

            MetTower met = mm.getMetTower(metId);
            var readValue =mm.readHumidity(metId);
            Thread.Sleep(500);
            bool  primQuaality = Convert.ToBoolean(met.HumidityBadQuality.ToString());
            bool primOutOfRange = Convert.ToBoolean(met.HumidityOutOfRng.ToString());

            var inputStatus =!Convert.ToBoolean(failureExpected);
            Thread.Sleep(500);
            Assert.AreEqual(inputStatus, primOutOfRange,"Input Status: {0}, prim Humidity Out of Range: {1}, Read Value: {2}",inputStatus,primOutOfRange, readValue);
            Assert.AreEqual(inputStatus, primQuaality,"Input Status: {0},prim Humidity Quality{1}, Read Value: {2}",inputStatus,primQuaality, readValue);



        }

    }
}
