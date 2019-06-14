using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    interface TurbineInterface
    {
        //Getter and setters
        string turbinePrefix { get; set; }
        int turbineCtrMinute { get; set; }
        int operatinState { get; set; } 
        double rotorSpeed { get; set; }
        double windspeed{ get; set; }
        bool deRate{ get; set; }
        bool nrsMode{ get; set; }
        double scaleFactor{ get; set; }
        MetTower primMetTowerReference{ get; set; }
        MetTower secMetTowerReference{ get; set; }
        bool isMetBackup{ get; set; }
        Queue<double> tempereatureQueue { get; set; }
        string Derp { get; set; }

        //Function calls. The following get and set the OPC tags for a variety of turbine parameters and include a read value to read the content of an OPC Tag
        //Some have write, but not too many

        //Wind Speed
        void getWindSpeedTag();
        void setWindSpeedTag();
        double readWindSpeedTagValue();

        //Rotor Speed
        void getRotorSpeedTag();
        void setRotorSpeedTag();
        double readRotorSpeedTagValue();

        //Operating Status
        void getOperatingStateTag();
        void setOperatingStateTag();
        int readOperatingStateTagValue();

        //NRS
        void getNrsStateTag();
        void setNrsStateTag();
        int readNrsStateTagValue();

        //Scaling factor
        void getScalingFactorTag();
        void setScalingFactorTag();
        void readScalingFactorTagValue();

        //CTR
        void getCtrTag();
        void setCtrTag();
        void readCtrTagValue();

        //Derate
        void getDerateTag();
        void setDerateTag();
        void readDerateTagValue();
        bool isDerated();

        //These are fucntions to check to see if the conditions for the various algorithms have been met
        bool setAmbientAlg();
        bool setOperatingStateAlg();
        bool setTurbinePerfAlg();
        bool setDerateAlg();
        bool setNrsAlg();

        //Functions to set the primary and secondary met tower 
        void setPrimMetReference();
        void setSecMetReference();

        //Determins if the met tower is a designated backup tower for a met tower
        void metBackup();

        //Function to add met tower temperature to a queue. Only used if met tower is a designated met backup tower
        //Note that there is no humidity backup (requirement didn't sepcify)
        void addToTemperatureQueue();
    }
}
