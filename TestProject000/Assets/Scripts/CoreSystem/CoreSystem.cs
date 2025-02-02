using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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
}