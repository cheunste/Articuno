using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Articuno;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ArticunoTest
{
    [TestClass]
    public class RotorSpeedFilterTest
    {

        [TestMethod]
        [DataRow(2.93,true,10.194)]
        [DataRow(4.12,false,10.268)]
        [DataRow(3.75,true,10.255)]
        public void binarySearchTest(double windSpeed,bool nrsState, double expectedRS)
        {
            RotorSpeedFilter rft = new RotorSpeedFilter();
            Tuple<double,double> resultTuple =  rft.search(windSpeed,nrsState);
            Console.WriteLine("WindSpeed: {0} nrs: {1} RotorSpeed: {2} StdDev: {3} expectedRS {4}",windSpeed, nrsState, resultTuple.Item1, resultTuple.Item2, expectedRS);

            Assert.AreEqual(resultTuple.Item1, expectedRS, 0.01, "The Rotor speed are not relatively equal");
        }
    }
}
