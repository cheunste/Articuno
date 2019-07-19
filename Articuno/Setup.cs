using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace Articuno
{

    /*
     * The Articuno protcol is a program that is designed to pause turbines if there is ice on the blade of the wind turbine.
     * 
     * The specifics are on the github page (or whatever tf I'm using as a VCS)
     * The setup class is...well...sets the application up before letting the other classes do their work.
     * 
     * The Setup class does the following
     * 1) Pulls the requried OPC tags and default settings from the database
     * 2) Stashes the rotor speed data into a hash table (or arrayList) wahtever
     * 3) Create a turbine factory instance and have it create turbines
     * 4) Create a met tower based on how many are in the database
     * 5) Set up the Timer class (CTR)
     * 6) Load turbines in the  'PausedByArticuno' table from the database.
     * 7) For each turbine already in the 'PausedByArticuno', check to see if any of the current turbines are now running 
     * or if they're throwing a different alarm.
     * 8) Execute the main Articuno program
     */

    class Setup
    {
        private Hashtable performanceHash;

        private static readonly ILog log = LogManager.GetLogger(typeof(Setup));

        static void Main(string[] args)
        {

            //Create instance to the Database
            DatabaseInterface di = DatabaseInterface.Instance;

            //Create instance to the Logging class

            //Check to see if the DB exists. Exit if it doesn't exist
            if (di.databaseNotFound()) {
                //TODO: Add Log "Error: SQLite DB not found"
                return;
            }


            //Pull required information from the database
            di.getTurbineList();
            String opcServer =di.getOpcServer();
            String metTower =di.getMetTower();


            //Create Met Tower References
            MetTower met1 = new MetTower(metTower, 0.00, 0.00,"SV.OPCDAServer.1");

            //Create Articuno instance
            //ArticunoMain artic = new ArticunoMain(opcServer, metTower, di.getTurbineList());

            //Execute Articuno
            //artic.start();
        }

        public Hashtable PerformanceHash { get; set; }
        /// <summary>
        /// Function used to 
        /// </summary>
        /// <param name="query"></param>
        public void queryDatabase(string query) { }
        /// <summary>
        /// Function used to perform an update to the database
        /// </summary>
        /// <param name="query">A String query used to update the DB </param>
        public void updateDatabase(string query) { }
        public void databaseToHash() { }
        public void createMetTower() { }
        public void startTimer() { }
        public Boolean databaseCheck()
        {
            return false;
        }

        private void Articuno(Turbine[] turbineArray, MetTower metTower) { }
    }
}
