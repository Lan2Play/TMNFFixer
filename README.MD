# TMNFFixer

This software updates the installed Trackmania Nations Forever installation (for a successful autodetection it has to be started one time before this fix) and adds the l2ptmnf url handler and the corresponding urllauncher.exe (https://github.com/Lan2Play/URLLauncher)


# Usage
Just start the exe

# Building

To build the software, the dotnetcore SDK 3..1 is required. https://dotnet.microsoft.com/download

    dotnet.exe publish -c Release -p:PublishSingleFile=true -r win-x64 --self-contained true -p:PublishTrimmed=true

The single exe file can now be found in bin\Release\netcoreapp3.1\win-x64\publish