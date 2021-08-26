# Human Navigation Project

This is a Unity project for the competition of Human Navigation task of the RoboCup@Home Simulation.

Please prepare a common unitypackage and a dll file for text-to-speech (TTS) in advance, and import them to this project.  

For details of using the common unitypackage, please see an instruction in the following repository:  
https://github.com/RoboCupatHomeSim/common-unity.git

For details of creating the dll for TTS, please see the following repository:  
https://github.com/RoboCupatHomeSim/console-simple-tts

## Prerequisites

Same as below for OS and Unity version.
https://github.com/RoboCupatHomeSim/documents/blob/master/SoftwareManual/Environment.md#windows-pc

Please install [Steam](https://store.steampowered.com/about/) and [SteamVR](https://store.steampowered.com/app/250820/SteamVR/) on your PC. Download the installer from the official website and install it.  
And please install [Oculus Software](https://www.oculus.com/setup/) to use Oculus Headsets. [Oculus Link](https://support.oculus.com/articles/headsets-and-accessories/oculus-link/index-oculus-link) is also required.


## How to Build

### Import unitypackages

1. Download SteamVR Unity Plugin v2.7.3 from the following link.  
https://github.com/ValveSoftware/steamvr_unity_plugin/releases/download/2.7.3/steamvr_2_7_3.unitypackage
1. Open this project with Unity.
1. Click [Continue] in the [Unity Package Manager Error] window.
1. Click [Ignore] in the [Enter Safe Mode?] window.
1. Click [Assets]-[Import Package]-[Custom Package...].
1. Select a common unitypackage (e.g. robocup-common.unitypackage) and open the file.
1. Click [Import] button.
1. Click [Assets]-[Import Package]-[Custom Package...].
1. Select the steamvr_2_7_3.unitypackage and open the file.
1. Click [Import] button.
1. If the Valve.VR.SteamVR_UnitySettingsWindow appears, click [Ignore All], and then click [Yes, Ignore All].
1. Click [Assets]-[Reimport All].
1. Click [Reimport] button.
1. Please confirm that no error occurred in Console window.


### Import executable file and dll for TTS
1. Prepare "ConsoleSimpleTTS.exe" and "Interop.SpeechLib.dll".
2. Copy those files to the [TTS] folder in the same directory as README.md.

### Build
1. Create a "Build" folder in this project folder.
2. Open this project with Unity.
3. Click [File]-[Build Settings].
4. Click [Build].
5. Select the "Build" folder.
6. Type a file name (e.g. HumanNavigation) and save the file.  

## How to Set Up

### Modify Configuration

1. Open this project with Unity.
2. Click [SIGVerse]-[SIGVerse Settings].  
SIGVerse window will be opened.
3. Type the IP address of ROS to "Rosbridge IP" in SIGVerse window.

### Set Up Configuration File for Human Navigation

1. Open the [SIGVerseConfig]-[HumanNavi]-[sample] folder in this project folder.
2. Copy "HumanNaviConfig.json" to the [SIGVerseConfig]-[HumanNavi] folder.  
(Note: If there is no configuration file in the HumanNavi folder, a configuration file will be automatically copied from the sample folder when the Unity project is opened.)

## How to Execute Human Navigation

Please start the ROS side application beforehand.  
See [human-navigation-ros](https://github.com/RoboCupatHomeSim/human-navigation-ros) for an example application.

On the Windows side, launch Oculus Software and connect the VR headset to the PC via Oculus Link (Oculus Air Link), and also launch SteamVR and set the VR headset to standby.

### Execute on Unity Editor
1. Click [SIGVerse]-[Set Default GameView Size].
2. Double click "Assets/Competition/HumanNavi/HumanNavi(.unity)" in Project window.
3. Click the Play button at the top of the Unity editor.  

### Execute the Executable file
1. Copy the "SIGVerseConfig" folder into the "Build" folder.
2. Double Click the "HumanNavigation.exe" in the "Build" folder.

## License

This project is licensed under the SIGVerse License - see the LICENSE.txt file for details.
