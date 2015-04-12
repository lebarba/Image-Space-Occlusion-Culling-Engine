# Image Space Occlusion Culling Engine 

ISOCE is an Image Space Occlusion Culling Engine optimized to perform occlusion culling in CPU. If you have a densely occluded scene (e.g. urban, indoor scene) you can use this module in you 3D project to speed up the rendering.

ISOCE does not depend on any Graphics API ( OpenGL, DirectX) since it is implemented in CPU and it is executed before sending the objects to the rendering pipeline.

ISOCE is DLL module programmed in C++, optimized using SSE intrinsics and is based on Hierarchical Occlusion Maps.

Inline-style: 
![image](https://raw.githubusercontent.com/lebarba/Image-Space-Occlusion-Culling-Engine/master/images/ISOCE%20Screen.jpg "")


## Overview of how it works:

Select and generate the best occluders of your scene. Calculate their conservative axis aligned boxes.
In every frame project the occluder´s conservative axis aligned boxes into screen space.
Call ISOCE DLL module and send the occluder´s visible faces for rasterization.
For every visible object you want to test for occlusion send the object´s 2D bounding box and depth. ISOCE will determine if your object is occluded or it is potentially visible.
How to use the module:

## Visit Documentation page
This engine is based on the paper called: Techniques for Image Space Occlusion Culling Engine

## Want to help to improve it or have any comments?

Please contact me at lebarba  at  gmail.com


## Authors:

Leandro Barbagallo  (lebarba  at  gmail.com)

Matias Leone  (leonematias at  gmail.com)
