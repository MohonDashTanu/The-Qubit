using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// A utility script to create a grid cell prefab in the Unity editor
public class GridCellPrefabCreator
{
    [MenuItem("Quantum Tower Defense/Create Grid Cell Prefab")]
    public static void CreateGridCellPrefab()
    {
        // Make sure the GridCellVisual script exists
        if (System.Type.GetType("GridCellVisual") == null)
        {
            //Debug.LogError("GridCellVisual script not found! Please create this script first.");
            return;
        }
        
        // Create a new GameObject for the cell
        GameObject cellObject = new GameObject("GridCellPrefab");
        
        // Add a sprite renderer
        SpriteRenderer spriteRenderer = cellObject.AddComponent<SpriteRenderer>();
        
        // Create a white square texture
        Texture2D texture = new Texture2D(32, 32);
        Color[] colors = new Color[32 * 32];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        texture.SetPixels(colors);
        texture.Apply();
        
        // Create a sprite from the texture
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32);
        
        // Set the sprite in the renderer
        spriteRenderer.sprite = sprite;
        
        // Set the color to a semi-transparent white
        spriteRenderer.color = new Color(1f, 1f, 1f, 0.2f);
        
        try
        {
            // Add the GridCellVisual script
            cellObject.AddComponent<GridCellVisual>();
            
            // Create the prefab path
            string prefabPath = "Assets/Prefabs/GridCellPrefab.prefab";
            
            // Create directories if they don't exist
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // Create the prefab using the appropriate method for your Unity version
            GameObject prefab = null;
            
            #if UNITY_2018_3_OR_NEWER
                prefab = PrefabUtility.SaveAsPrefabAsset(cellObject, prefabPath);
            #else
                prefab = PrefabUtility.CreatePrefab(prefabPath, cellObject);
            #endif
            
            // Display a confirmation message
            if (prefab != null)
            {
                //Debug.Log("Grid Cell Prefab created successfully at " + prefabPath);
                EditorGUIUtility.PingObject(prefab);
            }
            else
            {
               // Debug.LogError("Failed to create Grid Cell Prefab!");
            }
        }
        catch (System.Exception e)
        {
            //Debug.LogError("Error creating prefab: " + e.Message);
        }
        finally
        {
            // Always clean up the temporary GameObject
            Object.DestroyImmediate(cellObject);
        }
    }
}
#endif