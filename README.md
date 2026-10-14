# Custom Music for NIGHT-RUNNERS

Add your own music to the in-game music player of **NIGHT-RUNNERS**.
Drop `.mp3` / `.ogg` / `.wav` files into a folder — they play in the garage, on the streets, at meetspots and in races, right alongside the original soundtrack, with the usual crossfades and the *Now Playing* popup.

> **Game version:** 3.1.9 (Private Alpha) · **Loader:** BepInEx 6 IL2CPP (be.788+)

[Русская версия ниже](#русский)

---

## Features

- Your tracks are added as real tracks of the in-game player — skip, crossfade, shuffle and *Now Playing* all work as usual
- Choose which playlists get your music: `Cruise`, `Meetspot`, `Garage`, `Race`, `RaceWin`, `RaceLoss`, `MainMenu`, `Loading`
- Optional: **replace** the original soundtrack completely
- Tracks are kept compressed in memory, so a large playlist does not eat your RAM
- No game files are modified

## Installation

1. Install **BepInEx 6 (IL2CPP, x64)**, build **be.788 or newer**: <https://builds.bepinex.dev/projects/bepinex_be>
   Unzip it into the game folder (next to `NIGHT-RUNNERS PRIVATE ALPHA.exe`) and launch the game once — the first launch takes a minute while BepInEx prepares itself.
2. Download `NR.CustomMusic-x.y.z.zip` from [Releases](../../releases) and unzip it into the game folder.
3. Put your music into the `CustomMusic` folder in the game folder.
4. Launch the game.

The final layout:

```
NIGHT-RUNNERS PRIVATE ALPHA.exe
BepInEx/
  plugins/
    NR.CustomMusic/
      NR.CustomMusic.dll
CustomMusic/
  Artist - Title.mp3
  ...
```

**Track names** are taken from file names. The original soundtrack uses the `title - artist` style, so naming your files the same way keeps the *Now Playing* popup consistent.

## Configuration

After the first launch, edit `BepInEx/config/nr.custommusic.cfg`:

| Setting | Default | Description |
|---|---|---|
| `MusicFolder` | `CustomMusic` | Folder with your tracks. Relative paths start from the game folder. |
| `Playlists` | `Cruise,Meetspot,Garage,Race` | Which playlists get your tracks (comma separated). |
| `ReplaceOriginal` | `false` | `true` = only your tracks play in the selected playlists. |
| `Weight` | `1` | Chance of a track being picked in shuffle (original tracks use 1). |
| `Verbose` | `false` | Detailed logging for troubleshooting. |

## Troubleshooting

Open `BepInEx/LogOutput.log` and look for lines starting with `[Custom Music]`.

- **`Found 0 track(s)`** — the music folder is empty or the files have an unsupported extension.
- **`Failed to load '...'`** — the file is broken or in an unsupported format; re-encode it to `.ogg` or `.mp3`.
- **Nothing from the mod in the log** — BepInEx is not installed correctly (wrong build, or Mono instead of IL2CPP).

## Building from source

Requirements: .NET SDK 6+ and the game with BepInEx launched at least once (it generates `BepInEx/interop`).

```
copy GameDir.props.example GameDir.props   # then set your game path inside
dotnet build
```

The DLL is copied into the game's `BepInEx/plugins/NR.CustomMusic` automatically. `package.ps1` builds the release zip.

All code that depends on the game's internals lives in `GameBridge.cs` — that's the file to check after a game update.

## How it works

Each file is loaded into a Unity `AudioClip` and registered as a regular `MusicTrackEntry` in the game's `MusicLibrary` with its own `TrackId`. The loaded clip is placed directly into the cache of the game's resource loader, so when the player asks for the track it gets the clip instead of going to Addressables. No Harmony patches are used.

## License

[MIT](LICENSE). This is an unofficial fan mod, not affiliated with the developers of NIGHT-RUNNERS. Do not upload copyrighted music together with the mod.

---

## Русский

Мод добавляет твою музыку во встроенный плеер **NIGHT-RUNNERS**. Кидаешь `.mp3` / `.ogg` / `.wav` в папку — треки играют в гараже, на улицах, на митспотах и в гонках вместе с родным саундтреком, с переходами и всплывающим *Now Playing*.

> **Версия игры:** 3.1.9 · **Загрузчик:** BepInEx 6 IL2CPP (be.788+)

### Установка

1. Установи **BepInEx 6 (IL2CPP, x64)**, сборку **be.788 или новее**: <https://builds.bepinex.dev/projects/bepinex_be>. Распакуй в папку игры (рядом с `NIGHT-RUNNERS PRIVATE ALPHA.exe`) и один раз запусти игру — первый запуск идёт около минуты.
2. Скачай `NR.CustomMusic-x.y.z.zip` из [Releases](../../releases) и распакуй в папку игры.
3. Положи свою музыку в папку `CustomMusic` в папке игры.
4. Запускай игру.

Название трека берётся из имени файла. Родные треки подписаны как `название - исполнитель` — если называть файлы так же, всё будет выглядеть единообразно.

### Настройки

Файл `BepInEx/config/nr.custommusic.cfg` (появляется после первого запуска):

- `MusicFolder` — папка с треками (по умолчанию `CustomMusic` в папке игры);
- `Playlists` — в какие плейлисты добавлять: `Cruise, Meetspot, Garage, Race, RaceWin, RaceLoss, MainMenu, Loading`;
- `ReplaceOriginal` — `true`, чтобы играли только твои треки;
- `Weight` — шанс выпадения трека при перемешивании (у родных треков 1);
- `Verbose` — подробный лог.

### Если не работает

Смотри `BepInEx/LogOutput.log`, строки `[Custom Music]`:
- `Found 0 track(s)` — папка пустая или формат не поддерживается;
- `Failed to load` — файл битый, пережми его в `.ogg` или `.mp3`;
- от мода в логе ничего нет — BepInEx установлен неправильно (не та сборка или Mono вместо IL2CPP).
