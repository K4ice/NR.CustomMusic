using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime;
using PlanetJem.Audio;
using PlanetJem.Core;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace NR.CustomMusic;

/// <summary>
/// Всё, что зависит от внутренностей игры (проверено на 3.0.32), собрано здесь.
/// После обновления игры в первую очередь смотреть сюда.
///
/// Как это работает:
///  1. Каждый трек становится обычным MusicTrackEntry с собственным TrackId
///     и фейковым AssetReferenceAudioClip (GUID из имени файла).
///  2. Его MusicTrack добавляется в нужные плейлисты MusicLibrary.
///  3. В кэш PlanetJem.Core.ResourceProvider кладётся уже загруженный клип
///     под ключом этого GUID. Когда плеер просит загрузить трек,
///     ResourceProvider находит его в кэше и не ходит в Addressables.
/// </summary>
internal static class GameBridge
{
    /// <summary>Наши TrackId начинаются отсюда, чтобы не пересекаться с треками игры.</summary>
    public const int FirstCustomId = 100000;

    private static MusicLibrary library_;
    private static readonly Dictionary<int, MusicTrackEntry> entries_ = new();

    // Как ResourceProvider строит ключ кэша из GUID. Определяем по трекам игры.
    private static string keyPrefix_ = "";
    private static string keySuffix_ = "";
    private static bool keyFormatKnown_;

    public static void Sync(List<CustomTrack> tracks)
    {
        var library = FindLibrary();
        if (library == null)
            return;

        if (!keyFormatKnown_)
            DetectCacheKeyFormat(library);

        InjectIntoLibrary(library, tracks);
        RegisterInResourceCache(tracks);
    }

    private static MusicLibrary FindLibrary()
    {
        if (library_ is not null && !library_.WasCollected && library_ != null)
            return library_;

        library_ = null;
        var found = Resources.FindObjectsOfTypeAll(Il2CppType.Of<MusicLibrary>());
        if (found.Length > 0)
        {
            library_ = found[0].Cast<MusicLibrary>();
            Plugin.Logger.LogInfo($"MusicLibrary found: '{library_.name}', {library_.AllTracks?.Count ?? 0} tracks");
        }
        return library_;
    }

    private static MusicTrackEntry GetEntry(CustomTrack track)
    {
        if (!entries_.TryGetValue(track.Id, out var entry))
        {
            entry = new MusicTrackEntry
            {
                TrackName = track.Title,
                TrackId = (TrackId)track.Id,
                Clip = new AssetReferenceAudioClip(track.Guid),
            };
            entries_[track.Id] = entry;
        }
        return entry;
    }

    private static void InjectIntoLibrary(MusicLibrary library, List<CustomTrack> tracks)
    {
        var all = library.AllTracks;
        var lookup = library.musicAssets_; // null, пока игра не вызвала Cache()

        foreach (var track in tracks)
        {
            var entry = GetEntry(track);
            if (all != null && !ContainsEntry(all, track.Id))
                all.Add(entry);
            if (lookup != null && !lookup.ContainsKey((TrackId)track.Id))
                lookup.Add((TrackId)track.Id, entry);
        }

        foreach (var name in Plugin.Playlists.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var playlist = GetPlaylist(library, name);
            if (playlist?.Tracks == null)
            {
                Plugin.Logger.LogWarning($"Unknown or empty playlist '{name}', check config.");
                continue;
            }

            var list = playlist.Tracks;
            if (Plugin.ReplaceOriginal.Value)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                    if ((int)list[i].TrackId < FirstCustomId)
                        list.RemoveAt(i);
            }

            int added = 0;
            foreach (var track in tracks)
            {
                if (ContainsTrack(list, track.Id))
                    continue;
                list.Add(new MusicTrack { TrackId = (TrackId)track.Id, Weight = Plugin.Weight.Value });
                added++;
            }

            if (added > 0)
                Plugin.Logger.LogInfo($"Playlist {name}: +{added} track(s), now {list.Count}");
        }
    }

    private static MusicPlaylist GetPlaylist(MusicLibrary library, string name) => name.ToLowerInvariant() switch
    {
        "loading" => library.Loading,
        "garage" => library.Garage,
        "meetspot" => library.Meetspot,
        "cruise" => library.Cruise,
        "race" => library.Race,
        "racewin" => library.RaceWin,
        "raceloss" => library.RaceLoss,
        "mainmenu" => library.MainMenu,
        _ => null,
    };

    private static void RegisterInResourceCache(List<CustomTrack> tracks)
    {
        var cache = ResourceProvider.assets_;
        if (cache == null)
            return;

        foreach (var track in tracks)
        {
            string key = keyPrefix_ + track.Guid + keySuffix_;
            if (cache.ContainsKey(key))
                continue;

            // Игра могла выгрузить запись (MusicClipBuffer вытесняет старые треки) —
            // тогда просто создаём новую.
            var handle = Addressables.ResourceManager.CreateCompletedOperation<AudioClip>(track.Clip, string.Empty);
            AsyncOperationHandle untyped = handle;
            var entry = new ResourceProvider.AssetEntry(untyped);
            // +1 ссылка от мода, чтобы обычный Release не выгружал запись.
            entry.RefCount = 1;
            cache.Add(key, entry);

            if (Plugin.Verbose.Value)
                Plugin.Logger.LogInfo($"Cached '{track.Title}' as '{key}'");
        }
    }

    /// <summary>
    /// Ищем в кэше ResourceProvider ключ, содержащий GUID трека игры,
    /// и запоминаем, что стоит до и после GUID. Пока ни один трек игры
    /// не загружен, считаем, что ключ — это просто GUID.
    /// </summary>
    private static void DetectCacheKeyFormat(MusicLibrary library)
    {
        var cache = ResourceProvider.assets_;
        var all = library.AllTracks;
        if (cache == null || all == null || cache.Count == 0)
            return;

        var keys = new List<string>();
        foreach (var key in cache.Keys)
            keys.Add(key);

        for (int i = 0; i < all.Count; i++)
        {
            var entry = all[i];
            if ((int)entry.TrackId >= FirstCustomId || entry.Clip == null)
                continue;

            string guid = entry.Clip.AssetGUID;
            if (string.IsNullOrEmpty(guid))
                continue;

            foreach (var key in keys)
            {
                int at = key.IndexOf(guid, StringComparison.Ordinal);
                if (at < 0)
                    continue;

                keyPrefix_ = key.Substring(0, at);
                keySuffix_ = key.Substring(at + guid.Length);
                keyFormatKnown_ = true;
                Plugin.Logger.LogInfo($"Cache key format: '{keyPrefix_}<guid>{keySuffix_}' (from '{entry.TrackName}')");
                return;
            }
        }
    }

    private static bool ContainsEntry(Il2CppSystem.Collections.Generic.List<MusicTrackEntry> list, int id)
    {
        for (int i = 0; i < list.Count; i++)
            if ((int)list[i].TrackId == id)
                return true;
        return false;
    }

    private static bool ContainsTrack(Il2CppSystem.Collections.Generic.List<MusicTrack> list, int id)
    {
        for (int i = 0; i < list.Count; i++)
            if ((int)list[i].TrackId == id)
                return true;
        return false;
    }
}
