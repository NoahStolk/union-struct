rem TODO: Clear packages here.
dotnet pack -c Release -o ./artifacts -p:Version=0.0.0-temp
dotnet restore ./UnionStruct.Tests.NuGetIntegration --packages ./packages --configfile "nuget.integration-tests.config"
dotnet build ./UnionStruct.Tests.NuGetIntegration -c Release --packages ./packages --no-restore
dotnet test ./UnionStruct.Tests.NuGetIntegration -c Release --no-build --no-restore
