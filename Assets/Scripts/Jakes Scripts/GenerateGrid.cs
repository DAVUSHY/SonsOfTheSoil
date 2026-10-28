using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateGrid : MonoBehaviour
{

    public GameObject blockGameObject;

    public GameObject objToSpawn;

    public GameObject player;

    public int worldSizeX = 10;

    public int worldSizeZ = 10;

    public int noiseHeight = 3;

    public float gridOffset = 1.1f;

    public bool infinite = true; 

    private List<Vector3> blockPositions = new List<Vector3>();

    private Vector3 startPos;

    private Hashtable blockContainer = new Hashtable();

    //Spawns a worldSizeX by worldSizeZ grid using the noise hight to chnage the variation of the blocks hight
    void Start()
    {
        if (!infinite)
        {
            for (int i = 0; i < worldSizeX; i++)
            {
                for (int y = 0; y < worldSizeZ; y++)
                {
                    Vector3 pos = new Vector3(i * gridOffset , Noisegeneration(i, y, 8f) * noiseHeight, y * gridOffset);

                    GameObject block = Instantiate(blockGameObject, pos, Quaternion.identity) as GameObject;

                    blockContainer.Add(pos, block);

                    blockPositions.Add(block.transform.position);

                    block.transform.SetParent(this.transform);
                }
            }
            SpawnObject();
        }
    }


    private void Update()
    {
        if (infinite)
        {
            if (Mathf.Abs(xPlayerMove) >= 1 || Mathf.Abs(zPlayerMove) >= 1)
            {
                for (int i = -worldSizeX; i < worldSizeX; i++)
                {
                    for (int y = -worldSizeZ; y < worldSizeZ; y++)
                    {
                        Vector3 pos = new Vector3(i * 1 + xPlayerLocation, Noisegeneration(i + xPlayerLocation, y + zPlayerLocation, 8f) * noiseHeight, y * 1 + zPlayerLocation);

                        if (!blockContainer.ContainsKey(pos))
                        {
                            GameObject block = Instantiate(blockGameObject, pos, Quaternion.identity) as GameObject;

                            blockContainer.Add(pos, block);

                            blockPositions.Add(block.transform.position);

                            block.transform.SetParent(this.transform);
                        }


                    }
                }
            }
        }
    }

    public int xPlayerMove
    {
        get
        {
            return (int)(player.transform.position.x - startPos.x);
        }
    }
    public int zPlayerMove
    {
        get
        {
            return (int)(player.transform.position.z - startPos.z);
        }
    }

    private int xPlayerLocation
    {
        get
        {
            return (int)Mathf.Floor(player.transform.position.x);
        }
    }

    private int zPlayerLocation
    {
        get
        {
            return (int)Mathf.Floor(player.transform.position.z);
        }
    }

    private Vector3 ObjectSpawnLocation ()
    {
        int randIndex = Random.Range(0, blockPositions.Count);
        Vector3 newPos = new Vector3(blockPositions[randIndex].x, blockPositions[randIndex].y + 1f, blockPositions[randIndex].z);
        blockPositions.RemoveAt(randIndex);
        return newPos;
    }

    private void SpawnObject()
    {
        //loop to decide how many objects ytou want to spawn
        for (int c = 0; c < 20; c++)
        {
            GameObject objToPlace = Instantiate(objToSpawn, ObjectSpawnLocation(), Quaternion.identity);
        }
    }

    private float Noisegeneration (int x, int z, float detailScale)
    {
        float xNoise = (x + this.transform.position.x) / detailScale;
        float zNoise = (z + this.transform.position.y) / detailScale;

        return Mathf.PerlinNoise(xNoise, zNoise);
    }
}
