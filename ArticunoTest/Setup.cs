using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArticunoTest
{
    class Setup
    {
        List<String> MetTowerInput;
        List<String> MetTmp1;
        List<String> MetTmp2;
        List<String> MetRH1;
        List<String> MetRH2;
        List<String> MetSwitch;
        List<String> MetAlarms;
        List<String> SystemInput;
        List<String> SystemOutput;
        List<String> Participation;
        List<String> OperatingState;
        List<String> RotorSpeed;
        List<String> TurbineTemperature;
        List<String> NrsMode;
        List<String> WindSpeed;
        List<String> PauseCmd;
        List<String> StartCmd;
        public string sitePrefix;
        public readonly string opcServerName = "SV.OPCDAServer.1";
        public string TmpThreshold;
        public string CurtailmentEnable;
        public string TmpDelta;
        public string CtrTime;

        public Setup()
        {
            MetTowerInput = new List<String>();
            MetTmp1 = new List<string>();
            MetTmp2 = new List<string>();
            MetRH1 = new List<string>();
            MetRH2 = new List<string>();
            MetSwitch = new List<string>();

            MetAlarms = new List<String>();
            SystemInput = new List<String>();
            SystemOutput = new List<String>();
            Participation = new List<string>();
            OperatingState = new List<String>();
            RotorSpeed = new List<String>();
            TurbineTemperature = new List<String>();
            NrsMode = new List<String>();
            WindSpeed = new List<String>();
            PauseCmd = new List<String>();
            StartCmd = new List<String>();

            sitePrefix = "SCRAB.";
            MetTowerInput.AddRange(new string[] {
                sitePrefix+"Articuno.Met.AmbTmp1", sitePrefix+"Articuno.Met.AmbTmp2",   sitePrefix+"Articuno.Met.RH1", sitePrefix+"Articuno.Met.RH2",   sitePrefix+"Articuno.Met.Switch",
                sitePrefix+"Articuno.Met2.AmbTmp1", sitePrefix+"Articuno.Met2.AmbTmp2", sitePrefix+"Articuno.Met2.RH1",sitePrefix+ "Articuno.Met2.RH2", sitePrefix+"Articuno.Met2.Switch"
            });

            MetTmp1.AddRange(new string[] { sitePrefix+"Articuno.Met.AmbTmp1", sitePrefix+"Articuno.Met2.AmbTmp1" });
            MetTmp2.AddRange(new string[] { sitePrefix+"Articuno.Met.AmbTmp2", sitePrefix+"Articuno.Met2.AmbTmp2" });
            MetRH1.AddRange(new string[] { sitePrefix+"Articuno.Met.RH1", sitePrefix+"Articuno.Met2.RH1" });
            MetRH2.AddRange(new string[] { sitePrefix+"Articuno.Met.RH2", sitePrefix+"Articuno.Met2.RH2" });
            MetSwitch.AddRange(new string[] { sitePrefix+"Articuno.Met2.Switch", sitePrefix+"Articuno.Met.Switch" });

            MetAlarms.AddRange(new string[]{
                sitePrefix + "Articuno.Met.TempQualityAlm", sitePrefix + "Articuno.Met.TempS1OutOfRangeAlm",    sitePrefix + "Articuno.Met.TempHiDispAlm",  sitePrefix + "Articuno.Met.TempS2OutOfRangeAlm",    sitePrefix + "Articuno.Met.TempS2QualityAlm",   sitePrefix + "Articuno.Met.TempHiDispAlm",  sitePrefix + "Articuno.Met.RHS1OutRngAlm",  sitePrefix + "Articuno.Met.RHQualityAlm",   sitePrefix + "Articuno.Met.IcePossible",    sitePrefix + "Articuno.Met.TowerAlm",   sitePrefix + "Articuno.Met.CtrTemperature", sitePrefix + "Articuno.Met.CtrDew", sitePrefix + "Articuno.Met.CtrHumidity",
                sitePrefix + "Articuno.Met2.TempQualityAlm",    sitePrefix + "Articuno.Met2.TempS1OutOfRangeAlm",   sitePrefix + "Articuno.Met2.TempHiDispAlm", sitePrefix + "Articuno.Met2.TempS2OutOfRangeAlm",   sitePrefix + "Articuno.Met2.TempS2QualityAlm",  sitePrefix + "Articuno.Met2.TempHiDispAlm", sitePrefix + "Articuno.Met2.RHS1OutRngAlm", sitePrefix + "Articuno.Met2.RHQualityAlm",  sitePrefix + "Articuno.Met2.IcePossible",   sitePrefix + "Articuno.Met2.TowerAlm",  sitePrefix + "Articuno.Met2.CtrTemperature",    sitePrefix + "Articuno.Met2.CtrDew",    sitePrefix + "Articuno.Met2.CtrHumidity"
                });

            NrsMode.AddRange(new string[] { sitePrefix + "T001.WTUR.NoiseLev.actVal", sitePrefix + "T002.WTUR.NoiseLev.actVal", sitePrefix + "T003.WTUR.NoiseLev.actVal", sitePrefix + "T004.WTUR.NoiseLev.actVal" });
            Participation.AddRange(new string[] { sitePrefix + "Articuno.T001.Participation", sitePrefix + "Articuno.T002.Participation", sitePrefix + "Articuno.T003.Participation", sitePrefix + "Articuno.T004.Participation" });
            OperatingState.AddRange(new string[] { sitePrefix + "T001.WTUR.TURST.ACTST", sitePrefix + "T002.WTUR.TURST.ACTST", sitePrefix + "T003.WTUR.TURST.ACTST", sitePrefix + "T004.WTUR.TURST.ACTST" });
            RotorSpeed.AddRange(new string[] { sitePrefix + "T001.WROT.RotSpd", sitePrefix + "T002.WROT.RotSpd", sitePrefix + "T003.WROT.RotSpd", sitePrefix + "T004.WROT.RotSpd" });
            TurbineTemperature.AddRange(new string[] { sitePrefix + "T001.WNAC.ExTmp", sitePrefix + "T002.WNAC.ExTmp", sitePrefix + "T003.WNAC.ExTmp", sitePrefix + "T004.WNAC.ExTmp" });
            WindSpeed.AddRange(new string[] { sitePrefix + "T001.WNAC.WdSpd", sitePrefix + "T002.WNAC.WdSpd", sitePrefix + "T003.WNAC.WdSpd", sitePrefix + "T004.WNAC.WdSpd" });
            PauseCmd.AddRange(new string[] { sitePrefix + "T001.WTUR.SetTurOp.ActSt.Stop", sitePrefix + "T002.WTUR.SetTurOp.ActSt.Stop", sitePrefix + "T003.WTUR.SetTurOp.ActSt.Stop", sitePrefix + "T004.WTUR.SetTurOp.ActSt.Stop" });
            StartCmd.AddRange(new string[] { sitePrefix + "T001.WTUR.SetTurOp.ActSt.Str", sitePrefix + "T002.WTUR.SetTurOp.ActSt.Str", sitePrefix + "T003.WTUR.SetTurOp.ActSt.Str", sitePrefix + "T004.WTUR.SetTurOp.ActSt.Str" });
            TmpThreshold = sitePrefix+"Articuno.TmpThreshold";
            CurtailmentEnable=sitePrefix+"Articuno.CurtailEna";
            TmpDelta = sitePrefix+"Articuno.TmpDelta";
            CtrTime=sitePrefix+"Articuno.EvalTm";

        }
        public void DefaultCase()
        {
            //Clear alll alarms
            foreach (string alarm in MetAlarms) { Articuno.OpcServer.writeOpcTag(opcServerName, alarm, 0); }
            //Set Met Tower temperature to 8C
            foreach (string tag in MetTmp1) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 8); };
            foreach (string tag in MetTmp2) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 8); };
            //Set Met Tower humidity to whatever. I really do nto care
            foreach (string tag in MetRH1) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 97); };
            foreach (string tag in MetRH2) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 97); };
            foreach (string tag in MetSwitch) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, false); };

            //For turbines. God, this sucks 
            foreach (string tag in NrsMode) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, true); };
            foreach (string tag in Participation) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, true); };
            foreach (string tag in OperatingState) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 100); };
            foreach (string tag in TurbineTemperature) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 10); };
            foreach (string tag in WindSpeed) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 5); };
            foreach (string tag in RotorSpeed) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 10.5); };
            foreach (string tag in PauseCmd) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 0); };
            foreach (string tag in StartCmd) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 0); };
        }

        public void IcingCase()
        {
            //Set Met Tower temperature to 8C
            foreach (string tag in MetTmp1) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, -7); };
            foreach (string tag in MetTmp2) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, -7); };
            //Set Met Tower humidity to whatever. I really do nto care
            foreach (string tag in MetRH1) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 97); };
            foreach (string tag in MetRH2) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 97); };

            //Turbines
            foreach (string tag in NrsMode) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, true); };
            foreach (string tag in Participation) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, true); };
            foreach (string tag in OperatingState) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 100); };
            foreach (string tag in TurbineTemperature) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 10); };
            foreach (string tag in WindSpeed) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 5); };
            foreach (string tag in RotorSpeed) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 10.5); };
            foreach (string tag in PauseCmd) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 0); };
            foreach (string tag in StartCmd) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 0); };

        }

        public void turbineLowRotorCondition()
        {
            //Turbines
            foreach (string tag in NrsMode) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, true); };
            foreach (string tag in Participation) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, true); };
            foreach (string tag in OperatingState) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 100); };
            foreach (string tag in TurbineTemperature) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 10); };
            foreach (string tag in WindSpeed) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 5); };
            foreach (string tag in RotorSpeed) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 10.0); };
        }

        public void setMetTowerIcingCondition()
        {
            //Set Met Tower temperature to 8C
            foreach (string tag in MetTmp1) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, -7); };
            foreach (string tag in MetTmp2) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, -7); };
            //Set Met Tower humidity to whatever. I really do nto care
            foreach (string tag in MetRH1) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 97); };
            foreach (string tag in MetRH2) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 97); };
        }

        public void setMetTowerNormalCondition()
        {
            //Set Met Tower temperature to 8C
            foreach (string tag in MetTmp1) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 8); };
            foreach (string tag in MetTmp2) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 8); };
            //Set Met Tower humidity to whatever. I really do nto care
            foreach (string tag in MetRH1) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 97); };
            foreach (string tag in MetRH2) { Articuno.OpcServer.writeOpcTag(opcServerName, tag, 97); };

        }

        public void updateCtrTime()
        {

        }

        public List<string> getMetAlarms()
        {
            return MetAlarms;
        }
    }
}
