using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data;

namespace ArticunoTest
{
    [TestClass]
    public class TurbineOpcTagTest
    {

        DatabaseInterface dbi;
        OpcServer opcServer;
        string prefix;
        string opcServerName;

        [TestInitialize]
        public void setup()
        {

            dbi = DatabaseInterface.Instance;
            opcServerName = dbi.getOpcServerName();
            opcServer = new OpcServer(dbi.getOpcServerName());
            prefix = dbi.getSitePrefixValue();
        }

        [TestMethod]
        public void ParticipationOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select Participation from TurbineInputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["Participation"].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }

        }

        [TestMethod]
        public void NrsOpcTagExistance()
        {
            Assert.Fail("Haven't finished writing test");
            DataTable table = dbi.readQuery("Select NrsMode from TurbineInputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["NrsMode"].ToString());
                try
                {
                    string value = opcServer.readTagValue(tag);
                }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
        [TestMethod]
        public void RotorSpeedOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select RotorSpeed from TurbineInputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["RotorSpeed"].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
        [TestMethod]
        public void TurbineTemperatureOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select Temperature from TurbineInputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["Temperature"].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
        [TestMethod]
        public void PauseOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select Pause from TurbineInputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["Pause"].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
        [TestMethod]
        public void StartOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select Start from TurbineInputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["Start"].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
        [TestMethod]
        public void WindSpeedOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select WindSpeed from TurbineInputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["WindSpeed"].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }

        [TestMethod]
        public void AlarmOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select Alarm from TurbineOutputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["Alarm"].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
        [TestMethod]
        public void AgcBlockingOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select AGCBlocking from TurbineOutputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["AGCBlocking"].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
        [TestMethod]
        public void LowRotorSpeedFlagOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select LowRotorSpeedFlag from TurbineOutputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["LowRotorSpeedFlag"].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
        [TestMethod]
        public void TurbineCtrCountdownOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select CTRCountdown from TurbineOutputTags");
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row["CTRCountdown"].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }

    }
}
