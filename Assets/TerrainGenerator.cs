using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{

    //todo: procedurally infinite world, only load tiles that camera is on
    public Material grassTex;
    public int size = 10;

    public GameObject TilePrefab;

    // Start is called before the first frame update
    void Start()
    {
        for(int i = 0;i<size;i++)
        {
            for(int j = 0;j<size;j++){
               GameObject go = Instantiate(TilePrefab);
               go.transform.position = new Vector3(i,0,j);
               go.transform.parent = transform;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
