using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class fluidSim2D : MonoBehaviour

{

    public ComputeShader shader;
    public ComputeShader drawShader;
    public ComputeShader worldShader;
    //public int TexResolution = 256;

    Renderer rend;
    // RenderTexture[] myRt;
    // int current;
    RenderTexture[] myRt = new RenderTexture[2];
    public Texture2D input;
    public Texture2D inputElements;
    public Texture2D brush;
    public Texture2D colors;
    Vector2Int TexResolution;
    ComputeBuffer testAreaBuffer;
    int[] testArea;
    public int elementsNum;
    int el;
    int current;

    struct Pixel{
        public int element;
        public int movable;
        public Vector2 velocity;
        public int particle;
        public Vector2 subPixPos;
    }

    Pixel[] pixelArray;
    ComputeBuffer[] pixelBuffer = new ComputeBuffer[2];
   
    


    //int currTex = 0;
    
    // Start is called before the first frame update
    void Start()
    {
        TexResolution = new Vector2Int(input.width,input.height);
        for(int i = 0; i<2; i++){
            myRt[i]= new RenderTexture(TexResolution.x,TexResolution.y,24);
            myRt[i].enableRandomWrite = true;
            myRt[i].filterMode = FilterMode.Point;
            myRt[i].Create();
        }
        
            
        Graphics.Blit(input, myRt[0]);
        

        rend = GetComponent<Renderer>();
        rend.enabled = true;


        pixelArray = new Pixel[TexResolution.x*TexResolution.y];
        for(int x = 0; x<TexResolution.x; x++){
            for(int y = 0; y<TexResolution.y; y++){
                pixelArray[y*TexResolution.x+x].element = (int)(inputElements.GetPixel(x,y).r*255);
            }
        }
        for(int i = 0; i<2; i++){
            pixelBuffer[i] = new ComputeBuffer(pixelArray.Length, sizeof(int)*3 +sizeof(float)*4,ComputeBufferType.Default);
            pixelBuffer[i].SetData(pixelArray);
        }
        

        testArea = new int[pixelArray.Length * 8];
        testAreaBuffer = new ComputeBuffer(testArea.Length, sizeof(int),ComputeBufferType.Default);
        testAreaBuffer.SetData(testArea);





        //rend.material.SetTexture("_MainTex",myRt[0]);
        UpdateTextureFromCompute();
    }
    void doStuff(bool rerun){
        
        shader.SetBool("rerun",rerun);

        int kernelHandle = shader.FindKernel("CSMain");
        shader.SetTexture(kernelHandle,"colors",colors);
         shader.SetBuffer(kernelHandle,"pixelBuffer",pixelBuffer[current]);
         shader.SetTexture(kernelHandle,"Result",myRt[current]);
         shader.SetBuffer(kernelHandle,"testArea",testAreaBuffer);
        shader.Dispatch(kernelHandle,Mathf.CeilToInt((float)TexResolution.x/8),Mathf.CeilToInt((float)TexResolution.y/8),1);

        kernelHandle = shader.FindKernel("moveStuff");
         shader.SetTexture(kernelHandle,"colors",colors);
        shader.SetTexture(kernelHandle,"Result",myRt[current]);
         shader.SetBuffer(kernelHandle,"pixelBuffer",pixelBuffer[current]);
         shader.SetBuffer(kernelHandle,"testArea",testAreaBuffer);
        shader.Dispatch(kernelHandle,Mathf.CeilToInt((float)TexResolution.x/8),Mathf.CeilToInt((float)TexResolution.y/8),1);

        kernelHandle = shader.FindKernel("wipe");
        shader.SetBuffer(kernelHandle,"testArea",testAreaBuffer);
        shader.Dispatch(kernelHandle,Mathf.CeilToInt((float)TexResolution.x/8),Mathf.CeilToInt((float)TexResolution.y/8),1);
    }
    void UpdateTextureFromCompute(){
        shader.SetInt("TexSizeX",TexResolution.x);
        shader.SetInt("TexSizeY",TexResolution.y);
        shader.SetFloat("dt",Time.deltaTime);
        shader.SetInt("pixelsPerUnit",100);
        shader.SetInt("testAreaSize",65);
            doStuff(false);
        for(int i = 0; i<5; i++){

            doStuff(true);
        }
            // kernelHandle = shader.FindKernel("densitySwap");
            // shader.SetBuffer(kernelHandle,"elements",elementBuffer);
            // shader.SetTexture(kernelHandle,"Result",myRt);
            
            // shader.Dispatch(kernelHandle,TexResolution.x/8,TexResolution.y/8,1);
        
        
        rend.material.SetTexture("_MainTex",myRt[current]);
        
    }
    

    float t = 0;
    void Update()
    {
        t+= Time.deltaTime;
        UpdateTextureFromCompute();
        t= 0;
        if(Input.GetKeyDown(KeyCode.UpArrow)){
            el = (el+1)%elementsNum;
        }
        else if(Input.GetKeyDown(KeyCode.DownArrow)){
            el = (el-1)-elementsNum*Mathf.FloorToInt(((float)(el-1)/(float)elementsNum));
        }
        if(Input.GetButton("Fire1")){
            draw();
        }
        
       
    }
    void draw(){
         int kernelHandle = drawShader.FindKernel("CSMain");
        drawShader.SetInt("element",el);
        RaycastHit hit;
        if(!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit)){
            return;
        }
        if(hit.transform.GetComponent<Renderer>() != rend){
            return;
        }
        Vector2 pixelUV = hit.textureCoord;
        pixelUV.x *= TexResolution.x;
        pixelUV.y *= TexResolution.y;
        drawShader.SetInt("posX", (int)pixelUV.x);
        drawShader.SetInt("posY", (int)pixelUV.y);
        drawShader.SetInt("brushSize",brush.height);
        drawShader.SetInt("TexSizeX",TexResolution.x);
        drawShader.SetInt("TexSizeY",TexResolution.y);

        drawShader.SetTexture(kernelHandle,"brush",brush);
        drawShader.SetTexture(kernelHandle,"colors",colors);
        drawShader.SetTexture(kernelHandle,"Result",myRt[current]);
        drawShader.SetBuffer(kernelHandle,"pixelBuffer",pixelBuffer[current]);
        drawShader.Dispatch(kernelHandle,Mathf.CeilToInt((float)brush.width/8),Mathf.CeilToInt((float)brush.height/8),1);

    }
    public void loadChunk(Vector2Int lastChunk, Vector2Int nextChunk){
        int prev = current;
        current = (prev+1)%2;
        Vector2Int difference = new Vector2Int((nextChunk.x-lastChunk.x)*(TexResolution.x/2),(nextChunk.y-lastChunk.y)*(TexResolution.y/2));
        worldShader.SetInts("TexSize",new int[2]{TexResolution.x,TexResolution.y});
        worldShader.SetInts("chunk",new int[2]{lastChunk.x,lastChunk.y});
        worldShader.SetInts("offset",new int[2]{difference.x,difference.y});

        int kernelHandle = worldShader.FindKernel("CSMain");
        worldShader.SetTexture(kernelHandle,"prev",myRt[prev]);
        worldShader.SetTexture(kernelHandle,"Result",myRt[current]);
        worldShader.SetBuffer(kernelHandle,"prevPixels",pixelBuffer[prev]);
        worldShader.SetBuffer(kernelHandle,"pixels",pixelBuffer[current]);
        worldShader.Dispatch(kernelHandle,Mathf.CeilToInt((float)TexResolution.x/8),Mathf.CeilToInt((float)TexResolution.y/8),1);
         rend.material.SetTexture("_MainTex",myRt[current]);
    }
}
