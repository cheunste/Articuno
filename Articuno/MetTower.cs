﻿using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    /// <summary>
    /// The Met Tower class is responsible for fetching humidity and temperature from the field's met tower and perform calculations when valid
    /// Fetches: humidty and temperature values from the field
    /// Calculates: Dew Point, Delta Temp threshold
    /// Errors: Thrwos errors if values are bad 
    /// </summary>
    class MetTower
    {
        //Constants. Can't Fully Capitalize these ones. Too hard to read. Note the staritng capital letter though
        //Temperature table columns
        private static string PrimTempValueTag         = "PrimTempValueTag";
        private static string SecTempValueTag          = "SecTempValueTag";
        private static string TempBadQualityTag        ="TempBadQualityTag";
        private static string TempOutOfRangeTag        ="TempOutOfRangeTag" ;

        //Humidity table columns
        private static string PrimHumidityValueTag         = "PrimHumidityValueTag";
        private static string SecHumidityValueTag          = "SecHumidityValueTag";
        private static string HumidityBadQualityTag        ="HumidityBadQualityTag";
        private static string HumidityOutOfRangeTag        ="HumidityOutOfRangeTag" ;

        //Other table column 
        private static string NoDataAlarmTag        ="NoDataAlarmTag" ;
        private static string IceIndicationTag        ="IceIndicationTag" ;

        //Query constants
        String TEMPERATURE_QUERY = "SELECT " + PrimTempValueTag + "," + SecTempValueTag + "," + TempBadQualityTag + ","+TempOutOfRangeTag+" FROM MetTower";
        String RH_QUERY = "SELECT " + PrimHumidityValueTag + "," + SecHumidityValueTag + "," + HumidityBadQualityTag + ","+HumidityOutOfRangeTag+" FROM MetTower";
        String OTHER_PARAM_QUERY = "SELECT "+NoDataAlarmTag+", "+IceIndicationTag+" FROM MetTower";

        //Member variables;
        //Member doubles 
        private double ambientTemperature;
        private double relativeHumidity;
        private double dewPointTemperature;
        private double ambTempThreshold;
        private double deltaTempThreshold;

        //Member bool
        private bool metTowerFailure;

        //Member variable OPC Tags (strings)
        //For temperature
        private string primTempTag;
        private string secTempTag;
        private string tempBadQualityTag;
        private string tempOutOfRangeTag;

        //For humidity
        private string primRHTag;
        private string secRHTag;
        private string rhBadQualityTag;
        private string rhOutOfRangeTag;

        //For other tags relating to the met tower 
        private string nodataAlarmTag;
        private string iceIndicationTag;

        /// <summary>
        /// Constructor for the Met Tower Class. Takes in a Met Id. Using  the met Id, the constructor will then query
        /// the DB for all the relevant values
        /// </summary>
        /// <param name="MetId"></param>
        public MetTower(string MetId, double ambTempThreshold, double deltaTempThreshold)
        {
            //Open a connection to the DB
            DatabaseInterface dbi = new DatabaseInterface();
            dbi.openConnection();
            //Get everything relating to the Temperaturea
            SQLiteDataReader reader= dbi.readCommand(TEMPERATURE_QUERY +" WHERE MetId="+MetId);
            reader.Read();
            this.primTempTag = reader[PrimTempValueTag].ToString();
            this.secTempTag = reader[SecTempValueTag].ToString();
            this.tempBadQualityTag = reader[TempBadQualityTag].ToString();
            this.tempOutOfRangeTag = reader[TempOutOfRangeTag].ToString();

            //Get everything relating to the Humidity
            reader =dbi.readCommand(RH_QUERY +" WHERE MetId="+MetId);
            reader.Read();
            this.primRHTag = reader[PrimHumidityValueTag].ToString();
            this.secRHTag = reader[SecHumidityValueTag].ToString();
            this.rhBadQualityTag = reader[HumidityBadQualityTag].ToString();
            this.rhOutOfRangeTag = reader[HumidityOutOfRangeTag].ToString();

            //Get the other tags relating to the met tower
            reader =dbi.readCommand(OTHER_PARAM_QUERY +" WHERE MetId="+MetId);
            this.nodataAlarmTag =reader[NoDataAlarmTag].ToString();
            this.iceIndicationTag =reader[IceIndicationTag].ToString();
            dbi.closeConnection();

            //set the member thresholds
            this.ambTempThreshold = ambTempThreshold;
            this.deltaTempThreshold = deltaTempThreshold;

        }

        /// <summary>
        /// Get Relative Humidity from OPC Server
        /// </summary>
        /// <param name="opcServer">the OpcServer you'll be looking the value from</param>
        /// <returns></returns>
        public string getRelativeHumidity(OpcServer opcServer)
        {
            return OpcServer.readTags(primRHTag).ToString();
        }
        /// <summary>
        /// Set the Relativie Humidity Tag
        /// </summary>
        public void setRelativeHumidityTag(string tag)
        {
            this.primRHTag = tag;
        }


        /// <summary>
        /// Get the Primary Temperature value from the met tower
        /// </summary>
        /// <param name="opcServer">the OpcServer you'll be looking the value from</param>
        /// <returns></returns>
        public string getPrimTemperature(OpcServer opcServer)
        {
            return OpcServer.readTags(primTempTag).ToString();
        }
        /// <summary>
        /// Set the primary temperature tag.
        /// </summary>
        public void setPrimTemperature(string tag)
        {
            this.primTempTag = tag;
        }

        /// <summary>
        /// Get the SEcondary Temperature value from the met tower
        /// </summary>
        /// <param name="opcServer">the OpcServer you'll be looking the value from</param>
        /// <returns></returns>
        public string getSecTemperature(OpcServer opcServer)
        {
            return OpcServer.readTags(secTempTag).ToString();
        }
        /// <summary>
        /// Set the secondary temperature tag.
        /// </summary>
        public void setSecTemperature(string tag)
        {
            this.secTempTag = tag;

        }

        /// <summary>
        /// Calculates the Dew Point Temperature given the ambient temperature and the the relative humidity
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="rh">The relative humidity in double format</param>
        /// <returns></returns>
        public double calculateDewPoint(double rh, double ambTemp)
        {
            return Math.Pow(rh, 1.0 / 8.0) * (112 + (0.9 * ambTemp)) + (0.1 * ambTemp) - 112;
        }

        /// <summary>
        /// Calculates the Delta Temperature given the ambient Temperature and a dew point temperature (from calculateDewPoitn function)
        /// </summary>
        /// <param name="ambTemp">The ambient temperature value (Celcius) from the met tower in double format</param>
        /// <param name="dewPointTemp">The dew point temperature from calculateDewPoint</param>
        /// <returns></returns>
        public double calculateDelta(double ambTemp,double dewPointTemp)
        {
            return Math.Abs(ambTemp - dewPointTemp);
        }

        /// <summary>
        /// Check the quality of the met tower
        /// </summary>
        /// <param name="ambTemp"></param>
        /// <param name="rh"></param>
        /// <returns></returns>
        public bool checkMetTower(double ambTemp, double rh)
        {

            return false;
        }
        /// <summary>
        /// Checks the quality of the relative humidity of the current met tower
        /// </summary>
        /// <returns></returns>
        public bool metRHCheck()
        {
            return false;
        }
    }
}
