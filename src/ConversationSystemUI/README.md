# ConversationSystemUI - 对话UI系统

## 概述

ConversationSystemUI 是一个功能完整的游戏对话系统UI实现，支持传统对话框模式和单位气泡模式两种展示方式，提供了打字机效果、选择分支、跳过控制等丰富功能。

## 核心特性

- 🎭 **双显示模式**: 支持传统对话面板和单位头顶气泡两种显示方式
- ⌨️ **打字机效果**: 文字逐字显示（可选功能，默认关闭），可配置速度和跳过行为
- 🔀 **选择分支**: 支持多选项分支，可配置启用/禁用状态，支持图标显示
- ⏭️ **跳过控制**: 精确控制对话跳过行为（AllowSkip、WaitForConfirmation）
- ⏱️ **自动计时**: 支持对话自动完成和等待确认两种模式
- ⌨️ **键盘支持**: 选择项支持F1-F12快捷键绑定
- 🎨 **高度可定制**: 基于抽象类设计，易于扩展和自定义
- 🧭 **方向配置系统**: BubbleUI支持上下左右四个方向的独立布局配置
- 📋 **GameData配置**: 通过GameData灵活配置UI外观和行为
- 🎯 **智能定位**: 选择面板根据立绘位置自动调整，气泡位置防抖动优化

## 文件结构

```
ConversationSystemUI/
├── Advanced/                           # 高级UI组件
│   ├── ConversationPanelBase.cs      # 对话面板抽象基类
│   ├── ConversationPanel.cs          # 对话面板默认实现
│   ├── ConversationChoiceItem.cs     # 对话选择项UI
│   ├── BubbleUI.cs                   # 气泡UI组件（单位头顶）
│   └── TypewriterEffectBehavior.cs   # 打字机效果行为
├── Data/                              # 数据模板
│   ├── BubbleDirection.cs            # 气泡方向枚举
│   ├── GameDataBubbleUILayout.cs     # 气泡UI方向布局配置
│   ├── GameDataControlBubbleUI.cs    # 气泡UI控制数据模板
│   ├── GameDataControlConversationPanel.cs  # 对话面板配置
│   └── GameDataControlConversationChoiceItem.cs    # 选择项数据模板
├── ScopeData.cs                       # Scope数据配置
└── README.md                          # 本文档
```

## 核心组件

### 1. ConversationPanelBase（对话面板基类）

抽象基类，定义了对话系统的核心逻辑和接口。

#### 核心属性

- **`CanFinish`** (bool): 当前台词是否可以完成
  - `AllowSkip = true`: CanFinish 立即为 true
  - `WaitForConfirmation = true`: 计时器结束后 CanFinish 变为 true

#### 抽象方法（需派生类实现）

```csharp
// 显示台词
public abstract void ShowLine(ConversationLineInfo lineInfo, GameDataConversationLine? lineData);

// 显示选择项
public abstract void ShowChoices(List<ConversationChoiceInfo> choices, ConversationChoicePromptInfo? promptInfo, GameDataConversationChoiceGroup? choiceGroupData);

// 隐藏台词
public abstract void HideLine();

// 隐藏选择项
public abstract void HideChoices();

// 清空对话内容
public abstract void Clear();

// 隐藏对话面板
public abstract void Hide();
```

#### 受保护方法（派生类调用）

```csharp
// 完成台词显示
protected virtual void CompleteLineDisplay();

// 完成选择
protected virtual void CompleteChoiceSelection(int choiceIndex);

// 取消台词显示
protected virtual void CancelLineDisplay();

// 取消选择
protected virtual void CancelChoiceSelection();

// CanFinish状态变化回调
protected virtual void OnCanFinishChanged(bool canFinish);
```

#### CanFinish机制

基类通过属性setter自动触发虚方法回调，派生类重写 `OnCanFinishChanged` 来响应状态变化：

```csharp
public bool CanFinish 
{ 
    get => _canFinish;
    protected set
    {
        if (_canFinish != value)
        {
            _canFinish = value;
            OnCanFinishChanged(value);  // 触发回调
        }
    }
}
```

### 2. ConversationPanel（对话面板）

ConversationPanelBase 的默认实现，包含两种显示模式，支持通过 GameData 配置。

#### UI 布局

```
ConversationPanel
├── backgroundPanel        # 背景面板
├── mainPanel             # 主容器
│   ├── portraitPanel     # 角色立绘（位置决定choicesPanel位置）
│   ├── textPanel         # 文本面板
│   │   ├── nameLabel     # 角色名称
│   │   └── textLabel     # 对话文本
│   └── canFinishPanel    # 可完成提示图标
├── choicesPanel          # 选择项面板（滚动，动态位置）
└── skipButton            # 跳过按钮
```

#### GameData 配置

通过 `GameDataControlConversationPanel` 可以配置以下内容：

- **背景和分隔线**：`BackgroundImage`、`BackgroundColor`、`SplitLineImage`、`SplitLineColor`
- **跳过按钮**：`SkipButtonImage`、`SkipButtonColor`
- **完成提示图标**：`CanFinishIndicatorImage`
- **打字机效果**：`EnableTypewriter`、`TypewriterSpeed`
- **BubbleUI配置**：`BubbleUITemplate`、`BubbleDirection`（默认方向）
- **选择项模板**：`ChoiceItemTemplate`

**注意**：字体大小和颜色（名称、文本、按钮）目前为硬编码，保持一致的UI风格。

```csharp
// 使用GameData创建对话面板
var panelData = new GameDataControlConversationPanel
{
    EnableTypewriter = true,
    TypewriterSpeed = 0.5f,
    BubbleDirection = BubbleDirection.Right,
    // ... 其他配置
};

var panel = new ConversationPanel(panelData.ToLink());
```

#### 两种显示模式

显示模式由 `ConversationLineInfo.DisplayMode` 属性控制：

**1. UI 模式（ConversationDisplayMode.UI）**
- 默认模式
- 显示完整对话框：背景、立绘、文本
- 使用 textPanel 在屏幕底部或中央显示
- 适合剧情对话、任务对话、NPC重要对话

**2. OnUnit 模式（ConversationDisplayMode.OnUnit）**
- 单位头顶显示气泡
- 自动跟随单位位置
- 需要角色绑定游戏单位（InGameUnit）
- 适合环境对话、NPC闲聊、战斗中的简短提示

#### 核心功能实现

**打字机效果配置**
```csharp
// ConversationPanel 提供打字机效果配置属性
public bool EnableTypewriter { get; set; }      // 是否启用打字机效果（默认false）
public float TypewriterSpeed { get; set; }      // 打字速度（默认0.5，每帧显示字符数）

// 启用打字机效果
panel.EnableTypewriter = true;
panel.TypewriterSpeed = 0.5f;  // 每帧0.5个字符
```

**点击行为控制**
```csharp
// 点击行为：
// 1. 打字机播放中 → 立即完成打字机效果
// 2. 打字机结束且CanFinish=true → 完成台词显示
this.OnPointerClicked += (sender, e) =>
{
    if (typewriterEffect?.IsPlaying == true)
    {
        typewriterEffect.Complete();
    }
    else if (CanFinish)
    {
        CompleteLineDisplay();
    }
};
```

**CanFinish状态响应**
```csharp
protected override void OnCanFinishChanged(bool canFinish)
{
    canFinishPanel.Visible = canFinish;  // 显示/隐藏可完成提示图标
}
```

**选择面板智能定位**

选择面板会根据立绘位置自动调整，避免遮挡立绘：

```csharp
// 立绘在左侧时，选择面板显示在右侧
if (promptInfo.PortraitSide == PortraitSide.Left)
{
    choicesPanel.HorizontalAlignment = HorizontalAlignment.Right;
}
// 立绘在右侧或无立绘时，选择面板显示在右侧（默认）
else
{
    choicesPanel.HorizontalAlignment = HorizontalAlignment.Right;
}
```

### 3. ConversationChoicePromptInfo（选择提示信息）

在显示选择项之前，可以提供上下文信息帮助玩家理解选择的背景。所有字段都是可选的。

#### 属性说明

- **`PromptText`** (string?): 提示文本内容
- **`CharacterName`** (string?): 提示角色名称
- **`Portrait`** (Texture?): 提示角色立绘
- **`PortraitSide`** (PortraitSide): 立绘显示位置（Left/Right）
- **`DisplayMode`** (ConversationDisplayMode): 显示模式
  - `UI`: 在对话框中显示（默认）
  - `OnUnit`: 在单位头顶显示气泡

#### 使用示例

```csharp
var promptInfo = new ConversationChoicePromptInfo
{
    PromptText = "你该如何选择？",
    CharacterName = "旁白",
    Portrait = myTexture,
    PortraitSide = PortraitSide.Left,
    DisplayMode = ConversationDisplayMode.UI
};

// 在ShowChoicesAsync中传入promptInfo
int selectedIndex = await panel.ShowChoicesAsync(choices, promptInfo, choiceGroupData);
```

如果 `promptInfo` 为 `null` 或 `PromptText` 为空，则不显示提示信息，直接显示选择项。

### 4. ConversationChoiceItem（对话选择项）

单个选择项的UI实现。

#### 核心属性

- **`ChoiceInfo`**: 选择项信息（文本、启用状态等）
- **`Opacity`**: 根据 `IsEnabled` 自动调整透明度
  - 启用: 1.0f（完全不透明）
  - 禁用: 0.5f（半透明）

#### 图标支持

选择项支持显示图标，通过 `GameDataConversationChoiceItem` 配置：

```csharp
// 在对话数据中配置选择项图标
var choiceData = new GameDataConversationChoiceItem
{
    Icon = iconImage.ToLink(),  // 设置图标
    // ... 其他配置
};

// ConversationPanel会自动处理图标显示
// 图标路径通过 ChoiceDataContext 传递给 ConversationChoiceItem
// 在 OnChoiceItemInitialized 中调用 SetIcon 设置图标
```

#### 快捷键支持

选择项会自动绑定 F1-F12 快捷键（由 ConversationPanel 管理）：
- 第1个选项 → F1
- 第2个选项 → F2
- ...最多12个选项

### 5. BubbleUI（气泡UI）

跟随单位显示的UI气泡组件，支持四个方向的独立布局配置。

#### 核心特性

- 实现 `IThinker` 接口，每帧更新位置
- 世界坐标到UI坐标的自动转换
- 内置打字机效果支持
- **方向配置系统**：支持上下左右四个方向的独立布局
- **防抖动优化**：位置变化超过1像素才更新，避免微小抖动
- 动态宽度计算：根据字符数、字体大小和边距自动调整

#### 方向配置系统

BubbleUI 使用 `BubbleDirection` 枚举定义四个方向：

```csharp
public enum BubbleDirection
{
    Up,      // 上方
    Down,    // 下方
    Left,    // 左侧
    Right    // 右侧（默认）
}
```

每个方向可以通过 `GameDataBubbleUILayout` 独立配置：

```csharp
public class GameDataBubbleUILayout
{
    public Image? BubbleImage { get; set; }      // 气泡背景图片
    public UIPosition UIOffset { get; set; }      // UI坐标偏移（屏幕XY，不受镜头旋转影响）
    public float OffsetZ { get; set; }            // 世界坐标Z轴偏移（深度）
    public int CharsPerLine { get; set; } = 20;   // 每行字符数
    public float FontSize { get; set; } = 36;     // 字体大小
    public Thickness TextMargin { get; set; }     // 文字边距
    public Thickness? SlicedEdges { get; set; }   // 九宫格切片边界
    public string SocketName { get; set; } = "socket_root";  // 绑点名称
}
```

**注意**：
- `Width` 自动计算：`CharsPerLine * FontSize + TextMargin.Left + TextMargin.Right`
- `Height` 固定为 -1（自适应内容高度）
- `HorizontalAlignment` 和 `VerticalAlignment` 固定为 Center

#### GameData 配置示例

```csharp
// 配置气泡UI的不同方向布局
var bubbleUIData = new GameDataControlBubbleUI
{
    DefaultDirection = BubbleDirection.Right,
    DirectionLayouts = new Dictionary<BubbleDirection, IGameLink<GameDataBubbleUILayout>>
    {
        [BubbleDirection.Up] = new GameDataBubbleUILayout
        {
            BubbleImage = upBubbleImage,
            UIOffset = new UIPosition(0, -20),  // 屏幕坐标向上偏移20
            OffsetZ = -50,  // 世界坐标向后偏移50（避免遮挡）
            CharsPerLine = 15,
            FontSize = 32,
            TextMargin = new Thickness(20, 15, 20, 15),
            SocketName = "socket_head"  // 跟随头部绑点
        }.ToLink(),
        
        [BubbleDirection.Right] = new GameDataBubbleUILayout
        {
            BubbleImage = rightBubbleImage,
            UIOffset = new UIPosition(10, 0),  // 屏幕坐标向右偏移10
            OffsetZ = -75,  // 世界坐标向后偏移75
            CharsPerLine = 20,
            FontSize = 36,
            TextMargin = new Thickness(26, 10, 15, 15),
            SocketName = "socket_root"  // 跟随根绑点（默认）
        }.ToLink(),
        
        // ... Down 和 Left 配置
    },
    FontSize = 36,
    TypewriterSpeed = 0.5f,
    CanFinishIndicatorImage = indicatorImage
};
```

#### 使用示例

```csharp
// 方式1：使用默认配置创建
var bubble = new BubbleUI();
bubble.BindUnit = unit;
bubble.Direction = BubbleDirection.Up;  // 设置方向（自动应用对应配置）

// 方式2：通过GameData模板创建
var bubble = new BubbleUI(bubbleUITemplate);
bubble.BindUnit = unit;
// Direction 已从 DefaultDirection 初始化

// 方式3：在 ConversationPanel 中使用
// 面板会根据配置的 BubbleDirection 创建气泡
var panel = new ConversationPanel(panelData.ToLink());
// 当显示 OnUnit 模式对话时，自动使用配置的方向

// 显示文本
bubble.EnableTypewriter = true;
bubble.TypewriterSpeed = 0.5f;
bubble.ShowText("你好，世界！");

// 动态切换方向
bubble.Direction = BubbleDirection.Left;  // 气泡会立即应用新方向的配置

// 监听点击事件
bubble.OnPointerClicked += (s, e) => {
    if (bubble.IsTypewriterPlaying)
        bubble.CompleteTypewriter();
};
```

#### 防抖动机制

BubbleUI 实现了位置更新的防抖动机制：

```csharp
// Think 方法中的防抖动逻辑
var newPosition = CalculatePosition();
float deltaX = Math.Abs(newPosition.X - _lastPosition.X);
float deltaY = Math.Abs(newPosition.Y - _lastPosition.Y);

const float threshold = 1.0f;  // 1像素阈值
if (deltaX > threshold || deltaY > threshold)
{
    this.Position = newPosition;
    _lastPosition = newPosition;
}
```

这确保了由于 `ActualSize` 微小变化导致的位置抖动不会影响视觉效果。

### 6. TypewriterEffectBehavior（打字机效果）

文字逐字显示的效果实现。

#### 核心属性

- **`Enabled`** (bool): 是否启用打字机效果
- **`CharsPerFrame`** (float): 每帧显示的字符数（控制速度）
- **`IsPlaying`** (bool): 是否正在播放

#### 使用方法

```csharp
var typewriter = new TypewriterEffectBehavior(label);
typewriter.CharsPerFrame = 0.5f;  // 每帧0.5个字符
typewriter.Enabled = true;

// 播放文本
typewriter.Play("这是需要逐字显示的文本");

// 立即完成
typewriter.Complete();

// 停止播放
typewriter.Stop();
```

## 对话模式详解

### 四种台词显示模式

系统根据 `AllowSkip` 和 `WaitForConfirmation` 的组合，提供四种不同的显示模式：

#### 1. 立即跳过模式
```
AllowSkip = true
```
- ✅ CanFinish 立即为 true
- ✅ 跳过按钮可见
- ✅ canFinishPanel 可见
- ✅ 随时可以点击完成

**适用场景**: 常规剧情对话，玩家可随时跳过

#### 2. 自动完成模式
```
AllowSkip = false
WaitForConfirmation = false
Duration = 3秒
```
- ⏱️ Duration 计时器结束后自动完成
- ❌ 无跳过按钮
- ❌ 无 canFinishPanel
- ❌ 点击无法跳过

**适用场景**: 强制播放的过场动画对话

#### 3. 等待确认模式（重要）
```
AllowSkip = false
WaitForConfirmation = true
Duration = 3秒
```
- ⏱️ CanFinish 初始为 false
- ⏱️ Duration 结束后 CanFinish 变为 true
- ✅ canFinishPanel 显示（计时器结束后）
- ✅ 只能在计时器结束后点击完成

**适用场景**: 重要剧情对话，确保玩家看完最短时长后才能继续

#### 4. 强制等待模式
```
AllowSkip = false
WaitForConfirmation = true
Duration = 无
```
- ❌ CanFinish 始终为 false
- ❌ 无法通过点击完成
- 🔧 需要其他逻辑触发完成

**适用场景**: 需要特殊事件触发才能继续的对话（如等待动画播放完成）

### 打字机效果与跳过的关系

无论 `CanFinish` 状态如何，**打字机播放时点击都会立即完成打字机效果**：

```csharp
if (typewriterEffect?.IsPlaying == true)
{
    typewriterEffect.Complete();  // 始终允许跳过打字机效果
}
else if (CanFinish)
{
    CompleteLineDisplay();  // 只有CanFinish=true时才能完成台词
}
```

这样设计的好处：
- 玩家可以快速看完文字
- 但不能跳过重要对话的最短显示时间

## 快速开始

### 1. 基本使用

对话系统通常由对话脚本系统自动调用，但也可以手动使用：

```csharp
// 获取对话面板实例（由系统管理）
var panel = ConversationPanel.Instance;

// (可选) 启用打字机效果
if (panel is ConversationPanel defaultPanel)
{
    defaultPanel.EnableTypewriter = true;
    defaultPanel.TypewriterSpeed = 0.5f;
}

// 显示台词（UI模式 - 默认）
var lineInfo = new ConversationLineInfo 
{
    Text = "你好，欢迎来到游戏世界！",
    DisplayMode = ConversationDisplayMode.UI,  // UI模式（默认）
    AllowSkip = true,
    WaitForConfirmation = false,
    Duration = TimeSpan.FromSeconds(3)
};

await panel.ShowLineAsync(lineInfo, lineData);

// 显示气泡对话（OnUnit模式）
var bubbleLineInfo = new ConversationLineInfo 
{
    Text = "有敌人！",
    DisplayMode = ConversationDisplayMode.OnUnit,  // 单位头顶显示
    AllowSkip = true,
    Duration = TimeSpan.FromSeconds(2)
};

await panel.ShowLineAsync(bubbleLineInfo, lineData);

// 显示选择项（可选：添加提示信息）
var promptInfo = new ConversationChoicePromptInfo
{
    PromptText = "你该如何选择？",
    CharacterName = "旁白",
    Portrait = null,  // 可选：角色立绘
    PortraitSide = PortraitSide.Left,
    DisplayMode = ConversationDisplayMode.UI
};

var choices = new List<ConversationChoiceInfo>
{
    new() { Text = "选项1：继续冒险", IsEnabled = true },
    new() { Text = "选项2：返回城镇", IsEnabled = true },
    new() { Text = "选项3：查看背包", IsEnabled = false }  // 禁用状态
};

int selectedIndex = await panel.ShowChoicesAsync(choices, promptInfo, choiceGroupData);
```

### 2. 自定义对话面板

继承 `ConversationPanelBase` 创建自定义实现：

```csharp
public class MyConversationPanel : ConversationPanelBase
{
    // 实现抽象方法
    public override void ShowLine(ConversationLineInfo lineInfo, GameDataConversationLine? lineData)
    {
        // 自定义UI显示逻辑
    }
    
    public override void ShowChoices(List<ConversationChoiceInfo> choices, ConversationChoicePromptInfo? promptInfo, GameDataConversationChoiceGroup? choiceGroupData)
    {
        // 如果有提示信息，先显示提示
        if (promptInfo != null && !string.IsNullOrEmpty(promptInfo.PromptText))
        {
            // 显示提示信息（文本、角色名、立绘等）
        }
        
        // 自定义选择项显示
    }
    
    // 重写虚方法响应状态变化
    protected override void OnCanFinishChanged(bool canFinish)
    {
        // 自定义CanFinish变化时的UI响应
        myCustomIcon.Visible = canFinish;
    }
    
    // 实现其他抽象方法...
}
```

### 3. 使用气泡UI

在单位头顶显示简短对话：

```csharp
// 创建气泡
var bubble = new BubbleUI();
bubble.BindUnit = npcUnit;
bubble.OffsetZ = 120.0f;

// 显示文本
bubble.ShowText("有任务给你！");

// 3秒后自动销毁
await Task.Delay(3000);
bubble.Destroy();
```

## 设计模式与架构

### 1. 模板方法模式

`ConversationPanelBase` 作为抽象基类定义算法骨架，派生类实现具体步骤：

```csharp
// 基类定义流程
public async Task ShowLineAsync(...)
{
    // 1. 初始化状态
    CanFinish = lineInfo.AllowSkip;
    
    // 2. 调用派生类实现
    ShowLine(lineInfo, lineData);
    
    // 3. 等待完成
    await tcs.Task;
    
    // 4. 清理状态
    CanFinish = false;
    HideLine();
}
```

### 2. 观察者模式

`CanFinish` 属性使用观察者模式通知派生类：

```csharp
// 基类发布状态变化
public bool CanFinish 
{ 
    set
    {
        if (_canFinish != value)
        {
            _canFinish = value;
            OnCanFinishChanged(value);  // 通知观察者
        }
    }
}

// 派生类订阅状态变化
protected override void OnCanFinishChanged(bool canFinish)
{
    canFinishPanel.Visible = canFinish;
}
```

优势：
- **解耦**: 基类不知道派生类的UI细节
- **扩展性**: 派生类可以添加自定义响应（动画、音效等）
- **性能**: 只在状态真正改变时触发，避免轮询

### 3. 策略模式

显示模式的选择基于 `DisplayMode` 属性：

```csharp
// 根据 DisplayMode 决定使用哪种显示方式
if (lineInfo.DisplayMode == ConversationDisplayMode.OnUnit && unit != null && unit.IsValid)
{
    // 策略1：OnUnit模式 - BubbleUI气泡显示
    currentBubbleUI = new BubbleUI();
    currentBubbleUI.BindUnit = unit;
    currentBubbleUI.ShowText(displayText);
}
else
{
    // 策略2：UI模式（默认）- TextPanel对话框显示
    textPanel.Visible = true;
    if (typewriterEffect?.Enabled == true)
        typewriterEffect.Play(displayText);
    else
        textLabel.Text = displayText;
}
```

## 事件清理与内存管理

系统特别注重资源清理，防止内存泄漏：

### 选择项事件管理

```csharp
// 订阅事件
choicesPanel.OnChildPostInitialization += OnChoiceItemInitialized;

private void OnChoiceItemInitialized(object? sender, ApplyControlTemplateEventArgs e)
{
    if(e.Control is ConversationChoiceItem choiceItem)
    {
        choiceItem.OnPointerClicked += OnChoiceItemClicked;
    }
}

// 清理时取消订阅
private void UnsubscribeAllChoiceItems()
{
    foreach (var child in choicesPanel.Children)
    {
        if (child is ConversationChoiceItem choiceItem)
        {
            choiceItem.OnPointerClicked -= OnChoiceItemClicked;
        }
    }
}

// 销毁时清理
protected override void DisposeManaged()
{
    choicesPanel.OnChildPostInitialization -= OnChoiceItemInitialized;
    UnsubscribeAllChoiceItems();
    typewriterEffect?.Destroy();
    currentBubbleUI?.Destroy();
    base.DisposeManaged();
}
```

## 配置与数据

### ScopeData 配置

```csharp
public static class ScopeData
{
    public static class Control
    {
        // 默认对话选择项模板
        public static GameDataControlConversationChoiceItem DefaultConversationChoiceItem { get; set; }
        
        // 气泡UI模板
        public static GameDataControlBubbleUI DefaultBubbleUI { get; set; }
        
        // 默认对话面板配置
        public static GameDataControlConversationPanel ConversationPanel { get; set; }
    }
}
```

### GameData 数据模板

对话系统支持通过 GameData 进行全面配置：

#### 核心数据类

- **GameDataConversationLine**: 台词数据（文本、角色、立绘、持续时间等）
- **GameDataConversationChoiceGroup**: 选择组数据
- **GameDataConversationChoiceItem**: 单个选择项数据（包含图标配置）
- **GameDataCharacter**: 角色数据（名称、立绘、头像等）

#### UI控制数据类

- **GameDataControlConversationPanel**: 默认对话面板配置
  - 背景、分隔线、跳过按钮、完成提示图标
  - 打字机效果设置
  - BubbleUI模板和默认方向
  - 选择项模板

- **GameDataControlBubbleUI**: 气泡UI整体配置
  - `DefaultDirection`: 默认方向
  - `DirectionLayouts`: 各方向布局配置字典
  - `FontSize`: 字体大小
  - `TypewriterSpeed`: 打字机速度
  - `CanFinishIndicatorImage`: 完成提示图标

- **GameDataBubbleUILayout**: 气泡UI方向布局配置
  - `BubbleImage`: 背景图片
  - `UIOffset`: UI坐标偏移（UIPosition，屏幕XY坐标，不受镜头旋转影响）
  - `OffsetZ`: Z轴偏移（float，世界坐标深度，影响前后遮挡关系）
  - `CharsPerLine`: 每行字符数（用于计算宽度）
  - `FontSize`: 字体大小
  - `TextMargin`: 文字边距
  - `SlicedEdges`: 九宫格切片边界（可选）
  - `SocketName`: 绑点名称（默认 "socket_root"）

- **GameDataControlConversationChoiceItem**: 选择项UI配置
  - 背景图片、颜色
  - 图标显示配置

#### 方向枚举

- **BubbleDirection**: 气泡方向枚举
  - `Up`: 上方
  - `Down`: 下方
  - `Left`: 左侧
  - `Right`: 右侧（默认）

### 配置加载示例

```csharp
// 在游戏启动时加载配置
ScopeData.Control.DefaultBubbleUI = LoadBubbleUIConfig();
ScopeData.Control.ConversationPanel = LoadPanelConfig();
ScopeData.Control.DefaultConversationChoiceItem = LoadChoiceItemConfig();

// 创建组件时自动使用配置
var panel = new ConversationPanel(
    ScopeData.Control.ConversationPanel?.ToLink()
);
```

## 最佳实践

### 1. 选择合适的显示模式

根据对话场景选择 `DisplayMode`：

**UI 模式（ConversationDisplayMode.UI）**
- ✅ 剧情对话：重要故事情节，AllowSkip = true
- ✅ 任务对话：接受/完成任务，WaitForConfirmation = true
- ✅ 教学对话：新手教程，确保玩家阅读
- ✅ 过场动画：配合动画系统，自动完成模式
- ✅ 关键抉择：需要玩家仔细思考的选择

**OnUnit 模式（ConversationDisplayMode.OnUnit）**
- ✅ 环境对话：NPC闲聊、环境氛围
- ✅ 战斗提示：简短的战术提示、技能提示
- ✅ 动态反馈：单位状态变化的即时反馈
- ✅ 快速交互：不打断游戏流程的信息
- ❌ 不适合：长文本、重要剧情、需要选择的对话

### 2. 打字机效果配置

打字机效果默认关闭，如需启用需要手动配置：

```csharp
// 获取对话面板实例
var panel = ConversationPanel.Instance;

// 启用打字机效果
panel.EnableTypewriter = true;

// 慢速：营造氛围（恐怖、紧张）
panel.TypewriterSpeed = 0.3f;

// 标准速度：常规对话
panel.TypewriterSpeed = 0.5f;

// 快速：战斗提示、系统消息
panel.TypewriterSpeed = 1.0f;

// 禁用打字机效果：瞬间显示完整文本
panel.EnableTypewriter = false;
```

**何时使用打字机效果？**
- ✅ 剧情对话、重要叙事场景
- ✅ 需要营造氛围的对话（恐怖、悬疑）
- ✅ 教学内容，引导玩家逐步阅读
- ❌ 快节奏战斗中的提示
- ❌ 大量重复的系统消息
- ❌ 玩家可能需要快速阅读的信息

### 3. 选择项设计

- 最多12个选项（F1-F12快捷键限制）
- 使用 `IsEnabled = false` 标记暂时不可用的选项（显示为半透明）
- 重要选项放在前面（F1、F2容易触达）

### 4. 气泡方向配置

根据游戏场景选择合适的气泡方向：

**Up（上方）**
- ✅ 适合：地面单位、玩家角色对话
- ✅ 不遮挡单位本体，易于阅读
- 建议：OffsetY 设为负值（-80 到 -120）

**Down（下方）**
- ✅ 适合：飞行单位、高处的NPC
- ✅ 气泡自然悬挂在单位下方
- 建议：OffsetY 设为正值（60 到 100）

**Left（左侧）**
- ✅ 适合：屏幕右侧的单位
- ✅ 避免气泡超出屏幕
- 建议：OffsetX 设为负值（-50 到 -80）

**Right（右侧，默认）**
- ✅ 适合：屏幕左侧的单位
- ✅ 符合从左到右的阅读习惯
- 建议：OffsetX 设为正值（50 到 80）

**配置建议**：
- 为所有四个方向都配置布局，游戏中根据单位位置动态选择
- 使用 `CharsPerLine` 控制文本换行，避免气泡过宽
- `TextMargin` 确保文字不贴边，提高可读性
- 测试不同分辨率下的显示效果

### 5. 选择项图标使用

- 为重要选择项添加图标，增强视觉识别
- 图标大小建议 32x32 或 64x64 像素
- 禁用的选择项图标也会显示为半透明

### 6. 内存管理

- 对话结束后及时清理 BubbleUI
- 大量选择项时注意取消事件订阅
- 长时间运行的对话系统定期检查资源释放
- BubbleUI 的 IThinker 在无绑定单位时自动停止

## 扩展示例

### 添加音效支持

```csharp
public class MyConversationPanel : ConversationPanelBase
{
    protected override void OnCanFinishChanged(bool canFinish)
    {
        base.OnCanFinishChanged(canFinish);
        
        if (canFinish)
        {
            // 播放提示音效
            AudioManager.PlaySound("conversation_ready");
        }
    }
    
    protected override void CompleteLineDisplay()
    {
        // 播放确认音效
        AudioManager.PlaySound("conversation_complete");
        
        base.CompleteLineDisplay();
    }
}
```

### 添加动画效果

```csharp
protected override void OnCanFinishChanged(bool canFinish)
{
    if (canFinish)
    {
        // 播放呼吸动画
        canFinishPanel.AnimateOpacity(0.5f, 1.0f, 0.5f, loop: true);
    }
    else
    {
        // 停止动画
        canFinishPanel.StopAnimations();
        canFinishPanel.Visible = false;
    }
}
```

### 自定义气泡样式

```csharp
public class DamageNumberBubble : BubbleUI
{
    public DamageNumberBubble()
    {
        // 红色文字
        Label.TextColor = Color.Red;
        Label.FontSize = 48;
        Label.Bold = true;
        
        // 禁用打字机
        EnableTypewriter = false;
        
        // 向上飘动
        OffsetZ = 100.0f;
    }
    
    public void ShowDamage(int damage)
    {
        ShowText($"-{damage}");
        
        // 1秒后消失
        Task.Delay(1000).ContinueWith(_ => Destroy());
    }
}
```

## 常见问题

### Q: 为什么对话文本不是逐字显示的？

A: 打字机效果默认关闭，需要手动启用：
```csharp
panel.EnableTypewriter = true;
panel.TypewriterSpeed = 0.5f;
```

### Q: 如何让对话显示更快？

A: 调整打字机速度或禁用打字机效果：
```csharp
panel.TypewriterSpeed = 2.0f;  // 加快速度
// 或
panel.EnableTypewriter = false;  // 禁用打字机效果，瞬间显示
```

### Q: 如何实现"重要对话必须看完3秒才能继续"？

A: 使用等待确认模式：
```csharp
var lineInfo = new ConversationLineInfo 
{
    AllowSkip = false,
    WaitForConfirmation = true,
    Duration = TimeSpan.FromSeconds(3)
};
```

### Q: BubbleUI 位置不准确怎么办？

A: 有两种方式调整位置：

1. **运行时动态调整**（不推荐，仅用于测试）：
```csharp
bubble.OffsetX = 10.0f;   // X方向微调
bubble.OffsetY = -20.0f;  // Y方向微调
bubble.OffsetZ = 150.0f;  // 高度调整（废弃属性）
```

2. **通过GameData配置**（推荐）：
```csharp
// 在对应方向的 GameDataBubbleUILayout 中调整
var layout = new GameDataBubbleUILayout
{
    OffsetX = 50.0f,   // 更精确的位置控制
    OffsetY = -80.0f,
    // ... 其他配置
};
```

### Q: 选择项太多超过12个怎么办？

A: 建议拆分为多轮对话，或使用滚动列表（需要自定义实现）。

### Q: 如何让气泡显示在单位头部而不是脚底？

A: 配置 `SocketName` 为头部绑点：
```csharp
_ = new GameDataBubbleUILayout(BubbleDirectionLayout.Right)
{
    SocketName = "socket_head",  // 使用头部绑点
    UIOffset = new UIPosition(0, -10),  // 屏幕坐标微调（向上10像素）
    OffsetZ = -50,  // 向后偏移避免遮挡
    // ... 其他配置
};
```

**常用绑点名称**：
- `"socket_root"`: 脚底/根部（默认）
- `"socket_head"`: 头部
- `"socket_blood_bar"`: 血条位置
- `"socket_weapon"`: 武器位置
- 其他自定义绑点名称（需确保单位模型中存在）

### Q: BubbleUI 的偏移坐标系统是怎样的？

A: **混合坐标系统** - 结合 UI 坐标和世界坐标，适应镜头旋转场景。

**两种偏移类型**：

1. **UIOffset**（UIPosition 类型）- UI 屏幕坐标
   - 不受镜头旋转影响
   - 始终相对于屏幕方向
   - `Left > 0`: 向右，`< 0`: 向左
   - `Top > 0`: 向下，`< 0`: 向上（注意Y轴向下）
   - 常用值范围：-100 到 +100

2. **OffsetZ**（float 类型）- 世界坐标深度
   - 影响前后遮挡关系
   - `> 0`: 向前，`< 0`: 向后（负值可避免遮挡角色）
   - 常用值范围：-200 到 +200

**配置示例**：
```csharp
UIOffset = new UIPosition(10, -20),  // 屏幕右10，上20
OffsetZ = -75  // 世界坐标向后75（避免遮挡角色）
```

**优势**：
- UI坐标：气泡始终在屏幕上的固定相对位置，不受镜头旋转影响
- 世界Z轴：精确控制深度，避免角色遮挡
- 完美适配旋转镜头的3D游戏

### Q: 如何在对话过程中动态改变 CanFinish？

A: 在派生类中直接设置：
```csharp
protected void OnSpecialEvent()
{
    // 满足某些条件后允许完成
    CanFinish = true;
}
```

### Q: 如何动态切换气泡方向？

A: 直接设置 `Direction` 属性即可：
```csharp
// 气泡会立即应用新方向的配置（图片、偏移、宽度等）
bubble.Direction = BubbleDirection.Left;

// 可以根据单位位置动态选择方向
if (unit.ScreenPosition.X > Screen.Width / 2)
    bubble.Direction = BubbleDirection.Left;
else
    bubble.Direction = BubbleDirection.Right;
```

### Q: 为什么气泡有时会抖动？

A: BubbleUI 已实现防抖动机制（1像素阈值），如果仍有抖动：
- 检查是否频繁修改 `Direction` 导致配置切换
- 检查 `ActualSize` 是否异常变化（如文本内容频繁更新）
- 考虑增加阈值（需修改源码中的 `threshold` 值）

### Q: 如何为选择项添加图标？

A: 在 `GameDataConversationChoiceItem` 中配置：
```csharp
var choiceData = new GameDataConversationChoiceItem
{
    Icon = myIconImage.ToLink(),
    // ... 其他配置
};
```

`ConversationPanel` 会自动读取并显示图标。

### Q: 气泡宽度如何计算？

A: 宽度由以下公式自动计算：
```
Width = CharsPerLine * FontSize + TextMargin.Left + TextMargin.Right
```

如需调整宽度：
- 修改 `CharsPerLine`（每行字符数）
- 修改 `FontSize`（字体大小）
- 修改 `TextMargin`（文字边距）

## 性能优化建议

1. **避免频繁创建销毁**: 考虑使用对象池管理 BubbleUI
2. **限制同时显示的气泡数量**: 战斗中大量单位时只显示关键提示
3. **IThinker 智能管理**: BubbleUI 在无绑定单位时自动停止 Think
4. **事件及时清理**: 防止事件累积导致的性能问题
5. **方向配置预加载**: 在 ScopeData 中预先配置好所有方向的布局，避免运行时创建
6. **防抖动机制**: BubbleUI 的1像素阈值已优化位置更新性能
7. **动态宽度计算**: 基于配置的宽度计算比固定宽度更高效
8. **选择项图标**: 使用适当大小的图标（32x32 或 64x64），避免大图缩放

## 总结

ConversationSystemUI 提供了一个功能完整、易于扩展的对话UI解决方案。通过合理使用各种模式和配置，可以实现从简单提示到复杂剧情对话的各种需求。

关键设计优势：
- ✅ **清晰的架构**: 基于抽象类的模板方法模式，易于扩展
- ✅ **状态管理**: 观察者模式的 CanFinish 通知机制  
- ✅ **内存安全**: 完善的事件清理和资源管理
- ✅ **双显示模式**: UI面板和单位气泡满足不同场景
- ✅ **方向配置系统**: BubbleUI 支持四个方向的独立布局
- ✅ **GameData驱动**: 通过配置灵活控制UI外观和行为
- ✅ **性能优化**: 防抖动机制、智能 Think 管理
- ✅ **丰富功能**: 打字机效果、选择分支、图标支持、快捷键绑定
- ✅ **灵活扩展**: 多个扩展点支持自定义实现

核心特色功能：
1. **方向配置系统**: 通过 `Dictionary<BubbleDirection, GameDataBubbleUILayout>` 管理四个方向的独立配置
2. **动态宽度计算**: `CharsPerLine * FontSize + TextMargin` 自动适配气泡宽度
3. **智能定位**: 选择面板根据立绘位置自动调整，避免遮挡
4. **图标支持**: 选择项可显示图标，增强视觉识别
5. **防抖动**: 1像素阈值机制防止位置微小抖动

如有问题或建议，欢迎提出！

