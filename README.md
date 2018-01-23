# Human Navigation Project

This is a Unity project for the competition of Human Navigation.  
Please prepare a common unitypackage and a dll file for text-to-speech (TTS) in advance, and import them to this project.  
For details of creating the common unitypackage, please see the following page.  
https://github.com/PartnerRobotChallengeVirtual/common-unity.git
For details of creating the dll for TTS, please see the following page.  
https://github.com/PartnerRobotChallengeVirtual/console-simple-tts

## Prerequisites

- OS: Windows 10
- Unity version: 2017.3

## How to Build

### Import common Unitypackage

1. Prepare a common unitypackage (e.g. wrs-virtual-common.unitypackage).
2. Open this project with Unity.
3. Click [Assets]-[Import Package]-[Custom Package...].
3. Select and open the common unitypackage.
4. Click [Import] button.

### Import Oculus Utilities for Unity

1. Download Oculus Utilities for Unity ver.1.21.0 from the following link.  
https://developer.oculus.com/downloads/package/oculus-utilities-for-unity-5/1.21.0/
2. Unzip the downloaded file.
3. Open this project with Unity.
4. Click [Assets]-[Import Package]-[Custom Package...].
5. Select and open "OculusUtilities.unitypackage".
6. Click [Import] button.
7. Click [Yes] on "Update Oculus Utilities Plugin" window.
8. Click [Restart] on "Restart Unity" window.

### Import dll for TTS
1. Prepare "Interop.SpeechLib.dll".
2. Open this project with Unity.
3. Copy the "Interop.SpeechLib.dll" to [Assets]-[Plugins].

### Build
1. Create a "Build" folder in this project folder.
2. Open this project with Unity.
3. Click [File]-[Build Settings].
4. Click [Build].
5. Select the "Build" folder.
6. Type a file name (e.g. HumanNavigation) and save the file.  
(Note: For now, please ignore errors of "ReflectionTypeLoadException" in NewtonVR)

## How to Set Up

### Modify Configuration

1. Open this project with Unity.
2. Click [SIGVerse]-[SIGVerse Settings].  
SIGVerse window will be opened.
3. Type the IP address of ROS to "Rosbridge IP" in SIGVerse window.

## How to Execute Human Navigation Program

### Execute On Unity Editor
1. Double click "Assets/Competition/HumanNavi/HumanNavi(.unity)" in Project window.
2. Click the Play button at the top of the Unity editor.  
(Note: For now, please ignore errors of "ReflectionTypeLoadException" in NewtonVR)

### Execute the Executable file
1. Copy the "SIGVerseConfig" folder into the "Build" folder.
2. Double Click the "HumanNavigation.exe" in the "Build" folder.

## License

This project is licensed under the SIGVerse License - see the LICENSE.txt file for details.
