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

        public OpcServerTest()
        {
            opcServer = new OpcServer("SV.OPCDAServer.1");
            prefix = "SCRAB";
        }


        [TestMethod]
        //Test to see if tags will read from the server.
        public void readValueFromOpcTagTest()
        {
            Assert.Fail("Need to read Opc Tag from Database. You currently have dummy tags");
            string opcStringTestTag = "Folder1.StringItem";
            string opcBoolTestTag = "Folder1.BooleanItem";
            string opcIntTestTag = "Folder1.IntegerItem";

            string result = opcServer.readTagValue(opcStringTestTag);
            Assert.IsNotNull(opcStringTestTag);

            result = opcServer.readTagValue(opcBoolTestTag);
            Assert.IsNotNull(result);

            result = opcServer.readTagValue(opcIntTestTag);
            Assert.IsNotNull(result);

            int flatLineSamples = DatabaseInterface.Instance.getSampleCountForStaleDataTag();
        }


        [TestMethod]
        //Tests to see if the values will write to the Opc Server
        public void writeValueToOpcTagTest()
        {

            Assert.Fail("Need to write Opc Tag from Database. You currently have dummy tags");
            string opcStringTestTag = "Folder1.StringItem";
            string opcBoolTestTag = "Folder1.BooleanItem";
            string opcIntTestTag = "Folder1.IntegerItem";

            string result = opcServer.readTagValue(opcStringTestTag);
            Assert.IsNotNull(opcStringTestTag);

            result = opcServer.readTagValue(opcBoolTestTag);
            Assert.IsNotNull(result);

            string opcStringTestTag = "Folder1.StringItem";
            string opcBoolTestTag = "Folder1.BooleanItem";
            string opcIntTestTag = "Folder1.IntegerItem";

            string stringTestValue = "Fuck this shit";
            int intTestValue = 1234512345;
            bool boolTestValue = false;

            opcServer.writeTagValue(opcStringTestTag, stringTestValue);
            string result = opcServer.readTagValue(opcStringTestTag);
            Assert.AreEqual(result, stringTestValue);

            opcServer.writeTagValue(opcBoolTestTag, boolTestValue);
            result = opcServer.readTagValue(opcBoolTestTag);
            Assert.AreEqual(result.ToString(), boolTestValue.ToString());

            opcServer.writeTagValue(opcIntTestTag, intTestValue);
            result = opcServer.readTagValue(opcIntTestTag);
            Assert.AreEqual(result, intTestValue.ToString());

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
            var tagThatDoesntExist = opcServer.readTagValue(tagName);
            opcServer.writeTagValue(tagName, 0);
            Assert.IsNull(tagThatDoesntExist);
        }



    }
}
