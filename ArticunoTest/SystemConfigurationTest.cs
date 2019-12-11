using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArticunoTest
{
    [TestClass]
    public class SystemConfigurationTest
    {
        [TestMethod]
        public void getTemperatreThresholdOpcTag() { }

        [TestMethod]
        public void getArticunoEnableOpcTag() { }

        [TestMethod]
        public void getCtrPeriodOpcTag() { }
        [TestMethod]
        public void getScalingFactorOpcTag() { }
        [TestMethod]
        public void getOpcServerNameTag() { }
        [TestMethod]
        public void getHeartbeatIntervalValue() {  }
        [TestMethod]
        public void getTurbineStartupTimeValue() { }
        [TestMethod]
        public void getFlatlineSamplesValue() { }
        [TestMethod]
        public void getMetTowerCtrCountDownTag()
        {
            Assert.Fail("Met Count Samples not implemented Test ");

        }

    }
}
