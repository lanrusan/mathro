using System.Numerics;
using Mathro.Core;
using Raylib_cs;
using Card2 = Mathro.Core.Card;

namespace Mathro.Client;

struct Card(CardKind kind)
{
    public CardKind Kind = kind;
}

enum CardKind
{
    One,
    Two,
    Three,
};

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

    public bool IsSelected = isSelected;

    public void UpdatePosition(float scale)
    {
        if (Math.Abs((Position - TargetPosition).Length()) < 0.001)
        {
            Position = TargetPosition;
        }
        else
        {
            Position = Raymath.Vector2Lerp(Position, TargetPosition, scale);
        }

        // Console.WriteLine($"X: {Position.X}, Y: {Position.Y}");
    }

    public void UpdateRotation(float scale)
    {
        if (Math.Abs(Rotation - TargetRotation) < 0.001)
        {
            Rotation = TargetRotation;
        }
        else
        {
            Rotation = Raymath.Lerp(Rotation, TargetRotation, scale);
        }
    }

    public void UpdateScale(float scale)
    {
        if (Math.Abs(Scale - TargetScale) < 0.001)
        {
            Scale = TargetScale;
        }
        else
        {
            Scale = Raymath.Lerp(Scale, TargetScale, scale);
        }
    }

    public void Draw(Assets assets)
    {
        var widthOffset = (Width * Scale) - Width;
        var heightOffset = (Height * Scale) - Height;

        Rectangle rectangle = new(
            Position.X - (widthOffset / 2),
            Position.Y - (heightOffset / 2),
            Width + widthOffset,
            Height + heightOffset
        );
        // Raylib.DrawRectanglePro(rectangle, cardOrigin, card.Rotation, Color.RayWhite);

        Raylib.DrawTexturePro(
            assets.Card,
            new(0, 0, Width * 2, Height * 2),
            rectangle,
            CardOrigin,
            Rotation,
            Color.White
        );
    }
}

interface IState
{
    public void Update(float dt, UserInput input);

    public void Draw(Assets assets);
}

struct Assets
{
    public Texture2D Card;

    public Assets()
    {
        Card = Raylib.LoadTexture("Assets/card4.png");
        Raylib.GenTextureMipmaps(ref Card);
        Raylib.SetTextureFilter(Card, TextureFilter.Bilinear);
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
    public List<Card> Deck = [];
}

class PickingState(RunInfo info) : IState
{
    const int DeckX = 350;
    const int DeckY = 450;
    const float DeckWidth = 596;

    private bool IsDebug = true;

    public RunInfo State = info;
    public List<CardView> Hand = [];

    private CardView? Hovered;
    private CardView? Dragged;
    private Vector2 DraggedDisplacement = Vector2.Zero;

    private void UpdateHand(Vector2 mousePosition)
    {
        const float DeckWidthBounded = DeckWidth - (CardView.Width / 2);
        const float DeckWidthHalf = DeckWidthBounded / 2;
        const float Rotation = 5;

        const float CardY = DeckY + (CardView.Height / 2);
        const float CardX = DeckX + (CardView.Width / 4);

        var cardSpaces = DeckWidthBounded / Hand.Count;
        var cardStartOffset = DeckWidthBounded / (2 * Hand.Count);

        for (int i = 0; i < Hand.Count; i++)
        {
            var curOffsetX = cardStartOffset + (cardSpaces * i);
            var distanceScale = (curOffsetX - DeckWidthHalf) / DeckWidthHalf;
            var curOffsetY = 10 * Math.Abs(distanceScale);

            Vector2 position = new(CardX + curOffsetX, CardY + curOffsetY);
            var scale = 1f;

            if (Dragged is not null && Hand[i] == Dragged)
            {
                position = mousePosition - DraggedDisplacement;
                scale += 0.1f;
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
        }
    }

    private void UpdateCollisions(UserInput input)
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
                switch (input.MouseInput)
                {
                    case MouseInput.LeftClick:
                        card.IsSelected = !card.IsSelected;
                        break;
                    case MouseInput.LeftHold:
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

    public void Update(float dt, UserInput input)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            IsDebug = !IsDebug;

        if (IsDebug)
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                if (Raylib.IsKeyDown(KeyboardKey.LeftShift))
                {
                    Hand.Add(new CardView(new Card(CardKind.One), new(1400, 700), true));
                }
                else
                {
                    Hand.Add(new CardView(new Card(CardKind.One), new(1400, 700), false));
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && Hand.Count != 0)
            {
                Hand.RemoveAt(Hand.Count - 1);
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backslash) && Hand.Count != 0)
            {
                Hand.Clear();
            }
        }

        Hand.Sort(
            (x, y) =>
            {
                return x.Position.X.CompareTo(y.Position.X);
            }
        );

        UpdateCollisions(input);

        if (Hand.Count != 0)
        {
            UpdateHand(input.MousePosition);
        }

        const float ShuffleScale = 10;
        const float DragScale = 15;
        const float RotationScale = 3;
        const float ScaleScale = 50;

        var shuffleSpeed = 1 - MathF.Exp(-ShuffleScale * dt);
        var dragSpeed = 1 - MathF.Exp(-DragScale * dt);
        var rotationSpeed = 1 - MathF.Exp(-RotationScale * dt);
        var scaleSpeed = 1 - MathF.Exp(-ScaleScale * dt);

        foreach (CardView card in Hand)
        {
            var speed = (Dragged is not null && card == Dragged) ? dragSpeed : shuffleSpeed;

            card.UpdatePosition(speed);
            card.UpdateRotation(rotationSpeed);
            card.UpdateScale(scaleSpeed);
        }
    }

    public void Draw(Assets assets)
    {
        Raylib.DrawRectangle(0, 0, Program.VirtualWidth, Program.VirtualHeight, Color.DarkGreen);

        Raylib.DrawRectangleRounded(
            new(DeckX, DeckY + 5, (int)DeckWidth, (int)CardView.Height),
            0.25f,
            0,
            new Color(0, 0, 0, 40)
        );

        Raylib.DrawRectangleRounded(
            new(DeckX, 35, (int)DeckWidth, (int)CardView.Height),
            0.25f,
            0,
            new Color(0, 0, 0, 40)
        );

        Raylib.DrawRectangle(100, 100, 200, 500, new Color(0, 0, 0, 40));

        if (Hand.Count != 0)
        {
            foreach (CardView card in Hand)
            {
                if (Dragged is not null && card == Dragged)
                {
                    continue;
                }

                card.Draw(assets);
            }

            Dragged?.Draw(assets);
        }

        if (IsDebug)
        {
            var fps = Raylib.GetFPS();
            Raylib.DrawText($"FPS: {fps}", 0, 0, 24, Color.RayWhite);
        }
    }
}

enum MouseInput
{
    None,
    LeftClick,
    LeftHold,
}

struct UserInput(Vector2 mousePosition, MouseInput mouseInput)
{
    public Vector2 MousePosition = mousePosition;
    public MouseInput MouseInput = mouseInput;
};

static class Program
{
    public const int VirtualWidth = 1280;
    public const int VirtualHeight = 720;

    struct InputReader()
    {
        private float HoldTime = 0f;
        private Vector2 StartingMousePosition = Vector2.Zero;

        public UserInput ReadInput(float dt, Vector2 worldMouse)
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

            MouseInput mouseInput = MouseInput.None;
            if (Raylib.IsMouseButtonDown(MouseButton.Left))
            {
                HoldTime += dt;

                if (
                    (StartingMousePosition - virtualMouse).Length() > 5
                    || HoldTime >= holdThreshold
                )
                {
                    mouseInput = MouseInput.LeftHold;
                }
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.Left))
            {
                if (HoldTime < holdThreshold)
                {
                    mouseInput = MouseInput.LeftClick;
                }

                HoldTime = 0;
            }

            // PreviousMousePosition = virtualMouse;

            return new UserInput(virtualMouse, mouseInput);
        }
    }

    public static void ConsoleMain()
    {
        var Deck = new Deck();
        List<Card2> deck = new();

        Deck.deckInitialize(deck);
        Deck.DeckShuffle(deck);
        Deck.DeckDraw(deck);
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
