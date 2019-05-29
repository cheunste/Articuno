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

        public OpcServerTest()
        {
            opcServer = new OpcServer("");
        }

        [TestMethod ]
        //Test to see if tags will read from the server 
        public void readTagTest()
        {
            Assert.Fail();

            string opcStringTestTag = "";
            string opcBoolTestTag = "";
            string opcIntTestTag = "";

            string result = opcServer.readTagValue(opcStringTestTag);
            Assert.IsNotNull(opcStringTestTag);

            result = opcServer.readTagValue(opcBoolTestTag);
            Assert.IsNotNull(result);

            result =opcServer.readTagValue(opcIntTestTag);
            Assert.IsNotNull(result);

        }

        [TestMethod ]
        //Tests to see if the values will write to the Opc Server
        public void setTagTest()
        {

            Assert.Fail();

            //Opc Tags These 
            string opcStringTestTag = "";
            string opcBoolTestTag = "";
            string opcIntTestTag = "";

            opcServer.setTagValue(opcStringTestTag, "");
            string result = opcServer.readTagValue(opcStringTestTag);
            Assert.AreEqual(result, "");

            opcServer.setTagValue(opcBoolTestTag, "");
            result = opcServer.readTagValue(opcStringTestTag);
            Assert.AreEqual(result, "");

            opcServer.setTagValue(opcIntTestTag, "");
            result = opcServer.readTagValue(opcStringTestTag);
            Assert.AreEqual(result, "");

        }

    }
}
