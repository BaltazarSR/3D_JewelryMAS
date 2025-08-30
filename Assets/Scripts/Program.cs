using System;
using System.Collections.Generic;

class Program
{
    static void Main()
    {
        // Create a world
        World world = new World();
        world.Start();       // initializes robots, jewels, targets

        // Print the grid
        world.PrintGrid();   // the function we added
    }
}
