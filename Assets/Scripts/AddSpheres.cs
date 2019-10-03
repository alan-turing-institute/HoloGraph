 using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.WSA.Input;

public struct Edge
{
    public static int None = -1;
    public int v; // target vertex
    public int d; // distance
    public int reverse_edge;

    



    public Edge(int v_, int d_, int reverse_edge_)
    {
        v = v_;
        d = d_;
        reverse_edge = reverse_edge_;
    }
    public Edge(int v_, int d_)
    {
        v = v_;
        d = d_;
        reverse_edge = Edge.None;
    }
    public override string ToString() => "Edge{v=" + v + ", d=" + d + "}";
}

// an Edge is identified by its starting vertex and the index into that vertex's edge array
public struct EdgeId
{
    public int v;
    public int e_idx;
    public EdgeId(int v_, int e_idx_) { v = v_; e_idx = e_idx_; }
}

public interface GraphInterface
{
    IEnumerable<Edge> this[int i] { get; }
    int CountEdges(int i);
    int CountNodes();
}

public class Graph : GraphInterface
{
    Edge[][] edges;
    public Graph(Edge[][] edges_)
    {
        edges = edges_;
    }

    public Graph(int[][] edges_unweighted)
    {
        edges = new Edge[edges_unweighted.Length][];
        for (int i = 0; i < edges_unweighted.Length; i++) {
            edges[i] = new Edge[edges_unweighted[i].Length];
            for (int j = 0; j < edges_unweighted[j].Length; j++) {
                edges[i][j] = new Edge(edges_unweighted[i][j], 5);
            }
        }
        
    }

    public IEnumerable<Edge> this[int i]
    {
        get
        {
            return edges[i];
        }
    }

    public System.Tuple<int, int> PrincipalEdge(int from, int edge_id)
    {
        return System.Tuple.Create(from, edge_id);
        // Edge e = edges[from][edge_id];
        // if (from < e.v || e.reverse_edge == Edge.None)
        // {
        //     return System.Tuple.Create(from, edge_id);
        // }
        // else
        // {
        //     return System.Tuple.Create(e.v, e.reverse_edge);
        // }
    }

    public int CountNodes() => edges.GetLength(0);
    public int CountEdges(int i) => edges[i].Length;
}

public class Dijkstra
{
    public static int INFINITY = int.MaxValue;
    public static int INVALID = -1;
    public static EdgeId NOEDGE = new EdgeId(-1, -1);
    List<int> queue;
    GraphInterface graph;
    // distances from "start" vertex
    int[] dist;
    EdgeId[] prev;

    int MinVertex()
    {
        int argmin = INVALID;
        int min = INFINITY;
        foreach (int a in queue)
        {
            if (dist[a] <= min)
            {
                min = dist[a];
                argmin = a;
            }
        }
        return argmin;
    }

    public Dijkstra(GraphInterface g, int start)
    {
        graph = g;
        dist = new int[g.CountNodes()];
        prev = new EdgeId[g.CountNodes()];
        queue = new List<int>();
        for (int i = 0; i < g.CountNodes(); i++)
        {
            queue.Add(i);
            dist[i] = INFINITY;
            prev[i] = NOEDGE;
        }
        dist[start] = 0;
    }

    public IEnumerator Run()
    {
        while (queue.Count != 0)
        {
            int u = MinVertex();
            Debug.Log(u);
            queue.Remove(u);
            // starting to consider a vertex
            yield return System.Tuple.Create(u, false);
            int e_idx = 0;
            //bool best_so_far = false;
            foreach (Edge e in graph[u])
            {
                // I'm considering an edge...
                yield return new EdgeId(u, e_idx);
                int alt = dist[u] + e.d;
                EdgeId old_prev = prev[e.v];
                if (alt < dist[e.v])
                {
                    dist[e.v] = alt;
                    prev[e.v] = new EdgeId(u, e_idx);
                    //best_so_far = true;
                }
                e_idx++;
                // The best edge to e.v after considering this edge
                yield return System.Tuple.Create(prev[e.v], old_prev);
            }
            // finished with vertex u
            yield return System.Tuple.Create(u, true);
        }
        yield return -1;
    }
}

// public class Test {
//     public Graph mygraph;
//     public Test() {
//          mygraph = new Graph( new Edge[][] {
//             new Edge[] { new Edge(1,1), new Edge(2,1) },
//             new Edge[] { new Edge(0,1) },
//             new Edge[] { }
//         } );
//     }
//     static void Main() {
//         Test t = new Test();
//         var D = new Dijkstra(t.mygraph, 0);

//         var R = D.Run();
//         while (R.MoveNext()) {
//             System.Console.WriteLine(R.Current);
//         }
//         //System.Console.WriteLine(((Edge[])t.mygraph[0])[1].v);
//     }
// }

public class AddSpheres : MonoBehaviour
{
    public Rigidbody mySpherePrefab;
    public GameObject stickPrefab;

    private GestureRecognizer r;

    Rigidbody[] balls;
    GameObject[][] sticks;
    SpringJoint[][] springs;

    public Graph myGraph;
    public Dijkstra dijkstra;
    public IEnumerator dijkstra_run;

    private bool tapDetect = false;

    public void HighlightVertex(int i, Color col) => balls[i].GetComponent<MeshRenderer>().material.color = col;
    public void HighlightEdge(int i, int e_id, Color col) {
        var from_and_edge_id = myGraph.PrincipalEdge(i, e_id);
        int from = from_and_edge_id.Item1;
        int edge_id = from_and_edge_id.Item2;
        sticks[from][edge_id].GetComponent<MeshRenderer>().material.color = col;
    }

    void Init(GraphInterface g)
    {
        int n = g.CountNodes();
        //balls = new Rigidbody[n];
        //sticks = new Rigidbody[g.CountNodes()][];
        springs = new SpringJoint[n][];
        sticks = new GameObject[n][];
        for (int i = 0; i < n; i++)
        {
            balls[i].velocity = 0.0f * Random.insideUnitSphere;
            springs[i] = new SpringJoint[g.CountEdges(i)];
            sticks[i] = new GameObject[g.CountEdges(i)];
        }
        balls[0].velocity = Vector3.zero;
        for (int i = 0; i < n; i++)
        {
            int j = 0;
            foreach (Edge e in g[i])
            {
                //if (i < e.v || e.reverse_edge == Edge.None)
                //{
                    sticks[i][j] = Instantiate(stickPrefab);
                //}
                springs[i][j] = balls[i].gameObject.AddComponent<SpringJoint>();
                springs[i][j].connectedBody = balls[e.v];
                springs[i][j].spring = 1.0f;
                springs[i][j].minDistance = 0.1f * e.d - Vector3.Distance(balls[i].position, balls[e.v].position);
                springs[i][j].maxDistance = 0.1f * e.d - Vector3.Distance(balls[i].position, balls[e.v].position);
                j++;
            }
        }
    }

    void Start()
    {
        // myGraph = new Graph(new Edge[][] {
        //     new Edge[] { new Edge(1,1,0), new Edge(2,1,Edge.None) },
        //     new Edge[] { new Edge(0,1,0) },
        //     new Edge[] { },
        //     new Edge[] { new Edge(0,20,Edge.None), new Edge(1,10,Edge.None), new Edge(2,10,Edge.None)}
        // });

        // cube

       r  = new GestureRecognizer();
       r.SetRecognizableGestures(GestureSettings.Tap | GestureSettings.DoubleTap);
       r.StartCapturingGestures();

        r.TappedEvent += (source, tapCount, ray) =>
        {
            tapDetect = true; 
        };












        myGraph = new Graph(new int[][] {
            new int[] { 1, 3, 4 },
            new int[] { 0, 2, 5 },
            new int[] { 1, 3, 6 },
            new int[] { 0, 2, 7 },
            new int[] { 0, 5, 7 },
            new int[] { 1, 4, 6 },
            new int[] { 2, 5, 7 },
            new int[] { 3, 4, 6 }
        });

        balls = new Rigidbody[8];
        Vector3[] posns = {
            new Vector3(0.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 0.0f, 1.0f),
            new Vector3(1.0f, 0.0f, 1.0f),
            new Vector3(1.0f, 0.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 0.0f),
            new Vector3(0.0f, 1.0f, 1.0f),
            new Vector3(1.0f, 1.0f, 1.0f),
            new Vector3(1.0f, 1.0f, 0.0f)
        };
        for (int i = 0; i < 8; i++) {
            balls[i] = Instantiate(mySpherePrefab, 0.1f * (posns[i] - new Vector3(0.5f, 0.5f, 0.5f)) + new Vector3(0.0f,0.0f,2.0f), Quaternion.identity);
        }
        balls[0].mass = 1.0e9f;
        

        Init(myGraph);

        // foreach (var ball in balls) {
        //     ball.position -= new Vector3(0.5f, 0.5f, 0.5f);
        //     ball.position *= 5.0f;
        // }

        dijkstra = new Dijkstra(myGraph, 0);
        dijkstra_run = dijkstra.Run();
    }

    void Update()
    {
        Vector3 balls_com = Vector3.zero;
        for (int i = 0; i < myGraph.CountNodes(); i++)
        {
            balls_com += balls[i].position;
        }
        balls_com /= myGraph.CountNodes();

        for (int i = 0; i < myGraph.CountNodes(); i++)
        {
            balls[i].AddForce(0.1f*(balls[i].position - balls_com));
            int j = 0;
            foreach (Edge e in myGraph[i])
            {
                //if (i < e.v || e.reverse_edge == Edge.None)
                //{
                    sticks[i][j].transform.position = 0.5f * (balls[i].position + balls[e.v].position);
                    var scale = sticks[i][j].transform.localScale;
                    scale.y = (balls[e.v].position - balls[i].position).magnitude / 2.0f;
                    sticks[i][j].transform.localScale = scale;
                    sticks[i][j].transform.rotation = Quaternion.FromToRotation(Vector3.up, balls[e.v].position - balls[i].position);

                    // shift it by a small amount, to indicate directional edges
                    sticks[i][j].transform.position += 0.01f * Vector3.Cross(balls[i].position - balls[e.v].position, new Vector3(0,0,1)).normalized;
                    j++;
                //}
            }
        }
        object dijkstra_run_last = -1;
        if (Input.GetKeyDown(KeyCode.Space) ||  tapDetect )
        {
            tapDetect = false;
            dijkstra_run_last = dijkstra_run.Current;
            dijkstra_run.MoveNext();
            Debug.Log("current:" + dijkstra_run.Current);
            Debug.Log("last   :" + dijkstra_run_last);
            switch (dijkstra_run_last)
            {
                case EdgeId e:
                    HighlightEdge(e.v, e.e_idx, Color.white);
                    break;
            }
            switch (dijkstra_run.Current)
            {
                case System.Tuple<int, bool> c:
                        HighlightVertex(c.Item1, c.Item2?Color.blue:Color.red);
                    break;
                case System.Tuple<EdgeId, EdgeId> t:
                        if (t.Item2.v != -1) {
                            HighlightEdge(t.Item2.v, t.Item2.e_idx, Color.white);
                        }
                        //if ((myGraph[t.Item1.v] as Edge[])[t.Item1.e_idx].v != 0) {
                            HighlightEdge(t.Item1.v, t.Item1.e_idx, Color.blue);
                        //}
                    break;
                case EdgeId e:
                    HighlightEdge(e.v, e.e_idx, Color.red);
                    break;
            }
        }

    }
}
