#if CLIENT
using GameUI.Control.Primitive;
using GameUI.ConversationSystem.Proto;
using GameUI.ConversationSystem.Data;
using GameCore.Timers;
using System.Drawing;
using GameUI.Struct;
using GameUI.ConversationSystem.Data.Enum;
using GameUI.ConversationSystem;
using GameUI.Control.Struct;
using GameCore.GameSystem.Enum;
using System.Diagnostics;
using GameSystemUI.ConversationSystemUI.Data;

namespace GameSystemUI.ConversationSystemUI.Advanced;

/// <summary>
/// 对话面板的默认实现
/// </summary>
public class ConversationPanel : ConversationPanelBase
{
    private Panel backgroundPanel = null!;
    private Panel portraitPanel = null!;
    private Label nameLabel = null!;
    private Label textLabel = null!;
    private PanelScrollable choicesPanel = null!;
    private Panel mainPanel = null!;
    private Panel LinePanel = null!;
    private Button skipButton = null!;
    private Panel textPanel = null!;
    private Panel canFinishPanel = null!;
    private PortraitSide portraitSide = PortraitSide.Left;
    private TypewriterEffectBehavior? typewriterEffect;
    private BubbleUI bubbleUI = null!;  // 重用的BubbleUI实例
    
    /// <summary>
    /// 是否启用打字机效果（默认关闭）
    /// </summary>
    public bool EnableTypewriter
    {
        get => typewriterEffect?.Enabled ?? false;
        set
        {
            if (typewriterEffect != null)
            {
                typewriterEffect.Enabled = value;
            }
        }
    }
    
    /// <summary>
    /// 打字机速度：每帧显示的字符数（默认0.5）
    /// </summary>
    public float TypewriterSpeed
    {
        get => typewriterEffect?.CharsPerFrame ?? 0.5f;
        set
        {
            if (typewriterEffect != null)
            {
                typewriterEffect.CharsPerFrame = value;
            }
        }
    }
    
    /// <summary>
    /// BubbleUI模板配置
    /// </summary>
    public IGameLink<GameDataControlBubbleUI> BubbleUITemplate { get; set; } = ScopeData.Control.DefaultBubbleUI;
    
    /// <summary>
    /// BubbleUI气泡方向
    /// </summary>
    public BubbleDirection BubbleDirection { get; set; } = BubbleDirection.Right;
    
    /// <summary>
    /// 根据立绘位置和是否有立绘更新布局对齐方式
    /// </summary>
    /// <param name="side">立绘位置（左侧或右侧）</param>
    private void UpdatePortraitSide(PortraitSide side)
    {
        portraitSide = side;
        
        // 检查是否有立绘（通过Image是否为空来判断）
        bool hasPortrait = !string.IsNullOrEmpty(portraitPanel.Image);
        
        if (hasPortrait)
        {
            // 有立绘：按照PortraitSide调整布局
            if(portraitSide == PortraitSide.Left){
                this.nameLabel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
                this.LinePanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right;
                this.portraitPanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
            }else{
                this.nameLabel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right;
                this.LinePanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
                this.portraitPanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right;
            }
        }
        else
        {
            // 没有立绘：其他元素居中
            this.nameLabel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center;
            this.LinePanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center;
            // portraitPanel的对齐不重要，因为它不可见
        }
    }
    
    /// <summary>
    /// 根据立绘位置更新选择面板位置
    /// 选择面板与立绘位置相反，默认为右侧
    /// </summary>
    /// <param name="portraitSide">立绘位置（左侧或右侧）</param>
    private void UpdateChoicesPanelPosition(PortraitSide portraitSide)
    {
        // 立绘在左侧时，选择面板在右侧
        // 立绘在右侧时，选择面板在左侧
        if (portraitSide == PortraitSide.Left)
        {
            this.choicesPanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right;
        }
        else
        {
            this.choicesPanel.HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left;
        }
    }
    public bool BackgroundVisible{
        get{
            return backgroundPanel.Visible;
        }
        set{
            backgroundPanel.Visible = value;
        }
    }

    public ConversationPanel() : this(null)
    {
    }

    public ConversationPanel(IGameLink<GameDataControlConversationPanel>? link)
    {
        var data = link?.Data;
        
        this.WidthStretchRatio = 1.0f;
        this.WidthCompactRatio = 1.0f;
        this.HeightStretchRatio = 1.0f;
        this.HeightCompactRatio = 1.0f;

        this.ZIndex = StandardUIType.Dialogue.ExpectedZIndex ?? 0;

        this.backgroundPanel = new Panel(){
            WidthStretchRatio = 1.0f,
            HeightStretchRatio = 1.0f,
            WidthCompactRatio = 1.0f,
            HeightCompactRatio = 1.0f,
            Image = data?.BackgroundImage?.Path ?? "@gameui/image/conversation/背景.png",
        };
        this.mainPanel = new Panel(){
            Width = 1820,
            HeightStretchRatio = 1.0f,
            Margin = new Thickness(left: 254, top: 0, right: 254, bottom: 0),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Center,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
        };
        this.portraitPanel = new Panel(){
            Width = 600,
            Height = 600,
            Image = "@gameui/image/conversation/英雄立绘.png",
            Visible = false,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
        };
        this.nameLabel = new Label(){
            FontSize = 52,
            TextColor = Color.FromArgb(255, 224, 207, 172),
            Bold = true,
        };
        var splitLine = new Panel(){
            WidthStretchRatio = 1.0f,
            WidthCompactRatio = 1.0f,
            Height = 10,
            Image = data?.SplitLineImage?.Path ?? "@gameui/image/conversation/分割线.png",
        };
        this.textPanel = new Panel(){
            WidthStretchRatio = 1.0f,
            WidthCompactRatio = 1.0f,
            Margin = new Thickness(left: 0, top: 10, right: 0, bottom: 0),
            Height = 140,
        };
        var textScrollable = new PanelScrollable(){
            WidthStretchRatio = 1.0f,
            WidthCompactRatio = 1.0f,
            Height = 90,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
        };
        this.textLabel = new Label(){
            WidthStretchRatio = 1.0f,
            WidthCompactRatio = 1.0f,
            Height = 90,
            TextColor = Color.White,
            FontSize = 32,
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,
            VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top,
        };
        this.canFinishPanel = new Panel(){
            Width = 52,
            Height = 36,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            Visible = false,
            Image = data?.CanFinishIndicatorImage?.Path ?? "@gameui/image/conversation/可完成.png",
        };
        this.LinePanel = new Panel(){
            Width = 1230,
            Height = 200,
            Visible = false,
            Margin = new Thickness(left: 0, top: 0, right: 0, bottom: 80),
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            FlowOrientation = GameUI.Enum.Orientation.Vertical,
            VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top,
        };
        this.choicesPanel = new PanelScrollable(){
            Width = 600,
            Height = 600,
            Margin = new Thickness(left: 254, top: 0, right: 254, bottom: 310),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,  // 始终居右
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Bottom,
            ItemTemplate = data?.ChoiceItemTemplate ?? ScopeData.Control.DefaultConversationChoiceItem,
            FlowOrientation = GameUI.Enum.Orientation.Vertical,
        };
        this.skipButton = new Button(){
            Width = 234,
            Height = 126,
            Margin = new Thickness(left: 0, top: 20, right: 20, bottom: 0),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Right,
            VerticalAlignment = GameUI.Enum.VerticalAlignment.Top,
            Visible = false,
            Image = data?.SkipButtonImage?.Path ?? "@gameui/image/conversation/跳过.png",
        };
        skipButton.AddChild(new Label(){
            Text = "跳过",
            FontSize = 42,
            TextColor = Color.FromArgb(255, 209, 204, 198),
            HorizontalAlignment = GameUI.Enum.HorizontalAlignment.Left,
            Margin = new Thickness(left: 50, top: 0, right: 0, bottom: 0),
        });

        this.AddChild(this.backgroundPanel);
        this.AddChild(this.mainPanel);
        this.AddChild(this.choicesPanel);
        this.AddChild(this.skipButton);

        mainPanel.AddChild(this.LinePanel);
        mainPanel.AddChild(this.portraitPanel);
        
        this.LinePanel.AddChild(this.nameLabel);
        this.LinePanel.AddChild(splitLine);
        this.LinePanel.AddChild(this.textPanel);
        
        this.textPanel.AddChild(textScrollable);
        this.textPanel.AddChild(canFinishPanel);
        textScrollable.AddChild(this.textLabel);

        // 初始化打字机效果（从 GameData 读取配置）
        typewriterEffect = new TypewriterEffectBehavior(textLabel)
        {
            CharsPerFrame = data?.TypewriterSpeed ?? 0.5f,
            Enabled = data?.EnableTypewriter ?? false,
        };
        
        // 从 GameData 读取 BubbleUI 配置
        if (data?.BubbleUITemplate != null)
        {
            BubbleUITemplate = data.BubbleUITemplate;
        }
        BubbleDirection = data?.BubbleDirection ?? BubbleDirection.Right;

        // 创建 BubbleUI 实例（默认隐藏）
        bubbleUI = new BubbleUI(BubbleUITemplate)
        {
            EnableTypewriter = data?.EnableTypewriter ?? false,
            TypewriterSpeed = data?.TypewriterSpeed ?? 0.5f,
            Direction = BubbleDirection,
        };
        bubbleUI.Hide();  // 默认隐藏

        // 订阅选择项初始化事件
        choicesPanel.OnChildPostInitialization += OnChoiceItemInitialized;

        this.OnPointerClicked += (sender, e) =>
        {
            // BubbleUI模式：处理打字机效果和完成逻辑
            if (bubbleUI.Visible)
            {
                if (bubbleUI.IsTypewriterPlaying)
                {
                    bubbleUI.CompleteTypewriter();
                }
                else if (CanFinish)
                {
                    CompleteLineDisplay();
                }
                return;
            }
            
            // LinePanel模式：如果打字机正在播放，点击跳过效果
            if (typewriterEffect?.IsPlaying == true)
            {
                typewriterEffect.Complete();
            }
            else if (CanFinish)
            {
                // 只有 CanFinish 为 true 时，才能点击完成台词显示
                CompleteLineDisplay();
            }
        };

        skipButton.OnPointerClicked += (sender, e) => HandleSkip();
        Visible = false;
    }
    
    /// <summary>
    /// 选择项初始化时的处理
    /// </summary>
    private void OnChoiceItemInitialized(object? sender, ApplyControlTemplateEventArgs e)
    {
        if(e.Control is ConversationChoiceItem choiceItem && choiceItem.DataContext is ChoiceDataContext choiceDataContext)
        {
            choiceItem.ChoiceInfo = choiceDataContext.ChoiceInfo;
            choiceItem.OnPointerClicked += OnChoiceItemClicked;
            
            // 设置图标（如果有）
            if (!string.IsNullOrEmpty(choiceDataContext.IconPath))
            {
                choiceItem.SetIcon(choiceDataContext.IconPath);
            }
        }
    }
    
    /// <summary>
    /// 处理跳过操作：立即完成打字机效果并完成台词显示
    /// </summary>
    private void HandleSkip()
    {
        if (bubbleUI.Visible)
        {
            bubbleUI.CompleteTypewriter();
        }
        else if (typewriterEffect?.IsPlaying == true)
        {
            typewriterEffect.Complete();
        }
        CompleteLineDisplay();
    }
    
    private void OnChoiceItemClicked(object? sender, PointerEventArgs e)
    {
        if(sender is ConversationChoiceItem choiceItem && choiceItem.DataContext is ChoiceDataContext choiceDataContext)
        {
            // 检查选择项是否可用，不可用则不响应点击
            if (choiceDataContext.ChoiceInfo?.IsEnabled == false)
            {
                return;
            }
            
            CompleteChoiceSelection(choiceDataContext.ChoiceIndex);
            choicesPanel.Visible = false;
        }
    }

    private void ClearLine()
    {
        bubbleUI.Hide();
        nameLabel.Text = string.Empty;
        textLabel.Text = string.Empty;
        portraitPanel.Image = "";
        typewriterEffect?.Stop();
        BackgroundVisible = false;
    }

    private void ClearChoices()
    {
        // 清理前先取消所有现有选择项的事件订阅，防止内存泄漏
        UnsubscribeAllChoiceItems();
        
        choicesPanel.ItemsSource = null;
        choicesPanel.GenerateChildren();
    }
    
    /// <summary>
    /// 取消所有选择项的事件订阅，防止内存泄漏
    /// </summary>
    private void UnsubscribeAllChoiceItems()
    {
        if (choicesPanel?.Children == null) return;
        
        foreach (var child in choicesPanel.Children)
        {
            if (child is ConversationChoiceItem choiceItem)
            {
                choiceItem.OnPointerClicked -= OnChoiceItemClicked;
            }
        }
    }
    
    public override void Clear()
    {
        ClearLine();
        ClearChoices();
    }

    public override void Hide()
    {
        this.Visible = false;
    }

    public override void ShowChoices(List<ConversationChoiceInfo> choices, ConversationChoicePromptInfo? promptInfo, GameDataConversationChoiceGroup? choiceGroupData)
    {
        ClearChoices();
        
        this.Visible = true;
        choicesPanel.Visible = true;
        
        // 默认选择面板在右侧
        PortraitSide portraitSideForChoices = PortraitSide.Left;
        
        // 如果有提示信息，根据DisplayMode决定显示方式
        if (promptInfo != null && !string.IsNullOrEmpty(promptInfo.PromptText))
        {
            // 记录立绘位置，用于决定选择面板位置
            portraitSideForChoices = promptInfo.PortraitSide;
            
            if (promptInfo.DisplayMode == ConversationDisplayMode.OnUnit)
            {
                // OnUnit 模式：使用 BubbleUI 显示提示信息
                // 注意：这里需要有单位引用，但promptInfo中没有提供单位信息
                // 所以 OnUnit 模式在选择提示中可能需要额外处理
                BackgroundVisible = false;
            }
            else
            {
                // UI 模式（默认）：使用 LinePanel 显示提示信息
                BackgroundVisible = true;
                LinePanel.Visible = true;
                portraitPanel.Visible = true;
                
                // 显示提示角色名称
                nameLabel.Text = promptInfo.CharacterName ?? "";
                
                // 显示提示文本
                textLabel.Text = promptInfo.PromptText;
                
                // 显示提示立绘
                portraitPanel.Image = promptInfo.Portrait?.Path ?? "";
                
                // 更新布局（在设置Image之后调用，确保能正确判断是否有立绘）
                UpdatePortraitSide(promptInfo.PortraitSide);
            }
        }
        
        // 更新选择面板位置：与立绘位置相反，默认为右侧
        UpdateChoicesPanelPosition(portraitSideForChoices);
        
        // 创建 ChoiceDataContext 列表，包含图标路径
        var items = choices.Select((choice, index) => 
        {
            string? iconPath = null;
            
            // 从 choiceGroupData 中获取对应的图标路径
            if (choiceGroupData?.Choices != null && index < choiceGroupData.Choices.Count)
            {
                var choiceData = choiceGroupData.Choices[index]?.Data;
                if (choiceData?.Icon != null)
                {
                    iconPath = choiceData.Icon.Value.Path;
                }
            }
            
            return new ChoiceDataContext() 
            { 
                ChoiceIndex = index, 
                ChoiceInfo = choice,
                IconPath = iconPath
            };
        }).ToList();
        
        choicesPanel.ItemsSource = items;
        choicesPanel.GenerateChildren();
    }

    public override void ShowLine(ConversationLineInfo lineInfo, GameDataConversationLine? lineData)
    {

        ClearLine();

        this.Visible = true;
        // 根据lineInfo.AllowSkip控制跳过按钮的显示
        skipButton.Visible = lineInfo.AllowSkip;

        // 准备显示文本
        string displayText;
        displayText = lineInfo.Text ?? "???";


        var character = lineData?.Character;
        var unit = character?.Data?.InGameUnit;
        
        // 根据 DisplayMode 决定使用哪种显示方式
        if (lineInfo.DisplayMode == ConversationDisplayMode.OnUnit && unit != null && unit.IsValid)
        {
            BackgroundVisible = false;
            // OnUnit 模式：重用 BubbleUI 在单位头顶显示
            bubbleUI.EnableTypewriter = EnableTypewriter;
            bubbleUI.TypewriterSpeed = TypewriterSpeed;
            bubbleUI.Direction = BubbleDirection;
            bubbleUI.BindUnit = unit;
            bubbleUI.Show();
            bubbleUI.ShowText(displayText);
        }
        else
        {
            // UI 模式（默认）：使用 LinePanel 在对话框中显示
            BackgroundVisible = true;
            LinePanel.Visible = true;
            portraitPanel.Visible = true;
            
            portraitPanel.Image = lineData?.Character?.Data?.Portrait?.Path ?? "";
            nameLabel.Text = lineData?.Character?.Data?.DisplayName ?? "???";
            
            // 更新布局（在设置Image之后调用，确保能正确判断是否有立绘）
            UpdatePortraitSide(lineData?.PortraitSide ?? PortraitSide.Left);
            
            // 根据打字机效果启用状态决定如何显示文本
            if (typewriterEffect != null && typewriterEffect.Enabled)
            {
                typewriterEffect.Play(displayText);
            }
            else
            {
                // 直接显示完整文本
                textLabel.Text = displayText;
            }
        }
    }

    protected override void CompleteLineDisplay()
    {
        // 顺序很重要
        if(bubbleUI.Visible)
        {
            bubbleUI.CompleteTypewriter();
        }
        base.CompleteLineDisplay();

    }

    /// <summary>
    /// 隐藏台词显示UI（文本、角色名、立绘等）
    /// </summary>
    public override void HideLine()
    {   
        // 停止打字机效果
        if(typewriterEffect?.IsPlaying == true)
        {
            typewriterEffect.Stop();
        }
        
        // 隐藏BubbleUI（不销毁，保留实例）
        bubbleUI.Hide();

        LinePanel.Visible = false;
        portraitPanel.Visible = false;
        BackgroundVisible = false;
        skipButton.Visible = false;
    }
    
    /// <summary>
    /// 当 CanFinish 状态改变时的回调
    /// </summary>
    protected override void OnCanFinishChanged(bool canFinish)
    {       
        // 更新可完成指示器
        if (bubbleUI.Visible)
        {
            bubbleUI.ShowCanFinish = canFinish;
        }
        else
        {
            canFinishPanel.Visible = canFinish;
        }
    }

    /// <summary>
    /// 隐藏选择列表UI，但保持面板可见
    /// </summary>
    public override void HideChoices()
    {
        // 隐藏选择面板
        choicesPanel.Visible = false;
    }

    public class ChoiceDataContext
    {
        public int ChoiceIndex { get; set; }
        public ConversationChoiceInfo? ChoiceInfo { get; set; }
        public string? IconPath { get; set; }
    }

    protected override void DisposeManaged()
    {
        // 取消订阅事件，防止内存泄漏
        choicesPanel.OnChildPostInitialization -= OnChoiceItemInitialized;
        UnsubscribeAllChoiceItems();
        
        // 清理资源
        typewriterEffect?.Destroy();
        bubbleUI.Destroy();
        
        base.DisposeManaged();
    }
}
#endif

