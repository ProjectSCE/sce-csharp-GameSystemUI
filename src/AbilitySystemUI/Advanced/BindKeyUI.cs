#if CLIENT
using GameUI.Control.Primitive;
using GameCore.Platform.SDL;
using GameCore.ResourceType;
using GameUI.Brush;
using GameUI.Enum;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameUI.TriggerEvent;
using Events;
using GameCore.Event;
using GameUI.Control.Enum;
using System.Drawing;
using GameUI.Struct;
using System.Collections.ObjectModel;
using GameUI.Control.Struct;


namespace GameSystemUI.AbilitySystemUI.Advanced;
/// <summary>
/// 按键绑定UI控件，继承自Panel，包含背景图片和标签
/// </summary>
public class BindKeyUI : Panel
{
    
    /// <summary>
    /// 背景图片属性
    /// </summary>
    public Image BackgroundImage 
    { 
        get => _backgroundImage; 
        set 
        { 
            _backgroundImage = value;  
            this.Image = _backgroundImage.Path;
        } 
    }
    
    private Image _backgroundImage = new Image("@gameui/image/control/底框_快捷键.png");
    
    /// <summary>
    /// 标签子控件
    /// </summary>
    public Label Label { get; private set; }
    
    /// <summary>
    /// 标签文本（快捷访问Label.Text）
    /// </summary>
    public string? Text 
    { 
        get => Label?.Text; 
        set 
        {
            if (Label != null && Label.IsValid)
            {
                Label.Text = value;
            }
        }
    }
    
    /// <summary>
    /// 绑定的按键（设置为null解除绑定）
    /// </summary>
    public VirtualKey? BindKey 
    { 
        get => _boundKey;
        set
        {
            if (_boundKey == value) return;
            
            _boundKey = value;
            
            // 更新 KeyboardAccelerators
            UpdateKeyboardAccelerators();
            
            // 更新显示文本
            if (_boundKey.HasValue)
            {
                if (Label != null && Label.IsValid)
                {
                    Label.Text = GetDisplayText(_boundKey.Value);
                }
            }
            else
            {
                if (Label != null && Label.IsValid)
                {
                    Label.Text = "";
                }
            }
        }
    }
    
    private VirtualKey? _boundKey;
    
    /// <summary>
    /// 按键被按下时的事件
    /// </summary>
    public event Action<BindKeyUI, VirtualKey>? KeyPressed;
    
    /// <summary>
    /// 按键被松开时的事件
    /// </summary>
    public event Action<BindKeyUI, VirtualKey>? KeyReleased;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    public BindKeyUI()
    {
        this.Width = 50;
        // this.Margin = new Thickness(10, 0, 10, 0);
        this.Height = 33;
        this.Image = "@gameui/image/control/底框_快捷键.png";
        Label = new Label()
        {
            Text = "",
            TextTrimming = TextTrimming.Shrink,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Center,
            FontSize = 26,
            Bold = true,
            TextColor = Color.Black,
        };
        this.AddChild(Label);
        
        // 初始化 KeyboardAccelerators
        InitializeKeyboardAccelerators();
    }
    
    /// <summary>
    /// 获取按键的显示文本（对数字键进行特殊处理）
    /// </summary>
    /// <param name="key">虚拟按键</param>
    /// <returns>显示文本</returns>
    private static string GetDisplayText(VirtualKey key)
    {
        var keyName = key.ToString();
        
        // 特判数字键：Number1 -> 1, Number2 -> 2, 等等
        if (keyName.StartsWith("Number") && keyName.Length > 6)
        {
            var numberPart = keyName.Substring(6); // 提取"Number"之后的部分
            return numberPart;
        }
        
        return keyName;
    }
    
    /// <summary>
    /// 初始化键盘加速器
    /// </summary>
    private void InitializeKeyboardAccelerators()
    {
        // 初始化空的 KeyboardAccelerators 集合
        KeyboardAccelerators = new ObservableCollection<KeyboardAccelerator>();

        // 绑定事件处理器
        OnKeyboardAcceleratorInvoked += OnKeyboardAcceleratorInvokedHandler;
        OnKeyboardAcceleratorReleased += OnKeyboardAcceleratorReleasedHandler;
    }
    
    /// <summary>
    /// 更新键盘加速器集合
    /// </summary>
    private void UpdateKeyboardAccelerators()
    {
        // 清空现有的加速器
        KeyboardAccelerators?.Clear();
        
        // 如果有绑定的按键，添加到集合中
        if (_boundKey.HasValue && KeyboardAccelerators != null)
        {
            KeyboardAccelerators.Add(new KeyboardAccelerator { Key = _boundKey.Value });
        }
    }
    
    /// <summary>
    /// 按键按下事件处理
    /// </summary>
    private void OnKeyboardAcceleratorInvokedHandler(object? sender, KeyboardAcceleratorEventArgs e)
    {
        try
        {
            // 触发按键按下事件
            KeyPressed?.Invoke(this, e.Accelerator.Key);
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "BindKeyUI按键按下事件处理时发生错误：{Key}", e.Accelerator.Key);
        }
    }

    /// <summary>
    /// 按键释放事件处理
    /// </summary>
    private void OnKeyboardAcceleratorReleasedHandler(object? sender, KeyboardAcceleratorEventArgs e)
    {
        try
        {
            // 触发按键释放事件
            KeyReleased?.Invoke(this, e.Accelerator.Key);
        }
        catch (Exception ex)
        {
            Game.Logger.LogError(ex, "BindKeyUI按键释放事件处理时发生错误：{Key}", e.Accelerator.Key);
        }
    }
    
    
    /// <summary>
    /// 释放托管资源
    /// </summary>
    protected override void DisposeManaged()
    {
        try
        {
            // 清理按键绑定
            _boundKey = null;
            
            // 清理 KeyboardAccelerators
            KeyboardAccelerators?.Clear();
            
            // 清理事件
            KeyPressed = null;
            KeyReleased = null;
            
            // 解除事件绑定
            OnKeyboardAcceleratorInvoked -= OnKeyboardAcceleratorInvokedHandler;
            OnKeyboardAcceleratorReleased -= OnKeyboardAcceleratorReleasedHandler;
        }
        catch (Exception ex)
        {
            Game.Logger?.LogWarning("BindKeyUI释放资源时出错: {ex}", ex.Message);
        }
        finally
        {
            base.DisposeManaged();
        }
    }
}
#endif
