using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Generation : MonoBehaviour
{
    public float fps;

    float counter;

    public int

            width,
            height;

    public int iterations;

    public string seed;

    public bool useRandomSeed;

    [Range(0, 100)]
    public int randomFillPercent;

    int[,] noiseMap;

    int[,] cellMap;

    void Start()
    {
        fps = 1 / fps;
        GenerateMap();
    }

    void FixedUpdate()
    {
        if (Input.GetMouseButtonDown(1))
        {
            GenerateMap();
        }
        else
        {
            if(Input.GetMouseButton(0)){
            SmoothMap();
            }
        }
        /*counter += Time.fixedDeltaTime;
        if(counter>fps){
            counter = 0;
            SmoothMap();
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

    int GetSurroundingWallCount(int gridX, int gridY)
    {
        int wallCount = 0;
        for (int neighborX = gridX - 1; neighborX <= gridX + 1; neighborX++)
        {
            for (int neighborY = gridY - 1; neighborY <= gridY + 1; neighborY++)
            {
                if (
                    neighborX >= 0 &&
                    neighborX < width &&
                    neighborY >= 0 &&
                    neighborY < height
                )
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
}
