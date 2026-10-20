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

    private List<Vector3> blockPositions = new List<Vector3>();

    private Vector3 startPos;

    private Hashtable blockContainer = new Hashtable();
    void Start()
    {
        for (int i = 0; i < worldSizeX; i++)
        {
            for (int y = 0; y < worldSizeZ; y++)
            {
                Vector3 pos = new Vector3(i * gridOffset, Noisegeneration(i, y, 8f) * noiseHeight, y * gridOffset);

                GameObject block = Instantiate(blockGameObject, pos, Quaternion.identity) as GameObject;

                blockPositions.Add(block.transform.position);

                block.transform.SetParent(this.transform);
            }
        }
        SpawnObject();
    }

    //YOU GOT HERE
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
