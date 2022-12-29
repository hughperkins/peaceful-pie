# Peaceful Pie

Connect Python with Unity for reinforcement learning!

## Installation

You need to install both into Unity project, and into your Python environment.

### In Unity

- in Unity, in your project's "Assets" folder, create a "Plugins" folder, if it doesn't already exist
- First install AustinHarris.JsonRPC:
    - Download https://www.nuget.org/api/v2/package/AustinHarris.JsonRpc/1.2.3
    - rename to have filename suffix be `.zip` (you might need to turn on options to see all file extensions)
    - unzip the resulting zip file
    - copy `lib/netstandard2.1/AustinHarris.JsonRpc.dll` into your `Plugins` folder
    - select the file, in your Plugins, and in 'Inspector' unselect 'validate references', and click 'Apply'
- from this repo, copy `PeacefulPie.dll` into your `Plugins` folder
    - select the file, in your Plugins, and in 'Inspector' unselect 'validate references', and click 'Apply'
- if on Mac silicon, make sure to change 'CPU' to 'Any CPU', for each dll, clicking 'Apply' each time

You should be good to go :)

### In Python

```
pip install -U peaceful-pie
```

# Requirements

- currently tested with:
    - python 3.10
    - Unity 2021.3.16.f1
- please create an issue if your preferred platform is not supported (I'm guessing I might need to downgrade Python a little? Let me know!)
