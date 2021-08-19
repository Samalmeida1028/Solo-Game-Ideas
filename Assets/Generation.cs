using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class Generation : MonoBehaviour
{
    public float fps;

    float counter;

    public int tileType;
    public bool isDijkstra;

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

    int[,] mapSteps;

    int[,] dijkstraMapPlayerPos;

    public Grid foregroundGrid;
    public Tile wallTile;
    public Tile floorTile;
    public Tile oreIndicator;
    public Tile enemyIndicator;
    public Tile playerIndicator;

    public Tilemap foregroundTiles;
    public Tilemap backgroundTiles;


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
        foregroundTiles.ClearAllTiles();
        backgroundTiles.ClearAllTiles();

        noiseMap = new int[width, height];
        cellMap = new int[width, height];
        dijkstraMapPlayerPos = new int[width,height];
        mapSteps = new int[width, height];

        RandomFillMap();
        for (int i = 0; i < iterations; i++)
        {
            SmoothMap();
        }
        FillInvalidRegions (tileType, dungeonMinSize);
        List<List<Coord>> regions = GetRegions(tileType);
        List<Room> validRooms = new List<Room>();
        foreach (List<Coord> Room in regions)
        {
            validRooms.Add(new Room(Room, noiseMap));
        }
        validRooms.Sort();
        validRooms[0].isMainRoom = true;
        validRooms[0].isAccessibleFromMain = true;
        Coord startPos = GenerateRandomStart(validRooms[0].roomTiles);
        GenerateStepsFromStart(startPos.tileX, startPos.tileY,false);
        ConnectClosestRoom (validRooms);
        GenerateStepsFromStart(startPos.tileX, startPos.tileY,true);
        
        List<List<Coord>> wallRegions = GetRegions(1);
        List<List<Coord>> floorMap = GetRegions(0);
        foreach(List<Coord> wallRegion in wallRegions){
            foreach(Coord wall in wallRegion){
                dijkstraMapPlayerPos[wall.tileX,wall.tileY]=Int32.MaxValue;
            }
        }
        TilePlacer();
        OrePlacer();
        EnemySpawner(floorMap,startPos);
        //foregroundTiles.RefreshAllTiles();


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
            float random = UnityEngine.Random.Range(0f, 1000f);
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
        float range = 0;
        Color gradient = new Color();
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
                    if(!isDijkstra){
                    range -= mapSteps[x, y] / 300;
                    }
                    else  range -= dijkstraMapPlayerPos[x, y] / 300;


                    if (Math.Abs(range) < 5)
                    {
                        Gizmos.color  = Color.white;
                    }
                    
                    else if (Math.Abs(range) >= Mathf.Abs(5) && Math.Abs(range) < Mathf.Abs(10))
                    {
                         Gizmos.color  = Color.magenta;
                    }
                    else if (Math.Abs(range) >= Mathf.Abs(10) && Math.Abs(range) < Mathf.Abs(15))
                    {
                         Gizmos.color  = Color.red;
                    }
                    else if (Math.Abs(range) >= Mathf.Abs(15) && Math.Abs(range) < Mathf.Abs(20))
                    {
                         Gizmos.color  = Color.yellow;
                    }
                    else if (Math.Abs(range) >= Mathf.Abs(20) && Math.Abs(range) < Mathf.Abs(25))
                    {
                         Gizmos.color = Color.green;
                    }
                    else if (Math.Abs(range) >= Mathf.Abs(25) && Math.Abs(range) < Mathf.Abs(30))
                    {
                         Gizmos.color = Color.cyan;
                    }
                    else if (Math.Abs(range) >= Mathf.Abs(30) && Math.Abs(range) < Mathf.Abs(35))
                    {
                         Gizmos.color = Color.gray;
                    }
                    else if (Math.Abs(range) >= Mathf.Abs(35) && Math.Abs(range) < Mathf.Abs(60))
                    {
                         Gizmos.color = Color.black;
                    }
                    //gradient.g -= mapSteps[x,y]/1000;
                    //gradient.b -= mapSteps[x,y]/1000;
                    //Debug.Log(mapSteps[x,y]);
                    if(!isDijkstra){
                    if (mapSteps[x, y] == 0)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(pos, Vector3.one * 1.5f);
                    }
                    }
                    else{
                        if (dijkstraMapPlayerPos[x, y] == 0)
                    {
                        Gizmos.color = Color.red;
                        Gizmos.DrawCube(pos, Vector3.one * 1.5f);
                    }

                    }
                    Gizmos.DrawCube(pos, Vector3.one / 3);
                    range = 0;
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

    void ConnectClosestRoom(List<Room> allRooms, bool forceAccess = false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> roomListB = new List<Room>();
        if (forceAccess)
        {
            foreach (Room room in allRooms)
            {
                if (room.isAccessibleFromMain)
                {
                    roomListB.Add (room);
                }
                else
                    roomListA.Add(room);
            }
        }
        else
        {
            roomListA = allRooms;
            roomListB = allRooms;
        }

        int bestDistance = 0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;

        foreach (Room roomA in roomListA)
        {
            if (!forceAccess)
            {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0)
                {
                    continue;
                }
            }

            foreach (Room roomB in roomListB)
            {
                if (roomA == roomB || roomA.isConnected(roomB))
                {
                    continue;
                }

                for (
                    int tileIndexA = 0;
                    tileIndexA < roomA.edgeTiles.Count;
                    tileIndexA++
                )
                {
                    for (
                        int tileIndexB = 0;
                        tileIndexB < roomB.edgeTiles.Count;
                        tileIndexB++
                    )
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distanceBetweenRooms =
                            (
                            int
                            )(Mathf.Pow((tileA.tileX - tileB.tileX), 2) +
                            Mathf.Pow((tileA.tileY - tileB.tileY), 2));
                        if (
                            distanceBetweenRooms < bestDistance ||
                            !possibleConnectionFound
                        )
                        {
                            bestDistance = distanceBetweenRooms;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }
            if (possibleConnectionFound && !forceAccess)
            {
                CreatePassage (bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }
        if (possibleConnectionFound && forceAccess)
        {
            CreatePassage (bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRoom(allRooms, true);
        }

        if (!forceAccess)
        {
            ConnectClosestRoom(allRooms, true);
        }
    }

    void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms (roomA, roomB);

        //Debug.DrawLine(CoordToPos(tileA), CoordToPos(tileB), Color.black, 100f);
        List<Coord> line = GetLine(tileA, tileB);
        foreach (Coord c in line)
        {
            DrawCircle(c, 2);
        }
    }

    void DrawCircle(Coord c, int r)
    {
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y < r * r)
                {
                    int realX = c.tileX + x;
                    int realY = c.tileY + y;
                    if (IsInMapRange(realX, realY))
                    {
                        noiseMap[realX, realY] = 0;
                    }
                }
            }
        }
    }

    List<Coord> GetLine(Coord tileA, Coord tileB)
    {
        List<Coord> line = new List<Coord>();
        int x = tileA.tileX;
        int y = tileA.tileY;

        int dx = tileB.tileX - x;
        int dy = tileB.tileY - y;

        bool inverted = false;
        int step = Math.Sign(dx);
        int gradientStep = Math.Sign(dy);

        int longest = Math.Abs(dx);
        int shortest = Math.Abs(dy);

        if (longest < shortest)
        {
            inverted = true;
            longest = Math.Abs(dy);
            shortest = Math.Abs(dx);
            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coord(x, y));
            if (inverted)
            {
                y += step;
            }
            else
            {
                x += step;
            }
            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                    y += gradientStep;
                gradientAccumulation -= longest;
            }
        }

        return line;
    }

    Vector3 CoordToPos(Coord tile)
    {
        return new Vector3(-width / 2 + .5f + tile.tileX,
            -height / 2 + .5f + tile.tileY,
            0);
    }

    class Room : IComparable<Room>
    {
        public bool isAccessibleFromMain;

        public bool isMainRoom;

        public List<Coord> roomTiles;

        public List<Coord> edgeTiles;

        public List<Room> connectedRooms;

        public int roomSize;

        public Room()
        {
        }

        public Room(List<Coord> tiles, int[,] map)
        {
            roomTiles = tiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();
            edgeTiles = new List<Coord>();
            foreach (Coord tile in roomTiles)
            {
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if (x == tile.tileX || y == tile.tileY)
                        {
                            if (map[x, y] == 1)
                            {
                                edgeTiles.Add (tile);
                            }
                        }
                    }
                }
            }
        }

        public void SetAccessibleToMain()
        {
            if (!isAccessibleFromMain)
            {
                isAccessibleFromMain = true;
                foreach (Room connectedRoom in connectedRooms)
                {
                    isAccessibleFromMain = true;
                }
            }
        }

        public int CompareTo(Room otherRoom)
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }

        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.isAccessibleFromMain)
            {
                roomB.SetAccessibleToMain();
            }
            else if (roomB.isAccessibleFromMain)
            {
                roomA.SetAccessibleToMain();
            }

            roomA.connectedRooms.Add (roomB);
            roomB.connectedRooms.Add (roomA);
        }

        public bool isConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }
    }

    Coord GenerateRandomStart(List<Coord> mainRoom)
    {
        Coord startPos = mainRoom[UnityEngine.Random.Range(0, mainRoom.Count)];
        return startPos;
    }

    void GenerateStepsFromStart(int startX, int startY, bool dijkStra)
    {
        Queue<Coord> queue = new Queue<Coord>();
        int count = 1;
        queue.Enqueue(new Coord(startX, startY));
        int[,] mapFlags = new int[width, height];

        mapFlags[startX, startY] = 1;
        mapSteps[startX, startY] = 0;
        if(dijkStra){
            dijkstraMapPlayerPos[startX,startY] = 0;
        }

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if(!dijkStra){
                    if (
                        IsInMapRange(x, y) &&
                        (y == tile.tileY || x == tile.tileX)
                    )
                    {
                        if (mapFlags[x, y] == 0)
                        {
                            mapSteps[x, y] = count;
                            queue.Enqueue(new Coord(x, y));
                            mapFlags[x, y] = 1;
                        }
                    }
                    }
                    else{
                        if (
                        IsInMapRange(x, y) &&
                        (y == tile.tileY || x == tile.tileX)
                    )
                    {
                        if (mapFlags[x, y] == 0 && noiseMap[x,y] == noiseMap[startX,startY])
                        {
                            dijkstraMapPlayerPos[x, y] = count;
                            queue.Enqueue(new Coord(x, y));
                            mapFlags[x, y] = 1;
                        }

                    }

                    }
                }
            }
            count += 1;
        }
    }

    void TilePlacer(){
        for(int tileX = 0; tileX < noiseMap.GetLength(0); tileX++){
            for(int tileY= 0; tileY< noiseMap.GetLength(0); tileY++){
            if(noiseMap[tileX,tileY] == 0){
                backgroundTiles.SetTile(new Vector3Int(-width / 2 + tileX,-height / 2 + tileY,0),floorTile);
            }
            if(noiseMap[tileX,tileY] == 1){
                foregroundTiles.SetTile(new Vector3Int(-width / 2 + tileX,-height / 2 + tileY,0),wallTile);
            }
            }
        }
    }

    void OrePlacer(){

        for(int tileX = 0; tileX < noiseMap.GetLength(0); tileX++){
            for(int tileY= 0; tileY< noiseMap.GetLength(0); tileY++){
                        int spawnChance = (int)(UnityEngine.Random.Range(0f,1f) * (Mathf.Pow(mapSteps[tileX,tileY],UnityEngine.Random.Range(1f,1.2f))/500));
                        if(spawnChance>15 && spawnChance<23 && noiseMap[tileX,tileY] == 1){
                            //Debug.Log("hello");
                            //foregroundTiles.SetTile(new Vector3Int(-width / 2 + tileX,-height / 2 + tileY,0),null);
                            foregroundTiles.SetTile(new Vector3Int(-width / 2 + tileX,-height / 2 + tileY,0),oreIndicator);
                        }
                    }
                }

    }

    void EnemySpawner(List<List<Coord>> floorMap, Coord startPos){
        List<Coord> floor = floorMap[0];
        foreach ( Coord floorTile in floor)
        {
            if(floorTile.tileX == startPos.tileX && floorTile.tileY == startPos.tileY){
                foregroundTiles.SetTile(new Vector3Int(-width / 2 + floorTile.tileX,-height / 2 + floorTile.tileY,0),playerIndicator);
            }
            int spawnChance = (int)(UnityEngine.Random.Range(0f,.1f) * (Mathf.Pow(dijkstraMapPlayerPos[floorTile.tileX,floorTile.tileY],UnityEngine.Random.Range(1f,2f)))/1000);
            Debug.Log(spawnChance);
            if(spawnChance>=7 && spawnChance<11){
                foregroundTiles.SetTile(new Vector3Int(-width / 2 + floorTile.tileX,-height / 2 + floorTile.tileY,0),enemyIndicator);
            }

        }
    }

}
