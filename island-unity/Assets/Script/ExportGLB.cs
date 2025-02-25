using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GLTFast.Export;
using System.IO;

public class ExportGLB : MonoBehaviour
{
    string projectPath = Directory.GetParent(Application.dataPath).FullName;
    string directoryPath = "Output/glb";
    
    async void DoExport(GameObject obj, string name)
    {
        var exportsetting = new ExportSettings()
        {
            Format = GltfFormat.Binary
        };

        var export = new GameObjectExport(exportsetting);
        export.AddScene(new[] {obj}, obj.transform.worldToLocalMatrix, "glb scene");

        string p = Path.Combine(projectPath, directoryPath, name + ".glb");
        var success = await export.SaveToFileAndDispose(p);

        if (!success)
        {
            Debug.LogError("Something went wrong exporting a glb");
        }
    }
}
