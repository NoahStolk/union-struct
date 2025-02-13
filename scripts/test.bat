del "%~dp0\..\src\artifacts\NoahStolk.UnionStruct.0.0.0-temp.nupkg"
cd "..\src"

dotnet build UnionStruct.slnx -c Release
dotnet test UnionStruct.slnx -c Release --no-build
dotnet pack -c Release -o ./artifacts -p:Version=0.0.0-temp
dotnet restore UnionStruct.Tests.NuGetIntegration/UnionStruct.Tests.NuGetIntegration.csproj --packages ./packages --configfile "nuget.integration-tests.config"
dotnet build UnionStruct.Tests.NuGetIntegration/UnionStruct.Tests.NuGetIntegration.csproj -c Release --packages ./packages --no-restore
dotnet test UnionStruct.Tests.NuGetIntegration/UnionStruct.Tests.NuGetIntegration.csproj -c Release --no-build --no-restore

pause
