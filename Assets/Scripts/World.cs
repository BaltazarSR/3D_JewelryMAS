using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class World
{
    public int numRedJewels = 3;
    public int numGreenJewels = 3;
    public int numBlueJewels = 3;
    public int U = 7;
    public int V = 7;
    public static Cell[,] grid = null!;  // Se inicializa en InitWorld()
    public static Cell[,] sharedKnowledge = null!;
    public int seed = -1;
    public System.Random rng = null!;
    public Robot robotRed = null!;
    public Robot robotGreen = null!;
    public Robot robotBlue = null!;
    Zone zone;
    List<Jewel> jewels = null!;

    public void Start()
    {
        rng = (seed < 0) ? new System.Random() : new System.Random(seed);
        InitWorld();
    }

    void InitWorld()
    {
        // create grid
        // U = width
        // V = height
        sharedKnowledge = new Cell[U, V];
        grid = new Cell[U, V];
        for (int x = 0; x < U; x++)
            for (int y = 0; y < V; y++)
                grid[x, y] = new Cell(x, y);

        // Initialize jewels list
        jewels = new List<Jewel>();

        // place robots at random positions
        robotRed = new Robot();
        robotRed.Color = 'R';
        robotBlue = new Robot();
        robotBlue.Color = 'B';
        robotGreen = new Robot();
        robotGreen.Color = 'G';

        // Place red robot
        robotRed.X = rng.Next(U);
        robotRed.Y = rng.Next(V);
        grid[robotRed.X, robotRed.Y].state = CellState.robot;
        grid[robotRed.X, robotRed.Y].LayingRobot = robotRed;

        // Place blue robot
        do
        {
            robotBlue.X = rng.Next(U);
            robotBlue.Y = rng.Next(V);
        } while (grid[robotBlue.X, robotBlue.Y].state != CellState.free);
        grid[robotBlue.X, robotBlue.Y].state = CellState.robot;
        grid[robotBlue.X, robotBlue.Y].LayingRobot = robotBlue;

        // Place green robot
        do
        {
            robotGreen.X = rng.Next(U);
            robotGreen.Y = rng.Next(V);
        } while (grid[robotGreen.X, robotGreen.Y].state != CellState.free);
        grid[robotGreen.X, robotGreen.Y].state = CellState.robot;
        grid[robotGreen.X, robotGreen.Y].LayingRobot = robotGreen;

        // Create and place red jewels
        for (int i = 0; i < numRedJewels; i++)
        {
            Jewel jewel = new Jewel();
            jewel.Color = 'R';
            do
            {
                jewel.X = rng.Next(U);
                jewel.Y = rng.Next(V);
            } while (grid[jewel.X, jewel.Y].state != CellState.free);

            grid[jewel.X, jewel.Y].state = CellState.jewel;
            grid[jewel.X, jewel.Y].LayingJewel = jewel;
            jewels.Add(jewel);
        }

        // Create and place green jewels
        for (int i = 0; i < numGreenJewels; i++)
        {
            Jewel jewel = new Jewel();
            jewel.Color = 'G';
            do
            {
                jewel.X = rng.Next(U);
                jewel.Y = rng.Next(V);
            } while (grid[jewel.X, jewel.Y].state != CellState.free);

            grid[jewel.X, jewel.Y].state = CellState.jewel;
            grid[jewel.X, jewel.Y].LayingJewel = jewel;
            jewels.Add(jewel);
        }

        // Create and place blue jewels
        for (int i = 0; i < numBlueJewels; i++)
        {
            Jewel jewel = new Jewel();
            jewel.Color = 'B';
            do
            {
                jewel.X = rng.Next(U);
                jewel.Y = rng.Next(V);
            } while (grid[jewel.X, jewel.Y].state != CellState.free);

            grid[jewel.X, jewel.Y].state = CellState.jewel;
            grid[jewel.X, jewel.Y].LayingJewel = jewel;
            jewels.Add(jewel);
        }

        orderedJewelTargets();
    }

    private void orderedJewelTargets()
    {
        for (int i = 1; i < 6; i = i + 2)
        {
            grid[i, 5].color = 'R';
        }

        for (int i = 1; i < 6; i = i + 2)
        {
            grid[i, 3].color = 'B';
        }

        for (int i = 1; i < 6; i = i + 2)
        {
            grid[i, 1].color = 'G';
        }
    }

    private void randomJewelTargets()
    {
        // Create target zones for red jewels
        for (int i = 0; i < numRedJewels; i++)
        {
            int targetX, targetY;
            do
            {
                targetX = rng.Next(U);
                targetY = rng.Next(V);
            } while (grid[targetX, targetY].color != ' ');
            grid[targetX, targetY].color = 'R';
        }

        // Create target zones for green jewels
        for (int i = 0; i < numGreenJewels; i++)
        {
            int targetX, targetY;
            do
            {
                targetX = rng.Next(U);
                targetY = rng.Next(V);
            } while (grid[targetX, targetY].color != ' ');
            grid[targetX, targetY].color = 'G';
        }

        // Create target zones for blue jewels
        for (int i = 0; i < numBlueJewels; i++)
        {
            int targetX, targetY;
            do
            {
                targetX = rng.Next(U);
                targetY = rng.Next(V);
            } while (grid[targetX, targetY].color != ' ');
            grid[targetX, targetY].color = 'B';
        }
    }

    private string GetCellLabel(int x, int y)
    {
        var c = grid[x, y];

        // Primary (what's on the cell)
        string primary = "";
        if (c.state == CellState.robot)
        {
            char rc = c.LayingRobot?.Color ?? ' ';
            primary = rc switch
            {
                'R' => "RR",  // Red Robot
                'G' => "RG",  // Green Robot
                'B' => "RB",  // Blue Robot
                _ => "R?"
            };

            // Add marker if robot is carrying a jewel
            if (c.LayingRobot?.CarryingJewel != null)
            {
                char jc = c.LayingRobot.CarryingJewel.Color;
                primary += $"J{jc}"; // e.g. RRJG = Red Robot carrying Green Jewel
            }
        }
        else if (c.state == CellState.jewel)
        {
            char jc = c.LayingJewel?.Color ?? ' ';
            primary = jc switch
            {
                'R' => "JR",
                'G' => "JG",
                'B' => "JB",
                _ => "J?"
            };
        }

        // Secondary (the target space)
        string secondary = c.color switch
        {
            'R' => "SR",
            'G' => "SG",
            'B' => "SB",
            _ => ""
        };

        if (primary == "" && secondary != "") return secondary;     // only target space
        if (primary != "" && secondary != "") return $"{primary}/{secondary}";
        if (primary != "") return primary;                          // only content
        return "__";                                                // truly empty
    }

    public void PrintGrid()
    {
        if (grid == null)
        {
            Debug.Log("Grid not initialized.");
            return;
        }

        var sb = new StringBuilder();
        int cellWidth = 6; // fixed width, more space for labels like "JR/SR"

        // Column header
        sb.Append("    "); // left padding for row numbers
        for (int x = 0; x < U; x++)
            sb.Append(x.ToString().PadLeft(cellWidth));
        sb.AppendLine();

        // Rows
        for (int y = V - 1; y >= 0; y--) // print from top to bottom
        {
            sb.Append(y.ToString().PadLeft(3)); // row index
            sb.Append(" ");                     // spacing after row number

            for (int x = 0; x < U; x++)
            {
                string label = GetCellLabel(x, y);
                sb.Append(label.PadLeft(cellWidth));
            }
            sb.AppendLine();
        }

        Debug.Log(sb.ToString());

        // Legend
        Debug.Log(
            "Legend:\n" +
            " R   = Robot\n" +
            " JR/G/B = Jewel (Red/Green/Blue)\n" +
            " SR/G/B = Target Space\n" +
            " Combined like R/SR means Robot in Target Space"
        );
    }

    public void CheckCompletion()
{
    for (int x = 0; x < U; x++)
        for (int y = 0; y < V; y++)
        {
            var cell = grid[x, y];

            if (cell.color != ' ' && cell.LayingJewel != null)
            {
                if (cell.color == cell.LayingJewel.Color)
                {
                    cell.correct = true;
                }
            }
        }
}

}
