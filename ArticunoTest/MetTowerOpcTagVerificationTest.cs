﻿using System;
using System.Text;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Articuno;
using System.Data;

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
            checkTag(table, columnFilter);
        }

        [TestMethod]
        public void secTempValueOpcTagTest()
        {
            string columnFilter = "SecTempValueTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerInputTags", columnFilter));
            checkTag(table, columnFilter);
        }

        [TestMethod]
        public void primHumidityOpcTagTest()
        {
            string columnFilter = "PrimHumidityValueTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerInputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void MetTowerSwitchOpcTagTest()
        {
            string columnFilter = "Switch";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerInputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void MetTowerTurbineBackupOpcTagTest()
        {

            Assert.Fail("Function not implemented");
            string columnFilter = "MappedTurbine";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerInputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void TempPrimBadOpcTagTest()
        {
            string columnFilter = "TempPrimBadQualityTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void TempPRimOutOfRangeOpcTagTest()
        {
            string columnFilter = "TempPrimOutOfRangeTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void TEmpSecOutOfRangeTagOpcTagTest()
        {
            string columnFilter = "TempSecOutOfRangeTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void TempSecBadQualityTagOpcTagTest()
        {
            string columnFilter = "TempSecBadQualityTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            checkTag(table, columnFilter);
        }

        [TestMethod]
        public void HumidityOutOfRangeTag()
        {
            string columnFilter = "HumidityOutOfRangeTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void HumidityBadQualityTagTest()
        {
            string columnFilter = "HumidityBadQualityTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void IceInditionOpcTagTest()
        {
            string columnFilter = "IceIndicationTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void NoDataOpcTagTest()
        {
            string columnFilter = "NoDataAlarmTag";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            checkTag(table, columnFilter);
        }

        [TestMethod]
        public void CtrTemperatureOpcTagTest()
        {
            string columnFilter = "CtrTemperature";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void CtrDewOpcTagTest()
        {
            string columnFilter = "CtrDew";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
            checkTag(table, columnFilter);
        }
        [TestMethod]
        public void CtrHumidityOpcTagTest()
        {
            string columnFilter = "CtrHumidity";
            DataTable table = dbi.readQuery(string.Format("Select {0} from MetTowerOutputTags", columnFilter));
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