# Documentation of 05-Animation

## Author
Jakub Rollo

## Requirements

- .NET 6
- [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp/)
- [CommandLineParser](https://www.nuget.org/packages/CommandLineParser/)

## Video demonstration:

[Watch the Video Demonstration](https://www.youtube.com/watch?v=rFnlD5qFPKw&ab_channel=JakubRollo)

I used these commands for the video:
```shell
-w 1000 -h 1000 -s 60 -f 2000 -n Pi
```
```shell
-w 1000 -h 1000 -s 60 -f 2000 -n GoldenRatio
```
```shell
-w 1000 -h 1000 -s 60 -f 2000 -n Sqrt2
```

I also utilized my jump argument to skip recreation of frames and sped up the drawing.
f.e. like this:
```shell
-w 1000 -h 1000 -j 1000 -s 3000 -f 2000 -n Pi
```


## Command line arguments
- **-w, --width:** Image width in pixels. (Required)
- **-h, --height:** Image height in pixels. (Required)
- **-f, --frames:** Number of output frames. (Default: 500)
- **-s, --speed:** Speed of drawing. Must be an int larger than 0. (Default: 50)
- **-o, --output:** Output file-name mask. (Default: "anim/out{0:0000}.png")
- **-n, --number:** Type of the mandala number. Can write any number of type double or predefined numbers: Pi, GoldenRatio, Euler or Sqrt2(Default: "Pi")
- **-j, --jump:** Skips the generation of images for the given amount of frames. (Default: 0)

## Input data
The program generates animated frames of a Mandala drawing based on specified parameters. No external input data is required.

## Algorithm
The Mandala Drawer utilizes complex number calculations to create intricate patterns. The algorithm involves the following steps:

1. **Theta Calculation:**
   - θ (theta) is dynamically updated based on the speed parameter:
     ```
     thetas[i] = ((i * speed) * 2 * Math.PI / thetas.Length) + lastPos;
     ```
   - Where "i" is the current iteration, "speed" is the specified drawing speed, and "length_of_theta_list" is the total length of the theta list.

2. **Complex Number Calculation:**
   - For each theta value, the algorithm calculates two complex arms:
     ```
     Complex innerArm = Complex.Exp(Complex.ImaginaryOne * theta);
     Complex outerArm = Complex.Exp(numberToDraw * Complex.ImaginaryOne * theta);
     ```
     - `Complex.ImaginaryOne` represents the imaginary unit "i."
     - `Complex.Exp` calculates the exponential function for complex numbers.
     - `numberToDraw` is determined by the specified Mandala number type, such as Pi, GoldenRatio, Sqrt2, Euler, or a custom double.

3. **Combining Arms:**
   - The final complex number Z(θ) is obtained by adding the inner and outer arms:
     ```
     return innerArm + outerArm;
     ```
   - This complex number represents the position in the complex plane for a specific θ, contributing to the formation of the Mandala pattern.

The combination of these steps results in the generation of points in the complex plane, creating the evolving Mandala drawing throughout the frames.


## Extra work / Bonuses
- Implemented a skip frames feature with the `-j, --jump` option to enhance efficiency and control over frame generation.
- Added a variability feature allowing users to choose the Mandala type dynamically with the `-n, --number` option. Users can input any number of type double or select predefined numbers such as Pi, GoldenRatio, Euler, Sqrt2.


## Use of AI
ChatGPT assisted with small, simple parts of the code and helped me write this README file.

## Examples
```shell
-w 2000 -h 2000 -j 1000 -s 60 -f 1 -n 12
```
<img src="./Downsized images/12.png" alt="Downsized mandala using number 12" width="500" />

```shell
-w 2000 -h 2000 -j 293 -s 60 -f 1 -n Euler
```

<img src="./Downsized images/Euler.png" alt="Downsized mandala using Euler's number" width="500" />

```shell
-w 2000 -h 2000 -j 1000 -s 60 -f 1 -n 8.2
```
<img src="./Downsized images/8,2bigger.png" alt="Downsized mandala using number 8.2" width="1000" />

```shell
-w 2000 -h 2000 -j 1000 -s 60 -f 1 -n 8.234567891242
```
<img src="./Downsized images/8,234567891242.png" alt="Downsized mandala using number 8.234567891242" width="500" />
