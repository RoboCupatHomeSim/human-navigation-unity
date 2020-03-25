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

## How to Build

### Import common Unitypackage

1. Prepare a common unitypackage (e.g. robocup-common.unitypackage).
2. Open this project with Unity.
3. Click [Assets]-[Import Package]-[Custom Package...].
3. Select and open the common unitypackage.
4. Click [Import] button.

### Import the Oculus Integration for Unity

1. Download Oculus Integration for Unity ver.14.0 from the following link.  
https://developer.oculus.com/downloads/package/unity-integration-archive/14.0/
2. Open this project with Unity.
3. Click [Assets]-[Import Package]-[Custom Package...].
4. Select downloaded OculusIntegration_14.0.unitypackage and open the file.
5. Click [Import] button.
6. Click [Upgrade] when "Update Spatializer Plugins" window displayed.
7. Click [Restart] when "Restart Unity" window displayed.
8. Click [Yes] when "Update Oculus Utilities Plugin" window displayed.
9. Click [Restart] when "Restart Unity" window displayed.
10. Please confirm that no error occurred in Console window.

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

## How to Execute Human Navigation Program

### Execute On Unity Editor
1. Click [SIGVerse]-[Set Default GameView Size].
2. Double click "Assets/Competition/HumanNavi/HumanNavi(.unity)" in Project window.
3. Click the Play button at the top of the Unity editor.  

### Execute the Executable file
1. Copy the "SIGVerseConfig" folder into the "Build" folder.
2. Double Click the "HumanNavigation.exe" in the "Build" folder.

## License

This project is licensed under the SIGVerse License - see the LICENSE.txt file for details.
