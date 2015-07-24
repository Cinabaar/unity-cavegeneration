using System;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using Random = System.Random;

namespace Assets.Scripts
{
    public class MapGenerator : MonoBehaviour
    {
        public int width;
        public int height;
        [Range(0, 100)]
        public int randomFillPercent;
        public int wallThresholdSize = 50;
        public int roomThresholdSize = 50;

        public string Seed;
        public bool UseRandomSeed;

        private int[,] map;

        void Start()
        {
            GenerateMap();
        }

        void GenerateMap()
        {
            map = new int[width, height];
            RandomFillMap();

            for (var i = 0; i < 5; i++)
            {
                SmoothMap();
            }

            ProcessMap();

            var borderSize = 5;
            var borderedMap = new int[width + borderSize * 2, height + borderSize * 2];

            for (var x = 0; x < borderedMap.GetLength(0); x++)
            {
                for (var y = 0; y < borderedMap.GetLength(1); y++)
                {
                    if (x >= borderSize && x < width + borderSize && y >= borderSize && y < height + borderSize)
                    {
                        borderedMap[x, y] = map[x - borderSize, y - borderSize];
                    }
                    else
                    {
                        borderedMap[x, y] = 1;
                    }
                }
            }
            var meshGen = GetComponent<MeshGenerator>();
            meshGen.GenerateMesh(borderedMap, 1);
        }

        void RandomFillMap()
        {
            Seed = UseRandomSeed ? Guid.NewGuid().ToString() : Seed;

            var rng = new Random(Seed.GetHashCode());
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (x == 0 || x == width - 1 || y == 0 || y == height - 1)
                    {
                        map[x, y] = 1;
                    }
                    else
                    {
                        map[x, y] = (rng.Next(0, 100) < randomFillPercent) ? 1 : 0;
                    }
                }
            }
        }

        void SmoothMap()
        {
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    var neighbourWallTiles = GetSurroundingWallCount(x, y);
                    if (neighbourWallTiles > 4)
                    {
                        map[x, y] = 1;
                    }
                    else if (neighbourWallTiles < 4)
                    {
                        map[x, y] = 0;
                    }
                }
            }
        }

        int GetSurroundingWallCount(int x, int y)
        {
            var wallCount = 0;
            for (var nx = x - 1; nx <= x + 1; nx++)
            {
                for (var ny = y - 1; ny <= y + 1; ny++)
                {
                    if (nx == x && ny == y) continue;
                    if (IsInMapRange(nx, ny))
                    {
                        wallCount += map[nx, ny];
                    }
                    else
                    {
                        wallCount++;
                    }
                }
            }
            return wallCount;
        }

        bool IsInMapRange(int x, int y)
        {
            return x >= 0 && x < width && y >= 0 && y < height;
        }
        void OnDrawGizmos()
        {
            /*if (map == null) return;
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    Gizmos.color = (map[x, y] == 1) ? Color.black : Color.white;
                    var pos = new Vector3(-width/2.0f + x + 0.5f, -height/2.0f + y+0.5f);
                    Gizmos.DrawCube(pos, Vector3.one);
                }
            }*/
        }

        void Update()
        {
            if (Input.GetMouseButton(0))
            {
                GenerateMap();
            }
        }

        List<Coord> GetRegionTiles(int startX, int startY)
        {
            var tiles = new List<Coord>();
            var mapFlags = new int[width, height];
            var tileType = map[startX, startY];

            var queue = new Queue<Coord>();
            queue.Enqueue(new Coord(startX, startY));
            mapFlags[startX, startY] = 1;

            while (queue.Count > 0)
            {
                var tile = queue.Dequeue();
                tiles.Add(tile);

                for (var x = tile.x - 1; x <= tile.x + 1; x++)
                {
                    for (var y = tile.y - 1; y <= tile.y + 1; y++)
                    {
                        if (!IsInMapRange(x, y) || (y != tile.y && x != tile.x)) continue;
                        if (mapFlags[x, y] != 0 || map[x, y] != tileType) continue;

                        mapFlags[x, y] = 1;
                        queue.Enqueue(new Coord(x, y));
                    }
                }
            }
            return tiles;
        }

        List<List<Coord>> GetRegions(int tileType)
        {
            var regions = new List<List<Coord>>();
            var mapFlags = new int[width, height];
            for (var x = 0; x < width; x++)
            {
                for (var y = 0; y < height; y++)
                {
                    if (mapFlags[x, y] != 0 || map[x, y] != tileType) continue;
                    var newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);
                    foreach (var tile in newRegion)
                    {
                        mapFlags[tile.x, tile.y] = 1;
                    }
                }
            }
            return regions;
        }

        void ProcessMap()
        {
            var walls = GetRegions(1);
            foreach (var wall in walls)
            {
                if (wall.Count >= wallThresholdSize) continue;
                foreach (var tile in wall)
                {
                    map[tile.x, tile.y] = 0;
                }
            }
            var rooms = GetRegions(0);
            var survivingRooms = new List<Room>();
            foreach (var room in rooms)
            {
                if (room.Count < roomThresholdSize)
                {
                    foreach (var tile in room)
                    {
                        map[tile.x, tile.y] = 1;
                    }
                }
                else
                {
                    survivingRooms.Add(new Room(room, map));
                }
            }
            survivingRooms.Sort();
            survivingRooms[0].isAccesableFromMainRoom = true;
            survivingRooms[0].isMainRoom = true;
            ConnectClosestRooms(survivingRooms);
        }

        void ConnectClosestRooms(List<Room> allRooms, bool forceAccessabilityToMainRoom = false)
        {
            var roomListA = new List<Room>();
            var roomListB = new List<Room>();

            if (forceAccessabilityToMainRoom)
            {
                foreach (var room in allRooms)
                {
                    if (room.isAccesableFromMainRoom)
                    {
                        roomListB.Add(room);
                    }
                    else
                    {
                        roomListA.Add(room);
                    }
                }
            }
            else
            {
                roomListA = allRooms;
                roomListB = allRooms;
            }

            var bestDistance = 0f;
            var bestTileA = new Coord();
            var bestTileB = new Coord();
            var bestRoomA = new Room();
            var bestRoomB = new Room();
            var possibleConnectionFound = false;

            foreach (var roomA in roomListA)
            {
                if (!forceAccessabilityToMainRoom)
                {
                    possibleConnectionFound = false;
                    if (roomA.connectedRooms.Count > 0)
                    {
                        continue;
                    }
                }
                foreach (var roomB in roomListB)
                {
                    if (roomA == roomB || roomA.IsConnected(roomB)) continue;
                    
                    for (var tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                    {
                        for (var tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                        {
                            var tileA = roomA.edgeTiles[tileIndexA];
                            var tileB = roomB.edgeTiles[tileIndexB];
                            var distanceBetweenRooms = Mathf.Pow(tileA.x - tileB.x, 2) + Mathf.Pow(tileA.y - tileB.y, 2);
                            if (distanceBetweenRooms < bestDistance || !possibleConnectionFound)
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

                if (possibleConnectionFound && !forceAccessabilityToMainRoom)
                {
                    CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
                }
            }

            if (possibleConnectionFound && forceAccessabilityToMainRoom)
            {
                CreatePassage(bestRoomA, bestRoomB, bestTileA, bestTileB);
                ConnectClosestRooms(allRooms, true);
            }
            if (!forceAccessabilityToMainRoom)
            {
                ConnectClosestRooms(allRooms, true);
            }
        }

        void CreatePassage(Room roomA, Room roomB, Coord tileA, Coord tileB)
        {
            Room.ConnectRooms(roomA, roomB);
            //Debug.DrawLine(CoordToWorldPoint(tileA), CoordToWorldPoint(tileB), Color.green, 100);

            var line = GetLine(tileA, tileB);
            foreach (var c in line)
            {
                DrawCircle(c, 5);
            }
        }

        void DrawCircle(Coord c, int r)
        {
            for (var x = -r; x <= r; x++)
            {
                for (var y = -r; y<= r; y++)
                {
                    if (x*x + y*y <= r*r)
                    {
                        var realX = c.x + x;
                        var realY = c.y + y;
                        if (IsInMapRange(realX, realY))
                        {
                            map[realX, realY] = 0; 
                        }
                    }
                }
            }

        }

        List<Coord> GetLine(Coord from, Coord to)
        {
            var line = new List<Coord>();
            var x = from.x;
            var y = from.y;

            var dx = to.x - x;
            var dy = to.y - y;

            var step = Math.Sign(dx);
            var gradientStep = Math.Sign(dy);

            var longest = Math.Abs(dx);
            var shortest = Math.Abs(dy);

            var inverted = false;
            if (longest < shortest)
            {
                inverted = true;
                longest = Math.Abs(dy);
                shortest = Math.Abs(dx);
                step = Math.Sign(dy);
                gradientStep = Math.Sign(dx);
            }

            var gradientAccum = longest/2;
            for (var i = 0; i < longest; i++)
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
                gradientAccum += shortest;
                if (gradientAccum >= longest)
                {
                    if (inverted)
                    {
                        x += gradientStep;
                    }
                    else
                    {
                        y += gradientStep;
                    }
                    gradientAccum -= longest;
                }
            }
            return line;
        }

        Vector3 CoordToWorldPoint(Coord tile)
        {
            return new Vector3(-width / 2f + 0.5f + tile.x, 2, -height / 2f + 0.5f + tile.y);
        }

        struct Coord
        {
            public int x, y;

            public Coord(int _x, int _y)
            {
                x = _x;
                y = _y;
            }
        }

        private class Room : IComparable<Room>
        {
            public List<Coord> tiles;
            public List<Coord> edgeTiles;
            public List<Room> connectedRooms;
            public int roomSize;
            public bool isAccesableFromMainRoom;
            public bool isMainRoom;

            public Room(List<Coord> roomTiles, int[,] map)
            {
                tiles = roomTiles;
                roomSize = tiles.Count;
                connectedRooms = new List<Room>();

                edgeTiles = new List<Coord>();
                foreach (var tile in tiles)
                {
                    for (var x = tile.x - 1; x <= tile.x + 1; x++)
                    {
                        for (var y = tile.y - 1; y <= tile.y + 1; y++)
                        {
                            if (x != tile.x && y != tile.y) continue;
                            if (map[x, y] == 1)
                            {
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }

            public Room() { }

            public static void ConnectRooms(Room roomA, Room roomB)
            {
                if (roomA.isAccesableFromMainRoom)
                {
                    roomB.SetAccesableFromMainRoom();
                }
                else if (roomB.isAccesableFromMainRoom)
                {
                    roomA.SetAccesableFromMainRoom();
                }
                roomA.connectedRooms.Add(roomB);
                roomB.connectedRooms.Add(roomA);
            }

            public bool IsConnected(Room otherRoom)
            {
                return connectedRooms.Contains(otherRoom);
            }

            public int CompareTo(Room other)
            {
                return other.roomSize.CompareTo(roomSize);
            }

            public void SetAccesableFromMainRoom()
            {
                if (!isAccesableFromMainRoom)
                {
                    isAccesableFromMainRoom = true;
                    foreach (var room in connectedRooms)
                    {
                        room.SetAccesableFromMainRoom();
                    }
                }
            }
        }
    }
}
