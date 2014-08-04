HaskellPackage
==============

Haskell integration for Visual Studio

I make absolutely no guarantees about it working properly but I would appreciate bug reports if anyone tries using it.

You need to have ghc/ghci installed. Preferably the Haskell Platform because that's what I tested it on.
You also need hdevtools, the fork with Windows support.
Clone this project https://github.com/mvoidex/hdevtools, go into the folder and run 'cabal install'. 
All of those need to be in your PATH. They should be there by default.

Aside from that, I've only tested it on VS2013 ultimate and have NO idea how well, if at all, it works on other versions.

When you run the extension for the first time you might be asked to add the hdevtools server to firewall exceptions. It doesn't matter if you do or don't.

To use the extension just open any .hs file in Visual Studio. If you see syntax coloring, then it works.
Everything works by invoking hdevtools from the command line so to use features make sure you save your file.

To see the type of an identifier, mouse over it. This works only if the file doesn't contain errors.

The errors get updated automatically on save.

The comment/uncomment block feature also works.

To get the GHCi window go to View -> Other Windows -> GHCi.
GHCi automatically refreshes and loads the currently open file whenever you hit save. If you get stuck with GHCi outputting too much data (for example, typing [1..]) just Ctrl-S on the currently open file and it will restart.

Notes
=====

Due to certain limitations, you can only use the extension for a single project at a time. If you want to switch to another project, close Visual Studio and open up a new file.

Installation
============

The .VSIX is in the releases (https://github.com/LukaHorvat/HaskellPackage/releases/)
Download it and install.
