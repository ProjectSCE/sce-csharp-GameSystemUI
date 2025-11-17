#if CLIENT
using EngineInterface.Enum;
using GameUI.Device;
using GameCore;
using System;
namespace GameSystemUI;

public static class Utility
{
    /// <summary>
    /// 获取镜头角度偏移量（基于当前活跃镜头的旋转角度）
    /// </summary>
    /// <returns>镜头yaw角度 + 90度的偏移值</returns>
    public static float GetCameraAngleOffset()
    {
        try
        {
            // 获取当前活跃的镜头
            var camera = DeviceInfo.PrimaryViewport.Camera;
            if (camera == null) return 0f;
            
            // 获取镜头的Transform信息
            var cameraTransform = camera.Transform;
            
            // 获取Y轴旋转角度（通常用于水平旋转）
            // CameraRotation.Yaw 表示围绕Y轴的旋转（水平旋转）
            float yawAngle = cameraTransform.Rotation.Yaw;
            
            // 返回Y轴旋转角度作为偏移量
            // 注意：可能需要根据具体的坐标系调整角度
            return yawAngle + 90;
        }
        catch (Exception ex)
        {
            Game.Logger.LogWarning("获取当前镜头角度失败：{ex}", ex.Message);
            return 0f;
        }
    }
    public static bool IsPC()
    {
        var platform = DeviceInfo.Platform;
        return platform == Platform.Windows || 
            platform == Platform.macOS || 
            platform == Platform.tvOS ||
            platform == Platform.Linux ||
            platform == Platform.Browser || //浏览器暂时放到pc吧
            platform == Platform.RaspberryPi; //真有人用树莓派玩游戏啊
    }

    public static bool IsMobile()
    {
        var platform = DeviceInfo.Platform;
        return platform == Platform.Android || 
            platform == Platform.iOS || 
            platform == Platform.WeChatMiniProgram ||
            platform == Platform.EditorMobileSimulation;
    }
}
#endif