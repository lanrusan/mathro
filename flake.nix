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
    in
    {
      devShells.${system}.default = pkgs.mkShell {
        name = "csharp-devshell";

        packages = with pkgs; [
          dotnet-sdk

          roslyn-ls
          csharpier
        ];

        DOTNET_ROOT = "${dotnet-sdk}";

        DOTNET_CLI_TELEMETRY_OPTOUT = 1;

        NIX_LD_LIBRARY_PATH = pkgs.lib.makeLibraryPath (
          with pkgs;
          [
            # zlib
            # openssl
            # icu
            # stdenv.cc.cc.lib
          ]
        );

        CSHARP_DEV_ENV = "1";
      };
    };
}
