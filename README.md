HaskellPackage
==============

Haskell integration for Visual Studio

I make absolutely no guarantees about it working properly but I would appreciate bug reports if anyone tries using it.

You need to have ghc/ghci installed. Preferably the Haskell Platform because that's what I tested it on.
You also need ghc-mod which you can get via cabal. All of those need to be in your PATH.
Aside from that, I've only tested it on VS2013 ultimate and have NO idea how well, if at all, it works on other versions.

To use the extension just open any .hs file in Visual Studio. If you see syntax coloring, then it works.
Everything works by invoking ghc-mod from the command line so to use features make sure you save your file.

To see the type of an identifier, mouse over it. This works only if the file doesn't contain errors.

The errors get updated automatically on save.

The comment/uncomment block feature also works.

To get the GHCi window go to View -> Other Windows -> GHCi.
GHCi automatically refreshes and loads the currently open file whenever you hit save. If you get stuck with GHCi outputting too much data (for example, typing [1..]) just Ctrl-S on the currently open file and it will restart.

Installation
============

The .VSIX is in the releases (https://github.com/LukaHorvat/HaskellPackage/releases/)
Download it and install.
