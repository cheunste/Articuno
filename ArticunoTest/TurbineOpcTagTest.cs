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
            string columnFilter = "Participation";
            checkTag(table, columnFilter);
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
            string columnFilter = "RotorSpeed";
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void TurbineTemperatureOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select Temperature from TurbineInputTags");
            string columnFilter = "Temperature";
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void PauseOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select Pause from TurbineInputTags");
            string columnFilter = "Pause";
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void StartOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select Start from TurbineInputTags");
            string columnFilter = "Start";
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void WindSpeedOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select WindSpeed from TurbineInputTags");
            string columnFilter = "WindSpeed";
            checkTag(table, columnFilter);
        }

        [TestMethod]
        public void AlarmOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select Alarm from TurbineOutputTags");
            string columnFilter = "Alarm";
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void AgcBlockingOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select AGCBlocking from TurbineOutputTags");
            string columnFilter = "AGCBlocking";
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void LowRotorSpeedFlagOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select LowRotorSpeedFlag from TurbineOutputTags");
            string columnFilter = "LowRotorSpeedFlag";
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void TurbineCtrCountdownOpcTagExistance()
        {
            DataTable table = dbi.readQuery("Select CTRCountdown from TurbineOutputTags");
            string columnFilter = "CTRCountdown";
            checkTag(table, columnFilter);
        }
        private void checkTag(DataTable table, string columnFilter)
        {
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row[columnFilter].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
    }
}
