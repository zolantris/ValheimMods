# Unity Probuild Mesh Generation

Using unity probuilder we can easily generate cutout meshes using the Experimental Boolean feature provided by probuilder.

## Getting Started

Using ProBuilder in Unity, you can easily create and manipulate meshes without needing to write custom mesh generation code. ProBuilder allows for creating complex shapes and performing boolean operations, which is perfect for your case of cutting a cylinder mesh out of a cube mesh.

Here’s how to do it step by step in Unity with ProBuilder:

Steps to Cut a Cylinder from a Cube Using ProBuilder
Install ProBuilder

If you don’t have ProBuilder installed, follow these steps:

Go to Window > Package Manager.
In the Package Manager, find ProBuilder and click Install.
Create the Cube and Cylinder

In the Unity Hierarchy window, right-click and choose 3D Object > Cube to create a cube mesh.
Right-click again and choose 3D Object > Cylinder to create a cylinder mesh.
Open ProBuilder Window

Go to Tools > ProBuilder > ProBuilder Window to open the ProBuilder window. This is where you’ll manage your meshes.
Edit the Cube with ProBuilder

Select the Cube in the Hierarchy.
In the ProBuilder window, click on Edit to enable ProBuilder editing mode. This allows you to modify the geometry of the cube.
Position the Cylinder for the Cut

Select the Cylinder and move it into position inside the cube where you want the hole to be.
Rotate the cylinder to the desired angle (e.g., aligning it along the z-axis).
Resize the cylinder as needed (scale it to fit the desired hole size).
The cylinder will be the shape you want to "cut out" of the cube.

Perform the Boolean Operation

Select the Cube in the Hierarchy.

In the ProBuilder window, click on the Boolean button (it might be found under the Actions tab or similar, depending on your ProBuilder version). This tool allows you to perform Boolean operations like subtraction, union, and intersection.

Click on the Subtract button (or the equivalent) after selecting the Cylinder.

ProBuilder will use the cylinder to subtract its volume from the cube, creating a hole in the cube where the cylinder intersects.

Finishing the Operation

After the operation is completed, you should see a cube with a hole where the cylinder was.
You can now edit the new mesh further (move vertices, refine the shape) or export it if needed.


## Exporting the mesh

After creating the mesh, export the mesh and save it as a package. Then unpack the package and move the mesh into the ValheimVehicles/Meshes folder.

Reference the mesh now with any gameobject meshfilter.