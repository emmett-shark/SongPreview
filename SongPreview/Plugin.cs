using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using SimpleJSON;
using TrombLoader.Data;
using TrombLoader.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace SongPreview
{
    [HarmonyPatch]
    [BepInDependency("TrombLoader")]
    [BepInPlugin("SongPreview", "Song Preview", "1.0.4")]
    public class Plugin : BaseUnityPlugin
    {
        private static int songIndex;
        private static LevelSelectController levelSelectController;
        private static List<SingleTrackData> allTrackslist;
        private static bool selectedNewTrack;

        private static bool changing;
        private static float lastTimeSelect;

        private static Plugin Instance;

        private void Awake()
        {
            Instance = this;

            new Harmony("com.hypersonicsharkz.songpreview").PatchAll();
        }

        void Update()
        {
            if (selectedNewTrack && !changing)
            {
                selectedNewTrack = false;

                Task.Run(() =>
                {
                    int songIndexToLoad = songIndex;
                    AudioClip clip = null;
                    changing = true;
                    try
                    {
                        string trackRef = allTrackslist[songIndexToLoad].trackref;
                        string baseSongPath = Application.dataPath + "/StreamingAssets/trackassets/" + trackRef;
                        if (!File.Exists(baseSongPath))
                        {
                            if (Globals.ChartFolders.TryGetValue(trackRef, out string path))
                            {
                                path = path + "/song.ogg";
                                IEnumerator audioClipSync = TrombLoader.Plugin.Instance.GetAudioClipSync(path, null);
                                while (audioClipSync.MoveNext())
                                {
                                    if (audioClipSync.Current != null)
                                    {
                                        if (audioClipSync.Current is string)
                                        {
                                            Plugin.Instance.Logger.LogError("Couldnt Load OGG FILE!!");
                                        }
                                        else
                                        {
                                            clip = (audioClipSync.Current as AudioClip);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Instance.Logger.LogWarning($"Could not find {trackRef}");
                                return;
                            }
                        }
                        else
                        {
                            AssetBundle myLoadedAssetBundle = AssetBundle.LoadFromFile(baseSongPath);
                            AudioSource component = myLoadedAssetBundle.LoadAsset<GameObject>("music_" + allTrackslist[songIndexToLoad].trackref).GetComponent<AudioSource>();

                            if (!selectedNewTrack)
                                clip = component.clip;

                            myLoadedAssetBundle.Unload(false);
                        }
                    }
                    catch (Exception e)
                    {
                        //Logger.LogError(e.Message);
                    }
                    finally
                    {
                        BepInEx.ThreadingHelper.Instance.StartSyncInvoke(new Action(() =>
                        {
                            if (!selectedNewTrack && clip != null)
                            {
                                levelSelectController.bgmus.clip = clip;
                                levelSelectController.bgmus.time = clip.length / 2;
                                levelSelectController.bgmus.Play();

                            }

                            changing = false;
                        }));
                    }
                });
            }
        }

        [HarmonyPatch(typeof(LevelSelectController), "populateSongNames")]
        private static void Postfix(LevelSelectController __instance, int ___songindex, List<SingleTrackData> ___alltrackslist)
        {
            levelSelectController = __instance;
            allTrackslist = ___alltrackslist;
            songIndex = ___songindex;

            selectedNewTrack = true;
            lastTimeSelect = Time.time;
        }
    }
}
