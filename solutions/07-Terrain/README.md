# Documentation of the Task "07-Terrain"

## Author
Jakub Rollo

## Requirements
This project requires the following packages:
- SixLabors.ImageSharp
- CommandLineParser
- Silk.Net

## Command Line Arguments
The following command line arguments are available:
- `-w, --width`: Window width in pixels. Default is 800.
- `-h, --height`: Window height in pixels. Default is 600.
- `-t, --texture`: User-defined texture. (Activated by pressing `T`)

## Algorithm
This project implements the Diamond-square algorithm for fractal terrain generation. The terrain is displayed in a 3D scene using the OpenGL library, bound to .NET by the Silk.NET library. 
The color of the terrain varies based on its elevation.
The terrain's triangle mesh can be interactively changed using `Up` and `Down` arrow keys (Updivide/Subdivide). If you revert to the simplest level of terrain and updivide again, the heights of the 4 main corners will change.

### Basic solution

 The program calculates normal vectors for each vertex which are used for Phong Shading (activated by pressing `I`.). The calculation of these normals is independent of the terrain’s subdivision level (It works on all levels of subdivision). The Hausdorff coefficient can be increased and decreased using `PageUp` and `PageDown`. (You can even change the hausdorff coefficient in-between subdivisons.)

 ### Added features

It also implements three different camera modes ( toggled by pressing `H`):

    1. Trackball camera (default)

    2. Hovercraft - Third-person mode
    
    3. Hovercraft - First-person mode

#### Trackball mode

In the default trackball camera mode, you can look around by dragging with the left mouse button and move the terrain by dragging with the right mouse button. The terrain can be rotated using the `Left` and `Right` arrow keys and moved using `W, A, S, D`. You can zoom in and out using mouse scroll.

#### Hovercraft modes

Both hovercraft modes are designed to control a drone that copies the terrain while moving. The movement is designed to be smooth, with some inertia, and is dependent on the direction the camera is facing. The drone can be controlled using `W, A, S, D`, with `E` to ascend and `Q` to descend.

(If the drone moves out of the bounds of the generated terrain, it will continue to mimic the closest edge of the terrain.)

In hovercraft mode , the terrain movement used in trackball camera mode is disabled,(so you cannot f.e. drag the terrain and move it around).However you can still zoom in and out on the drone using mouse scroll in the third-person mode.

In third-person mode, the hovercraft moves a bit faster.

In first-person mode, you can look around by moving the mouse (and you also move based on what direction you are facing). The hovercraft moves slower in first-person mode for a more pleasant experience, and zooming in and out is disabled.

#### Texture mapping

I have also experimented with texture mapping, mapping every square in the most subdivided terrain to a specific texture (given as an argument). However this feature is not really fleshed out.

## Extra Work / Bonuses
-Vertex color according to elevation

-Interactive hovercraft modes 
(which includes implementation of a first-person camera)

(- Texture mapping)

## Use of AI
I have used ChatGPT as my consultant. I really couldn't explain the whole code to ChatGPT because even the pilot solution is quite big so when I wanted to give a broader context I tried a new feature in Microsoft's Bing Copilot (which uses GPT-4) called Notebook and it was quite useful.

It mostly helped me with fixing bugs but it also helped me with the implementaion of the colours based on height and the camera movement in first-person mode. However he doesn't really know how Silk.Net works so he was quite bad at giving advice on how to use it.

## Controls
The following controls are implemented in the application:
- `T`: Toggle texture (currently not working)
- `I`: Toggle Phong shading
- `P`: Toggle perspective
- `V`: Toggle VSync
- `C`: Camera reset
- `Left, Right`: Rotate the object
- `Home`: Reset the object
- `F1`: Print help
- `Esc`: Quit the program
- `Mouse.left`: Trackball rotation
- `Mouse.right`: Drag current object
- `Mouse.wheel`: Zoom in/out
- `Up, Down`: Updivide/Subdivide
- `PageUp`: Increase Hausdorff coefficient
- `PageDown`: Decrease Hausdorff coefficient
- `H`: Switch between camera modes
- `W,A,S,D`: Move hovercraft and move terrain
- `Mouse Movement`: Look around in First-person mode
- `Q`: Lower hovercraft hover
- `E`: Higher hovercraft hover
