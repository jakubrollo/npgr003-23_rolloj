using System.Numerics;

class Hovercraft
{
    private Vector3 position;
    private Vector3 velocity;
    private float hoverHeight;
    private float inertia;
    private float gravity;
    public float risingSpeedCoefficient = 5;
    public float fallingSpeedCoefficient = 2;

    public Hovercraft(float hoverHeight, float inertia, float gravity, float posX, float posY)
    {
        this.position = new Vector3(posX, 0, posY);
        this.velocity = new Vector3(0, 0, 0);
        this.hoverHeight = hoverHeight;
        this.inertia = inertia;
        this.gravity = gravity;
    }

    public void Simulate(double time, Vector3 userAcceleration, float[,] terrainData, float terrainScale)
    {
        // Update velocity based on user input
        this.velocity += (Vector3)userAcceleration * (float)time;

        // Apply inertia
        this.velocity *= this.inertia;

        // Update position
        this.position += this.velocity * (float)time;

        // Get terrain height at new position
        int x = (int)(this.position.X/terrainScale);
        int z = (int)(this.position.Z/terrainScale);
        //Console.WriteLine(x + "/" + z);
        float terrainHeight = GetTerrainHeight(terrainData, x, z);

        // Adjust vertical position based on terrain height and hover height
        
        if (this.position.Y < terrainHeight + this.hoverHeight)
        {
            this.velocity.Y += this.gravity*risingSpeedCoefficient * (float)time;
        }
        else
        {
            // Apply gravity
            this.velocity.Y -= this.gravity*fallingSpeedCoefficient * (float)time;
        }
        
    }

    private static float GetTerrainHeight(float[,] terrainData, int x, int z)
    {
        int terrainWidth = terrainData.GetLength(0);
        int terrainDepth = terrainData.GetLength(1);

        // Clamp x and z values to stay within terrain bounds
        x = Math.Max(0, Math.Min(x, terrainWidth - 1));
        z = Math.Max(0, Math.Min(z, terrainDepth - 1));

        // Return terrain height at specified position
        return terrainData[x, z];
    }

    public void ChangeHeight(float newHeight)
    {
        this.position.Y = newHeight;
    }

    public void ChangeHoverHeight(float newHoverHeight)
    {
        hoverHeight = newHoverHeight;
    }

    public Vector3 Position
    {
        get { return this.position; }
    }
}
