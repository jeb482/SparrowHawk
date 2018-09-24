SparrowHawk
============

SparrowHawk is the Augmented Reality and User Interface powering [RoMA: Robotic Modelling 
Assistant](https://www.youtube.com/watch?v=K_wWuYD1Fkg). SparrowHawk is designed as a plugin
for the [Rhino 5](https://www.rhino3d.com/) CAD tool. It can be run with or without 
the [RobotPrint](https://github.com/billynew/RobotPrintPublic) plugin, which
operates the robotic 3D printer.


The project is currently being developed under the [Meta](https://github.com/jeb482/SparrowHawk/tree/meta)
branch for a user study using and Optical See-Through (OST) system. 




Setup notes
===========
* Before running or debugging the plugin, make sure to get the .dll from
openvr/bin/win64/openvr.dll and put it in the SparrowHawk bin directory.

* Use NuGet Package Manager to set up MathNet.Numerics.


History
=======
SparrowHawk is the C# port and extension of the Kestrel robotic printing plugin
for Rhinoceros, orignally written in C++. SparrowHawk's name comes from a 
colloquial term for the [american kestrel](https://en.wikipedia.org/wiki/American_kestrel).
SparrowHawk's architecture was developed by [Jimmy Briggs](https://jimmybriggs.net), who 
also developed much of the code base. SparrowHawk also has important contributions from
[Huaishu Peng](http://www.huaishu.me/) (system design), 
[Cheng-Yao Eric Wang](http://ericwang.info/) (interaction implementation), and 
[Kevin Guo](http://www.kevinguo.net/) (interaction design).
