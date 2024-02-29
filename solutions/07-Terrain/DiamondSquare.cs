using System;

namespace TerrainGeneration
{


    public class DiamondSquare
    {
        private static Random random = new();

        public static float[,] ChangeCorners(int size, float minHeight, float maxHeight)
        {
            float[,] terrainData = new float[size, size];

            // Set corner values
            terrainData[0, 0] = GetRandomHeight(minHeight, maxHeight);
            terrainData[0, size - 1] = GetRandomHeight(minHeight, maxHeight);
            terrainData[size - 1, 0] = GetRandomHeight(minHeight, maxHeight);
            terrainData[size - 1, size - 1] = GetRandomHeight(minHeight, maxHeight);
            return terrainData;
        }

        public static float[,] Generate(int size, float roughness, float minHeight, float maxHeight)
        {
            float[,] terrainData = ChangeCorners(size, minHeight, maxHeight);

            int chunkSize = size - 1;

            while (chunkSize > 1)
            {
                // Diamond step
                for (int y = 0; y < size - 1; y += chunkSize)
                {
                    for (int x = 0; x < size - 1; x += chunkSize)
                    {
                        DiamondStep(terrainData, x, y, chunkSize, roughness);
                    }
                }

                // Square step
                for (int y = 0; y < size; y += chunkSize / 2)
                {
                    for (int x = (y + chunkSize / 2) % chunkSize; x < size; x += chunkSize)
                    {
                        SquareStep(terrainData, x, y, chunkSize, roughness);
                    }
                }

                chunkSize /= 2;
                roughness /= 2;
            }

            return terrainData;
        }

        public static float[,] Subdivide(float[,] terrainData, int recursionLevel, int size, float roughness)
        {
            int chunkSize = (size - 1) / (int)(Math.Pow(2, recursionLevel));
            roughness /= (int)(Math.Pow(2, recursionLevel));

            // Diamond step
            for (int y = 0; y < size - 1; y += chunkSize)
            {
                for (int x = 0; x < size - 1; x += chunkSize)
                {
                    DiamondStep(terrainData, x, y, chunkSize, roughness);
                }
            }

            // Square step
            for (int y = 0; y < size; y += chunkSize / 2)
            {
                for (int x = (y + chunkSize / 2) % chunkSize; x < size; x += chunkSize)
                {
                    SquareStep(terrainData, x, y, chunkSize, roughness);
                }
            }

            return terrainData;
        }

        private static void DiamondStep(float[,] terrainData, int x, int y, int stepSize, float roughness)
        {
            float average = (terrainData[x, y] + terrainData[x + stepSize, y] + terrainData[x, y + stepSize] + terrainData[x + stepSize, y + stepSize]) / 4;
            terrainData[x + stepSize / 2, y + stepSize / 2] = average + GetRandomHeight(-roughness, roughness);
        }

        private static void SquareStep(float[,] terrainData, int x, int y, int stepSize, float roughness)
        {
            // Calculate the midpoint of the square edges
            int halfSize = stepSize / 2;

            // Calculate the average of the four corners
            float sum = 0;
            int count = 0;
            if (x - halfSize >= 0) { sum += terrainData[x - halfSize, y]; count++; }
            if (x + halfSize < terrainData.GetLength(0)) { sum += terrainData[x + halfSize, y]; count++; }
            if (y - halfSize >= 0) { sum += terrainData[x, y - halfSize]; count++; }
            if (y + halfSize < terrainData.GetLength(1)) { sum += terrainData[x, y + halfSize]; count++; }

            float average = sum / count;
            terrainData[x, y] = average + GetRandomHeight(-roughness, roughness);
        }

        private static float GetRandomHeight(float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
