#if CLIENT
using System.Resources;
using System.Reflection;
using System.Linq;
using GameCore.BaseInterface;
using Microsoft.Extensions.Logging;
using GameCore.Localization;

namespace GameSystemUI.Localization;

/// <summary>
/// 本地化设置管理器 - 负责整个程序集的资源管理器配置
/// </summary>
public static class LocalizationSetup
{
    private static ResourceManager? _extendedEnumsResourceManager;
    private static bool _initialized = false;
    
    /// <summary>
    /// 初始化本地化资源管理器
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        
        try
        {
            // 创建扩展枚举的ResourceManager
            _extendedEnumsResourceManager = new ResourceManager(
                "GameSystemUI.Resources.ExtendedEnums", 
                Assembly.GetExecutingAssembly()
            );
            
            // TODO: 添加到LocalizationManager（需要根据实际的LocalizationManager API进行调整）
            // 注：当LocalizationManager API可用时，在此处添加资源管理器注册代码
            LocalizationManager.Primary.AddResourceManager(_extendedEnumsResourceManager);
            
            _initialized = true;
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "本地化设置初始化失败：{Message}", ex.Message);
        }
    }
    
}

/// <summary>
/// 本地化初始化器 - 在游戏启动时自动注册资源管理器
/// </summary>
public class LocalizationInitializer : IGameClass
{
    /// <summary>
    /// 注册游戏类，设置本地化初始化时机
    /// </summary>
    public static void OnRegisterGameClass()
    {
        Game.OnGameDataInitialization += OnGameDataInitialization;
    }
    
    private static void OnGameDataInitialization()
    {
        // 在游戏数据初始化时设置本地化资源
        LocalizationSetup.Initialize();
    }
}
#endif
