using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class WriteToFile
{

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void WriteStringToFile(double value, double time)
    {
        string path = "Assets/VORTEX/Resources/PulseData.txt";

        StreamWriter writer = new StreamWriter(path, true);
        string str = value.ToString() + " " + time.ToString() ;
        writer.WriteLine(str);
        writer.Close();

        //AssetDatabase.ImportAsset(path);
       // TextAsset asset = (TextAsset)Resources.Load("test");

        
    }
}
