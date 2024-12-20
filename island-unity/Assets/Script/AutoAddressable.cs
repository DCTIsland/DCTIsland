#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets;

public class AutoAddressable : AssetPostprocessor
{
    string modelPath = "Assets/Art/Model";
    public void OnPreprocessModel()
    {
        AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
        AddressableAssetGroup group = settings.DefaultGroup;
        Debug.Log(assetPath);

        if (assetPath.StartsWith(modelPath) && assetPath.EndsWith(".FBX"))
        {
            //add to addressable
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (settings.FindAssetEntry(guid) == null)
            {
                settings.CreateOrMoveEntry(guid, group).address = assetPath;
                Debug.Log("add to addressable success");
                BuildAddressable();
            }
            else
            {
                Debug.Log("add to addressable error");
            }
        }
    }

    void BuildAddressable()
    {
        EditorApplication.delayCall += () =>
        {
            Debug.Log("Begin building addressable bundle");
            AddressableAssetSettings.CleanPlayerContent();
            AddressableAssetSettings.BuildPlayerContent();
            Debug.Log("Build bundle success");
        };
    }
}
#endif