using System.Numerics;
using Mathro.Core;
using Raylib_cs;

namespace Mathro.Client;

class CardView(Card card, Vector2 position, bool isSelected = false)
{
    public const float Width = 100;
    public const float Height = 140;
    public static readonly Vector2 CardOrigin = new(Width / 2, Height / 2);
    public static readonly Rectangle CollisionOrigin = new(-Width / 2, -Height / 2, Width, Height);

    public Card Card = card;

    public Vector2 Position = position;
    public Vector2 TargetPosition = position;

    public float Rotation = 0f;
    public float TargetRotation = 0f;

    public float Scale = 1f;
    public float TargetScale = 1f;

    public float Elevation = 4f;
    public float TargetElevation = 4f;

    public bool IsSelected = isSelected;

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
        if (Math.Abs(original - target) < 0.001)
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
        if (Math.Abs((Position - TargetPosition).Length()) < 0.001)
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

        const int CenterSize = 50;
        var centerText = Raylib.MeasureTextEx(Raylib.GetFontDefault(), Card.Value, CenterSize, 1);

        Raylib.DrawTextPro(
            Raylib.GetFontDefault(),
            Card.Value,
            Position,
            new(centerText.X / 2, centerText.Y / 2),
            Rotation,
            CenterSize,
            1,
            Color.Black
        );

        const int TopLeftSize = 15;
        Vector2 topLeftOffset = new(-Width * 0.35f, -Height * 0.38f);
        var topLeft = Raymath.Vector2Rotate(topLeftOffset, Raylib.DEG2RAD * Rotation);
        var topLeftText = Raylib.MeasureTextEx(Raylib.GetFontDefault(), Card.Value, TopLeftSize, 1);

        Raylib.DrawTextPro(
            Raylib.GetFontDefault(),
            Card.Value,
            Position + topLeft,
            new(topLeftText.X / 2, topLeftText.Y / 2),
            Rotation,
            TopLeftSize,
            1,
            Color.Black
        );
    }
}

interface IState
{
    public void Update(float dt, MouseInput input);
    public void Draw(Assets assets);
}

struct Assets
{
    public Texture2D Card;

    public Assets()
    {
        Card = Raylib.LoadTexture("Assets/card7.png");
        Raylib.GenTextureMipmaps(ref Card);
        Raylib.SetTextureFilter(Card, TextureFilter.Bilinear);
    }

    public static Rectangle CardSource()
    {
        return new(0, 0, CardView.Width * 2, CardView.Height * 2);
    }
}

class World(IState state)
{
    public Assets Assets = new();
    public IState State = state;
    public Camera2D Camera = new();
}

struct RunInfo()
{
    public uint Currency = 0;

    // public List<Card> Deck = [];
    public Deck Deck = new();

    public int HandSize = 9;
    public int DiscardSize = 4;
    public int PlaySize = 5;
    public int SelectSize = 7;
}

class PickingState : IState
{
    const int HandX = 350;
    const int HandY = 450;
    const float HandWidth = 596;

    const int DeckX = 1050;
    const int DeckY = 600;

    private bool IsDebug = true;

    public RunInfo State;
    public List<CardView> Hand = [];
    public List<CardView> DiscardingHand = [];

    private int DiscardsLeft = 3;
    private int PlaysLeft = 4;

    public RoundController Controller = new();

    private CardView? Hovered;
    private CardView? Dragged;
    private Vector2 DraggedDisplacement = Vector2.Zero;

    private Vector2 LightSource = new(640, -100);

    public PickingState(RunInfo info)
    {
        State = info;
        Controller.StartRound(1);
    }

    private void UpdateHand(Vector2 mousePosition)
    {
        const float HandWidthBounded = HandWidth - (CardView.Width / 2);
        const float HandWidthHalf = HandWidthBounded / 2;
        const float Rotation = 5;

        const float CardY = HandY + (CardView.Height / 2);
        const float CardX = HandX + (CardView.Width / 4);

        var cardSpaces = HandWidthBounded / Hand.Count;
        var cardStartOffset = HandWidthBounded / (2 * Hand.Count);

        for (int i = 0; i < Hand.Count; i++)
        {
            var curOffsetX = cardStartOffset + (cardSpaces * i);
            var distanceScale = (curOffsetX - HandWidthHalf) / HandWidthHalf;
            var curOffsetY = 10 * Math.Abs(distanceScale);

            Vector2 position = new(CardX + curOffsetX, CardY + curOffsetY);
            var scale = 1f;
            var elevation = 4f;

            if (Dragged is not null && Hand[i] == Dragged)
            {
                position = mousePosition - DraggedDisplacement;
                scale += 0.1f;
                elevation = 20f;
            }
            else if (Hand[i].IsSelected)
            {
                position.Y -= 25;
            }
            else if (Hovered is not null && Hand[i] == Hovered)
            {
                position.Y -= 10;
            }

            Hand[i].TargetPosition = position;
            Hand[i].TargetRotation = Rotation * distanceScale;
            Hand[i].TargetScale = scale;
            Hand[i].TargetElevation = elevation;
        }
    }

    private void UpdateCollisions(MouseInput input)
    {
        var hasCollided = false;
        foreach (CardView card in Hand.AsEnumerable().Reverse())
        {
            var mouse = Raymath.Vector2Rotate(
                input.MousePosition - card.Position,
                Raylib.DEG2RAD * -card.Rotation
            );

            if (Raylib.CheckCollisionPointRec(mouse, CardView.CollisionOrigin))
            {
                switch (input.MouseAction)
                {
                    case MouseAction.LeftClick:
                        card.IsSelected = !card.IsSelected;
                        break;
                    case MouseAction.LeftHold:
                        if (Dragged is null)
                        {
                            Dragged = card;
                            DraggedDisplacement = mouse;
                        }
                        break;
                    default:
                        Dragged = null;
                        DraggedDisplacement = Vector2.Zero;
                        Hovered = card;
                        break;
                }

                hasCollided = true;
                break;
            }
        }
        if (!hasCollided)
        {
            Hovered = null;
        }
    }

    private void UpdateAction()
    {
        if (
            Raylib.IsKeyPressed(KeyboardKey.Backspace)
            && DiscardsLeft > 0
            && DiscardingHand.Count == 0
        )
        {
            for (int i = Hand.Count - 1; i >= 0; i--)
            {
                if (Hand[i].IsSelected)
                {
                    var card = Hand[i];
                    card.TargetPosition = new(Program.VirtualWidth + 200, card.TargetPosition.Y);
                    card.TargetRotation = 15;

                    DiscardingHand.Add(card);
                    Hand.RemoveAt(i);
                }
            }

            DiscardsLeft--;
        }

        if (Raylib.IsKeyPressed(KeyboardKey.Space))
        {
            var selectedViews = Hand.Where(c => c.IsSelected).ToList();

            if (selectedViews.Count > 0 && selectedViews.Count <= 5)
            {
                List<Card> playCards = selectedViews.Select(v => v.Card).ToList();

                bool roundPassed = Controller.PlayHand(playCards);

                foreach (var view in selectedViews)
                {
                    Hand.Remove(view);
                }

                if (roundPassed)
                {
                    Controller.StartRound(Controller.CurrentRound + 1);
                }
            }
        }
    }

    public void Update(float dt, MouseInput input)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            IsDebug = !IsDebug;

        if (IsDebug && Raylib.IsKeyDown(KeyboardKey.LeftShift))
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                Hand.Add(
                    new CardView(new Card(CardCategory.Number, "3"), new(DeckX, DeckY), false)
                );
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backslash) && Hand.Count != 0)
            {
                Hand.Clear();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && Hand.Count != 0)
            {
                Hand.RemoveAt(Hand.Count - 1);
            }
        }

        Hand.Sort(
            (x, y) =>
            {
                return x.Position.X.CompareTo(y.Position.X);
            }
        );

        UpdateCollisions(input);
        UpdateAction();
        UpdateHand(input.MousePosition);

        const float ShuffleScale = 10;
        const float DragScale = 15;
        const float RotationScale = 3;
        const float ScaleScale = 50;

        var shuffleSpeed = 1 - MathF.Exp(-ShuffleScale * dt);
        var dragSpeed = 1 - MathF.Exp(-DragScale * dt);
        var rotationSpeed = 1 - MathF.Exp(-RotationScale * dt);
        var scaleSpeed = 1 - MathF.Exp(-ScaleScale * dt);
        var elevationSpeed = 1 - MathF.Exp(-ScaleScale * dt);

        foreach (CardView card in Hand)
        {
            var moveSpeed = (Dragged is not null && card == Dragged) ? dragSpeed : shuffleSpeed;

            card.Update(moveSpeed, rotationSpeed, scaleSpeed, elevationSpeed);
        }

        const float DiscardingScale = 20;
        var discardingSpeed = 1 - MathF.Exp(-DiscardingScale * dt);

        for (int i = DiscardingHand.Count - 1; i >= 0; i--)
        {
            var card = DiscardingHand[i];

            card.Update(discardingSpeed, rotationSpeed, scaleSpeed, elevationSpeed);
            if (card.Position == card.TargetPosition)
            {
                DiscardingHand.RemoveAt(i);
            }
        }
    }

    private void DrawDeck(Assets assets)
    {
        CardView baseCard = new(new Card(CardCategory.Number, "4"), new(DeckX, DeckY), false);

        baseCard.Scale += 0.1f;
        Raylib.DrawTexturePro(
            assets.Card,
            Assets.CardSource(),
            baseCard.Rectangle(),
            CardView.CardOrigin,
            0,
            new Color(0, 0, 0, 0.4f)
        );
        baseCard.Scale -= 0.1f;

        for (int i = 0; i < Hand.Count; i++)
        {
            baseCard.DrawShadow(assets, LightSource);
            baseCard.DrawCard(assets);
            baseCard.Position.X += 1;
            baseCard.Position.Y -= 1;
        }
    }

    private void DrawHand(Assets assets)
    {
        foreach (CardView card in Hand)
        {
            if (Dragged is not null && card == Dragged)
            {
                continue;
            }

            card.DrawShadow(assets, LightSource);
        }

        foreach (CardView card in Hand)
        {
            if (Dragged is not null && card == Dragged)
            {
                continue;
            }

            card.DrawCard(assets);
        }

        Dragged?.DrawShadow(assets, LightSource);
        Dragged?.DrawCard(assets);

        foreach (CardView card in DiscardingHand)
        {
            card.DrawShadow(assets, LightSource);
        }

        foreach (CardView card in DiscardingHand)
        {
            card.DrawCard(assets);
        }
    }

    public void Draw(Assets assets)
    {
        Raylib.DrawRectangle(0, 0, Program.VirtualWidth, Program.VirtualHeight, Color.DarkGreen);

        Raylib.DrawRectangleRounded(
            new(HandX, HandY + 5, (int)HandWidth, (int)CardView.Height),
            0.25f,
            0,
            new Color(0, 0, 0, 40)
        );

        Raylib.DrawRectangleRounded(
            new(HandX, 35, (int)HandWidth, (int)CardView.Height),
            0.25f,
            0,
            new Color(0, 0, 0, 40)
        );

        Raylib.DrawRectangle(100, 100, 200, 500, new Color(0, 0, 0, 40));

        DrawDeck(assets);

        Raylib.DrawText($"Round: {Controller.CurrentRound}", 120, 120, 20, Color.RayWhite);
        Raylib.DrawText($"Plays Left: {PlaysLeft}", 120, 150, 20, Color.RayWhite);
        Raylib.DrawText($"Discards Left: {DiscardsLeft}", 120, 180, 20, Color.RayWhite);
        Raylib.DrawText(
            $"Score: {Controller.AccumulatedScore} / {Controller.TargetScore}",
            120,
            210,
            20,
            Color.RayWhite
        );

        DrawHand(assets);

        if (IsDebug)
        {
            var fps = Raylib.GetFPS();
            Raylib.DrawText($"FPS: {fps}", 0, 0, 24, Color.RayWhite);
        }
    }
}

enum MouseAction
{
    None,
    LeftClick,
    LeftHold,
}

struct MouseInput(Vector2 mousePosition, MouseAction mouseInput)
{
    public Vector2 MousePosition = mousePosition;
    public MouseAction MouseAction = mouseInput;
};

static class Program
{
    public const int VirtualWidth = 1280;
    public const int VirtualHeight = 720;

    struct InputReader()
    {
        private float HoldTime = 0f;
        private Vector2 StartingMousePosition = Vector2.Zero;

        public MouseInput ReadInput(float dt, Vector2 worldMouse)
        {
            const float holdThreshold = 0.15f;

            Vector2 virtualMouse = new(
                Math.Clamp(worldMouse.X, 0, VirtualWidth),
                Math.Clamp(worldMouse.Y, 0, VirtualHeight)
            );

            if (Raylib.IsMouseButtonPressed(MouseButton.Left))
            {
                StartingMousePosition = virtualMouse;
            }

            MouseAction mouseInput = MouseAction.None;
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                HoldTime += dt;

                if (
                    (StartingMousePosition - virtualMouse).Length() > 5
                    || HoldTime >= holdThreshold
                )
                {
                    mouseInput = MouseAction.LeftHold;
                }
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            {
                if (HoldTime < holdThreshold)
                {
                    mouseInput = MouseAction.LeftClick;
                }

                HoldTime = 0;
            }

            return new MouseInput(virtualMouse, mouseInput);
        }
    }

    public static void Main()
    {
        Raylib.SetConfigFlags(ConfigFlags.Msaa4xHint | ConfigFlags.ResizableWindow);
        Raylib.InitWindow(VirtualWidth, VirtualHeight, "Calculatro");

        // Raylib.SetTextureFilter(mainScreen.Texture, TextureFilter.Bilinear);

        Raylib.SetTargetFPS(60);

        World world = new(new PickingState(new()));
        InputReader inputReader = new();

        while (!Raylib.WindowShouldClose())
        {
            var dt = Raylib.GetFrameTime();

            var screenWidth = Raylib.GetScreenWidth();
            var screenHeight = Raylib.GetScreenHeight();

            var scale = Math.Min(
                (float)screenWidth / VirtualWidth,
                (float)screenHeight / VirtualHeight
            );

            var offsetX = (screenWidth - (VirtualWidth * scale)) * 0.5f;
            var offsetY = (screenHeight - (VirtualHeight * scale)) * 0.5f;

            world.Camera.Offset = new(offsetX, offsetY);
            world.Camera.Target = Vector2.Zero;
            world.Camera.Rotation = 0;
            world.Camera.Zoom = scale;

            var mouse = Raylib.GetScreenToWorld2D(Raylib.GetMousePosition(), world.Camera);
            var input = inputReader.ReadInput(dt, mouse);

            world.State.Update(dt, input);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.Black);
            Raylib.BeginMode2D(world.Camera);

            world.State.Draw(world.Assets);

            Raylib.EndMode2D();
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
