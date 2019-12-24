using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data;
using System.Text.RegularExpressions;

namespace ArticunoTest
{
    [TestClass]
    public class TurbineOpcTagVerificationTest
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
        public void ParticipationOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select Participation from TurbineInputTags");
            string columnFilter = "Participation";
            checkOpcTag(table, columnFilter);
        }

        [TestMethod]
        public void NrsOpcTagCheck()
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
        public void RotorSpeedOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select RotorSpeed from TurbineInputTags");
            string columnFilter = "RotorSpeed";
            checkOpcTag(table, columnFilter);
        }
        [TestMethod]
        public void TurbineTemperatureOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select Temperature from TurbineInputTags");
            string columnFilter = "Temperature";
            checkOpcTag(table, columnFilter);
        }
        [TestMethod]
        public void PauseOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select Pause from TurbineInputTags");
            string columnFilter = "Pause";
            checkOpcTag(table, columnFilter);
        }
        [TestMethod]
        public void StartOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select Start from TurbineInputTags");
            string columnFilter = "Start";
            checkOpcTag(table, columnFilter);
        }
        [TestMethod]
        public void WindSpeedOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select WindSpeed from TurbineInputTags");
            string columnFilter = "WindSpeed";
            checkOpcTag(table, columnFilter);
        }

        [TestMethod]
        public void AlarmOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select Alarm from TurbineOutputTags");
            string columnFilter = "Alarm";
            checkOpcTag(table, columnFilter);
        }
        [TestMethod]
        public void AgcBlockingOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select AGCBlocking from TurbineOutputTags");
            string columnFilter = "AGCBlocking";
            checkOpcTag(table, columnFilter);
        }
        [TestMethod]
        public void LowRotorSpeedFlagOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select LowRotorSpeedFlag from TurbineOutputTags");
            string columnFilter = "LowRotorSpeedFlag";
            checkOpcTag(table, columnFilter);
        }
        [TestMethod]
        public void TurbineCtrCountdownOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select CTRCountdown from TurbineOutputTags");
            string columnFilter = "CTRCountdown";
            checkOpcTag(table, columnFilter);
        }
        [TestMethod]
        public void TurbineIdValidationTest()
        {
            string columnFilter = "TurbineId";
            DataTable table = dbi.readQuery(string.Format("Select {0} from TurbineInputTags", columnFilter));
            foreach (DataRow row in table.Rows)
            {
                string metPrefix = string.Format("{0}", row[columnFilter].ToString());

                string pattern = @"\b\w{3}(\d{1})*\b";
                Regex lookup = new Regex(pattern, RegexOptions.Singleline);

                if (!lookup.IsMatch(metPrefix))
                    Assert.Fail("Format for {0} looks suspect", metPrefix);
                if (metPrefix.Equals(""))
                    Assert.Fail("the TurbineId field is empty for {0}", metPrefix);
            }
            table = dbi.readQuery(string.Format("Select {0} from TurbineOutputTags", columnFilter));
            foreach (DataRow row in table.Rows)
            {
                string metPrefix = string.Format("{0}", row[columnFilter].ToString());

                string pattern = @"\b\w{3}(\d{1})*\b";
                Regex lookup = new Regex(pattern, RegexOptions.Singleline);

                if (!lookup.IsMatch(metPrefix))
                    Assert.Fail("Format for {0} looks suspect", metPrefix);
                if (metPrefix.Equals(""))
                    Assert.Fail("the TurbineId field is empty for {0}", metPrefix);
            }
        }

        private void checkOpcTag(DataTable table, string columnFilter)
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
