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
            siteName = dbi.getSitePrefixValue();
            opcServer = new OpcServer(dbi.getOpcServerName());
            prefix = dbi.getOpcServerName();
        }


        [TestMethod]
        public void readValueFromOpcTagTest()
        {
            string opcStringTestTag = dbi.getActiveUccOpcTag();
            string opcBoolTestTag = siteName+dbi.getTurbineParticiaptionTag("T001");
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
        public void writeValueToOpcTagTest()
        {
            string opcBoolTestTag = siteName+dbi.getArticunoEnableTag();
            string result = opcServer.readTagValue(opcBoolTestTag);
            Assert.IsNotNull(result);

            bool boolTestValue = true;

            opcServer.writeTagValue(opcBoolTestTag, boolTestValue);
            result = opcServer.readTagValue(opcBoolTestTag);
            Assert.IsTrue(Convert.ToBoolean(result));

            boolTestValue = false;
            opcServer.writeTagValue(opcBoolTestTag, boolTestValue);
            result = opcServer.readTagValue(opcBoolTestTag);
            Assert.AreEqual(Convert.ToBoolean(result),boolTestValue);

            opcServer.writeTagValue(opcBoolTestTag, true);
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
            catch (Exception e)
            {
                Assert.IsTrue(true, String.Format("Null exception caught for tag {0} ",tagName));
            }
        }



    }
}
