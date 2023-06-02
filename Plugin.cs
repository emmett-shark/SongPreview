using System.Collections;
using BaboonAPI.Hooks.Tracks;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using TrombLoader.CustomTracks;
using TrombLoader.Helpers;
using UnityEngine;

namespace SongPreview;

[HarmonyPatch]
[BepInDependency("TrombLoader")]
[BepInDependency("ch.offbeatwit.baboonapi.plugin")]
[BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    private static Plugin Instance;
    private static ManualLogSource Log;

    private void Awake()
    {
        Instance = this;
        Log = Logger;
        new Harmony(PluginInfo.PLUGIN_GUID).PatchAll();
    }

    [HarmonyPatch(typeof(LevelSelectController), nameof(LevelSelectController.populateSongNames))]
    private static void Postfix(LevelSelectController __instance)
    {
        string trackRef = __instance.alltrackslist[__instance.songindex].trackref;
        //this no work: (technically compiles but will spam ur log and crash when you choose a song)
        //var clip = TrackLookup.lookup(trackRef).LoadTrack().LoadAudio().Clip;
        ThreadingHelper.Instance.StartCoroutine(Play(__instance, trackRef));
    }

    public static IEnumerator Play(LevelSelectController __instance, string trackRef)
    {
        var track = TrackLookup.lookup(trackRef);
        if (Globals.IsCustomTrack(trackRef))
        {
            var customTrack = track as CustomTrack;
            var audioClipSync = TrombLoader.Plugin.Instance.GetAudioClipSync(customTrack.folderPath + "/song.ogg");
            while (audioClipSync != null && audioClipSync.MoveNext())
            {
                yield return null;
                if (audioClipSync?.Current is AudioClip clip)
                {
                    __instance.bgmus.clip = clip;
                    __instance.bgmus.time = clip.length > 60 ? clip.length / 2 : 0;
                    __instance.bgmus.Play();
                }
            }
        }
    }
}
