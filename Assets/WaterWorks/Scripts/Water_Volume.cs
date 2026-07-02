using UnityEngine;
using UnityEngine.Rendering.Universal;

public class Water_Volume : ScriptableRendererFeature
{
    public override void Create()
    {
        Debug.LogWarning("Water_Volume is obsolete in URP 17+. Vui lòng sử dụng tính năng FullScreenPassRendererFeature có sẵn của Unity (Hệ thống đã tự động cấu hình giúp bạn).");
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Script này đã được làm rỗng để sửa lỗi compile error trên Unity 6 (URP 17).
        // Hiệu ứng mặt biển đã được chuyển sang sử dụng FullScreenPassRendererFeature.
    }
}
