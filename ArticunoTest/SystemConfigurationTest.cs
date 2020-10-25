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
            string tag = prefix+dbi.getTemperatureThresholdTag();
            readTag(tag);
        }

        [TestMethod]
        public void getArticunoEnableOpcTagTest()
        {
            string tag = prefix+dbi.getArticunoEnableTag();
            readTag(tag);
        }

        [TestMethod]
        public void getArticunoCtrPeriodOpcTagTest()
        {
            string tag = prefix+dbi.getArticunoCtrTag();
            readTag(tag);

        }
        [TestMethod]
        public void getScalingFactorOpcTagTest()
        {
            string value = prefix+dbi.GetTurbineScalingFactorValue();
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
            int value = Convert.ToInt32(dbi.GetTurbineStartupTime());
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
            string tag =prefix+ dbi.getMetTowerCtrCountdownTag();
            readTag(tag);
        }

        [TestMethod]
        public void getArticunoHeartbeatOpcTag()
        {
            string tag = prefix+dbi.GetArticunoHeartbeatTag();
            readTag(tag);

        }
        [TestMethod]
        public void getArticunoIcePossibleOpcTag()
        {

            string tag = prefix+dbi.GetArticunoIcePossibleOpcTag();
            readTag(tag);
        }
        [TestMethod]
        public void getArticunoNumberOfTurbinesPausedByIce()
        {

            string tag = prefix+dbi.GetArticunoNumbersOfTurbinesPausedTag();
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
