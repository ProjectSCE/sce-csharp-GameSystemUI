#if CLIENT
using GameUI.Control.Primitive;
using GameUI.Enum;
using GameUI.Struct;
using GameUI.Brush;
using System.Drawing;

namespace GameSystemUI.ChatSystem;
public class ChatMessageUI : Panel
{
    private Label messageLabel;
    // 分隔线
    private Panel divider;
    public string? Message { 
        get
        {
            return messageLabel.Text;
        }
        set
        {
            messageLabel.Text = value;
            divider.Visible = !string.IsNullOrEmpty(value);
        }
    }
    public ChatMessageUI()
    {
        this.FlowOrientation = Orientation.Vertical;
        this.VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top;
        this.WidthStretchRatio = 1.0f;
        this.WidthCompactRatio = 1.0f;
        this.Height = -1;
        
        messageLabel = new Label(){
            // Margin = new Thickness(10, 10, 10, 10),
            WidthCompactRatio = 1.0f,
            WidthStretchRatio = 1.0f,
            Height = -1,
            FontSize = 36,
            TextColor = Color.FromArgb(255, 255, 255, 255),
            HorizontalContentAlignment = GameUI.Enum.HorizontalContentAlignment.Left,
        };
        this.AddChild(messageLabel);

        divider = new Panel(){
            Margin = new Thickness(0, 10, 0, 10),
            WidthCompactRatio = 1.0f,
            WidthStretchRatio = 1.0f,
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(255, 76, 76, 81)),
        };
        this.AddChild(divider);
    }


}

public class ChatRecord : PanelScrollable
{
    public ChatRecord()
    {
        this.ScrollEnabled = true;
        this.FlowOrientation = Orientation.Vertical;
        this.VerticalContentAlignment = GameUI.Enum.VerticalContentAlignment.Top;
        this.ScrollOrientation = Orientation.Vertical;
        this.ScrollBarSize = 10;
    }

    public void AddMessage(string message)
    {
        var messageUI = new ChatMessageUI(){
            Message = message
        };
        this.AddChild(messageUI);
        this.ScrollBarValue = 1.0f;
    }
}

#endif