using System.Numerics;
using Mathro.Core;
using Raylib_cs;

namespace Mathro.Client;

class CardView(Card card, Vector2 position)
{
    public const float Width = 100;
    public const float Height = 140;
    public static readonly Vector2 CardOrigin = new(Width / 2, Height / 2);
    public static readonly Rectangle CollisionOrigin = new(-Width / 2, -Height / 2, Width, Height);

    const float Epsilon = 0.001f;

    public Card Card = card;

    public Vector2 Position = position;
    public Vector2 TargetPosition = position;

    public float Rotation = 0f;
    public float TargetRotation = 0f;

    public float Scale = 1f;
    public float TargetScale = 1f;

    public float Elevation = 4f;
    public float TargetElevation = 4f;

    public bool IsSelected = false;

    public Rectangle Rectangle()
    {
        var widthOffset = (Width * Scale) - Width;
        var heightOffset = (Height * Scale) - Height;

        return new(
            Position.X - (widthOffset / 2),
            Position.Y - (heightOffset / 2),
            Width + widthOffset,
            Height + heightOffset
        );
    }

    private static float ClampLerp(float original, float target, float scale)
    {
        if (Math.Abs(original - target) < Epsilon)
        {
            return target;
        }
        else
        {
            return Raymath.Lerp(original, target, scale);
        }
    }

    public void Update(
        float positionScale,
        float rotationScale,
        float scaleScale,
        float elevationScale
    )
    {
        if ((Position - TargetPosition).Length() < Epsilon)
        {
            Position = TargetPosition;
        }
        else
        {
            Position = Raymath.Vector2Lerp(Position, TargetPosition, positionScale);
        }

        Rotation = ClampLerp(Rotation, TargetRotation, rotationScale);
        Scale = ClampLerp(Scale, TargetScale, scaleScale);
        Elevation = ClampLerp(Elevation, TargetElevation, elevationScale);
    }

    public void DrawShadow(Assets assets, Vector2 lightSource)
    {
        var direction = Raymath.Vector2Normalize(Position - lightSource);

        var rectangle = Rectangle();
        rectangle.X += Elevation * direction.X;
        rectangle.Y += Elevation * direction.Y;

        Raylib.DrawTexturePro(
            assets.Card,
            Assets.CardSource(),
            rectangle,
            CardOrigin,
            Rotation,
            new Color(0, 0, 0, 0.45f)
        );
    }

    public void DrawCard(Assets assets)
    {
        Raylib.DrawTexturePro(
            assets.Card,
            Assets.CardSource(),
            Rectangle(),
            CardOrigin,
            Rotation,
            Color.White
        );

        var centerSize = 50f * Scale;
        var centerText = Raylib.MeasureTextEx(Raylib.GetFontDefault(), Card.Value, centerSize, 1);

        Raylib.DrawTextPro(
            Raylib.GetFontDefault(),
            Card.Value,
            Position,
            new(centerText.X / 2, centerText.Y / 2),
            Rotation,
            centerSize,
            1,
            Color.Black
        );

        var topLeftSize = 15f * Scale;
        Vector2 topLeftOffset = new(-Width * 0.35f * Scale, -Height * 0.38f * Scale);
        var topLeft = Raymath.Vector2Rotate(topLeftOffset, Raylib.DEG2RAD * Rotation);
        var topLeftText = Raylib.MeasureTextEx(Raylib.GetFontDefault(), Card.Value, topLeftSize, 1);

        Raylib.DrawTextPro(
            Raylib.GetFontDefault(),
            Card.Value,
            Position + topLeft,
            new(topLeftText.X / 2, topLeftText.Y / 2),
            Rotation,
            topLeftSize,
            1,
            Color.Black
        );
    }
}

class Button(string label, string hint, Rectangle bounds, Color color, Color hoverColor)
{
    const int FontSize = 24;
    const int HintSize = 14;

    private static readonly Color DisabledColor = new(90, 90, 90, 255);
    private static readonly Color DisabledText = new(170, 170, 170, 255);

    public readonly Rectangle Bounds = bounds;

    private bool Enabled;
    private bool Hovered;

    public bool Update(MouseInput input, bool enabled)
    {
        Enabled = enabled;
        Hovered = Raylib.CheckCollisionPointRec(input.MousePosition, Bounds);

        var clicked = false;
        if (input.MouseAction == MouseAction.LeftClick)
        {
            clicked = Hovered && Enabled;
        }

        return clicked;
    }

    public void Draw()
    {
        var face = Bounds;

        Raylib.DrawRectangleRounded(
            new Rectangle(Bounds.X, Bounds.Y + 4, Bounds.Width, Bounds.Height),
            0.3f,
            0,
            new Color(0, 0, 0, 70)
        );

        var fill =
            !Enabled ? DisabledColor
            : Hovered ? hoverColor
            : color;
        Raylib.DrawRectangleRounded(face, 0.3f, 0, fill);

        var labelWidth = Raylib.MeasureText(label, FontSize);
        Raylib.DrawText(
            label,
            (int)(face.X + ((face.Width - labelWidth) / 2)),
            (int)(face.Y + ((face.Height - FontSize) / 2)),
            FontSize,
            Enabled ? Color.White : DisabledText
        );

        var hintWidth = Raylib.MeasureText(hint, HintSize);
        Raylib.DrawText(
            hint,
            (int)(Bounds.X + ((Bounds.Width - hintWidth) / 2)),
            (int)(Bounds.Y + Bounds.Height + 8),
            HintSize,
            new Color(255, 255, 255, 140)
        );
    }
}
