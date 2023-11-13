# Documentation of the task "03-SFC"
## Author
Jakub Rollo

## Command Line Arguments

- `-i <filename>`: Input image file.
- `-m <method>`: Pixel order method (`0` for default scanline order, positive integers (from 0 to 5) for custom methods, `-1` for the Hilbert Curve).
- `-p <order>`: Order of the predictor (`1` or `2` but default is `1`).
- `-c <channel>`: Color channel selector (`R`, `G`, `B` or `Gray`). Default is grayscale.
- `-d`: Activates an optional drawing visualisation (in console) of the method which is selected.

## Input Data

The input data consists of raster images, and the project focuses on analyzing the pixels' spatial ordering. The images can be in various formats, including but not limited to PNG and JPEG.

## Algorithm

The project implements different Space Filling Curves (SFC) for 2D pixel orderings to evaluate their impact on predictive image compression. 

The possible pixel order methods are:
  - `ScanLine` - The simplest pixel order which goes through the image line by line from left to right.
  - `ZigZag scan`: This method is similar to the ScanLine approach. However, instead of moving consistently in one direction, ZigZag scan alternates directions as it progresses through the image, creating a zigzag pattern.
  - `Rectangle Spiral` - This method goes through the pixels of the image in a spiral. It starts at the upper-left corner, goes along the edges of the image and at the end it reaches the center of the image.
  - `Z-order curve`: This method employs a modified Z-order curve. The Z-order curve traditionally operates within squares with side lengths that are powers of 2. To adapt it for non-square images, the algorithm first identifies the smallest square with a side length as a power of 2 that can fully enclose the image. It then follows the Z-order curve within this square, skipping values outside the image area.
  - `Peano curve`: This method employs a modified Peano curve. The modification is very similair to the modification made in Z-order curve method. The Peano curve traditionally operates within squares with side lengths that are powers of 3. So the algorithm uses the smallest square with a side length as a power of 3 that can fully enclose the image.
  - `Hilbert curve` - This method uses a customized Hilbert curve.  The modification is designed to accommodate non-square images, and it offers an improvement over the modification used in the Z-order method. While it is a bit more complex, it avoids employing the same workaround for handling image sizes as the Z-order method.


The First-Order Predictive Compression scheme is used, involving predicting values based on previous samples and encoding the differences. A second-order predictor is also implemented for experimentation. The second-order predictor utilizes two previous samples, `last1` and `last2`, to predict the next symbol based on the formula: `2 * last1 - last2`.

## Extra Work / Bonuses

- Implemented multiple pixel ordering methods (ZigZag Scan, Spiral, Z-order Curve, Peano Curve, Hilbert Curve).
- Introduced the option to analyze specific color channels separately.
- Implemented a simple second-order predictor for extended analysis (`-p 2`).
- Added an optional drawing visualisation (`-d`).

## Use of AI

ChatGPT helped me in clarifying the objectives of the assignment and provided explanation of the given codebase. I also asked him to help with the implementation for the various pixel ordering methods, but it constantly failed, so I implemented almost everything myself for the methods to work. Additionally, ChatGPT contributed to the development of the Readme file.