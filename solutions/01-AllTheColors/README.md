# 01-AllTheColors

## Description

AllTheColors is a C# command-line program that creates a image with all possible RGB colors. The generated image must contain a minimum of 16 million pixels, covering the entire RGB color space.

The program provides several modes for generating the image:

- **Trivial Mode**: A simple and straightforward implementation.
- **Random Mode**: Creates a chaotic image with no recognizable pattern.
- **Pattern Mode**: Generates a visually appealing circle.
- **Mandala Mode**: Produces mandala-like patterns.

**Note that Pattern mode with 4096x4096 image takes approximately 20 seconds** (I really tried to make it as fast as possible so I personally think is quite fast, because it's pretty hard to draw fast a circular pattern that has all the colours.)

**Note that in Mandala Mode, all possible colors will be displayed only if the screen is filled completely, this is so that the user can have more creative freedom. (The screen can be filled by using -p fullC or -p square  without parameters.)**

The program accepts command-line arguments to define the desired image size, output file name, mode selection, and a random seed if relevant. It ensures that the image size is equal to or larger than 16 million pixels; otherwise, it displays an error message.

## Usage

- Open a command-line interface.
- Navigate to the directory where `01-AllTheColors.dll` is located. For example, if your program is in the `01-AllTheColors\bin\Release\net6.0` directory, go there.
- Execute the following command:

  ~~~
  dotnet 01-AllTheColors.dll <width> <height> <output_filename> <mode> [seed]
  ~~~

  (This order of commands is not necessary.)

Required:

- `-w, --width`: The width of the output image in pixels.
- `-h, --height`: The height of the output image in pixels.
- `-o, --output`: The name of the output PNG file.
- `-m, --mode`: The mode for generating the image (trivial, random, pattern, mandala).

Optional:
- `-i, --input` : The name of the input file with parameters for mandala mode.
- `-s, --seed` : A seed value for random mode.

## Mandala Usage

If you don't specify the input parameters using "--input" than a static mandala will be generated (picture below). If you want to parameterize the mandala you can write the parameters (describ below) into a file and then launch the program with "-i", so for example:"-m mandala -i input.txt". This allows you to design your mandala step by step. Here's how to use Mandala mode:

The parameters:
<br/><br/>

**`"-p fullC"`** with Parameters:` x y radius`:

Draws a filled circle with its center at coordinates (x, y) and a specified radius.

**`"-p edgeC"`** with Parameters:` -x -y --radius --width` :

Draws a circle with a specified radius and width at coordinates (x, y).

**`"-p square"`** with Parameters:` -x -y --sideSize --width` :

Draws a square with a specified side size and width at coordinates (x, y).

**`"-p multiC"`** with Parameters:` -x -y --radius --numberOfCircles --distance` :

Generates multiple equally distributed circles around a central point (x, y) with a specified radius, number of circles, and distance from the center.

**`*End of file*`** :

Exits the Mandala mode and saves the generated image.
<br/><br/>

**Be aware that what you add first will be displayed on top and you will not be able to overlay it with later commands.**

**To ensure your image utilizes all colors, include the commands 'square center center max max' or 'fullCircle center center max max' at least once when creating your mandala. These commands always fill the entire screen.**
<br/><br/>

As shapes are added from the given parameters, the program will display feedback, allowing you to review the changes made.


### Default values for parameters

- `-x, --centerX` and `-y, --centerY`: The default value for these parameters is the center of the x/y axis, so if you don't specify the location then the center will be used.
- `-r, --radius`: The default value for radius in fullC is so that the circle fills the entire screen.
  For edgeC the default value for radius is set to the maximum value that fits the circle inside the screen.
  For multiC the radius is on default set to 100.
- `-s, --side`:  The square's side length is set to the maximum of the image's width and height by default.
- `-w, --width`: The default value for width in edgeC is set to 1 and the default value for width in square is set to half of the side of the square so that it creates a filled square.
- `-d, --distance`: The default value for distance from the x and y coordinates in multiC works the same way as radius in edgeC, it uses the maximum distance where the circles still fit inside the image.
- `-n, --numberOfCircles`: The default value is set to 2.

## Dependencies

- The program uses the [SixLabors.ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp/) library for image processing.
  -[CommandLineParser](https://www.nuget.org/packages/CommandLineParser/)

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

I also asked ChatGPT to create a visually appealing pattern using my parameters and although the result isn't exactly beautiful, it still created a quite interesting image. The result and other examples can be seen below.

#### Algorithms

- **Fisher-Yates Shuffle**: The program utilizes the Fisher-Yates Shuffle algorithm to randomize lists. Fisher-Yates Shuffle is a widely-used method for shuffling elements in a collection to achieve a random order.
- **Bresenham's Circle Drawing Algorithm**: The program incorporates Bresenham's Circle Drawing Algorithm to efficiently draw circles on a discrete grid. This algorithm is known for its ability to render circular shapes using integer arithmetic.

## Examples

- Generate a trivial image:

~~~
dotnet 01-AllTheColors.dll -w 4096 -h 4096 -o output_trivial.png -m trivial
~~~

<img src="./Downsampled Images/trivial.png" alt="Downsampled Trivial" width="500" />

- Generate a random image with a seed:

~~~
dotnet 01-AllTheColors.dll -w 4096 -h 4096 -o output_random.png -m random -s 123
~~~

<img src="./Downsampled Images/random.png" alt="Downsampled Random" width="500" />

- Generate a pattern image:

~~~
dotnet 01-AllTheColors.dll -w 4096 -h 4096 -o output_pattern.png -m pattern
~~~

<img src="./Downsampled Images/pattern.png" alt="Downsampled Pattern" width="500" />

- Generate a static mandala-style image:

~~~
dotnet 01-AllTheColors.dll -w 4096 -h 4096 -o output_mandala.png -m mandala
~~~

<img src="./Downsampled Images/mandala.png" alt="Downsampled Mandala" width="500" />

Just for context this mandala image is made using these hardcoded parameters:
~~~
-p fullC -r 200
-p edgeC -r 2000 -w 100
-p multiC -r 100 -n 8 -d 600
-p multiC -r 400 -n 4 -d 1200
-p fullC -x 0 -y 0 -r 900
-p fullC -x 0 -y 4095 -r 900
-p fullC -x 4095 -y 0 -r 900
-p fullC -x 4095 -y 4095 -r 900
-p square -r 2686 -w 50
-p square -w 706
-p fullC

~~~

## Mandala examples using the parameters in file:
These images are created using the parameters in inpu2.txt - input6.txt .

All these images (except input5.txt) use all the possible colors and were created in a 4096x4096 image.

### input2.txt:

<img src="./Downsampled Images/mandala2.png" alt="Downsampled Mandala 2" width="500" />
<br/><br/>

### input3.txt was created by ChatGPT:

<img src="./Downsampled Images/mandala3.png" alt="Downsampled Mandala 3" width="500" />

### input4.txt was created by ChatGPT:

<img src="./Downsampled Images/mandala4.png" alt="Downsampled Mandala 4" width="500" />

### input5.txt (doesn't include all the possible colors):

<img src="./Downsampled Images/mandala5.png" alt="Downsampled Mandala 5" width="500" />

### input6.txt:

<img src="./Downsampled Images/mandala6.png" alt="Downsampled Mandala 6" width="500" />


## Author

Jakub Rollo
