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
        [DataRow(2.93,true)]
        [DataRow(4.12,false)]
        [DataRow(3.75,true)]
        public void binarySearchTest(double windSpeed,bool nrsState)
        {
            RotorSpeedFilter rft = new RotorSpeedFilter();
            Tuple<double,double> resultTuple =  rft.search(windSpeed,nrsState);
            Console.WriteLine("WindSpeed: {0} nrs: {1} RotorSpeed: {2} StdDev: {3}",windSpeed, nrsState, resultTuple.Item1, resultTuple.Item2);
        }
    }
}
