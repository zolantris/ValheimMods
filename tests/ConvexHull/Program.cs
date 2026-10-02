using UnityEngine;
using ValheimVehicles.SharedScripts;

var calculator = new ConvexHullCalculator();
var vertices = new List<Vector3>();
var triangles = new List<int>();
var normals = new List<Vector3>();
var passed = 0;

void Require(bool condition, string message)
{
  if (!condition) throw new InvalidOperationException(message);
}

void Reject(string name, List<Vector3> points)
{
  var previousVertices = vertices.ToArray();
  var previousTriangles = triangles.ToArray();
  var previousNormals = normals.ToArray();
  var generated = calculator.GenerateHull(points, false, ref vertices, ref triangles, ref normals, out var bailed);
  Require(!generated && bailed, name + ": invalid geometry was accepted");
  Require(vertices.SequenceEqual(previousVertices) && triangles.SequenceEqual(previousTriangles) &&
          normals.SequenceEqual(previousNormals), name + ": the previous hull was overwritten");
  Console.WriteLine("PASS " + name);
  passed++;
}

void Accept(string name, List<Vector3> points, bool splitVertices = false)
{
  Require(calculator.GenerateHull(points, splitVertices, ref vertices, ref triangles, ref normals, out var bailed) && !bailed,
    name + ": valid geometry was rejected");
  Require(vertices.Count >= 4 && triangles.Count >= 12 && triangles.Count % 3 == 0, name + ": incomplete mesh");
  double volume = 0;
  for (var i = 0; i < triangles.Count; i += 3)
  {
    Require(triangles.Skip(i).Take(3).All(index => index >= 0 && index < vertices.Count), name + ": invalid triangle index");
    var a = vertices[triangles[i]];
    var b = vertices[triangles[i + 1]];
    var c = vertices[triangles[i + 2]];
    var face = Vector3.Cross(b - a, c - a);
    Require(float.IsFinite(face.sqrMagnitude) && face.sqrMagnitude > 0, name + ": degenerate triangle");
    var unitNormal = face / face.magnitude;
    var tolerance = Math.Max(0.00001f, (b - a).magnitude * 0.00001f);
    foreach (var point in points)
      Require(Vector3.Dot(unitNormal, point - a) <= tolerance, name + ": a source point lies outside the hull");
    volume += Vector3.Dot(a - points[0], Vector3.Cross(b - points[0], c - points[0])) / 6.0;
  }
  Require(double.IsFinite(volume) && volume > 0, name + ": mesh has no positive enclosed volume");
  if (splitVertices)
    Require(normals.Count == vertices.Count && normals.All(n => Math.Abs(n.magnitude - 1f) < 0.001f), name + ": invalid normals");
  Console.WriteLine("PASS " + name);
  passed++;
}

List<Vector3> Cube(float scale = 1f)
{
  var points = new List<Vector3>();
  foreach (var x in new[] { -1f, 1f })
  foreach (var y in new[] { -1f, 1f })
  foreach (var z in new[] { -1f, 1f }) points.Add(new Vector3(x, y, z) * scale);
  return points;
}

Accept("tetrahedron", new List<Vector3> { Vector3.zero, Vector3.right, Vector3.up, Vector3.forward });
for (var count = 0; count < 4; count++) Reject("point count " + count, Cube().Take(count).ToList());
Reject("coincident points", Enumerable.Repeat(Vector3.one, 32).ToList());
Reject("duplicates leave three unique points", new List<Vector3>
  { Vector3.zero, Vector3.right, Vector3.up, Vector3.zero, Vector3.right, Vector3.up });
Reject("sanitization collapses tiny point cloud", new List<Vector3>
  { Vector3.zero, Vector3.right * 0.0001f, Vector3.up * 0.0001f, Vector3.forward * 0.0001f });
Reject("collinear points", Enumerable.Range(0, 100).Select(i => new Vector3(i, i, i)).ToList());
Reject("flat deck", Enumerable.Range(0, 10000).Select(i => new Vector3(i % 100, 0, i / 100)).ToList());
Reject("tilted plane", Enumerable.Range(0, 100).Select(i => new Vector3(i % 10, i / 10, i % 10 + i / 10)).ToList());
foreach (var invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
{
  var points = Cube();
  points.Add(new Vector3(invalid, 0, 0));
  Reject("non-finite coordinate " + invalid, points);
}
foreach (var scale in new[] { 0.01f, 1f, 1000f })
{
  Accept("cube scale " + scale, Cube(scale));
  var rotated = Cube(scale).Select(p => new Vector3(
    p.x * 0.6f - p.z * 0.8f, p.y, p.x * 0.8f + p.z * 0.6f) + new Vector3(17, 31, -47)).ToList();
  Accept("transformed cube scale " + scale, rotated);
}
Accept("duplicated cube", Cube().Concat(Cube()).ToList());
var random = new System.Random(1729);
var cloud = Enumerable.Range(0, 500).Select(_ => new Vector3(
  (float)random.NextDouble() * 10, (float)random.NextDouble() * 10, (float)random.NextDouble() * 10)).ToList();
Accept("dense point cloud", cloud);
Accept("reversed point order", cloud.AsEnumerable().Reverse().ToList());
var shell = Enumerable.Range(0, 512).Select(i =>
{
  var y = 1.0 - 2.0 * (i + 0.5) / 512;
  var radius = Math.Sqrt(1.0 - y * y);
  var angle = i * Math.PI * (3.0 - Math.Sqrt(5.0));
  return new Vector3((float)(radius * Math.Cos(angle)), (float)y, (float)(radius * Math.Sin(angle))) * 10f;
}).ToList();
var timer = System.Diagnostics.Stopwatch.StartNew();
Accept("512-point curved shell", shell);
Require(vertices.Count == shell.Count, "curved shell: exterior vertices were dropped");
Console.WriteLine($"Curved shell completed in {timer.ElapsedMilliseconds} ms.");
Accept("split-vertex cube", Cube(), true);
var previousLimit = ConvexHullCalculator.maxLoopDepthMultiplier;
try
{
  ConvexHullCalculator.maxLoopDepthMultiplier = 0;
  Reject("iteration limit preserves previous hull", Cube());
  Accept("complete seed at iteration limit", new List<Vector3>
    { Vector3.zero, Vector3.right, Vector3.up, Vector3.forward });
}
finally
{
  ConvexHullCalculator.maxLoopDepthMultiplier = previousLimit;
}
Accept("recovery after failure", Cube());
Console.WriteLine($"Passed {passed} convex hull regression cases.");

namespace Zolantris.Shared
{
  // Geometry tests exercise the production calculator without the game logger.
  internal static class LoggerProvider
  {
    public static void LogDebug(string message) { }
    public static void LogDev(string message) { }
  }
}
