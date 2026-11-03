using System.Numerics;
using Mathro.Core;
using Raylib_cs;

namespace Mathro.Client;

interface IScene
{
    public IScene? Update(float dt, MouseInput input);
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

record RunSummary(int Round, long Score, long Target, int RoundsToWin);

class RunInfo
{
    // public uint Currency = 0;
    public Deck Deck = new();
    public RoundSettings Settings = new();
    public int RoundsToWin = 4;
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

    public static bool Quit;
    public static string Title = "Calculatro";

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

        World world = new(new TitleScene());
        // World world = new(new PickingScene(new()));
        InputReader inputReader = new();

        while (!Raylib.WindowShouldClose() && !Quit)
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

            var nextScene = world.Scene.Update(dt, input);
            if (nextScene is not null)
            {
                world.Scene = nextScene;
            }

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
