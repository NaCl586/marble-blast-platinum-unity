# Marble Blast Platinum Unity Port

The Unity Port of Marble Blast Platinum, based on [Marble Blast Gold Unity remake](https://github.com/NaCl586/marble-blast-gold-unity/) that I did. This remake is based on Marble Blast Platinum 1.14. 
These features are not yet implemented (and might possibly not implemented later): Leaderboards, and Level Editor. Level Editor is not planned, so don't ask. Leaderboards is planned though, but I'm not sure when I'm gonna make it.

<b>As version 1.3, Leaderboards has been implemented, check below for more information.</b>

<img src="https://i.imgur.com/j1YrNlX.png" width="640">
<img src="https://i.imgur.com/yyTlJ6q.png" width="640">
<img src="https://i.imgur.com/WEB3hS8.png" width="640">
<img src="https://i.imgur.com/3HdzfdO.png" width="640">

As the time of me writing this, the game has not been fully tested as I don't have the skill and capability to complete all levels.
Known issues:
- Checkpoint respawn orientation when being upside down is wrong (for this reason, Space Station mis file is changed)
- Some interiors are not parsed correctly into the game (for this reason, Space Station interior is split into two)
- You can't scroll scrollbars using mouse wheel 
- Some Director's Cut levels can cause crash when loading (e.g. Cubed Maze), and some are impossible (e.g. Getting Squeezed, Bent Reality)

Please report other bugs that you find! Thank you

Also lightning is still not optimized enough, feel free to give feedback too

## Download the Windows build [here](https://github.com/NaCl586/marble-blast-platinum-unity/releases/)

If you find bugs or things that are not faithful with the original Marble Blast Platinum, feel free to message me on discord NaCl586#8479.

Special thanks to Vani and RandomityGuy for helping me whenever I have problems when making this project.

## Additional Controls

Press R for quick respawn, works when the game is paused. This button currently is not remappable because I wanted to create the same UI remake without additional things. Also, setting video driver and color mode is just pure cosmetic and does not work.

## Leaderboard System

Leaderboard is now here at version 1.3, it works similar to the original Marble Blast Platinum 1.14, but without chat, account, and rating features

The leaderboard system allows players to compete for the fastest times across different levels. After finishing a level, your time can be submitted to the leaderboard, where it will be ranked alongside other players' times. You can view the global records for each level, as well as your own personal best, making it easy to see how well you rank and keep track of your improvements.

Leaderboard records can also include a replay of the run. If a replay is available, you can watch the run directly from the leaderboard to see how another player achieved their time. This makes the leaderboard more than just a list of records, allowing players to compare their runs, learn from faster players, and compete for better times.

## Replay System

Replay system is now here at version 1.2, it works pretty much similar to the original Marble Blast Platinum 1.14. 

To start recording, click on the record button (the bottom-right most button on the level select menu), then you will be asked for a filename. Enter a filename, which must not be the same with any existing replay. When playing, a recording icon will show under the powerup panel (similar within the PlatinumQuest). You can end the recording prematurely or finish the level; either way stops the recording, in which after that you will be asked to fill a short form, containing recording name, recording author, and recording description. Click apply to save the replay or click cancel to discard the replay. If you restart a run then saved a new file, the old one will be overwritten. Also, marble skin used will be saved too.

To load the recording, on the main menu, open replay center, then click the replay you want to play. When playing a replay, it will play until the end, or you can end it prematurely by pressing escape.

Replays are stored in Marble Blast Platinum 1.14_Data\Replays with extension ".urec", short for Unity Port Rec (not using .rec to differentiate between the original vanilla recording format). You can share and exchange .urec files between machines (theoretically). You can see some sample replays I have in the repo.

## Custom Music for Jukebox

You can add custom musics into the jukebox by placing ".ogg" files in Marble Blast Platinum 1.14_Data\CustomMusics. If you have a decent number of files there, it will take a while to load all into the memory. Tested working (in the repo, I put .ogg musics from Marble Blast Future for testing).

## Save Data

<img src="https://i.imgur.com/u2wAziG.png" width="640">

Save data uses [PlayerPrefs](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/PlayerPrefs.html), which can be accessed via Registry Editor (see picture). If you wanna unlock the levels, you can create a key or edit existing key called "QualifiedLevel[Difficulty][Game]" to a large integer like 9999. The PlayerPrefs essentially is equivalent to prefs.cs in vanilla Marble Blast.

## Custom Level Support

You can add custom levels that are specifically made for Marble Blast Gold and Platinum by placing the mission file in Marble Blast Platinum 1.14_Data\StreamingAssets\marble\data\missions\custom and the interior file in Marble Blast_Data\StreamingAssets\marble\data\interiors (or wherever you put your .dif files when making the level). Adding new folders or custom levels that are not made for Marble Blast Gold do not work. You can technically add more levels to the main game with the same way. This feature is theoretically working but still untested.

## Custom Marble Support

Identical with the original MBP 1.14, you can modify a marble texture in the Marble Blast Platinum 1.14_Data\StreamingAssets\marble\custom_marbles folder. Make sure to have the exact same name as the original file when you are changing skins.
