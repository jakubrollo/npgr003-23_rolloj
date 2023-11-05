
# Documentation of the task 02-ImagePalette

This command-line tool automatically computes a representative color palette from an input image. It analyzes the input image and provides 3 to 10 characteristic colors, depending on the input from user and number of unique colours in the image.

## Features

- Read an input image and specify the desired number of colors.
- Output the color palette in various formats, including text, PNG, or SVG.
- Supports a "slower" mode for more precise color palette calculation.
- Provides visualization of the color palette.

## Requirements

- .NET 6
- [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp/)
- [CommandLineParser](https://www.nuget.org/packages/CommandLineParser/)
- [Accord.NET](http://accord-framework.net/) library for K-Means clustering


## Usage

### Basic Usage

```shell
02-ImagePalette.dll -i "input.png" -c 10
```

- `-i`: Input image file.
- `-c`: Number of desired colors (3 to 10).

### Visual Output

To save the palette in PNG or SVG format, use the `-o` option:

```shell
02-ImagePalette.dll -i "input.png" -c 10 -o "palette.png"
02-ImagePalette.dll -i "input.png" -c 10 -o "palette.svg"
```

## Options

- `-s` (Slower Mode): Use the `-s` option to enable slower but more precise calculation of the color palette.
- `-v` (Visualize Palette): Use the `-v` option to visualize the color palette and specify the output file.

## Examples

### Generate a Color Palette

```shell
02-ImagePalette.dll -i "input.png" -c 5
```

This command will generate a color palette with 5 characteristic colors and display it in text format.

### Generate and Visualize a Color Palette

```shell
02-ImagePalette.dll -i "input.png" -c 8 -o "palette.svg" -v "visualization.png"
```

This command will generate a color palette with 8 characteristic colors and save it in SVG format. It will also generate "visualization.png" which is the input image but only with the colours from the palette.

## Output Formats

The tool supports generating color palettes in three different formats:

- **Text**: The default output format is plain text, with each color represented by three numbers (R, G, B) on a separate line.
- **PNG**: Use the `-o` option with a ".png" file extension to save the palette as a PNG image.
- **SVG**: Use the `-o` option with a ".svg" file extension to save the palette as an SVG image.

## Handling Difficult Images

The tool is designed to handle a variety of input images. If the number of desired colours is less than the amount of colours in the image, then the number of colors in the output palette may be lower than the specified value.

## Palette Generation Algorithm

The algorithm follows a series of steps:

1. **Resizing**: If the user didn't choose slow mode, the image will be resized to improve the speed of the algorithm. The colour palette should not change much, because we usually want the colors that are most abundant. The resizing will be done only if the file has bigger width and height than 256 pixels. The resized image sets the smaller side to 256 pixels and the other side is calculated accordingly to keep the aspect ratio of the input image.

2. **Conversion to CIE Lab Space**: The `GeneratePalette` begins by converting the (possibly resized) input image to an array of colors in the CIE L\*a\*b* color space. CIE L\*a\*b* is a color space that separates color information into three components: L (lightness), a (green to red), and b (blue to yellow). This conversion allows for color analysis in a perceptually uniform color space.

3. **Limiting the Number of Colors**: The maximum number of colors in the palette is constrained by the `maxColors` parameter. If there are fewer unique colors in the image than the specified maximum, the number of output colors will match the number of unique colors.

4. **Weighted K-Means Clustering**: K-Means clustering is used to group similar colors together. Custom weights are assigned to the L, a, and b dimensions of the CIE Lab color space to prioritize certain components during clustering. This ensures that the clustering considers perceptual color differences more accurately.

5. **Unique Cluster Identification**: The function identifies the number of unique color clusters created by the K-Means algorithm.

6. **Visualizing Palette (Optional)**: If we used visualization mode, the function generates a clustered image to visualize the grouping of colors. This image can be helpful for understanding the clustering process and debugging. This new image will be smaller than the input image, if we used slow mode.

7. **Retrieving Cluster Centers**: The cluster centers, representing the most representative colors in each cluster, are extracted from the K-Means clustering results.

8. **Conversion to RGB**: The cluster centers, initially in the CIE Lab space, are converted back to RGB colors for practical use.

9. **Sorting by Brightness**: The resulting palette is sorted by brightness (luminance) to ensure that colors are presented in an aesthetically pleasing order.

The generated palette consists of RGB colors, and it's ready for output as required.

Please note that the use of custom weights in the K-Means clustering helps in preserving perceptually meaningful colors in the final palette.

## Use of AI

I utilized ChatGPT-3.5 during the development of this project. The AI assistant helped me with:

- **Command Line Parsing**: ChatGPT provided valuable guidance and assistance in learning how to use CommandLineParser, a key component for parsing command-line arguments in my project.

- **K-Means Clustering with Accord.NET**: The AI assistant helped me understand and implement K-Means clustering using the Accord.NET library. It provided insights into the custom weight assignment for clustering in the CIE Lab color space.

- **Palette Sorting**: ChatGPT offered assistance in sorting the generated palette by brightness (luminance), contributing to the final presentation of colors in a more aesthetically pleasing order.

- **Comments**: ChatGPT helped me to create comments with explanations to my code.

I valuated the AI's suggestions and tested them to ensure their correctness and suitability for the project.

I also tried to use ChatGPT to help me with color conversion between RGB and Lab, but it was pretty unhelpful, because it used old/non-existent methods from the SixLabors.ImageSharp library. So I just had to do it myself.

## Examples
```shell
02-ImagePalette.dll -i "snow.jpg" -c 5 -o palette.png -s -v visualization.png
```
snow.jpg:

(from: https://unsplash.com/photos/a-snow-covered-street-with-a-red-stop-sign-Ow3ycF_ZYI4)

<img src="./Downsampled Images/snow.jpg" alt="Downsampled input snow" width="500" />

palette.png and visualization.png:

<img src="./Downsampled Images/visualization.png" alt="Downsampled output1" width="500" />

```shell
02-ImagePalette.dll -i "ladder.jpg" -c 3 -o palette.svg -s -v visualization2.png
```

<br/><br/>

ladder.jpg:

(from: https://unsplash.com/photos/yellow-pool-ladder-c4Eh-VZcWoc)


<img src="./Downsampled Images/ladder.jpg" alt="Downsampled input ladder" width="500" />

palette.png and visualization.png:

<img src="./Downsampled Images/visualization2.png" alt="Downsampled output2" width="500" />

## Author

Jakub Rollo