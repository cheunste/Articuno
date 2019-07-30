using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;

namespace ArticunoTest
{
    /// <summary>
    /// Summary description for ArticunoTest
    /// </summary>
    [TestClass]
    public class ArticunoTest
    {
        ArticunoMain articuno;
        MetTowerMediator mm;
        TurbineMediator tm;
        public ArticunoTest()
        {
            articuno = new ArticunoMain();
            mm = MetTowerMediator.Instance;
            tm = TurbineMediator.Instance;

            mm.createMetTower();
            tm.createTestTurbines();
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
        public void SetupTest()
        {
            articuno = new ArticunoMain();
            
        }

        [TestMethod]
        [DataTestMethod]
        [DataRow("Met1",-20,-20,90.0)]
        public void IcedTowerTest(string metId,double temp1, double temp2, double humidity)
        {
            Assert.Fail();
        }

        //[TestMethod]
        //[DataTestMethod]
        //[DataRow("T001", false)]
        //[DataRow("T001", true)]
        //public void AlgorithmTest(string turbineId, bool state)
        //{
        //    //Note complete. Do this later once you get the delegates figured out
        //    tm.setTemperatureCondition(turbineId, state);
        //    tm.setOperatingStateCondition(turbineId, state);
        //    tm.setNrscondition(turbineId, state);
        //    tm.setTurbinePerformanceCondition(turbineId, state);
        //    tm.setDeRateCondition(turbineId, state);

        //    //If all five are true, then this turbine should be paused due to Ice
        //    //After some CTR Time
        //    //wait 90 seconds
        //    System.Threading.Thread.Sleep(90000);
        //    Assert.AreEqual(state,TurbineMediator.Instance.isPausedByArticuno(turbineId));

        //}

        [TestMethod]
        public void minuteTest()
        {
            Assert.Fail();
            var timer = new System.Threading.Timer((e) => { minuteTestFunction(); }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(60*1000));
        }

        private void minuteTestFunction() {

        }

    }
}
