using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using Il2CppInterop.Runtime.Injection;

namespace NR.CustomMusic;

[BepInPlugin(Guid, Name, Version)]
public sealed class Plugin : BasePlugin
{
    public const string Guid = "nr.custommusic";
    public const string Name = "Custom Music";
    public const string Version = "1.0.0";

    internal static ManualLogSource Logger;

    internal static ConfigEntry<string> MusicFolder;
    internal static ConfigEntry<string> Playlists;
    internal static ConfigEntry<bool> ReplaceOriginal;
    internal static ConfigEntry<float> Weight;
    internal static ConfigEntry<bool> Verbose;

    public override void Load()
    {
        Logger = Log;

        MusicFolder = Config.Bind("General", "MusicFolder", "CustomMusic",
            "Папка с треками (.mp3 / .ogg / .wav). Относительный путь считается от папки игры.");
        Playlists = Config.Bind("General", "Playlists", "Cruise,Meetspot,Garage,Race",
            "В какие плейлисты добавлять треки, через запятую. " +
            "Доступны: Loading, Garage, Meetspot, Cruise, Race, RaceWin, RaceLoss, MainMenu");
        ReplaceOriginal = Config.Bind("General", "ReplaceOriginal", false,
            "true = в выбранных плейлистах играют только твои треки.");
        Weight = Config.Bind("General", "Weight", 1f,
            "Вес трека при случайном выборе (у треков игры обычно 1).");
        Verbose = Config.Bind("Debug", "Verbose", false,
            "Подробный лог для отладки.");

        string folder = MusicFolder.Value;
        if (!Path.IsPathRooted(folder))
            folder = Path.Combine(Paths.GameRootPath, folder);
        Directory.CreateDirectory(folder);

        ClassInjector.RegisterTypeInIl2Cpp<CustomMusicBehaviour>();
        var behaviour = AddComponent<CustomMusicBehaviour>();
        behaviour.Init(folder);

        Logger.LogInfo($"{Name} {Version} loaded. Music folder: {folder}");
    }
}
