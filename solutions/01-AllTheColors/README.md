# AllTheColors

## Description

AllTheColors is a C# command-line program that creates a image with all possible RGB colors. The generated image must contain a minimum of 16 million pixels, covering the entire RGB color space.

The program provides several modes for generating the image:

- **Trivial Mode**: A simple and straightforward implementation.
- **Random Mode**: Creates a chaotic image with no recognizable pattern.
- **Pattern Mode**: Generates a visually appealing circle.
- **Mandala Mode**: Produces mandala-like patterns with circular symmetry.

** Note that Pattern mode with 4096x4096 image takes approximately 20 seconds ** 

The program accepts command-line arguments to define the desired image size, output file name, mode selection, and a random seed if relevant. It ensures that the image size is equal to or larger than 16 million pixels; otherwise, it displays an error message.

## Usage

- Open a command-line interface.

- Navigate to the directory where `01-AllTheColors.dll` is located. For example, if your program is in the `01-AllTheColors\bin\Release\net6.0` directory, go there.

- Execute the following command:
  ~~~
  dotnet 01-AllTheColors.dll <width> <height> <output_filename> <mode> [seed]
  ~~~

  - `<width>`: The width of the output image in pixels.
  - `<height>`: The height of the output image in pixels.
  - `<output_filename>`: The name of the output PNG file.
  - `<mode>`: The mode for generating the image (trivial, random, pattern, mandala).
  - `[seed]` (Optional): A seed value for random mode.

## Mandala Usage

In Mandala mode, the program enables you to create intricate and visually captivating mandala-like patterns with circular symmetry. The interactive interface allows you to design your mandala step by step. Here's how to use Mandala mode:

1. Run the program in Mandala mode

2. Once in Mandala mode, you'll be prompted to enter commands. The available commands are:

- `fullCircle x y radius`: Draws a full circle with its center at coordinates (x, y) and a specified radius.
- `circleEdge x y radius width`: Draws a circle with a specified radius and width at coordinates (x, y).
- `square x y sideSize width`: Draws a square with a specified side size and width at coordinates (x, y).
- `multiCircles x y radius numberOfCircles distanceFromCenter`: Generates multiple equally distributed circles around a central point (x, y) with a specified radius, number of circles, and distance from the center.
- `exit`: Exits the Mandala mode and saves the generated image.

** Be aware that what you add first will be displayed on top and you will not be able to overlay it with later commands. ** 

3. Follow the prompts to enter these commands and build your mandala. As you add shapes, the program will display feedback, allowing you to review the changes made.

4. Continue to add shapes and patterns until you are satisfied with the mandala's design.

Mandala mode provides a creative and interactive way to generate beautiful mandala-like patterns within your image. Enjoy the process of designing your unique mandala, and don't forget to save your masterpiece when you're finished.

### Special Arguments

- `center`: The keyword "center" can be used to specify the center of the image. For example, when creating shapes, you can use "center" as the x or y coordinate to place the shape at the center of the image.

- `max`: The keyword "max" can be used for specifying the maximum possible value. In the context of radius and sideSize, "max" indicates that the program should calculate the maximum value to fill the screen or image.


## Examples

- Generate a trivial image:
~~~
dotnet 01-AllTheColors.dll 4096 4096 output_trivial.png trivial
~~~
<img src="./Downsampled Images/trivial.png" alt="Downsampled Trivial" width="500" />

- Generate a random image with a seed:
~~~
dotnet 01-AllTheColors.dll 4096 4096 output_random.png random 123
~~~
<img src="./Downsampled Images/random.png" alt="Downsampled Random" width="500" />

- Generate a pattern image:
~~~
dotnet 01-AllTheColors.dll 4096 4096 output_pattern.png pattern
~~~
<img src="./Downsampled Images/pattern.png" alt="Downsampled Pattern" width="500" />

- Generate a mandala-style image and interactively add shapes:
~~~
dotnet 01-AllTheColors.dll 4096 4096 output_mandala.png mandala
~~~

## Mandala example
~~~
fullCircle center center 200
circleEdge center center 2000 100
multiCircles center center 100 8 600
multiCircles center center 400 4 1200
fullCircle 0 0 900 
fullCircle 0 4095 900
fullCircle 4095 0 900 
fullCircle 4095 4095 900 
square center center 2686 50
square center center max 706
fullCircle center center max max
exit
~~~

<img src="./Downsampled Images/mandala.png" alt="Downsampled Mandala" width="500" />

## Dependencies

- The program uses the [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp/) library for image processing.

## Build and Run

1. Make sure you have .NET 6 installed on your system.
2. Clone this repository and navigate to the project directory.
3. Build the project:
~~~
dotnet build
~~~
4. Run the program with the desired command-line arguments as shown in the examples.

## Credits

This project was created with the assistance of ChatGPT 3.5. The AI model provided valuable support in multiple aspects of the project, including:

- **Command-Line Argument Parsing**: ChatGPT assisted in parsing command-line arguments and helped ensure that the program could intelligently divide and interpret the arguments based on the selected pattern mode.

- **Readme File Creation**: The readme file you are currently reading was created with the guidance and support of ChatGPT. It assisted in structuring and enhancing the documentation to provide clarity and useful information for users.

- **Pattern Creation**: ChatGPT helped a little bit in the context of generating circles within the image.

#### Algorithms

- **Fisher-Yates Shuffle**: The program utilizes the Fisher-Yates Shuffle algorithm to randomize lists. Fisher-Yates Shuffle is a widely-used method for shuffling elements in a collection to achieve a random order.

- **Bresenham's Circle Drawing Algorithm**: The program incorporates Bresenham's Circle Drawing Algorithm to efficiently draw circles on a discrete grid. This algorithm is known for its ability to render circular shapes using integer arithmetic.

## Author

Jakub Rollo

## License

This project is open-source and available under the [MIT License](LICENSE).
