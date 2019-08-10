# Articuno

## Introduction

Articuno is a name of the Avangrid ice/sound abatement curtailment tool for CORE. This tool is designed to pause a turbine when the software determines the condition at the site is cold enough for ice to form on the turbine blades.

## Installation

### 1) Verify the Articuno Tags are Added in PcVUe
### 2) Update the Articuno.db database
### 3) Move to the UCC
### 4) Run the program

## How to Use

## Dispatcher Usage

Articuno is a backend program that will automatically pause a turbine when all the following conditions are met:
- the temperature of the met tower is below the threshold value set by the NCC
- the humidity is high enough for snowy and icy conditions
- the turbine is set for pariticipation 
- the rotor speed of a turbine is under performing given an average wind speed
- the turbine is in running state the entire Control Time Resolution Period. More on this in the ###CTR section.

### Articuno Control Displays (FrontVue)

The Articuno Control Display is located in the Enviornment section of FrontVue [Screenshot ACD 1]

### Turbine Participation

To let a turbine participate in Articuno, do the following:


### CTR

CTR is short for Control Time Resolution and it is the countdown time needed for each turbine to determine whether it is in an icy condition. 
CTR is determined by the NCC and is set in the faceplate shown in the screenshot:

[INSERT CTR SCREEN 1]

Once the CTR is set, the value is immediately carried out to all the turbines in the site and is updated on the secondary FrontVue Articuno screen

[INSERT CTR SCREEN 2]

The CTR value for the turbine is internal for each turbine and all CTR values from each turbine is independent from each other.

The Turbine's internal CTR is reset to the faceplate CTR value when a start command is sent. However, there is a small 30 second delay in order to give the turbine some room to start

#### Notes

1) When the CTR is entered in the faceplate, all turbines on site will immediately update its CTR time based on whats entered in the faceplate
2) CTR time is NOT synced between the turbines, so each turbine will keep track of its own CTR value
3) CTR time is immediately reseted whenever anyone attempts to run a turbine. 


## Database

The articuno database (articuno.db) is a SQLite Database that is used to store OPC Tags names that will be used in Articuno as well as the rotor speed filter tables provided by "Technical Division". 

The articuno database must be updated for each new site installation.


### Tables

The Database is comprised of eight tables, each of which is shown below with their column headers along with a description

**MetTowerInputTags**
|Column| Description|
|--|--|
|MetId| Met Tower Prefix |
|PrimTempValueTag|  Tag of primary met tower temperature sensor |  
|SecTempValueTag | Tag of secondary met tower temperature sensor |
|PrimHumidityValueTag| Tag of Met Tower Humidity Sensor | 
|Switch| Tag of Switch Command |  

**MetTowerOutputTags**
|Column | Description |
|--|--|
|MetId| Met Tower Prefix |
|TempPrimBadQualityTag| Temperature sensor 1 bad quality alarm | 
|TempPrimOutOfRangeTag| Primary temperature sensor Out of Range Alarm|
|TempPrimHiDispersionTag| Primary Temperature SensorDispersion |
|TempSecOutOfRangeTag| Secondary Temperature sensor  bad quality alarm |
|TempSecBadQualityTag| Secondary Temperature sensor  out of range alarm |
|TempSecHiDispersionTag|Secondary Temperature SensorDispersion |
|HumidityOutOfRangeTag| Humidity sensor out of range alarm |
|HumidityBadQualityTag|Humidity sensor bad quality alarm |
|IceIndicationTag| Icing Possible alarm for met tower |
|NoDataAlarmTag| No data indication alarm |

**RotorSpeedLookup Table**

Note that this table is developed by Nick Johansen from Technical Division 

|Column | Description |
|--|--|
|WindSpeed| The Wind Speed reference |
|RotorSpeed| The rotor speed reference given wind speed. |
|StdDev| The standard deviation |
|RotorSpeedNrs| The rotor speed reference given wind speed for NRS mode|
|StdDevNrs| The standard deviation for NRS mode |

**SystemInputTags Table**

The System Input and Output Tag tables are a tad different. These tables contain tags 
that are inconsistent with the rest of hte program. The input tag table contains the following items in "Description":
AmbTempThreshold
ArticunoEnable
CTRPeriod
DeltaTmpThreshold
DewTempThreshold
SitePrefix
OpcServerName


|Column | Description |
|--|--|
|Description| Description of the OpcTag |
|OpcTag| The OPC Tag name|

**SystemOutputTags Table**

The System Input and Output Tag tables are a tad different. These tables contain tags 
that are inconsistent with the rest of hte program. The output tag table contains the following items in "Description":
Heartbeat
IcePossible
NumTurbIced

|Column | Description |
|--|--|
|Description| Description of the OpcTag |
|OpcTag| The OPC Tag name|

**TurbineInputTags Table**

|Column | Description |
|--|--|
|TurbineId| The Turbine Id (T001, T002, etc.) | 
|NrsMode|  **Can be null or blank if site doesn't use NRS.** The Noise Level tag.|
|Participation| The participation tag|
|OperatingState| The tag indicating turbine status/operation (RUN,PAUSE, DRAFT, etc) |
|RotorSpeed| The instantaneous rotor speed from CORE|
|ScalingFactor| The Scaling Factor tag |
|Temperature| The instantaneous enviornmental temperature from the Turbine's sensor |
|TurbineCurrentSp| Not used actually |
|WindSpeed|The instantaneous wind speed from the Turbine's sensor |
|MetReference| **Not an OPC Tag**. This is the met tower the turbine will be referencing to see if the site is under ice condition |
|RedundancyForMet| **Not an OPC Tag. Can be null**. Setting the met prefix here (Met1, Met2) will allow this turbine to be used as the temperature backup if the met tower fails.| 
|Pause| The Pause/Loadshutdown command tag for the turbine | 
|Start| The start command tag for the turbine|

**TurbineOutputTags Table**
|Column | Description |
|--|--|
|TurbineId| The Turbine Id (T001, T002, etc.)|
|Alarm| This is the alarm indicating ice on blades. |
|OperatingState| The tag indicating turbine status/operation (RUN,PAUSE, DRAFT, etc). Yes, this is also the same tag noted in TurbineInputTags |


### Updating the Database

The easiest way to update the database is to 

1) Open the articuno.db  with "DB Browser for SQLite". Download this if needed
2) Copy the content of the table **EXCLUDING THE FIRST COLUMN**
3) Paste it into a text editor (vim, notepad++, etc)
4) Replace the site prefix with your desired site (ie replace 'SCRAB', with 'HOOS')
5) Copy back the content from the text editor to DB Browser for SQLite
Repeat the above stesp for all the tables in the articuno.db except for the RotorSpeedFilter table.


#### Notes

The choice was made for SQLite for the following reasons:

- The SQLite is much more light weight as a program compared to MS Access 
- SQLite was already installed on the devloper's machine
- The SQLite package is easily obtainable via NuGet

### Alarms

## Known Issues

- Program doesn't handle an abscene of an OPC Server. Meaning, if PcVue Crashes, Articuno will most likely die and needs to be restarted.
- The program doesn't handle bad OPC tag quality. This means unexpected things can happen if the humidity sensors on both the humidity met towers experience intermittent comms. To handle this type of issue, switch the met tower over to the secondary met

## Reference
Loren Ipusum