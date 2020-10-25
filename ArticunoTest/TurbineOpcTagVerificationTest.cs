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
            checkOpcTag(table);
        }

        [TestMethod]
        public void NrsOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select NrsMode from TurbineInputTags");
            foreach (DataRow row in table.Rows)
            {
                if (row[0].ToString().Equals(""))
                {
                    Assert.IsTrue(true, "No item in the NRSMode column, which can be intention");
                }
                else
                {
                    string tag = string.Format("{0}{1}", prefix, row[0].ToString());
                    try
                    {
                        string value = opcServer.readTagValue(tag);
                    }
                    catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
                }
            }
        }
        [TestMethod]
        public void RotorSpeedOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select RotorSpeed from TurbineInputTags");
            checkOpcTag(table);
        }
        [TestMethod]
        public void TurbineTemperatureOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select Temperature from TurbineInputTags");
            checkOpcTag(table);
        }
        [TestMethod]
        public void PauseOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select Pause from TurbineInputTags");
            checkOpcTag(table);
        }
        [TestMethod]
        public void StartOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select Start from TurbineInputTags");
            checkOpcTag(table);
        }
        [TestMethod]
        public void WindSpeedOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select WindSpeed from TurbineInputTags");
            checkOpcTag(table);
        }

        [TestMethod]
        public void AlarmOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select Alarm from TurbineOutputTags");
            checkOpcTag(table);
        }
        [TestMethod]
        public void AgcBlockingOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select AGCBlocking from TurbineOutputTags");
            checkOpcTag(table);
        }
        [TestMethod]
        public void LowRotorSpeedFlagOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select LowRotorSpeedFlag from TurbineOutputTags");
            checkOpcTag(table);
        }
        [TestMethod]
        public void TurbineCtrCountdownOpcTagCheck()
        {
            DataTable table = dbi.readQuery("Select CTRCountdown from TurbineOutputTags");
            checkOpcTag(table);
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

        private void checkOpcTag(DataTable table)
        {
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row[0].ToString());
                Console.WriteLine(tag);
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
    }
}
