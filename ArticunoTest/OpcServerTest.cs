using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;

namespace ArticunoTest
{
    /// <summary>
    /// Summary description for OpcServerTest
    /// </summary>
    [TestClass]
    public class OpcServerTest
    {
        OpcServer opcServer;
        public string prefix;
        private string siteName;
        DatabaseInterface dbi;

        public OpcServerTest()
        {
            dbi = DatabaseInterface.Instance;
            siteName = dbi.readQuery("SELECT DefaultValue from SystemInputTags where Description='SitePrefix'").Rows[0][0].ToString()+".";
            opcServer = new OpcServer(dbi.getOpcServerName());
            prefix = dbi.getOpcServerName();
        }


        [TestMethod]
        public void readValueFromOpcTagTest()
        {
            string opcStringTestTag = siteName+dbi.getActiveUccOpcTag();
            string opcBoolTestTag = siteName+dbi.getParticiaptionTag("T001");
            string opcIntTestTag =siteName+dbi.getTurbineRotorSpeedTag("T001");

            string result = opcServer.readTagValue(opcStringTestTag);
            Assert.IsNotNull(result);

            result = opcServer.readTagValue(opcBoolTestTag);
            Assert.IsNotNull(result);

            result = opcServer.readTagValue(opcIntTestTag);
            Assert.IsNotNull(result);

            int flatLineSamples = DatabaseInterface.Instance.getSampleCountForStaleData();
        }


        [TestMethod]
        //Tests to see if the values will write to the Opc Server
        public void writeValueToOpcTagTest()
        {
            string opcBoolTestTag =siteName + dbi.GetTowerBadPrimaryTempSensorTag("Met");

            string result = opcServer.readTagValue(opcBoolTestTag);
            Assert.IsNotNull(result);

            bool boolTestValue = true;

            opcServer.writeTagValue(opcBoolTestTag, boolTestValue);
            result = opcServer.readTagValue(opcBoolTestTag);
            Assert.AreEqual(result.ToString(), boolTestValue.ToString());
        }

        [TestMethod]
        //Test to read from turbine input tags and write to turbine output tags 
        public void turbineTagTest()
        {
            //Test the read/write from the input tag
            string[] turbineInputTags =
            {
                prefix + "." + "T001.WTUR.DmdW.actVal",
                prefix + "." + "T001.WTUR.NoiseLev",
                prefix + "." + "T001.WTUR.TURST.ACTST",
                prefix + "." + "T001.WTUR.SetTurOp.ActSt.Stop",
                prefix + "." + "T001.WROT.RotSpdAv",
                prefix + "." + "T001.wnac.ExTmp",
                prefix + "." + "T001.wnac.wdspda"
            };

            foreach (string inputTag in turbineInputTags)
            {
                var temp = opcServer.readTagValue(inputTag);
                Assert.AreNotEqual(temp, "-1");
            }


            //Test the write to the hearbeat
            string[] turbineOutputTags =
            {
                prefix+"."+"Articuno.T001.PlwArticunoStop",
                prefix + "." + "Articuno.T001.Participation"
            };

        }

        [TestMethod]
        //Test conditions for tags that doens't exist (because soemthing made a typo)
        public void tagNotFound()
        {
            string tagName = prefix + ".TagNotFound";
            try
            {
                var tagThatDoesntExist = opcServer.readTagValue(tagName);
                opcServer.writeTagValue(tagName, 0);
            }
            catch (NullReferenceException)
            {
                Assert.IsTrue(true, String.Format("Null exception caught for tag {0} ",tagName));
            }
        }



    }
}
