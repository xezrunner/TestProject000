using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using static CoreSystemFramework.Logging;

namespace CoreSystemFramework {

    partial class CoreSystem {
        public static bool UNITY_receiveLogMessages = true;
        
        public static Scene coreSystemScene;

        static void SCENEMANAGER_SceneLoaded(Scene scene, LoadSceneMode mode) {
            Debug.Log($"scene loaded: {scene.name}  mode: {(mode == LoadSceneMode.Single ? "single" : "additive")}");
        }

        public static List<EventSystem> eventSystemList;

        static void grabReferenceToEventSystemList() {
            Type type = typeof(EventSystem);
            var field = type.GetField("m_EventSystems", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            eventSystemList = (List<EventSystem>)field.GetValue(null);

            // EditorApplication.ExitPlaymode();
        }

        static void UPDATE_DeduplicateEventSystems() {
            if (eventSystemList.Count > 1) {
                log($"multiple event systems ({eventSystemList.Count}) - de-duplicating event systems...");

                // @Performance
                // Have to copy this list, because the acutal list changes as we disable the event systems:
                var eventSystems = new EventSystem[eventSystemList.Count];
                eventSystemList.CopyTo(eventSystems);
                
                foreach (var it in eventSystems) {
                    var obj = it.gameObject;

                    var sceneName = obj.scene.name;
                    if (sceneName == CORESYSTEM_SCENE_NAME) continue;

                    // TODO: are we sure we want to keep the first one (belonging to CoreSystem)?
                    #if false
                    log($"  - destroying object with EventSystem on it: {sceneName}::'{obj.name}'");
                    DestroyImmediate(obj);
                    #else
                    log($"  - disabling EventSystem belonging to {sceneName}::'{obj.name}'");
                    it.enabled = false;
                    #endif
                }
            }
        }

        // TODO: we'll have to think about this...
        public static bool IsInputCapturedByCoreSystem() {
            if (Instance) {
                if (Instance.DebugConsole?.getState() ?? false) return true;
            }
            
            return false;
        }
    }

}