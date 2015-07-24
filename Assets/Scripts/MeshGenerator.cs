using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class MeshGenerator : MonoBehaviour
    {
        public SquareGrid squareGrid;
        public List<Vector3> Vertices;
        public List<int> Triangles;
        public bool is2d;

        public MeshFilter cave;

        private Dictionary<int, List<Triangle>> triangleDictionary = new Dictionary<int, List<Triangle>>();
        private List<List<int>> outlines = new List<List<int>>();
        private HashSet<int> checkedVertices = new HashSet<int>();

        public MeshFilter walls = new MeshFilter();

        public void GenerateMesh(int[,] map, float squareSize)
        {
            outlines.Clear();
            checkedVertices.Clear();
            triangleDictionary.Clear();
            Vertices = new List<Vector3>();
            Triangles = new List<int>();
            squareGrid = new SquareGrid(map, squareSize);
            for (var x = 0; x < squareGrid.Squares.GetLength(0); x++)
            {
                for (var y = 0; y < squareGrid.Squares.GetLength(1); y++)
                {
                    TriangulateSquare(squareGrid.Squares[x, y]);
                }
            }
            var mesh = new Mesh();
            cave.mesh = mesh;

            mesh.vertices = Vertices.ToArray();
            mesh.triangles = Triangles.ToArray();
            mesh.RecalculateNormals();
            int tileAmount = 10;
            Vector2[] uvs = new Vector2[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                float percentX = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, Vertices[i].x) * tileAmount;
                float percentY = Mathf.InverseLerp(-map.GetLength(0) / 2 * squareSize, map.GetLength(0) / 2 * squareSize, Vertices[i].z) * tileAmount;
                uvs[i] = new Vector2(percentX, percentY);
            }
            mesh.uv = uvs;
            if (!is2d)
            {
                CreateWallMesh();
            }
            else
            {
                Generate2dColliders();
            }
        }

        void Generate2dColliders()
        {
            EdgeCollider2D[] currentColliders = gameObject.GetComponents<EdgeCollider2D>();
            for (int i = 0; i < currentColliders.Length; i++)
            {
                Destroy(currentColliders[i]);
            }

            CalculateMeshOutlines();

            foreach (List<int> outline in outlines)
            {
                EdgeCollider2D edgeCollider = gameObject.AddComponent<EdgeCollider2D>();
                Vector2[] edgePoints = new Vector2[outline.Count];

                for (int i = 0; i < outline.Count; i++)
                {
                    edgePoints[i] = new Vector2(Vertices[outline[i]].x, Vertices[outline[i]].z);
                }
                edgeCollider.points = edgePoints;
            }

        }
        void CreateWallMesh()
        {

            CalculateMeshOutlines();

            var wallVertices = new List<Vector3>();
            var wallTriangles = new List<int>();
            var wallMesh = new Mesh();
            float wallHeight = 5;

            foreach (var outline in outlines)
            {
                for (var i = 0; i < outline.Count - 1; i++)
                {
                    var startIndex = wallVertices.Count;
                    wallVertices.Add(Vertices[outline[i]]); // left
                    wallVertices.Add(Vertices[outline[i + 1]]); // right
                    wallVertices.Add(Vertices[outline[i]] - Vector3.up * wallHeight); // bottom left
                    wallVertices.Add(Vertices[outline[i + 1]] - Vector3.up * wallHeight); // bottom right

                    wallTriangles.Add(startIndex + 0);
                    wallTriangles.Add(startIndex + 2);
                    wallTriangles.Add(startIndex + 3);

                    wallTriangles.Add(startIndex + 3);
                    wallTriangles.Add(startIndex + 1);
                    wallTriangles.Add(startIndex + 0);
                }
            }
            wallMesh.vertices = wallVertices.ToArray();
            wallMesh.triangles = wallTriangles.ToArray();
            walls.mesh = wallMesh;
            var wallCollider = walls.gameObject.AddComponent<MeshCollider>();
            wallCollider.sharedMesh = wallMesh;
        }
        /*public void OnDrawGizmos()
        {
            if (squareGrid == null) return;
            for (var x = 0; x < squareGrid.Squares.GetLength(0); x++)
            {
                for (var y = 0; y < squareGrid.Squares.GetLength(1); y++)
                {
                    Gizmos.color = (squareGrid.Squares[x, y].TopLeft.Active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.Squares[x, y].TopLeft.Position, Vector3.one * 0.4f);

                    Gizmos.color = (squareGrid.Squares[x, y].TopRight.Active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.Squares[x, y].TopRight.Position, Vector3.one * 0.4f);

                    Gizmos.color = (squareGrid.Squares[x, y].BottomLeft.Active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.Squares[x, y].BottomLeft.Position, Vector3.one * 0.4f);

                    Gizmos.color = (squareGrid.Squares[x, y].BottomRight.Active) ? Color.black : Color.white;
                    Gizmos.DrawCube(squareGrid.Squares[x, y].BottomRight.Position, Vector3.one * 0.4f);

                    Gizmos.color = Color.gray;
                    Gizmos.DrawCube(squareGrid.Squares[x, y].CenterTop.Position, Vector3.one * 0.15f);
                    Gizmos.DrawCube(squareGrid.Squares[x, y].CenterRight.Position, Vector3.one * 0.15f);
                    Gizmos.DrawCube(squareGrid.Squares[x, y].CenterBottom.Position, Vector3.one * 0.15f);
                    Gizmos.DrawCube(squareGrid.Squares[x, y].CenterLeft.Position, Vector3.one * 0.15f);
                }
            }
        }*/

        void TriangulateSquare(Square square)
        {

            switch (square.Configuration)
            {
                case 0:
                    break;

                // 1 points:
                case 1:
                    MeshFromPoints(square.CenterLeft, square.CenterBottom, square.BottomLeft);
                    break;
                case 2:
                    MeshFromPoints(square.BottomRight, square.CenterBottom, square.CenterRight);
                    break;
                case 4:
                    MeshFromPoints(square.TopRight, square.CenterRight, square.CenterTop);
                    break;
                case 8:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterLeft);
                    break;

                // 2 points:
                case 3:
                    MeshFromPoints(square.CenterRight, square.BottomRight, square.BottomLeft, square.CenterLeft);
                    break;
                case 6:
                    MeshFromPoints(square.CenterTop, square.TopRight, square.BottomRight, square.CenterBottom);
                    break;
                case 9:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterBottom, square.BottomLeft);
                    break;
                case 12:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.CenterRight, square.CenterLeft);
                    break;
                case 5:
                    MeshFromPoints(square.CenterTop, square.TopRight, square.CenterRight, square.CenterBottom, square.BottomLeft, square.CenterLeft);
                    break;
                case 10:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterRight, square.BottomRight, square.CenterBottom, square.CenterLeft);
                    break;

                // 3 point:
                case 7:
                    MeshFromPoints(square.CenterTop, square.TopRight, square.BottomRight, square.BottomLeft, square.CenterLeft);
                    break;
                case 11:
                    MeshFromPoints(square.TopLeft, square.CenterTop, square.CenterRight, square.BottomRight, square.BottomLeft);
                    break;
                case 13:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.CenterRight, square.CenterBottom, square.BottomLeft);
                    break;
                case 14:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.BottomRight, square.CenterBottom, square.CenterLeft);
                    break;

                // 4 point:
                case 15:
                    MeshFromPoints(square.TopLeft, square.TopRight, square.BottomRight, square.BottomLeft);
                    checkedVertices.Add(square.TopLeft.VertexIndex);
                    checkedVertices.Add(square.TopRight.VertexIndex);
                    checkedVertices.Add(square.BottomRight.VertexIndex);
                    checkedVertices.Add(square.BottomLeft.VertexIndex);
                    break;
            }

        }

        void MeshFromPoints(params Node[] points)
        {
            AssigneVertices(points);
            if (points.Length >= 3)
            {
                CreateTriangle(points[0], points[1], points[2]);
            }
            if (points.Length >= 4)
            {
                CreateTriangle(points[0], points[2], points[3]);
            }
            if (points.Length >= 5)
            {
                CreateTriangle(points[0], points[3], points[4]);
            }
            if (points.Length >= 6)
            {
                CreateTriangle(points[0], points[4], points[5]);
            }
        }

        private void AssigneVertices(Node[] points)
        {
            foreach (var point in points)
            {
                if (point.VertexIndex == -1)
                {
                    point.VertexIndex = Vertices.Count;
                    Vertices.Add(point.Position);
                }
            }
        }

        void CreateTriangle(Node a, Node b, Node c)
        {
            Triangles.Add(a.VertexIndex);
            Triangles.Add(b.VertexIndex);
            Triangles.Add(c.VertexIndex);

            var triangle = new Triangle(a.VertexIndex, b.VertexIndex, c.VertexIndex);
            AddTriangleToDictionary(triangle.vertexIndexA, triangle);
            AddTriangleToDictionary(triangle.vertexIndexB, triangle);
            AddTriangleToDictionary(triangle.vertexIndexC, triangle);
        }


        int GetConnectedOutlineVertex(int vertexIndex)
        {
            var trianglesContainingVertex = triangleDictionary[vertexIndex];

            foreach (var triangle in trianglesContainingVertex)
            {
                for (var j = 0; j < 3; j++)
                {
                    var vertexB = triangle[j];
                    if (vertexB != vertexIndex && !checkedVertices.Contains(vertexB))
                    {
                        if (IsOutlineEdge(vertexIndex, vertexB))
                        {
                            return vertexB;
                        }
                    }
                }
            }

            return -1;
        }

        bool IsOutlineEdge(int vertexA, int vertexB)
        {
            var count = triangleDictionary[vertexA].Intersect(triangleDictionary[vertexB]).Count();
            return count == 1;
        }

        void AddTriangleToDictionary(int vertexIndex, Triangle triangle)
        {
            if (triangleDictionary.ContainsKey(vertexIndex))
            {
                triangleDictionary[vertexIndex].Add(triangle);
            }
            else
            {
                triangleDictionary[vertexIndex] = new List<Triangle> {triangle};
            }
        }
        void CalculateMeshOutlines()
        {

            for (var vertexIndex = 0; vertexIndex < Vertices.Count; vertexIndex++)
            {
                if (checkedVertices.Contains(vertexIndex)) continue;
                var newOutlineVertex = GetConnectedOutlineVertex(vertexIndex);
                if (newOutlineVertex == -1) continue;
                checkedVertices.Add(vertexIndex);

                var newOutline = new List<int> {vertexIndex};
                outlines.Add(newOutline);
                FollowOutline(newOutlineVertex, outlines.Count - 1);
                outlines[outlines.Count - 1].Add(vertexIndex);
            }
        }

        void FollowOutline(int vertexIndex, int outlineIndex)
        {
            outlines[outlineIndex].Add(vertexIndex);
            checkedVertices.Add(vertexIndex);
            int nextVertex = GetConnectedOutlineVertex(vertexIndex);

            if (nextVertex != -1)
            {
                FollowOutline(nextVertex, outlineIndex);
            }
        }


    }
    public class SquareGrid
    {
        public Square[,] Squares;

        public SquareGrid(int[,] map, float squareSize)
        {
            var nodeCountX = map.GetLength(0);
            var nodeCountY = map.GetLength(1);
            var mapWidth = nodeCountX * squareSize;
            var mapHeight = nodeCountY * squareSize;

            var controlNodes = new ControlNode[nodeCountX, nodeCountY];
            for (var x = 0; x < nodeCountX; x++)
            {
                for (var y = 0; y < nodeCountY; y++)
                {
                    var pos = new Vector3(-mapWidth / 2 + x * squareSize + squareSize / 2, 0,
                        -mapHeight / 2 + y * squareSize + squareSize / 2);
                    controlNodes[x, y] = new ControlNode(pos, map[x, y] == 1, squareSize);
                }
            }
            Squares = new Square[nodeCountX - 1, nodeCountY - 1];
            for (var x = 0; x < nodeCountX - 1; x++)
            {
                for (var y = 0; y < nodeCountY - 1; y++)
                {
                    Squares[x, y] = new Square(controlNodes[x, y + 1], controlNodes[x + 1, y + 1],
                        controlNodes[x, y], controlNodes[x + 1, y]);
                }

            }
        }
    }

    public class Square
    {
        public ControlNode TopLeft, TopRight, BottomLeft, BottomRight;
        public Node CenterTop, CenterRight, CenterBottom, CenterLeft;
        public int Configuration = 0;


        public Square(ControlNode topLeft, ControlNode topRight, ControlNode bottomLeft, ControlNode bottomRight)
        {
            TopLeft = topLeft;
            TopRight = topRight;
            BottomLeft = bottomLeft;
            BottomRight = bottomRight;

            CenterTop = topLeft.Right;
            CenterRight = bottomRight.Above;
            CenterBottom = bottomLeft.Right;
            CenterLeft = bottomLeft.Above;

            Configuration += Convert.ToInt32(TopLeft.Active) << 3;
            Configuration += Convert.ToInt32(TopRight.Active) << 2;
            Configuration += Convert.ToInt32(BottomRight.Active) << 1;
            Configuration += Convert.ToInt32(BottomLeft.Active);
        }
    }
    public class Node
    {
        public Vector3 Position;
        public int VertexIndex = -1;

        public Node(Vector3 position)
        {
            Position = position;
        }
    }

    public class ControlNode : Node
    {
        public bool Active;
        public Node Above, Right;

        public ControlNode(Vector3 position, bool active, float squareSize)
            : base(position)
        {
            Active = active;
            Above = new Node(position + Vector3.forward * squareSize / 2f);
            Right = new Node(position + Vector3.right * squareSize / 2f);

        }
    }

    public struct Triangle
    {
        public int vertexIndexA;
        public int vertexIndexB;
        public int vertexIndexC;
        private int[] vertices;

        public int this[int i]
        {
            get
            {
                return vertices[i];
            }
        }


        public bool Contains(int vertexIndex)
        {
            return vertexIndex == vertexIndexA || vertexIndex == vertexIndexB || vertexIndex == vertexIndexC;
        }

        public Triangle(int a, int b, int c)
        {
            vertexIndexA = a;
            vertexIndexB = b;
            vertexIndexC = c;

            vertices = new int[3];
            vertices[0] = a;
            vertices[1] = b;
            vertices[2] = c;
        }
    }
}
