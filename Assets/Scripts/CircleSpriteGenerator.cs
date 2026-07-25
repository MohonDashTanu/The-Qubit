using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// This utility script creates a circle sprite and prefab for attack range visualization
public class CircleSpriteGenerator : MonoBehaviour
{
    [MenuItem("Quantum Tower Defense/Create Range Circle")]
    public static void CreateRangeCircle()
    {
        // Create a texture for the circle
        int textureSize = 256;
        Texture2D texture = new Texture2D(textureSize, textureSize);
        Color[] colors = new Color[textureSize * textureSize];
        
        // Fill the texture with a circle
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float distX = x - textureSize/2;
                float distY = y - textureSize/2;
                float dist = Mathf.Sqrt(distX * distX + distY * distY);
                
                // Create a solid circle with soft edges
                if (dist < textureSize/2 - 1)
                {
                    colors[y * textureSize + x] = Color.white;
                }
                else if (dist < textureSize/2)
                {
                    float t = textureSize/2 - dist;
                    colors[y * textureSize + x] = new Color(1, 1, 1, t);
                }
                else
                {
                    colors[y * textureSize + x] = Color.clear;
                }
            }
        }
        
        texture.SetPixels(colors);
        texture.Apply();
        
        // Save the texture as an asset
        string texturePath = "Assets/Textures/RangeCircleTexture.png";
        
        // Create directory if it doesn't exist
        string directory = System.IO.Path.GetDirectoryName(texturePath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        // Save the texture
        byte[] pngData = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(texturePath, pngData);
        AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
        
        // Create a sprite from the texture
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 256;
            importer.filterMode = FilterMode.Bilinear;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate);
        }
        
        // Create a game object with the sprite
        GameObject circleObject = new GameObject("RangeCircle");
        SpriteRenderer spriteRenderer = circleObject.AddComponent<SpriteRenderer>();
        
        // Assign the sprite
        Sprite circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(texturePath);
        spriteRenderer.sprite = circleSprite;
        
        // Set color to semi-transparent yellow
        spriteRenderer.color = new Color(1f, 1f, 0f, 0.2f);
        
        // Set sorting order to be behind qubits
        spriteRenderer.sortingOrder = -1;
        
        // Save as prefab
        string prefabPath = "Assets/Prefabs/RangeCirclePrefab.prefab";
        
        // Create directory if it doesn't exist
        directory = System.IO.Path.GetDirectoryName(prefabPath);
        if (!System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }
        
        // Create the prefab
        PrefabUtility.SaveAsPrefabAsset(circleObject, prefabPath);
        
        // Clean up the scene object
        DestroyImmediate(circleObject);
        
        // Log success and ping the prefab
        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath));
    }
}
#endif