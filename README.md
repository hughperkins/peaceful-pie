# Peaceful Pie

Connect Python with Unity for reinforcement learning!

# CI

[![CircleCI](https://dl.circleci.com/status-badge/img/gh/hughperkins/peaceful-pie/tree/main.svg?style=svg)](https://dl.circleci.com/status-badge/redirect/gh/hughperkins/peaceful-pie/tree/main)

# Installation

You need to install both into Unity project, and into your Python environment.

## In Unity

- in Unity, in your project's "Assets" folder, create a "Plugins" folder, if it doesn't already exist
- First install AustinHarris.JsonRPC:
    - Download https://www.nuget.org/api/v2/package/AustinHarris.JsonRpc/1.2.3
    - rename to have filename suffix be `.zip` (you might need to turn on options to see all file extensions)
    - unzip the resulting zip file
    - copy `lib/netstandard2.1/AustinHarris.JsonRpc.dll` into your `Plugins` folder
    - select the file, in your Plugins, and in 'Inspector' unselect 'validate references', and click 'Apply'
- click on 'Releases', on the right of the github page
    - in the most recent release, download `PeacefulPie.dll`
- copy `PeacefulPie.dll` into your Unity project's `Plugins` folder
- if on Mac silicon:
    - in Unity, goto Plugins, click on `PeacefulPie.dll`
    - in Inspector, change 'CPU' setting from 'Intel' to 'Any CPU', then click 'Apply'
    - do the same for `AustinHarris.JsonRpc.dll`

You should be good to go :)

## In Python

```
pip install -U peaceful-pie
```

# Requirements

- currently tested with:
    - python 3.10
    - Unity 2021.3.16.f1
- please create an issue if your preferred platform is not supported (I'm guessing I might need to downgrade Python a little? Let me know!)

# Dev

I'm usng Visual Studio for Mac to write this.
