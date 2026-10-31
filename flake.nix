{
  description = "Flake for Dotnet";

  inputs = {
    nixpkgs.url = "github:NixOS/nixpkgs/nixos-unstable";
  };

  outputs =
    { nixpkgs, ... }:
    let
      system = "x86_64-linux";
      pkgs = import nixpkgs { inherit system; };
      dotnet-sdk = pkgs.dotnetCorePackages.sdk_10_0;
      runtimeLibs = with pkgs; [
        libGL

        wayland
        libxkbcommon
        alsa-lib
        xorg.libX11
        xorg.libXcursor
        xorg.libXi
        xorg.libXrandr
        xorg.libXinerama
      ];
    in
    {
      devShells.${system}.default = pkgs.mkShell {
        name = "csharp-devshell";

        packages = with pkgs; [
          dotnet-sdk

          roslyn-ls
          csharpier
        ];

        buildInputs = runtimeLibs;

        DOTNET_ROOT = "${dotnet-sdk}";

        DOTNET_CLI_TELEMETRY_OPTOUT = 1;

        LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath runtimeLibs;

        CSHARP_DEV_ENV = "1";
      };
    };
}
