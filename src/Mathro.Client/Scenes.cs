using System.Numerics;
using Mathro.Core;
using Raylib_cs;

namespace Mathro.Client;

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

                        var selected = Hand.Count(v => v.IsSelected);
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

        var canAct =
            Controller.Phase == RoundPhase.Selecting
            && ActingHand.Count == 0
            && Dragged is null
            && cards.Count > 0
            && cards.Count <= Controller.Settings.MaxCardsPerPlay;

        PlayClicked = PlayButton.Update(input, canAct && Controller.PlaysRemaining > 0);
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

    public IScene? Update(float dt, MouseInput input)
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
                if (Controller.CurrentRound >= Info.RoundsToWin)
                {
                    return new GameWinScene(Summary());
                }

                StartRound(Controller.CurrentRound + 1);
            }
            else if (Controller.Phase == RoundPhase.Lost)
            {
                return new GameOverScene(Summary());
            }
        }

        return null;
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
        const int StartX = StartBorderX + 20;

        Raylib.DrawRectangleRounded(
            new(HandX, 35, (int)HandWidth, (int)CardView.Height),
            0.25f,
            0,
            new Color(0, 0, 0, 40)
        );

        Raylib.DrawRectangle(StartBorderX, 100, 200, 500, new Color(0, 0, 0, 40));

        Raylib.DrawText(
            $"Score: \n  {Controller.AccumulatedScore} / {Controller.TargetScore}",
            StartX,
            120,
            20,
            Color.RayWhite
        );

        Raylib.DrawText($"Round: {Controller.CurrentRound}", StartX, 170, 20, Color.RayWhite);

        Raylib.DrawText(
            $"Plays Left: {Controller.PlaysRemaining}",
            StartX,
            200,
            20,
            Color.RayWhite
        );
        Raylib.DrawText(
            $"Discards Left: {Controller.DiscardsRemaining}",
            StartX,
            230,
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

    private RunSummary Summary()
    {
        return new(
            Controller.CurrentRound,
            Controller.AccumulatedScore,
            Controller.TargetScore,
            Info.RoundsToWin
        );
    }
}

abstract class MenuScene : IScene
{
    protected record Item(Button Button, Func<IScene?> OnSelect, KeyboardKey? Hotkey);

    protected readonly List<Item> Items = [];
    protected IReadOnlyList<string> Lines = [];

    protected abstract string Heading { get; }
    protected virtual Color HeadingColor => Color.RayWhite;
    protected virtual Color Background => Color.DarkGreen;

    // The primary button is blue and also triggered by Enter. onSelect returns the next scene,
    // or null to stay on this one.
    protected void AddButton(string label, bool primary, Func<IScene?> onSelect)
    {
        const float Width = 220;
        const float Height = 56;
        const float Gap = 36;
        const float FirstY = 440;

        var bounds = new Rectangle(
            (Program.VirtualWidth - Width) / 2,
            FirstY + (Items.Count * (Height + Gap)),
            Width,
            Height
        );

        var button = primary
            ? new Button(
                label,
                "Enter",
                bounds,
                new Color(52, 120, 210, 255),
                new Color(85, 150, 240, 255)
            )
            : new Button(
                label,
                "",
                bounds,
                new Color(90, 95, 110, 255),
                new Color(120, 125, 140, 255)
            );

        Items.Add(new Item(button, onSelect, primary ? KeyboardKey.Enter : null));
    }

    public IScene? Update(float dt, MouseInput input)
    {
        foreach (var item in Items)
        {
            var clicked = item.Button.Update(input, true);
            var hotkey = item.Hotkey is { } key && Raylib.IsKeyPressed(key);

            if (clicked || hotkey)
            {
                return item.OnSelect();
            }
        }

        return null;
    }

    protected virtual void DrawExtras(Assets assets) { }

    public void Draw(Assets assets)
    {
        Raylib.DrawRectangle(0, 0, Program.VirtualWidth, Program.VirtualHeight, Background);

        DrawCentered(Heading, 110, 72, HeadingColor);
        for (int i = 0; i < Lines.Count; i++)
        {
            DrawCentered(Lines[i], 215 + (i * 36), 26, Color.RayWhite);
        }

        DrawExtras(assets);

        foreach (var item in Items)
        {
            item.Button.Draw();
        }
    }

    private static void DrawCentered(string text, int y, int size, Color color)
    {
        var width = Raylib.MeasureText(text, size);
        Raylib.DrawText(text, (Program.VirtualWidth - width) / 2, y, size, color);
    }
}

class TitleScene : MenuScene
{
    private static readonly Vector2 LightSource = new(640, -100);
    private readonly List<CardView> Cards = [];

    protected override string Heading => Program.Title;

    public TitleScene()
    {
        Lines = ["A card game of arithmetic"];

        string[] faces = ["3", "+", "4", "*", "2"];
        for (int i = 0; i < faces.Length; i++)
        {
            var offset = i - 2;
            var category = i % 2 == 0 ? CardCategory.Number : CardCategory.Operator;

            Cards.Add(
                new CardView(
                    new Card(category, faces[i]),
                    new(640 + (offset * 95), 345 + (Math.Abs(offset) * 12))
                )
                {
                    Rotation = offset * 8,
                }
            );
        }

        AddButton("Play", true, () => new PickingScene(new RunInfo()));
        AddButton(
            "Quit",
            false,
            () =>
            {
                Program.Quit = true;
                return null;
            }
        );
    }

    protected override void DrawExtras(Assets assets)
    {
        foreach (var card in Cards)
            card.DrawShadow(assets, LightSource);
        foreach (var card in Cards)
            card.DrawCard(assets);
    }
}

class GameOverScene : MenuScene
{
    protected override string Heading => "Game Over";
    protected override Color HeadingColor => new(235, 90, 80, 255);
    protected override Color Background => new(45, 18, 18, 255);

    public GameOverScene(RunSummary run)
    {
        Lines =
        [
            $"You reached round {run.Round} of {run.RoundsToWin}",
            $"Score: {run.Score} / {run.Target}",
        ];

        AddButton("Try Again", true, () => new PickingScene(new RunInfo()));
        AddButton("Title Screen", false, () => new TitleScene());
    }
}

class GameWinScene : MenuScene
{
    protected override string Heading => "You Win!";
    protected override Color HeadingColor => Color.Gold;
    protected override Color Background => new(18, 55, 35, 255);

    public GameWinScene(RunSummary run)
    {
        Lines =
        [
            $"You cleared all {run.RoundsToWin} rounds",
            $"Final round score: {run.Score} / {run.Target}",
        ];

        AddButton("Play Again", true, () => new PickingScene(new RunInfo()));
        AddButton("Title Screen", false, () => new TitleScene());
    }
}
