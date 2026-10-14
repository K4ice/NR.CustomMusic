using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.Networking;

namespace NR.CustomMusic;

/// <summary>
/// Грузит аудиофайлы по одному и раз в секунду синхронизирует их с плеером игры.
/// Ничего не знает о внутренностях игры — всё это в GameBridge.
/// </summary>
public sealed class CustomMusicBehaviour : MonoBehaviour
{
    private static readonly Dictionary<string, AudioType> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".mp3"] = AudioType.MPEG,
        [".ogg"] = AudioType.OGGVORBIS,
        [".wav"] = AudioType.WAV,
    };

    private readonly Queue<CustomTrack> pending_ = new();
    private readonly List<CustomTrack> ready_ = new();
    private CustomTrack loading_;
    private UnityWebRequest request_;
    private float nextSync_;

    public CustomMusicBehaviour(IntPtr ptr) : base(ptr) { }

    public void Init(string folder)
    {
        var files = Directory.EnumerateFiles(folder)
            .Where(f => SupportedTypes.ContainsKey(Path.GetExtension(f)))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (int i = 0; i < files.Count; i++)
        {
            pending_.Enqueue(new CustomTrack
            {
                FilePath = files[i],
                Title = Path.GetFileNameWithoutExtension(files[i]),
                Id = GameBridge.FirstCustomId + i,
                Guid = MakeGuid(Path.GetFileName(files[i])),
            });
        }

        Plugin.Logger.LogInfo($"Found {files.Count} track(s).");
        if (files.Count == 0)
            Plugin.Logger.LogWarning($"Put .mp3/.ogg/.wav files into: {folder}");
    }

    private void Update()
    {
        PumpLoading();

        if (Time.unscaledTime < nextSync_)
            return;
        nextSync_ = Time.unscaledTime + 1f;

        if (ready_.Count == 0)
            return;

        try
        {
            GameBridge.Sync(ready_);
        }
        catch (Exception e)
        {
            Plugin.Logger.LogError($"Sync failed: {e}");
            nextSync_ = Time.unscaledTime + 10f;
        }
    }

    private void PumpLoading()
    {
        if (request_ == null)
        {
            if (pending_.Count == 0)
                return;

            loading_ = pending_.Dequeue();
            var type = SupportedTypes[Path.GetExtension(loading_.FilePath)];
            request_ = UnityWebRequestMultimedia.GetAudioClip(new Uri(loading_.FilePath).AbsoluteUri, type);
            // Держим клип в памяти сжатым, иначе каждый трек раздувается до ~10 МБ/мин.
            request_.downloadHandler.Cast<DownloadHandlerAudioClip>().compressed = true;
            request_.SendWebRequest();
            return;
        }

        if (!request_.isDone)
            return;

        try
        {
            if (!string.IsNullOrEmpty(request_.error))
            {
                Plugin.Logger.LogError($"Failed to load '{loading_.Title}': {request_.error}");
                return;
            }

            var clip = DownloadHandlerAudioClip.GetContent(request_);
            if (clip == null || clip.length <= 0f)
            {
                Plugin.Logger.LogError($"Failed to load '{loading_.Title}': empty clip");
                return;
            }

            clip.name = loading_.Title;
            // Не даём Resources.UnloadUnusedAssets выгрузить клип при смене сцены.
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            loading_.Clip = clip;
            ready_.Add(loading_);

            Plugin.Logger.LogInfo($"Loaded '{loading_.Title}' ({clip.length:0}s)");
        }
        finally
        {
            request_.Dispose();
            request_ = null;
            loading_ = null;
        }
    }

    /// <summary>Стабильный GUID в формате Unity (32 hex) из имени файла.</summary>
    private static string MakeGuid(string fileName)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes("nr.custommusic/" + fileName));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

internal sealed class CustomTrack
{
    public string FilePath;
    public string Title;
    public int Id;
    public string Guid;
    public AudioClip Clip;
}
