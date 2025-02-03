using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CoreSystem {

    partial class CoreSystem {
        public static Scene coreSystemScene;

        static void SCENEMANAGER_SceneLoaded(Scene scene, LoadSceneMode mode) {
            Debug.Log($"scene loaded: {scene.name}  mode: {(mode == LoadSceneMode.Single ? "single" : "additive")}");
        }

        public static List<EventSystem> eventSystemsList;

        static void grabReferenceToEventSystemList() {
            Type type = typeof(EventSystem);
            var field = type.GetField("m_EventSystems", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
            eventSystemsList = (List<EventSystem>)field.GetValue(null);

            // EditorApplication.ExitPlaymode();
        }

        static void DeduplicateEventSystems() {
            if (eventSystemsList.Count > 1) {
                Debug.Log($"[coresystem] multiple event systems ({eventSystemsList.Count}) - de-duplicating event systems...");
                for (int i = 1; i < eventSystemsList.Count; ++i) {
                    Debug.Log($"    destroying ES belonging to {eventSystemsList[i].gameObject.name}");
                    // TODO: are we sure we want to keep the first one (belonging to CoreSystem)?
                    DestroyImmediate(eventSystemsList[1].gameObject);
                }
            }
        }
    }

}