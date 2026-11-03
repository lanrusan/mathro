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

interface IScene
{
    public void Update(float dt, MouseInput input);
    public void Draw(Assets assets);
}

class Assets
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

class World(IScene scene)
{
    public Assets Assets = new();
    public IScene Scene = scene;
    public Camera2D Camera = new();
}

class RunInfo
{
    public uint Currency = 0;

    public Deck Deck = new();

    public RoundSettings Settings = new();
}

enum ActionState
{
    GameLost,
    GameWin,
    NextRound,
}

class PickingScene : IScene
{
    const int HandX = 250;
    const int HandY = 450;
    const float HandWidth = 800;

    const int DeckX = 1150;
    const int DeckY = 600;

    const float ButtonWidth = 170;
    const float ButtonHeight = 50;
    const float ButtonGap = 20;
    const float ButtonY = HandY + CardView.Height + 45;
    const float ButtonsX = HandX + ((HandWidth - ((2 * ButtonWidth) + ButtonGap)) / 2);

    private bool IsDebug = true;

    private static readonly Vector2 DeckPosition = new(DeckX, DeckY);

    public RunInfo Info;
    public List<CardView> Hand = [];
    public List<CardView> ActingHand = [];

    public RoundController Controller;

    private CardView? Hovered;
    private CardView? Dragged;
    private Vector2 DraggedDisplacement = Vector2.Zero;

    private Vector2 LightSource = new(640, -100);

    private readonly Button PlayButton = new(
        "Play",
        "Space",
        new(ButtonsX, ButtonY, ButtonWidth, ButtonHeight),
        new Color(52, 120, 210, 255),
        new Color(85, 150, 240, 255)
    );

    private readonly Button DiscardButton = new(
        "Discard",
        "Backspace",
        new(ButtonsX + ButtonWidth + ButtonGap, ButtonY, ButtonWidth, ButtonHeight),
        new Color(200, 70, 60, 255),
        new Color(230, 105, 95, 255)
    );

    private bool PlayClicked;
    private bool DiscardClicked;

    public PickingScene(RunInfo info)
    {
        Info = info;
        StartRound(1);
    }

    private void StartRound(int round)
    {
        Controller = new RoundController(Info.Deck, Info.Settings, round);
        Hand.Clear();
        ActingHand.Clear();
        Hovered = null;
        Dragged = null;
        SpawnCardViews(Controller.Hand);
    }

    private void SpawnCardViews(IEnumerable<Card> cards)
    {
        foreach (var card in cards)
            Hand.Add(new CardView(card, DeckPosition));
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

    private void UpdateHandCollisions(MouseInput input)
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
                        var selected = Hand.FindAll(v => v.IsSelected).ToList().Count;
                        if (selected >= Info.Settings.MaxCardsPerPlay && !card.IsSelected)
                        {
                            break;
                        }

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
        var selected = Hand.Where(v => v.IsSelected).ToList();
        if (selected.Count == 0)
        {
            return;
        }

        var cards = selected.Select(v => v.Card).ToList();

        if (Raylib.IsKeyPressed(KeyboardKey.Backspace) || DiscardClicked)
        {
            var result = Controller.Discard(cards);
            if (!result.Ok)
            {
                return;
            }

            SendAway(
                selected,
                v => new Vector2(Program.VirtualWidth + 200, v.TargetPosition.Y),
                15
            );
            SpawnCardViews(result.Drawn);
        }
        else if (Raylib.IsKeyPressed(KeyboardKey.Space) || PlayClicked)
        {
            var result = Controller.PlayHand(cards);
            if (!result.Ok)
            {
                return;
            }

            SendAway(selected, v => new Vector2(v.TargetPosition.X, v.TargetPosition.Y - 200), 0);
            SpawnCardViews(result.Drawn);
        }
    }

    private void UpdateButtons(MouseInput input)
    {
        var cards = Hand.Where(v => v.IsSelected).Select(v => v.Card).ToList();

        var isValid = cards.Count > 0;

        var canAct =
            Controller.Phase == RoundPhase.Selecting
            && ActingHand.Count == 0
            && Dragged is null
            && cards.Count > 0
            && cards.Count <= Controller.Settings.MaxCardsPerPlay;

        PlayClicked = PlayButton.Update(input, canAct && isValid && Controller.PlaysRemaining > 0);
        DiscardClicked = DiscardButton.Update(input, canAct && Controller.DiscardsRemaining > 0);
    }

    private void SendAway(List<CardView> views, Func<CardView, Vector2> destination, float rotation)
    {
        foreach (var view in views)
        {
            view.TargetPosition = destination(view);
            view.TargetRotation = rotation;
            ActingHand.Add(view);
            Hand.Remove(view);
        }
    }

    public void Update(float dt, MouseInput input)
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Tab))
            IsDebug = !IsDebug;

        if (
            IsDebug && Raylib.IsKeyDown(KeyboardKey.LeftShift)
            || Raylib.IsKeyDown(KeyboardKey.RightShift)
        )
        {
            if (Raylib.IsKeyPressed(KeyboardKey.Enter))
            {
                var drawn = Controller.DrawDebug(1);
                if (drawn.Count != 0)
                {
                    Hand.AddRange(new CardView(drawn[0], DeckPosition));
                }
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backslash) && Hand.Count != 0)
            {
                Hand.Clear();
            }

            if (Raylib.IsKeyPressed(KeyboardKey.Backspace) && Hand.Count != 0)
            {
                var discard = Hand[^1];
                Hand.RemoveAt(Hand.Count - 1);
                SendAway(
                    [discard],
                    v => new Vector2(Program.VirtualWidth + 200, v.TargetPosition.Y),
                    15
                );
            }
        }

        Hand.Sort(
            (x, y) =>
            {
                return x.TargetPosition.X.CompareTo(y.TargetPosition.X);
            }
        );

        UpdateHandCollisions(input);
        UpdateButtons(input);

        if (ActingHand.Count == 0 && Dragged is null && Controller.Phase == RoundPhase.Selecting)
        {
            UpdateAction();
        }

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

        const float ActingScale = 20;
        var actingSpeed = 1 - MathF.Exp(-ActingScale * dt);

        for (int i = ActingHand.Count - 1; i >= 0; i--)
        {
            var card = ActingHand[i];

            card.Update(actingSpeed, rotationSpeed, scaleSpeed, elevationSpeed);
            if (card.Position == card.TargetPosition)
            {
                ActingHand.RemoveAt(i);
            }
        }

        if (ActingHand.Count == 0)
        {
            if (Controller.Phase == RoundPhase.Won)
            {
                // TODO: Pick a joker scene.
                StartRound(Controller.CurrentRound + 1);
            }
            else if (Controller.Phase == RoundPhase.Lost)
            {
                // TODO: Game lose screen
                Info = new RunInfo();
                StartRound(1);
            }
        }
    }

    private void DrawDeck(Assets assets)
    {
        CardView baseCard = new(new Card(CardCategory.Operator, "?"), DeckPosition);

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

        const int MaxPileCards = 12;
        for (int i = 0; i < Math.Min(Controller.DeckRemaining, MaxPileCards); i++)
        {
            baseCard.DrawShadow(assets, LightSource);
            baseCard.DrawCard(assets);
            baseCard.Position.X += 1;
            baseCard.Position.Y -= 1;
        }
    }

    private void DrawHand(Assets assets)
    {
        Raylib.DrawRectangleRounded(
            new(HandX, HandY + 5, (int)HandWidth, (int)CardView.Height),
            0.25f,
            0,
            new Color(0, 0, 0, 40)
        );

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

        foreach (CardView card in ActingHand)
        {
            card.DrawShadow(assets, LightSource);
        }

        foreach (CardView card in ActingHand)
        {
            card.DrawCard(assets);
        }
    }

    public void DrawInfo()
    {
        const int StartBorderX = 25;
        const int StartX = StartBorderX + 25;

        Raylib.DrawRectangleRounded(
            new(HandX, 35, (int)HandWidth, (int)CardView.Height),
            0.25f,
            0,
            new Color(0, 0, 0, 40)
        );

        Raylib.DrawRectangle(StartBorderX, 100, 200, 500, new Color(0, 0, 0, 40));

        Raylib.DrawText($"Round: {Controller.CurrentRound}", StartX, 120, 20, Color.RayWhite);
        Raylib.DrawText($"Plays Left: {Controller.PlaysRemaining}", StartX, 150, 20, Color.RayWhite);
        Raylib.DrawText(
            $"Discards Left: {Controller.DiscardsRemaining}",
            StartX,
            180,
            20,
            Color.RayWhite
        );
        Raylib.DrawText(
            $"Score: \n  {Controller.AccumulatedScore} / {Controller.TargetScore}",
            StartX,
            210,
            20,
            Color.RayWhite
        );

        Raylib.DrawText(
            $"Cards Left: \n  {Controller.DeckRemaining} / {Controller.DeckTotal}",
            StartX,
            270,
            20,
            Color.RayWhite
        );
    }

    public void Draw(Assets assets)
    {
        Raylib.DrawRectangle(0, 0, Program.VirtualWidth, Program.VirtualHeight, Color.DarkGreen);

        DrawDeck(assets);

        DrawInfo();

        PlayButton.Draw();
        DiscardButton.Draw();

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

        World world = new(new PickingScene(new()));
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

            world.Scene.Update(dt, input);

            Raylib.BeginDrawing();
            Raylib.ClearBackground(Color.DarkGreen);
            Raylib.BeginMode2D(world.Camera);

            world.Scene.Draw(world.Assets);

            Raylib.EndMode2D();
            Raylib.EndDrawing();
        }

        Raylib.CloseWindow();
    }
}
