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
- [ ] OpenVr (OpenVr from VisualizerVr)
- [ ] Control loop (VisualizerVr)
- [ ] OpenGl (glad from Nanogui)
- [ ] Windowing system (GLFW from Nanogui)
- [ ] OVRVision Camera (VisualizerVr)
- [ ] Encoder (VisualizerVr and Encoder)
- [ ] Mesh, line, curve framework (Geometry and VisualizerVr)
- [ ] Ray Tracing (Scene and Geometry)
- [ ] Interaction framework (Interaction and VisualizerVr)
- [ ] Materials (Material)
- [ ] Vector Math Library (Eigen from Nanogui)


Setup notes
===========
* Before running or debugging the plugin, make sure to get the .dll from
openvr/bin/win64/openvr.dll and put it in the SparrowHawk bin directory.