using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class worldGen : MonoBehaviour
{
     Vector2Int chunk = new Vector2Int(0,0);
     public fluidSim2D sim;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        int x = 0;
        int y = 0;
        if(Input.GetKeyDown(KeyCode.L)){
            x= 1;
        }
        if(Input.GetKeyDown(KeyCode.I)){
            y= 1;
        }
        if(x!= 0 || y!= 0){
            sim.loadChunk(chunk, chunk + new Vector2Int(x,y));
            chunk = chunk + new Vector2Int(x,y);
        }
        
    }
}
