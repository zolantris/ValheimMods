# Convex hull regression checks

Runs the production calculator against the actual Unity vector types. No game,
world, network connection, or Unity scene is created. Requires .NET 12 and a
Valheim or Unity installation containing `UnityEngine.CoreModule.dll`.

```powershell
dotnet run --project tests/ConvexHull/ConvexHull.Tests.csproj --configuration Release -p:ManagedDataPath="<
```

Checks degenerate and non-finite inputs, retention of previous output on failure,
an exact four-point tetrahedron, complete enclosure and positive mesh volume,
duplicate points, transformed meshes at several scales, the iteration limit,
and reuse after failure. Client/server tests are still needed for Unity collider
cooking, rebuild scheduling, physics ownership, and world persistence.
