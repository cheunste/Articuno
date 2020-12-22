using System;
using System.Collections.Generic;
using System.Data.Entity.Core.Mapping;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    /// <summary>
    /// This is a class representing a generic met tower sensor. This is a generic class and must be implemented
    /// </summary>
    public class MetTowerSensor
    {
        //Member variables
        private int frozenCount;
        private double lastReadValue;

        private double maxValue;
        private double minValue;
        public int staleDataCount;

        //OPC Tag values
        public string sensorTag { get; set; }
        public string sensorOutofRangeTag { get; set; }
        public string sensorBadQualtiyTag { get; set; }
        private string opcServerName;

        public MetTowerSensor(string serverName, double minValue, double maxValue)
        {
            opcServerName = serverName;
            //sensorTag = valueTag;
            //sensorOutofRangeTag = outOfRangeTag;
            //sensorBadQualtiyTag = badQualityTag;
            this.maxValue = maxValue;
            this.minValue = minValue;

            frozenCount = 0;
            lastReadValue = 0.00;
        }


        /// <summary>
        /// Method to read the value from the OPC server. It will also check for stale data and out of range values as well.
        /// If it is a humidity sensor, then it will divide by 100
        /// </summary>
        /// <param name="isHumiditySensor">Optional Parameter</param>
        /// <returns></returns>
        public double readValue(bool isHumiditySensor = false)
        {
            double value = Convert.ToDouble(OpcServer.readAnalogTag(opcServerName, sensorTag));
            if (isHumiditySensor)
                value = value / 100.0;
            CheckStaleValue(value);
            BadQualityCheck();
            return outOfRangeCheck(value);
        }

        /// <summary>
        /// Writes a value to a tag. Used only for unit testing
        /// </summary>
        /// <param name="value"></param>
        public void writeValue(double value)=>
            OpcServer.writeOpcTag(opcServerName, sensorTag, value);

        /// <summary>
        /// Method to check if the sensor is stale. The MetTower class must handle the rest
        /// </summary>
        /// <returns></returns>
        public bool BadQualityCheck()
        {
            //If there are 50 or so samples (or whatever) that are equally the same, that implies the temperature from the sensor is flatlined. At this point, return a bad quality alert.
            if (frozenCount >= this.staleDataCount)
            {
                ArticunoLogger.DataLogger.Info("Flatline detected for {0}", sensorTag);
                ArticunoLogger.GeneralLogger.Info("Flatline detected for {0}", sensorTag);
                SetAlarmStatus(sensorBadQualtiyTag, true);
                return true;
            }
            else
            {
                SetAlarmStatus(sensorBadQualtiyTag, false);
                return false;
            }
        }



        public double outOfRangeCheck(double sensorValue)
        {
            if (sensorValue >= maxValue)
            {
                ArticunoLogger.DataLogger.Info("Sensor value overrange detected for {0}. Value: {1}", sensorTag,sensorValue);
                ArticunoLogger.GeneralLogger.Info("Sensor value overrange detected for {0}. Value: {1}", sensorTag,sensorValue);
                SetAlarmStatus(sensorOutofRangeTag, true);
                return maxValue;
            }
            else if (sensorValue <= minValue)
            {
                ArticunoLogger.DataLogger.Info("Sensor value underrange detected for {0}. Value: {1}", sensorTag,sensorValue);
                ArticunoLogger.GeneralLogger.Info("Sensor value underrange detected for {0}. Value: {1}", sensorTag,sensorValue);
                SetAlarmStatus(sensorOutofRangeTag, true);
                return minValue;
            }
            else
            {
                SetAlarmStatus(sensorOutofRangeTag, false);
                return sensorValue;
            }
        }

        /// <summary>
        /// Checks to see if the quality is bad. Used by the Met Tower Class
        /// </summary>
        /// <returns></returns>
        public bool isSensorBadQuality() => Convert.ToBoolean(OpcServer.readBooleanTag(opcServerName, sensorBadQualtiyTag));
        /// <summary>
        /// Checks to see if the sensor has an out of Range error. Used by the MetTower Class
        /// </summary>
        /// <returns></returns>
        public bool isSensorOutofRange() => Convert.ToBoolean(OpcServer.readBooleanTag(opcServerName, sensorOutofRangeTag));

        /// <summary>
        /// An alarm class that is used to write the alarm status to the OPC server
        /// </summary>
        /// <param name="opcTag"></param>
        /// <param name="alarmValue"></param>
        private void SetAlarmStatus(string opcTag, bool alarmValue)=> OpcServer.writeOpcTag(opcServerName, opcTag, alarmValue);
        /// <summary>
        /// Method to sample flatline. It checks the flatline of a value. If the value is stale, then it increments a frozenCount indicator
        /// </summary>
        /// <param name="value"></param>
        private void CheckStaleValue(double value)
        {
            double tolerance = 0.00001;

            double currentTemp = value;
            //If the current temperature is equal to the last stored sample, then increment the frozenTemperatureCnt
            // Note that temperatures are doubles
            if (Math.Abs(lastReadValue - currentTemp) <= tolerance) { frozenCount++; }

            //Reset the frozenTemperatureCnt if it isn't equal
            else { frozenCount = 0; }

            //Set the sample Temperature to the current temperature
            lastReadValue = currentTemp;
        }

    }
}
