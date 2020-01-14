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
    public class MetTowerOpcTagVerificationTest
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
        public void primTempOpcTagTest()
        {
            string columnFilter = "PrimTempValueTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerInputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }

        [TestMethod]
        public void secTempValueOpcTagTest()
        {
            string columnFilter = "SecTempValueTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerInputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }

        [TestMethod]
        public void primHumidityOpcTagTest()
        {
            string columnFilter = "PrimHumidityValueTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerInputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }
        [TestMethod]
        public void MetTowerSwitchOpcTagTest()
        {
            string columnFilter = "Switch";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerInputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }
        [TestMethod]
        public void MetTowerTurbineBackupOpcTagTest()
        {
            string columnFilter = "BackupTurbine";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerInputTags", columnFilter));
            foreach (DataRow row in table.Rows)
            {
                var turbine = row[0].ToString();
                try
                {
                    DataTable turbineTable = dbi.readQuery(String.Format("Select TurbineId from TurbineInputTags where TurbineId='{0}'", turbine));
                    turbineTable.Rows[0].ToString();
                    Assert.IsTrue(turbineTable.Rows.Count > 0);
                }
                catch (Exception e)
                {
                    Assert.Fail("Turbine {0} can't be found in the TurbineInputTagsTable", turbine);
                }
            }
        }
        [TestMethod]
        public void TempPrimBadOpcTagTest()
        {
            string columnFilter = "TempPrimBadQualityTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }
        [TestMethod]
        public void TempPRimOutOfRangeOpcTagTest()
        {
            string columnFilter = "TempPrimOutOfRangeTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }
        [TestMethod]
        public void TEmpSecOutOfRangeTagOpcTagTest()
        {
            string columnFilter = "TempSecOutOfRangeTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }
        [TestMethod]
        public void TempSecBadQualityTagOpcTagTest()
        {
            string columnFilter = "TempSecBadQualityTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }

        [TestMethod]
        public void HumidityOutOfRangeTag()
        {
            string columnFilter = "HumidityOutOfRangeTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }
        [TestMethod]
        public void HumidityBadQualityTagTest()
        {
            string columnFilter = "HumidityBadQualityTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }
        [TestMethod]
        public void IceInditionOpcTagTest()
        {
            string columnFilter = "IceIndicationTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }
        [TestMethod]
        public void NoDataOpcTagTest()
        {
            string columnFilter = "NoDataAlarmTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }

        [TestMethod]
        public void CtrTemperatureOpcTagTest()
        {
            string columnFilter = "CtrTemperature";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }
        [TestMethod]
        public void CtrDewOpcTagTest()
        {
            string columnFilter = "CtrDew";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }
        [TestMethod]
        public void CtrHumidityOpcTagTest()
        {
            string columnFilter = "CtrHumidity";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            verifyOpcTagOnServer(table, columnFilter);
        }

        [TestMethod]
        [DataRow("Met")]
        [DataRow("Met2")]
        public void VerifiyBackupTurbinesField(string metId)
        {
            string backupTurbine = dbi.GetBackupTurbine(metId);
            Console.WriteLine(backupTurbine);
            Assert.IsNotNull(backupTurbine);
            Assert.AreNotEqual(backupTurbine, "");

            string turbineRegexPattern = @"[AT]\d+\b";
            Regex lookup = new Regex(turbineRegexPattern, RegexOptions.Singleline);

            if (!lookup.IsMatch(backupTurbine))
                Assert.Fail("Format for {0} looks suspect", metId);
            if (backupTurbine.Equals(""))
                Assert.Fail("the BackupTurbine field is empty for {0}", metId);
            if (!turbineExistInConfig(backupTurbine))
                Assert.Fail("Turbine doesn't exist in both the Turbine Config tables");
        }



        [TestMethod]
        public void MetIdValidationTest()
        {
            string columnFilter = "MetId";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            foreach (DataRow row in table.Rows)
            {
                string metPrefix = string.Format("{0}", row[0].ToString());
                string metTowerIdPattern = @"\b\w{3}(\d{1})*\b";
                Regex lookup = new Regex(metTowerIdPattern, RegexOptions.Singleline);

                if (!lookup.IsMatch(metPrefix))
                    Assert.Fail("Format for {0} looks suspect", metPrefix);
                if (metPrefix.Equals(""))
                    Assert.Fail("the MetId field is empty for {0}", metPrefix);
            }
            table = dbi.readQuery(string.Format("Select {0} from MetTowerInputTags", columnFilter));
            foreach (DataRow row in table.Rows)
            {
                string metPrefix = string.Format("{0}", row[columnFilter].ToString());

                string metTowerIdPattern = @"\b\w{3}(\d{1})*\b";
                Regex lookup = new Regex(metTowerIdPattern, RegexOptions.Singleline);

                if (!lookup.IsMatch(metPrefix))
                    Assert.Fail("Format for {0} looks suspect", metPrefix);
                if (metPrefix.Equals(""))
                    Assert.Fail("the MetId field is empty for {0}", metPrefix);
            }
        }

        private void verifyOpcTagOnServer(DataTable table, string columnFilter)
        {
            foreach (DataRow row in table.Rows)
            {
                string tag = string.Format("{0}{1}", prefix, row[columnFilter].ToString());
                try { string value = opcServer.readTagValue(tag); }
                catch (Exception) { Assert.Fail("tag {0} cannot be found", tag); }
            }
        }
        private bool turbineExistInConfig(string turbineId)
        {
            string turbineIdFromTurbineInputTable = dbi.readQuery(String.Format("SELECT TurbineId from TurbineInputTags where TurbineId='{0}'", turbineId)).Rows[0]["TurbineId"].ToString();
            string turbineIdFromTurbineOutputTable = dbi.readQuery(String.Format("SELECT TurbineId from TurbineOutputTags where TurbineId='{0}'", turbineId)).Rows[0]["TurbineId"].ToString();
            if (turbineIdFromTurbineInputTable.Equals("") || turbineIdFromTurbineOutputTable.Equals(""))
                return false;
            else
                return true;
        }
    }
}
