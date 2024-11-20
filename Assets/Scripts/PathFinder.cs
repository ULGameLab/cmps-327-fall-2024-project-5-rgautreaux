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
using UnityEngine.XR;
using static UnityEngine.Networking.UnityWebRequest;
using UnityEditor;
using UnityEngine.SocialPlatforms.Impl;

public class Node
{
    public Node cameFrom = null; //parent node
    public Node enemyNode = null;   //enemy node

    public GameObject enemyGameObject;
    public Transform enemyLocation;

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
    public List<Node> TODOList = new();
    public List<Node> DoneList = new();
    public List<Node> EnemyPath = new();
    public Tile goalTile;
    public GameObject enemyGameObject;


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

        TODOList.Add(new Node(start, 0, null, 0, 0));
        goalTile = goal;

        while (TODOList.Count > 0)
        {
            TODOList.Sort((x, y) => (x.priority.CompareTo(y.priority))); // This will keep the TODO List sorted based on the F cost

            //Look for the lowest F cost square on the TODO list. We refer to this as the current square.
            Node current = TODOList[0];

            //Switch it to the DONE list
            DoneList.Add(current);
            TODOList.RemoveAt(0);

            if (current.tile == goal)
            {
                return RetracePath(current);  // Returns the Path if goal is reached
            }


            // For each of the 8 squares adjacent to this current square, calculate the costs
            // You just need to fill code inside this foreach only
            foreach (Tile nextTile in current.tile.Adjacents)
            {

                //If it is not walkable or if it is on the DONE list, ignore it.
                if (DoneList.FindIndex(match: n => n.tile = nextTile) || !nextTile.isPassable)
                {
                    //Move on to next
                    TODOList.RemoveAt(0);
                }
                // If it isn’t on the TODO list, add it to the TODO list. Make the current square the parent of 
                // this square.Record the F, G, and H costs of the square
                else if (!TODOList.FindIndex(match: n => n.tile = nextTile))
                {
                    TODOList.Add(nextTile);
                    nextTile.cameFrom = current;

                    nextTile.priority = HeuristicsDistance(current.tile, nextTile);
                    nextTile.costSoFar = HeuristicsDistance(start, nextTile);
                    nextTile.remainingDist = HeuristicsDistance(nextTile, goal);

                }
                // If it is on the TODO list already, check to see if this path to that square is better,using G
                // cost as the measure
                else if (TODOList.FindIndex(match: n => n.tile = nextTile))
                {
                    // A lower G cost means that this is a better path.

                    TODOList.Sort((x, y) => (x.costSoFar.CompareTo(y.costSoFar))); //Resort the list to compare G score
                    if (nextTile.costSoFar < current.costSoFar)
                    {
                        //If the G cost is lower, change the parent of the square to the current square, and recalculate the G and F scores of the
                        // square.

                        nextTile.cameFrom = current;
                        nextTile.priority = HeuristicsDistance(current.tile, nextTile);
                        nextTile.costSoFar = HeuristicsDistance(start, nextTile);

                    }
                }
                else
                {
                    nextTile.priority = HeuristicsDistance(current.tile, nextTile);
                    nextTile.costSoFar = HeuristicsDistance(start, nextTile);
                    nextTile.remainingDist = HeuristicsDistance(nextTile, goal);
                }
            }
        }
        return new Queue<Tile>(); // Returns an empty Path if no path is found
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

            // for each neighboring tile calculate the costs
            // You just need to fill code inside this foreach only
            // Just increase the F cost of the enemy tile and the tiles around it by a certain ammount (say 30)

            // increase the cost of the enemy tile(e.g. +100) and the tiles adjacent to it(e.g. +80).
            // The A* algorithm will then choose a path avoiding these tiles since any path passing through
            // the enemy area will have a higher cost

            foreach (Tile nextEnemy in enemyTile.Adjacents)
            {

                //if player makes contact with the enemy, enforce heavy cost
                if (current.tile == enemyTile)
                {
                    enemy.priority = EnemyDistance(current.tile, enemy.tile);
                    enemy.costSoFar = EnemyDistance(start, enemy.tile);
                    enemy.remainingDist = EnemyDistance(enemy.tile, goal);
                }
                //If it is not walkable or if it is on the DONE list, ignore it.
                else if (DoneList.FindIndex(n => n.tile = nextTile) || !nextTile.isPassable)
                {
                    //Move on to next
                    TODOList.RemoveAt(0);
                }
                
                // If it isn’t on the TODO list, add it to the TODO list. Make the current square the parent of 
                // this square.Record the F, G, and H costs of the square
                else if (!TODOList.FindIndex(n => n.tile = nextTile))
                {
                    TODOList.Add(n.tile);
                    nextEnemy.cameFrom = current;

                    nextEnemy.priority = EnemyNeighborDist(current.tile, nextEnemy);
                    nextEnemy.costSoFar = EnemyNeighborDist(start, nextEnemy);
                    nextEnemy.remainingDist = EnemyNeighborDist(nextEnemy, goal);

                }
                // If it is on the TODO list already, check to see if this path to that square is better,using G
                // cost as the measure
                else if (TODOList.FindIndex(n => n.tile = nextTile))
                {
                    // A lower G cost means that this is a better path.

                    TODOList.Sort((x, y) => (x.costSoFar.CompareTo(y.costSoFar))); //Resort the list to compare G score
                    if (n.tile.costSoFar < current.costSoFar)
                    {
                        //If the G cost is lower, change the parent of the square to the current square, and recalculate the G and F scores of the
                        // square.

                        n.tile.cameFrom = current;
                        n.tile.priority = EnemyNeighborDist(current.tile, nextEnemy);
                        n.tile.costSoFar = EnemyNeighborDist(start, nextEnemy);

                    }
                    else
                    {
                        n.tile.priority = EnemyNeighborDist(current.tile, nextEnemy);
                        n.tile.costSoFar = EnemyNeighborDist(start, nextEnemy);
                        n.tile.remainingDist = EnemyNeighborDist(nextEnemy, goal);

                    }
                }
            }
        }

        return new Queue<Tile>(); // Returns an empty Path if no path is found

    }


    // Manhattan Distance with horizontal/vertical cost of 10
    public double HeuristicsDistance(Tile currentTile, Tile goalTile)
        {
            int xdist = Math.Abs(goalTile.indexX - currentTile.indexX);
            int ydist = Math.Abs(goalTile.indexY - currentTile.indexY);
            // Assuming cost to move horizontally and vertically is 10
            //return manhattan distance
            return (xdist * 10 + ydist * 10);
        }


        // Manhattan Distance with horizontal/vertical cost of 30 due to
        // enemy presence, different from Heuristic Distance
       public  double EnemyDistance(Tile currentTile, Tile goalTile)
        {
            int xdist = Math.Abs(goalTile.indexX - currentTile.indexX);
            int ydist = Math.Abs(goalTile.indexY - currentTile.indexY);
            // The cost to move horizontally and vertically is 30
            // when an enemy is present, return corresponding distance
            return (xdist * 100 + ydist * 100);
        }


        // Manhattan Distance with horizontal/vertical cost of 30 due to
        // enemy presence, different from Heuristic Distance
        public double EnemyNeighborDist(Tile currentTile, Tile goalTile)
        {
            int xdist = Math.Abs(goalTile.indexX - currentTile.indexX);
            int ydist = Math.Abs(goalTile.indexY - currentTile.indexY);
            // The cost to move horizontally and vertically is 30
            // when an enemy is present, return corresponding distance
            return (xdist * 80 + ydist * 80);
        }


        // Calculate distance between current and neighbor
        public double TileDistance(Tile currentTile, Tile goalTile)
        {
            int xdist = Math.Abs(goalTile.indexX - currentTile.indexX);
            int ydist = Math.Abs(goalTile.indexY - currentTile.indexY);
            // Assuming cost to move horizontally and vertically is 10
            //return manhattan distance
            return (xdist + ydist);
        }

        // Retrace path from a given Node back to the start Node
       public Queue<Tile> RetracePath(Node node)
        {
            List<Tile> tileList = new();
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
        List<Tile> tileList = new();
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
                List<Tile> adjacentList = new(currentTile.Adjacents);
                    ShuffleTiles<Tile>(adjacentList);
                    if (tileList.Count <= 0) nextTile = adjacentList[0];
                    else
                    {
                        foreach (Tile tile in adjacentList)
                        {
                        if (tile != tileList[^1])
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
}
