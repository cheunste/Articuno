# Articuno

## Introduction

Articuno is a name of the Avangrid ice/sound abatement curtailment tool for CORE. This tool is designed to automatically pause a turbine once ice have started to build up on the turbine's blades. 

## System Overview

Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam et volutpat magna. In efficitur orci nec ligula blandit, nec venenatis est feugiat. Sed ultrices, est sed blandit suscipit, mi leo fringilla eros, at elementum purus nibh commodo velit. Donec sagittis, metus at mattis facilisis, nunc tortor interdum sem, id malesuada orci turpis id justo. Aliquam tempus libero vel felis dictum condimentum. Mauris maximus est libero, id dictum eros vestibulum sit amet. Proin pellentesque quam ultricies purus sodales hendrerit. Nulla maximus vulputate tellus, in posuere dolor ullamcorper sed. 


## Installation

Check out the project via the git repository (or download it as a zip if git isn't installed)

```git
git clone https://github.com/cheunste/Articuno.git
```

### Updating the Database

 1) Modify the articuno db in Articuno/articuno.db with the full names of the OPC tags.
    i) To do this, you must first:
        - Get the Wind farm site prefix (ie SCRAB, HOOSA, etc.)
        - Have access to SQLite tools, or the GUI application "DB Browser for SQLite"
        
 2) Open the articuno.db either by sqlite cmd tools or DB Browser for SQLite (GUI). More information on the columns of the table in the [tables section](###Tables)

    i) Replace the prefix of all the tags of the MetTowerInputTags table (the default is either XXXXX or 'SCRAB') with the new site prefix. There will be 10 changes total.

    ii) Replace the prefix of all the tags of the MetTowerOuputTags table (the default is either XXXXX or 'SCRAB') with the new site prefix. There will be 20 changes total.

    iii) Replace the prefix of all the tags of the SystemInputTags table (the default is either XXXXX or 'SCRAB') with the new site prefix. There will be 6 changes total.
    
    iv) Replace the prefix of all the tags of the SystemOutputTags table (the default is either XXXXX or 'SCRAB') with the new site prefix. There will be 3 changes total.

    v) Replace the prefix of all the tags of the TurbienInputTags table (the default is either XXXXX or 'SCRAB') with the new site prefix. This will depend on the number of turbines at the site.
       - In many cases, additional or fewer turbines will be needed. The number of turbines must match the number of turbines at the site
       - There will be 12 changes for each turbine at the site
       - **Important**: The NrsMode tag can be blank if the site does **NOT** have a noise reduction system mode. This can be left as null or an empty string
       - The 'MetReference' column refers to a met tower that the turbine will be referencing. This list is given by Nick Johanesen from technical Division
       - The 'RedundancyForMet' column allows this turbine to act as a temperature backup in case both temperature sensors for the met fails. Not all turbines need this column filled out and can be left as NULL or an empty string

    vi) Replace the prefix of all the tags of the TurbineOutputTags table (the default is either XXXXX or 'SCRAB') with the new site prefix. This will depend on the number of turbines at the site. there will be 2 changes for each turbine at the site
    
 3) Move the modified articuno.db to the release directory in (Articuno/bin/release)
 4) Verify the Articuno Tags added to the database are in both PcVue and FrontVue

 ### System Installaion
 
 To deploy Articuno on the UCC please do the following:

 1) Install .NET Framework 4.7 (minimum) on the target UCC
 2) Copy the release version of the project (the entire 'release' directory in Articuno/bin/release) onto the UCC's directory in "C:\Program Files\XXX\XXX"
 3) Launch the software

 ### Starting a service
 Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nullam et volutpat magna. In efficitur orci nec ligula blandit, nec venenatis est feugiat. Sed ultrices, est sed blandit suscipit, mi leo fringilla eros, at elementum purus nibh commodo velit. Donec sagittis, metus at mattis facilisis, nunc tortor interdum sem, id malesuada orci turpis id justo. Aliquam tempus libero vel felis dictum condimentum. Mauris maximus est libero, id dictum eros vestibulum sit amet. Proin pellentesque quam ultricies purus sodales hendrerit. Nulla maximus vulputate tellus, in posuere dolor ullamcorper sed. 

## How to Use

## Dispatcher 

Articuno is a backend program that will automatically pause a turbine when all the following conditions are met:
- the temperature of the met tower is below the threshold value set by the NCC
- the humidity is high enough for snowy and icy conditions
- the turbine is set for pariticipation 
- the rotor speed of a turbine is under performing given an average wind speed
- the turbine is in running state the entire Control Time Resolution Period. More on this in the ###CTR section.

For the NCC dispatchers, this is a completely hands off process as the turbine is designed to be stop automatically and the turbine is not designed to start unless the NCC send a start command to the turbine (or send a site wide start all command).

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

**MetTowerInputTags**: Table containing Met Tower sensor and met tower command OPC tags.
|Column| Description|
|--|--|
|MetId| Met Tower Prefix |
|PrimTempValueTag|  Tag of primary met tower temperature sensor |  
|SecTempValueTag | Tag of secondary met tower temperature sensor |
|PrimHumidityValueTag| Tag of Met Tower Humidity Sensor | 
|Switch| Tag of Switch Command |  

**MetTowerOutputTags**: Table containing Met Tower alarms OPC Tags.
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

Table containing the rotor speed filters.Note that this table is developed by Nick Johansen from Technical Division 

|Column | Description |
|--|--|
|WindSpeed| The Wind Speed reference |
|RotorSpeed| The rotor speed reference given wind speed. |
|StdDev| The standard deviation |
|RotorSpeedNrs| The rotor speed reference given wind speed for NRS mode|
|StdDevNrs| The standard deviation for NRS mode |

**SystemInputTags Table**

This table contains the command OPC tags that allows NCC to interact with Articuno. The System Input and Output Tag tables are a tad different. These tables contain tags that are inconsistent with the rest of the program. 

|Column | Description |
|--|--|
|Description| Description of the OpcTag |
|OpcTag| The OPC Tag name|


The SystemInputTags table contains the following items in "Description":

|Description | OpcTag| Notes | 
|--|--| --|
| AmbTempThreshold | SCRAB.Articuno.TmpThreshold | The temperature threshold set by NCC |
| ArticunoEnable | SCRAB.Articuno.CurtailEna | Enable/disable Articuno |  
| CTRPeriod | SCRAB.Articuno.EvalTm |  The CTR (Control Time Resolution) of Articuno set by NCC |  
| DeltaTmpThreshold | SCRAB.Articuno.TmpDelta |  The temperature threshold data set by NCC |
| DewTempThreshold | SCRAB.ArticunSCRABo.TmpDew |  The Dew Point threshold set by NCC |
| SitePrefix | SCRAB | Site prefix. Set by GCS | 
| OpcServerName | SV.OPCDAServer.1 |  The name of the OPC Server. **Should never be changed** | 


**SystemOutputTags Table**

This table contains OPC tags that inform NCC of the current status of Articuno. The System Input and Output Tag tables are a tad different. These tables contain tags 
that are inconsistent with the rest of hte program. The output tag table contains the following items in "Description":

|Column | Description |
|--|--|
|Description| Description of the OpcTag |
|OpcTag| The OPC Tag name|

|Description | OpcTag| Notes | 
|--|--| --|
|Heartbeat | SCRAB.Articuno.Heartbeat | Articuno Heartbeat. Used to inform NCC system is alive or not| 
|IcePossible | SCRAB.Articuno.IcePossible | Alarm to inform NCC that ice is possible on site|
|NumTurbIced | SCRAB.Articuno.NumTurbPause | The number of turbines paused by Articuno|

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