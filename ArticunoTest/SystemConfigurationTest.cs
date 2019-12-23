using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;

namespace ArticunoTest
{
    [TestClass]
    public class SystemConfigurationTest
    {
        DatabaseInterface dbi;
        OpcServer opcServer;
        string prefix;

        public SystemConfigurationTest()
        {
            dbi = DatabaseInterface.Instance;
            opcServer = new OpcServer(dbi.getOpcServerName());
            prefix = dbi.getSitePrefixValue();

        }
        [TestMethod]
        public void getActiveUccOpcTagTest()
        {
            string tag = dbi.getActiveUccOpcTag();
            readTag(tag);
        }


        [TestMethod]
        public void getTemperatreThresholdOpcTagTest()
        {
            string tag = dbi.getTemperatureThresholdTag();
            readTag(tag);
        }

        [TestMethod]
        public void getArticunoEnableOpcTagTest()
        {
            string tag = dbi.getArticunoEnableTag();
            readTag(tag);
        }

        [TestMethod]
        public void getArticunoCtrPeriodOpcTagTest()
        {
            string tag = dbi.getArticunoCtrTag();
            readTag(tag);

        }
        [TestMethod]
        public void getScalingFactorOpcTagTest()
        {
            string value = dbi.GetTurbineScalingFactor();
            Assert.IsNotNull(value);
            Assert.AreNotEqual(value, "");
        }
        [TestMethod]
        public void getOpcServerNameTagTest()
        {
            string value = dbi.getOpcServerName();
            Assert.IsNotNull(value);
            Assert.AreNotEqual(value, "");
            Assert.AreEqual(value, "SV.OPCDAServer.1");
        }
        [TestMethod]
        public void getHeartbeatIntervalValueTest()
        {
            int value = dbi.getHeartbeatIntervalValue();
            Assert.IsNotNull(value);
            Assert.AreNotEqual(value, "");
            Assert.IsTrue(value > 0);
        }
        [TestMethod]
        public void getTurbineStartupTimeValueTest()
        {
            int value = dbi.GetTurbineStartupTime();
            Assert.IsNotNull(value);
            Assert.AreNotEqual(value, "");
            Assert.IsTrue(value > 0);
        }
        [TestMethod]
        public void getStaleSamplesValueTest()
        {
            int tag = dbi.getSampleCountForStaleData();
            Assert.IsNotNull(tag);
            Assert.AreNotEqual("", tag);
        }
        [TestMethod]
        public void getMetTowerCtrCountDownTagTest()
        {
            Assert.Fail("Test Not implemented yet");
            string tag = dbi.getMetTowerCtrCountdownTag();
            readTag(tag);
        }

        [TestMethod]
        public void getArticunoHeartbeatOpcTag()
        {
            string tag = dbi.GetArticunoHeartbeatTag();
            readTag(tag);

        }
        [TestMethod]
        public void getArticunoIcePossibleOpcTag()
        {

            string tag = dbi.GetArticunoIcePossibleOpcTag();
            readTag(tag);
        }
        [TestMethod]
        public void getArticunoNumberOfTurbinesPausedByIce()
        {

            string tag = dbi.GetArticunoNumbersOfTurbinesPausedTag();
            readTag(tag);
        }

        private void readTag(string tag)
        {
            Assert.IsNotNull(tag);
            Assert.AreNotEqual(tag, "");
            try { opcServer.readTagValue(tag); }
            catch (Exception e) { Assert.Fail("Tag {0} does not exist", tag); }
        }
    }
}
