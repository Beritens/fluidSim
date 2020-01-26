using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class controls : MonoBehaviour
{
    public float speed = 1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        float x = Input.GetAxis("Horizontal")*Time.deltaTime;
        float y = Input.GetAxis("Vertical")*Time.deltaTime;
        transform.position += (Vector3)new Vector2(x*speed,y*speed);
    }
}
