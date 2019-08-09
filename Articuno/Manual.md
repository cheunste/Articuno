# Articuno

## Introduction

Articuno is a name of the Avangrid ice/sound abatement curtailment tool for CORE. This tool is designed to pause a turbine when the software determines the condition at the site is cold enough for ice to form on the turbine blades.

## Installation

Loren Ipusum

## How to Use

### Basic Usage

Articuno is a backend program that will automatically pause a turbine when all the following conditions are met:
- the temperature of the met tower is below the threshold value set by the NCC
- the humidity is high enough for snowy and icy conditions
- the turbine is set for pariticipation 
- the rotor speed of a turbine is under performing given an average wind speed
- the turbine is in running state the entire Control Time Resolution Period. More on this in ###CTR 

### Display
### Turbine Participation

To let a turbine participate in Articuno, do the following:


### CTR

CTR is short for Control Time Resolution and it is the countdown time needed for each turbine to determine whether its in an icy condition. 
CTR is set in the following screen

[INSERT CTR SCREEN 1]

#### Notes

1) When the CTR is entered in the faceplate, all turbines on site will immediately update its CTR time based on whats entered in the faceplate
2) CTR time is NOT synced between the turbines, so each turbine will keep track of its own CTR value
3) CTR time is immediately reseted whenever anyone attempts to run a turbine. 


### Database

### Alarms

## Known Issues

- Program doesn't handle an abscene of an OPC Server. Meaning, if PcVue Crashes, Articuno will most likely die and needs to be restarted.
- 
## Reference
Loren Ipusum