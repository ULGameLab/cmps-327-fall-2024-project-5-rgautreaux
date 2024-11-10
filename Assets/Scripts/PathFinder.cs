using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using MapGen;
using UnityEditor.TerrainTools;
using System.Linq;
using UnityEngine.UIElements;
using NUnit.Framework;
using NUnit.Framework.Internal;
using static UnityEngine.EventSystems.EventTrigger;
using System.Drawing;
using System.IO;
using UnityEditor.Experimental.GraphView;
using JetBrains.Annotations;

public class Node
{
    public Node cameFrom = null; //parent node
    public Node enemyNode = null;   //enemy node

    public GameObject enemyGameObject;
    Transform enemyLocation;

    public List<Node> EnemyPath;

    public double priority = 0; // F value (current estimated cost for point n)
    public double costSoFar = 0; // G Value (distance from the start point to point n)
    public double remainingDist = 0; // H value (estimated distance from point n to the goal point)

    public Tile tile;         //Player Tile
    public Tile enemyTile;    //Enemy Tile

    public Node(Tile _tile, double _priority, Node _cameFrom, double _costSoFar, double _remainingDist)
    {
        cameFrom = _cameFrom;
        priority = _priority; 
        costSoFar = _costSoFar;
        remainingDist = _remainingDist;
        tile = _tile;
    }
}

public class PathFinder
{
    List<Node> TODOList = new List<Node>();
    List<Node> DoneList = new List<Node>();
    private List<Node> EnemyPath;
    Tile goalTile;
    GameObject enemyGameObject;


    // This is the constructor
    public PathFinder()
    {
        goalTile = null;

    }

    // TODO: Find the path based on A-Star Algorithm
    public Queue<Tile> FindPathAStar(Tile start, Tile goal)
    {
        TODOList = new List<Node>();
        DoneList = new List<Node>();
        //Neighbors = new List<Node>();

        TODOList.Add(new Node(start, 0, null, 0, 0));
        goalTile = goal;

        while (TODOList.Count > 0)
        {
            TODOList.Sort((x, y) => (x.priority.CompareTo(y.priority))); // This will keep the TODO List sorted based on the F cost
            Node current = TODOList[0];
            DoneList.Add(current);
            TODOList.RemoveAt(0);

            if (current.tile == goal)
            {
                return RetracePath(current);  // Returns the Path if goal is reached
            }

            // for each neighboring tile calculate the costs
            // You just need to fill code inside this foreach only
            foreach (Tile nextTile in current.tile.Adjacents)
            {
                Node neighbor = TODOList[1];

                //if this neighboring tile is not traversable or
                //closed/done you SKIP to the next neighboring tile
                if (DoneList.Contains(neighbor) || !current.tile.Adjacents.Contains(neighbor.tile))
                {
                    //Move on to next neighbor
                    DoneList.Add(neighbor);
                    TODOList.RemoveAt(1);
                }

                //if the new path to this neighbor tile is shorter OR
                //this neighbor is not available/open, then edit the f
                //cost and path accordingly

                double oldPath = TileDistance(current.tile, neighbor.tile);
                DoneList.Add(neighbor);
                TODOList.RemoveAt(1);
                double newPath = TileDistance(current.tile, neighbor.tile);

                if (newPath < oldPath || !TODOList.Contains(neighbor))
                {
                    //set F Cost of neighbor
                    neighbor.priority = HeuristicsDistance(current.tile, neighbor.tile);

                    //set parent of neighor to current
                    neighbor.cameFrom = current;

                    //add available/open nodes to TODO List
                    if (!TODOList.Contains(neighbor))
                    {
                        DoneList.Add(neighbor);
                    }
                }
            }
        }
        return new Queue<Tile>(); // Returns an empty Path if no path is found
    }

    private Queue<Tile> RetracePath(Node current)
    {
        throw new NotImplementedException();
    }

    private double TileDistance(Tile tile1, Tile tile2)
    {
        throw new NotImplementedException();
    }

    private double HeuristicsDistance(Tile tile1, Tile tile2)
    {
        throw new NotImplementedException();
    }

    public GameObject GetEnemyGameObject()
    {
        return enemyGameObject;
    }

    // TODO: Find the path based on A-Star Algorithm
    // In this case avoid a path passing near an enemy tile
    // BONUS TASK (Required the for Honors Contract Students)
    public Queue<Tile> FindPathAStarEvadeEnemy(Tile start, Tile goal, GameObject enemyGameObject)
    {
        TODOList = new List<Node>();
        DoneList = new List<Node>();
        EnemyPath = new List<Node>();

        Tile enemyTile = enemyGameObject.GetComponent<Enemy>().currentTile;


        TODOList.Add(new Node(start, 0, null, 0, 0));
        EnemyPath.Add(new Node(enemyTile, 0, null, 0, 0));

        goalTile = goal;

        while (TODOList.Count > 0)
        {
            TODOList.Sort((x, y) => (x.priority.CompareTo(y.priority))); // This will keep the TODO List sorted

            Node current = TODOList[0];
            Node enemy = EnemyPath[0];

            DoneList.Add(current);
            TODOList.RemoveAt(0);

            EnemyPath.RemoveAt(0);

            // for each neighboring tile calculate the costs
            // You just need to fill code inside this foreach only
            // Just increase the F cost of the enemy tile and the tiles around it by a certain ammount (say 30)

            // increase the cost of the enemy tile(e.g. +100) and the tiles adjacent to it(e.g. +80).
            // The A* algorithm will then choose a path avoiding these tiles since any path passing through
            // the enemy area will have a higher cost

            foreach (Tile nextEnemy in enemyTile.Adjacents)
            {
                
                Node enemyNeighbor = TODOList[1];

                //if player makes contact with the enemy, enforce heavy cost
                if (current.tile == enemyTile)
                {
                    EnemyDistance(current.tile, enemyTile);
                }

                //if this neighboring tile is not traversable or
                //closed/done you SKIP to the next neighboring tile
                if (DoneList.Contains(enemyNeighbor) || !current.tile.Adjacents.Contains(enemyNeighbor.tile))
                {
                    //Move on to next neighbor
                    DoneList.Add(enemyNeighbor);
                    TODOList.RemoveAt(1);
                }

                //if the new path to this neighbor tile is shorter OR
                //this neighbor is not available/open, then edit the f
                //cost and path accordingly

                double oldPath = TileDistance(current.tile, enemyTile);
                DoneList.Add(enemyNeighbor);
                TODOList.RemoveAt(1);
                double newPath = TileDistance(current.tile, enemyTile);

                if (newPath < oldPath || !TODOList.Contains(enemyNeighbor))
                {
                    //set F Cost of neighbor (HIGHER than usual)
                    enemyNeighbor.priority = EnemyNeighborDist(current.tile, enemyTile);

                    //set parent of neighor to current
                    enemyNeighbor.cameFrom = current;

                    //add available/open nodes to TODO List
                    if (!TODOList.Contains(enemyNeighbor))
                    {
                        DoneList.Add(enemyNeighbor);
                    }

                }
            }
            return new Queue<Tile>(); // Returns an empty Path
        }

        // Manhattan Distance with horizontal/vertical cost of 10
        double HeuristicsDistance(Tile currentTile, Tile goalTile)
        {
            int xdist = Math.Abs(goalTile.indexX - currentTile.indexX);
            int ydist = Math.Abs(goalTile.indexY - currentTile.indexY);
            // Assuming cost to move horizontally and vertically is 10
            //return manhattan distance
            return (xdist * 10 + ydist * 10);
        }


        // Manhattan Distance with horizontal/vertical cost of 30 due to
        // enemy presence, different from Heuristic Distance
        double EnemyDistance(Tile currentTile, Tile goalTile)
        {
            int xdist = Math.Abs(goalTile.indexX - currentTile.indexX);
            int ydist = Math.Abs(goalTile.indexY - currentTile.indexY);
            // The cost to move horizontally and vertically is 30
            // when an enemy is present, return corresponding distance
            return (xdist * 100 + ydist * 100);
        }


        // Manhattan Distance with horizontal/vertical cost of 30 due to
        // enemy presence, different from Heuristic Distance
        double EnemyNeighborDist(Tile currentTile, Tile goalTile)
        {
            int xdist = Math.Abs(goalTile.indexX - currentTile.indexX);
            int ydist = Math.Abs(goalTile.indexY - currentTile.indexY);
            // The cost to move horizontally and vertically is 30
            // when an enemy is present, return corresponding distance
            return (xdist * 80 + ydist * 80);
        }


        // Calculate distance between current and neighbor
        double TileDistance(Tile currentTile, Tile goalTile)
        {
            int xdist = Math.Abs(goalTile.indexX - currentTile.indexX);
            int ydist = Math.Abs(goalTile.indexY - currentTile.indexY);
            // Assuming cost to move horizontally and vertically is 10
            //return manhattan distance
            return (xdist + ydist);
        }

        // Retrace path from a given Node back to the start Node
        Queue<Tile> RetracePath(Node node)
        {
            List<Tile> tileList = new List<Tile>();
            Node nodeIterator = node;
            while (nodeIterator.cameFrom != null)
            {
                tileList.Insert(0, nodeIterator.tile);
                nodeIterator = nodeIterator.cameFrom;
            }
            return new Queue<Tile>(tileList);
        }



        // Generate a Random Path. Used for enemies
        public Queue<Tile> RandomPath(Tile start, int stepNumber)
        {
            List<Tile> tileList = new List<Tile>();
            Tile currentTile = start;
            for (int i = 0; i < stepNumber; i++)
            {
                Tile nextTile;
                //find random adjacent tile different from last one if there's more than one choice
                if (currentTile.Adjacents.Count < 0)
                {
                    break;
                }
                else if (currentTile.Adjacents.Count == 1)
                {
                    nextTile = currentTile.Adjacents[0];
                }
                else
                {
                    nextTile = null;
                    List<Tile> adjacentList = new List<Tile>(currentTile.Adjacents);
                    ShuffleTiles<Tile>(adjacentList);
                    if (tileList.Count <= 0) nextTile = adjacentList[0];
                    else
                    {
                        foreach (Tile tile in adjacentList)
                        {
                            if (tile != tileList[tileList.Count - 1])
                            {
                                nextTile = tile;
                                break;
                            }
                        }
                    }
                }
                tileList.Add(currentTile);
                currentTile = nextTile;
            }
            return new Queue<Tile>(tileList);
        }
    }

    private static void ShuffleTiles<T>(List<T> list)
    {
        // Knuth shuffle algorithm :: 
        // courtesy of Wikipedia :) -> https://forum.unity.com/threads/randomize-array-in-c.86871/
        for (int t = 0; t < list.Count; t++)
        {
            T tmp = list[t];
            int r = UnityEngine.Random.Range(t, list.Count);
            list[t] = list[r];
            list[r] = tmp;
        }
    }

    internal Queue<Tile> RandomPath(Tile currentTile, int stepsAway)
    {
        throw new NotImplementedException();
    }
}
