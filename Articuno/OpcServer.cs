using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using OpcLabs.EasyOpc.DataAccess;

namespace Articuno
{
    /// <summary>
    /// The OpcServer class sets up an interface to the OpcServer (PcVue). This can be either a singleton or a static class...probably
    /// </summary>
    sealed internal class OpcServer
    {
        //member variables
        EasyDAClient client = new EasyDAClient();
        static EasyDAClient opcServer = new EasyDAClient();
        private string serverName;

        //log
        private static readonly ILog log = LogManager.GetLogger(typeof(OpcServer));

        //Constructor. Tages in a server name and sets the 
        public OpcServer(String serverName)
        {
            this.serverName = serverName;
            EasyDAClient.SharedParameters.TopicParameters.SlowdownWeight = 0.0f;
            EasyDAClient.SharedParameters.TopicParameters.SpeedupWeight = 0.0f;
        }

        /// <summary>
        /// The readTags function reads an OPC tag value given an OPC Tag. This always returns a String.
        /// Mainly because I don't trust what the server is returning. 
        /// </summary>
        /// <param name="tags"></param>
        /// <returns>A string value if successful. A Null if read failed</returns>
        public string readTagValue(string tag)
        {
            try
            {
                var value = client.ReadItemValue("", serverName, tag);
                return value.ToString();
            }
            catch (Exception e)
            {
                throw e;
                //Log Exception here
                log.ErrorFormat("Reading tag: {0} failed. Does {0} exist on the server?", tag);
                log.ErrorFormat("Error:\n{0}", e);
                return "";
            }
        }

        /// <summary>
        /// Sets a value given  an OPC tagname and a value
        /// </summary>
        /// <param name="tag">the name of hte OPC tag</param>
        /// <param name="value">the value to set to</param>
        /// <returns>true if write was successful. False otherwise</returns>
        //This method is used to set the value of an OPC tag to some value. mainly used for the sensors in the met tower so it doesn't go out of range 
        public bool writeTagValue(string tag, Object value)
        {
            try
            {
                //Only write to OPC Server if the UCC is active
                if (isActiveUCC())
                {
                    client.WriteItemValue(serverName, tag, value);
                    return true;
                }
                else
                    return false;
            }
            catch (Exception e)
            {
                log.ErrorFormat("Write to tag: {0} failed. Does {0} exist on the server? Did the server die?", tag);
                log.ErrorFormat("Error:\n{0}", e);
                return false;
            }
        }

        private static Object readOpcTag(string serverName, string tag)
        {
            try
            {
                object quality = opcServer.GetPropertyValue("", serverName, tag, DAPropertyIds.Quality);
                object value = opcServer.GetPropertyValue("", serverName, tag, DAPropertyIds.Value);

                //log.DebugFormat("{0}: {1}. Qual: {2}",tag,value,quality);
                return value;
            }
            catch (Exception e)
            {
                log.DebugFormat("Error relating to tag: {0}\nDetails: {1}", tag, e);
                return null;
            }
        }

        public static Object readAnalogTag(string serverName, string tag)
        {
            Object obj = readOpcTag(serverName, tag);
            if (obj == null)
                log.ErrorFormat("Issue reading tag: {0}. Will be sending back an {1} to Artiucno  ", tag, 0);
            return (obj ?? 0);
        }

        public static Object readStringTag(string serverName, string tag)
        {
            Object obj = readOpcTag(serverName, tag);
            if (obj == null)
                log.ErrorFormat("Issue reading tag: {0}. Will be sending back an {1} to Artiucno  ", tag, " empty string ");

            return (obj ?? "");
        }

        public static Object readBooleanTag(string serverName, string tag)
        {
            Object obj = readOpcTag(serverName, tag);
            if (obj == null)
                log.ErrorFormat("Issue reading tag: {0}. Will be sending back an {1} to Artiucno  ", tag, false);
            return (obj ?? false);
        }


        /// <summary>
        /// method to write a value to an OPC tag on the OPC server.  Use this method for production. Use writeTagValue for testing
        /// </summary>
        /// <param name="tag">The OPC Tag</param>
        /// <param name="value">The value you want to write</param>
        /// <param name="serverName">The OPC server Name</param>
        public static void writeOpcTag(string serverName, string tag, object value)
        {
            try
            {
                //Only write to OPC Server if the UCC is active
                if (isActiveUCC())
                {
                    opcServer.WriteItemValue(serverName, tag, value);
                }

            }
            catch (Exception e)
            {
                log.ErrorFormat("Write to tag: {0} failed. Does {0} exist on the server? Did the server die?", tag);
                log.ErrorFormat("Error:\n{0}", e);
            }
        }

        public static bool readOpcTagQuality(string serverName, string tag)
        {
            DAVtq vtq = opcServer.ReadItem("", serverName, tag);
            return vtq.Quality.IsGood ? true : false;
        }

        public static bool isActiveUCC(string serverName, string tag)
        {
            return Convert.ToBoolean(readOpcTag(serverName, tag));
        }

        public static bool isActiveUCC()
        {
            return Convert.ToBoolean(readOpcTag(DatabaseInterface.Instance.getOpcServerName(), DatabaseInterface.Instance.getActiveUccOpcTag()));
        }

    }
}
