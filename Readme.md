SparrowHawk
============

SparrowHawk is the C# port and extension of the Kestrel robotic printing plugin
for Rhinoceros. SparrowHawk's name comes from a colloquial term for the 
[american kestrel](https://en.wikipedia.org/wiki/American_kestrel). It is
intended that the full code base for Kestrel shall be ported to SparrowHawk
between January 14 and January 16, 2017, barring unknowns and risks.


Checklist for SparrowHawk
=========================

- [x] Rhino Plugin (Brep & Rhino Plugin)
- [x] Rhino-VR communication
- [x] OpenVr (OpenVr from VisualizerVr)
- [X] Control loop (VisualizerVr)
- [x] OpenGl (glad from Nanogui)
- [x] Windowing system (GLFW from Nanogui)
- [x] Scene tree structure (Scene)
- [x] GLShader framework (Nanogui)
- [X] OVRVision Camera (VisualizerVr)
- [ ] Encoder (VisualizerVr and Encoder)
- [X] Mesh, line, curve framework (Geometry and VisualizerVr)
- [ ] Ray Tracing (Scene and Geometry)
- [X] Interaction framework (Interaction and VisualizerVr)
- [X] Materials (Material)
- [x] Vector Math Library (Eigen from Nanogui)


Setup notes
===========
* Before running or debugging the plugin, make sure to get the .dll from
openvr/bin/win64/openvr.dll and put it in the SparrowHawk bin directory.

* Use NuGet Package Manager to set up MathNet.Numerics.