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
            opcServer = new OpcServer("SV.OPCDAServer.1");
        }

        [TestMethod ]
        //Test to see if tags will read from the server 
        public void readTagTest()
        {
            string opcStringTestTag = "Folder1.StringItem";
            string opcBoolTestTag = "Folder1.BooleanItem";
            string opcIntTestTag = "Folder1.IntegerItem";

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

            //Opc Tags These 
            string opcStringTestTag = "Folder1.StringItem";
            string opcBoolTestTag = "Folder1.BooleanItem";
            string opcIntTestTag = "Folder1.IntegerItem";

            string stringTestValue = "Fuck this shit";
            int intTestValue = 1234512345;
            bool boolTestValue = false;

            opcServer.setTagValue(opcStringTestTag, stringTestValue);
            string result = opcServer.readTagValue(opcStringTestTag);
            Assert.AreEqual(result, stringTestValue);

            opcServer.setTagValue(opcBoolTestTag,boolTestValue);
            result = opcServer.readTagValue(opcBoolTestTag);
            Assert.AreEqual(result.ToString(), boolTestValue.ToString());

            opcServer.setTagValue(opcIntTestTag, intTestValue);
            result = opcServer.readTagValue(opcIntTestTag);
            Assert.AreEqual(result,intTestValue.ToString());

        }

        [TestMethod]
        //Test to read from turbine input tags and write to turbine output tags 
        public void turbineTagTest()
        {
        }

    }
}
