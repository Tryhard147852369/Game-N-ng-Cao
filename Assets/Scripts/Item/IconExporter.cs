using UnityEngine;
using System.IO;

public class IconExporter : MonoBehaviour
{
    public string iconName = "Axe_Icon"; // Tên file ảnh sẽ lưu

    [ContextMenu("Export PNG")] // Nút bấm nhanh trong Unity
    public void ExportPNG()
    {
        Camera cam = GetComponent<Camera>();
        if (cam == null) cam = Camera.main;

        // Tạo Texture tạm thời để chụp ảnh
        int width = 512;  // Kích thước chiều rộng (px)
        int height = 512; // Kích thước chiều cao (px)

        RenderTexture rt = new RenderTexture(width, height, 24);
        cam.targetTexture = rt;
        Texture2D screenShot = new Texture2D(width, height, TextureFormat.RGBA32, false);

        cam.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        screenShot.Apply();

        // Dọn dẹp Target Texture của Camera
        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        // Chuyển thành file PNG nền trong suốt
        byte[] bytes = screenShot.EncodeToPNG();
        string folderPath = Application.dataPath + "/Icons";

        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string filePath = folderPath + "/" + iconName + ".png";
        File.WriteAllBytes(filePath, bytes);

#if UNITY_EDITOR
        UnityEditor.AssetDatabase.Refresh(); // Tự động reload lại folder Assets trong Unity
#endif
        Debug.Log("Đã tạo Icon thành công tại: Assets/Icons/" + iconName + ".png");
    }
}