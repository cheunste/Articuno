using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Articuno
{
    /// <summary>
    /// The Timer class is responsible for handling all the timing criteria in the Articuno project, such as...
    /// - CTR
    /// - Sensor sampling intervals
    /// - Setting flags signaling rest of program when to execute certain tasks
    /// - Clearing the noted flags from above
    /// 
    /// </summary>
    class Timer
    {
        private static bool minuteFlag;
        private static bool ctrlFlag;

        //log
        private static readonly ILog log = LogManager.GetLogger(typeof(Timer));

        public Timer()
        {

        }

        /// <summary>
        /// function to check the time to see if certain time periods have passed
        /// </summary>
        /// <returns></returns>
        public DateTime checkTime()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the timer flag
        /// </summary>
        /// <returns></returns>
        public bool getTimerFlag()
        {
            throw new NotImplementedException();
        }
        public void  setCtrTime()
        {

            throw new NotImplementedException();
        }

        public void setCtfFlag()
        {

            throw new NotImplementedException();
        }

        public void clearCtfFlag()
        {

            throw new NotImplementedException();
        }

        public void ctrChange()
        {
            throw new NotImplementedException();
        }

    }
}
