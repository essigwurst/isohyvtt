# Essigstudios ISOHYVTT

This application / game was made as a _spare time project_ and is currently under development.
Please note that i made this game to play D&D online with friends while they are not physically
available - so this application is designed for exactly this use- case.
This project was released into the public because many other people may have the same issue, and
want to self- host their tabletop game.

*Some things are untested, and of course there are some bugs present - under development!*

## Prerequisites

- .NET SDK 7.0
- Newtonsoft.Json (NuGet package); planned to be removed
- Godot 4.1.1

## Notes

- Currently the server application does only run on Windows
- It's actually not isometric, but you may use isometric assets ;)

## Open topics

- Replace HTTP Listener by a TCP socket (This was originally written for testing purposes only)
- GameHUD layering (You can't click on asset objects when there is an game element behind it)
- Implement layering on server side
- Input lag when scaling or moving objects which do overlap
- Full map zoom and drag and drop
- Dynamic health bars (the TextureRect's was replaced by GameElementNodes to be able to add subnodes more efficently)

# License

[Apache2.0](https://www.apache.org/licenses/LICENSE-2.0)