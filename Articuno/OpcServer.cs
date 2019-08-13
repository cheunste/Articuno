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
    internal class OpcServer
    {
        //member variables
        EasyDAClient client = new EasyDAClient();
        static EasyDAClient opcServer = new EasyDAClient();
        private string serverName;

        //log
        private static readonly ILog log = LogManager.GetLogger(typeof(MetTower));

        //Constructor. Tages in a server name and sets the 
        public OpcServer(String serverName) { this.serverName = serverName; }

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
                //Log Exception here
                log.ErrorFormat("Reading tag: {0} failed. Does {0} exist on the server?", tag);
                return null;
            }
        }

        /// <summary>
        /// method to read a value from the OPC server. Returns an Object. Use this for production. Use readTagValue for testing
        /// </summary>
        /// <param name="tag">The OPC tag name (String format)</param>
        /// <param name="serverName">The OPC server name. String.</param>
        /// <returns></returns>
        public static Object readOpcTag(string serverName, string tag)
        {
            try
            {
                object value = opcServer.ReadItemValue("", serverName, tag);
                return value;
            }
            catch (Exception e)
            {
                //Log Exception here
                log.ErrorFormat("Reading tag: {0} failed. Does {0} exist on the server?", tag);
                return null;
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
                client.WriteItemValue(serverName, tag, value);
                return true;
            }
            catch (Exception e)
            {
                log.ErrorFormat("Write to tag: {0} failed. Does {0} exist on the server? Did the server die?", tag);
                return false;
            }
        }

        /// <summary>
        /// method to write a value to an OPC tag on the OPC server.  Use this method for production. Use writeTagValue for testing
        /// </summary>
        /// <param name="tag">The OPC Tag</param>
        /// <param name="value">The value you want to write</param>
        /// <param name="serverName">The OPC server Name</param>
        public static void writeOpcTag(string serverName, string tag, object value)
        {
            try { opcServer.WriteItemValue(serverName, tag, value); }
            catch (Exception e) { log.ErrorFormat("Write to tag: {0} failed. Does {0} exist on the server? Did the server die?", tag); }
        }

        public static bool readOpcTagQuality(string serverName, string tag)
        {
            DAVtq vtq = opcServer.ReadItem("", serverName, tag);
            return vtq.Quality.IsGood ? true : false;
        }

    }
}
