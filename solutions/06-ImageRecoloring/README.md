# Documentation of the task "06-ImageRecoloring"

## Requirements

- .NET 6
- [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp/)
- [CommandLineParser](https://www.nuget.org/packages/CommandLineParser/)

## Usage

### Basic Usage

- `-i`, --input: Specifies the input image path.
- `-o`, --output: Specifies the output image path.
- `-h`, --hue-delta: Integer/float number to use for hue recoloring. Positive and negative values are allowed, representing degrees.

### Example:

```shell
06-ImageRecoloring.dll -i "input.png" -o "output.png" -h 100
```

## Input data
The program accepts any input image readable by the ImageSharp library.

## Algorithm
The solution utilizes the SixLabors.ImageSharp library to load and manipulate images. The skin tone detection is performed in HSV and YCbCr color spaces, with additional support for CieLab color space for almost skin pixels. The program applies a hue shift to non-skin pixels based on the specified hue delta.

### Skin Tone Detection
The SkinThreshold class provides methods for detecting skin pixels in HSV, YCbCr, and CieLab color spaces. The program uses a skin matrix to enhance the accuracy of skin tone detection.

### Morphological Erosion
The solution includes a basic morphological erosion function intended to refine the skin matrix.

## Use of AI

For general work with the code and assistance in refining the program, ChatGPT (powered by OpenAI) was consulted. However, it is important to note that ChatGPT did not contribute significantly to the development or make substantial changes to the program logic.


## Examples

```shell
06-ImageRecoloring.dll -i "11-bara.jpg" -o "output.jpg" -h 200
```

11-bara.jpg:

<img src="./Downsampled Images/11-bara.jpg" alt="Barbora Polakova" width="500" />

output.jpg:

<img src="./Downsampled Images/output.jpg" alt="Changed Colors" width="500" />


## Author
Jakub Rollo
