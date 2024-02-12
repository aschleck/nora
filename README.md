# nora

A Dota 2 bot.

This version of the code creates a lobby, ~~waits for a player to join, selects Nature's Prophet, and
makes a move towards the fountain~~ (this part is broken, because can't get correct IP address, if you want to fix this, start your investigation from ```string[] split = client.Lobby.connect.Split(':');```).
Past incarnations of this project have been used to make a spectactor site similar to TrackDOTA with real time updates.

This project stands on the shoulders of [SteamKit](https://github.com/SteamRE/SteamKit) among other libraries.
