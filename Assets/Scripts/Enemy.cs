using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;

// FSM States for the enemy
public enum EnemyState { STATIC, CHASE, REST, MOVING, DEFAULT };

public enum EnemyBehavior {EnemyBehavior1, EnemyBehavior2, EnemyBehavior3 };

public class Enemy : MonoBehaviour
{
    //pathfinding
    protected PathFinder pathFinder;
    public GenerateMap mapGenerator;
    protected Queue<Tile> path;
    protected GameObject playerGameObject;

    public Tile currentTile;
    protected Tile targetTile;
    public Vector3 velocity;

    //properties
    public float speed = 1.0f;
    public float visionDistance = 5;
    public int maxCounter = 5;
    protected int playerCloseCounter;

    protected EnemyState state = EnemyState.DEFAULT;
    protected Material material;

    public EnemyBehavior behavior = EnemyBehavior.EnemyBehavior1;

    // Start is called before the first frame update
    void Start()
    {
        path = new Queue<Tile>();
        pathFinder = new PathFinder();
        playerGameObject = GameObject.FindWithTag("Player");
        playerCloseCounter = maxCounter;
        material = GetComponent<MeshRenderer>().material;
    }

    // Update is called once per frame
    void Update()
    {
        if (mapGenerator.state == MapState.DESTROYED) return;

        // Stop Moving the enemy if the player has reached the goal
        if (playerGameObject.GetComponent<Player>().IsGoalReached() || playerGameObject.GetComponent<Player>().IsPlayerDead())
        {
            //Debug.Log("Enemy stopped since the player has reached the goal or the player is dead");
            return;
        }

        switch (behavior)
        {
            case EnemyBehavior.EnemyBehavior1:
                HandleEnemyBehavior1();
                break;
            case EnemyBehavior.EnemyBehavior2:
                HandleEnemyBehavior2();
                break;
            case EnemyBehavior.EnemyBehavior3:
                HandleEnemyBehavior3();
                break;
            default:
                break;
        }

    }

    public void Reset()
    {
        Debug.Log("enemy reset");
        path.Clear();
        state = EnemyState.DEFAULT;
        currentTile = FindWalkableTile();
        transform.position = currentTile.transform.position;
    }

    Tile FindWalkableTile()
    {
        Tile newTarget = null;
        while (newTarget == null || !newTarget.mapTile.Walkable)
        {
            int randomIndex = (int)(UnityEngine.Random.value * mapGenerator.width * mapGenerator.height - 1);
            newTarget = GameObject.Find("MapGenerator").transform.GetChild(randomIndex).GetComponent<Tile>();
        }
        return newTarget;
    }

    // Dumb Enemy: Keeps Walking in Random direction, Will not chase player
    private void HandleEnemyBehavior1()
    {
        switch (state)
        {
            case EnemyState.DEFAULT: // generate random path 

                //Changed the color to white to differentiate from other enemies
                material.color = Color.white;


                if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                if (path.Count > 0)
                {
                    targetTile = path.Dequeue();
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                //if target reached
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                break;
            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }

    // TODO: Enemy chases the player when it is nearby
    private void HandleEnemyBehavior2()
    {
        //This enemy will keep walking in random directions (like the previous) but will chase the player if it is
        //nearby (controlled by “visionDistance” parameter in the Enemy script).
        switch (state)
        {
            case EnemyState.DEFAULT: // generate random path 

                //Changed the color to blue to differentiate from other enemies
                material.color = Color.blue;

                if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                if (path.Count > 0)
                {
                    targetTile = path.Dequeue();
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                //if target reached
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                //if target is seen
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= visionDistance)
                {

                    //When Enemy2 sees the player, it selects the last tile the Player was at as their target tile.

                    Tile lastSeen = playerGameObject.GetComponent<Player>().currentTile;
                    Queue<Tile> newTargetPath = pathFinder.FindPathAStar(currentTile, lastSeen);

                    targetTile = newTargetPath.Dequeue();
                    currentTile = targetTile;

                    state = EnemyState.CHASE;
                }

                break;

            case EnemyState.CHASE:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                //Chase Target
                //Enemy will chase the player if it is nearby using pathfinder to find the path to that tile.
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) > 0.05f)
                {
                    Tile lastSeen = playerGameObject.GetComponent<Player>().currentTile;
                    Queue<Tile> newTargetPath = pathFinder.FindPathAStar(currentTile, lastSeen);

                    targetTile = newTargetPath.Dequeue();
                    currentTile = targetTile;
                    if (path.Count <= 0) path = pathFinder.FindPathAStar(currentTile, lastSeen);

                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                }
                else
                {
                    //Target is reached
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                break;

            default:
                state = EnemyState.DEFAULT;
                break;
        }

    }

    // TODO: Third behavior (Describe what it does)

    //When Enemy3 sees the player, it selects a tile which is a few tiles away 
    //(for example 2 tiles away) from the Player as their target tile.The enemy now
    //uses pathfinder to find the path to that tile
    private void HandleEnemyBehavior3()
    {
        switch (state)
        {
            case EnemyState.DEFAULT: // generate random path 

                //Changed the color to yellow to differentiate from other enemies
                material.color = Color.yellow;

                if (path.Count <= 0) path = pathFinder.RandomPath(currentTile, 20);

                if (path.Count > 0)
                {
                    targetTile = path.Dequeue();
                    state = EnemyState.MOVING;
                }
                break;

            case EnemyState.MOVING:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                //if target reached
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= 0.05f)
                {
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                //if target is seen
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) <= visionDistance)
                {

                    //When Enemy3 sees the player,it selects a tile which is a few tiles away 
                    //(for example 2 tiles away) from the Player as their target tile.

                    Tile lastSeen = playerGameObject.GetComponent<Player>().currentTile.Adjacents[3];
                    Queue<Tile> newTargetPath = pathFinder.FindPathAStar(currentTile, lastSeen);

                    targetTile = newTargetPath.Dequeue();
                    currentTile = targetTile;

                    state = EnemyState.CHASE;
                }

                break;

            case EnemyState.CHASE:
                //move
                velocity = targetTile.gameObject.transform.position - transform.position;
                transform.position = transform.position + (velocity.normalized * speed) * Time.deltaTime;

                //Chase Target
                //Enemy will chase the player if it is nearby using pathfinder to find the path to that tile.
                if (Vector3.Distance(transform.position, playerGameObject.transform.position) > 0.05f)
                {
                    //PURSUIT
                    Tile lastSeen = playerGameObject.GetComponent<Player>().currentTile;
                    if (path.Count <= 0) path = pathFinder.FindPathAStar(currentTile, lastSeen);

                    if (path.Count > 0)
                    {
                        targetTile = path.Dequeue();
                        state = EnemyState.MOVING;
                    }
                }
                else
                {
                    //Target is reached
                    currentTile = targetTile;
                    state = EnemyState.DEFAULT;
                }

                break;

            default:
                state = EnemyState.DEFAULT;
                break;
        }
    }
}
