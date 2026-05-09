using UnityEngine;
using UnityEditor;
using System.IO;

public class MakeSprite
{
    [MenuItem("Tools/Make Perfect Button")]
    public static void CreateSprite()
    {
        int size = 100;
        int r = 24; // Это радиус мягкого закругления
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);

        float cx = size / 2f;
        float cy = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Вычисляем форму и прозрачность
                float dx = Mathf.Max(Mathf.Abs(x - cx) - (cx - r), 0);
                float dy = Mathf.Max(Mathf.Abs(y - cy) - (cy - r), 0);
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // Делаем края гладкими (Anti-aliasing)
                float alpha = Mathf.Clamp01(r - d + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        // Проверяем, есть ли папка Sprites, если нет - создаем
        if (!Directory.Exists("Assets/Sprites"))
        {
            Directory.CreateDirectory("Assets/Sprites");
        }

        // Сохраняем идеальный PNG
        File.WriteAllBytes("Assets/Sprites/PerfectButton.png", tex.EncodeToPNG());
        AssetDatabase.Refresh();
        Debug.Log("Магия сработала! Ищи PerfectButton в папке Sprites.");
    }
}