using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generation : MonoBehaviour
{
    public float fps;

    float counter;

    public int tileType;

    public int

            width,
            height,
            dungeonMinSize;

    public int

            iterations,
            iterationCount,
            iterationMax;

    public string seed;

    public bool useRandomSeed;

    [Range(0, 100)]
    public int randomFillPercent;

    int[,] noiseMap;

    int[,] cellMap;

    void Start()
    {
        fps = 1 / fps;
        iterationCount = iterations;
        //GenerateMap();
    }

    void FixedUpdate()
    {
        counter += Time.deltaTime;

        if (Input.GetMouseButton(0) && counter > fps)
        {
            iterationCount = iterations;
            counter = 0;
            GenerateMap();
        }
        if (Input.GetMouseButton(1) && counter > fps)
        {
            counter = 0;
            SmoothMap();
            iterationCount += 1;
        }
        if (Input.GetKey(KeyCode.A)) tileType = 0;
        if (Input.GetKey(KeyCode.S)) tileType = 1;

        if (Input.GetKey(KeyCode.LeftArrow))
        {
            randomFillPercent -= 1;
        }
        if (Input.GetKey(KeyCode.RightArrow))
        {
            randomFillPercent += 1;
        }
        if (Input.GetKey(KeyCode.UpArrow))
        {
            dungeonMinSize += 5;
        }
        if (Input.GetKey(KeyCode.DownArrow))
        {
            dungeonMinSize -= 5;
        }
        if (Input.GetKey(KeyCode.Equals))
        {
            iterations += 1;
        }
        if (Input.GetKey(KeyCode.Minus))
        {
            iterations -= 1;
        }

        /*else
        {
            if(counter>fps){
                counter = 0;
            SmoothMap();
            iterations += 1;
            }
        }

        if(iterations>iterationMax){
            iterations = 0;
            GenerateMap();

        }*/
    }

    void GenerateMap()
    {
        noiseMap = new int[width, height];
        cellMap = new int[width, height];

        RandomFillMap();
        for (int i = 0; i < iterations; i++)
        {
            SmoothMap();
        }
        FillInvalidRegions (tileType, dungeonMinSize);
    }

    List<List<Coord>> FindValidRegions(int tileType, int minSize)
    {
        List<List<Coord>> allRegions = GetRegions(tileType);
        List<List<Coord>> validRegions = new List<List<Coord>>();

        foreach (List<Coord> region in allRegions)
        {
            if (region.Count >= minSize)
            {
                validRegions.Add (region);
            }
        }
        return validRegions;
    }

    List<List<Coord>> FindInvalidRegions(int tileType, int minSize)
    {
        List<List<Coord>> allRegions = GetRegions(tileType);
        List<List<Coord>> invalidRegions = new List<List<Coord>>();

        foreach (List<Coord> region in allRegions)
        {
            if (region.Count < minSize)
            {
                invalidRegions.Add (region);
            }
        }
        return invalidRegions;
    }

    void FillInvalidRegions(int tileType, int minSize)
    {
        List<List<Coord>> invalidRegions =
            FindInvalidRegions(tileType, minSize);
        foreach (List<Coord> region in invalidRegions)
        {
            foreach (Coord tile in region)
            {
                if (tileType == 1)
                {
                    noiseMap[tile.tileX, tile.tileY] = 0;
                }
                else
                    noiseMap[tile.tileX, tile.tileY] = 1;
            }
        }
    }

    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] mapFlags = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (mapFlags[x, y] == 0 && noiseMap[x, y] == tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add (newRegion);
                    foreach (Coord tile in newRegion)
                    {
                        mapFlags[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }

        return regions;
    }

    List<Coord> GetRegionTiles(int startX, int startY)
    {
        List<Coord> tiles = new List<Coord>();
        int[,] mapFlags = new int[width, height];
        int tileType = noiseMap[startX, startY];

        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(startX, startY));
        mapFlags[startX, startY] = 1;
        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            tiles.Add (tile);
            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (
                        IsInMapRange(x, y) &&
                        (y == tile.tileY || x == tile.tileX)
                    )
                    {
                        if (mapFlags[x, y] == 0 && noiseMap[x, y] == tileType)
                        {
                            mapFlags[x, y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }
        return tiles;
    }

    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            float random = Random.Range(0f, 1000f);
            seed = (Time.time + (int) random * random).ToString();
        }
        System.Random prng = new System.Random(seed.GetHashCode());

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                {
                    noiseMap[x, y] = 1;
                }
                else
                    noiseMap[x, y] =
                        (prng.Next(0, 100) < randomFillPercent) ? 1 : 0;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (noiseMap != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gizmos.color =
                        (noiseMap[x, y] == 1) ? Color.blue : Color.green;
                    Vector3 pos =
                        new Vector3(-width / 2 + x + .5f,
                            -height / 2 + y + .5f,
                            0);
                    Gizmos.DrawCube(pos, Vector3.one);
                }
            }
        }
    }

    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighborWallCount = GetSurroundingWallCount(x, y);

                //Game of Life Rules
                /*if(neighborWallCount >2 && noiseMap[x,y] == 1){
                        cellMap[x,y] = 0;
                    }
                    else if((neighborWallCount == 2 || neighborWallCount == 3) && noiseMap[x,y] == 1){
                        cellMap[x,y] = 1;
                    }
                    else if ((neighborWallCount>3) && noiseMap[x,y] == 1){
                        cellMap[x,y] = 0;
                    }
                    else if(neighborWallCount == 3 && noiseMap[x,y] == 0){
                        cellMap[x,y] = 1;
                    }*/
                if (neighborWallCount > 4)
                {
                    cellMap[x, y] = 1;
                }
                else
                {
                    cellMap[x, y] = 0;
                }
            }
        }
        noiseMap = cellMap;
        cellMap = new int[width, height];
    }

    bool IsInMapRange(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
        {
            for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                if (IsInMapRange(neighborX, neighborY))
                {
                    if (neighborX != gridX || neighborY != gridY)
                    {
                        wallCount += noiseMap[neighborX, neighborY];
                    }
                }
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    struct Coord
    {
        public int tileX;

        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }
}
