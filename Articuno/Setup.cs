using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
     * 6) Execute the main Articuno program
     */

    class Setup
    {

        static void Main(string[] args)
        {

            //Create instance to the Database
            DatabaseInterface di = new DatabaseInterface();

            //Create instance to the Logging class
            Logging logger = new Logging();

            //Pull required information from the database
            di.getTurbineList();
            di.getOPCServer();
            di.getmetTower();

            //Create Articuno instance
            Articuno artic = new Articuno();

            //Close the database

            //Execute Articuno
        }

        public void queryDatabase() { }
        public  void databaseToHash() { }
        public void createMetTower() { }
        public void startTimer() { }

        private void Articuno(Turbine[] turbineArray, MetTower metTower) { }
    }
}
