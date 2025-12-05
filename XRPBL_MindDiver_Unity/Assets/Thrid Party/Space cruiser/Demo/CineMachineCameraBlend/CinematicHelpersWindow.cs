
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace CinematicSampleUtilities
{
    public class CinematicHelpersWindow : EditorWindow
    {
        static bool frameOverlayToggle = false;

        // Add menu item named "My Window" to the Window menu
        [MenuItem("Window/Cinematic" + "/Helpers", priority = 3005)]
        public static void ShowWindow()
        {
            //Show existing window instance. If one doesn't exist, make one.
            EditorWindow.GetWindow(typeof(CinematicHelpersWindow), false, "Cinematic Helpers");
        }

        void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Layouts");
            GUILayout.BeginVertical();
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Cinematic"))
                LoadCustomLayout("Assets/CinematicTemplate/Layouts/CinematicToolbox.wlt");
            if (GUILayout.Button("Animation"))
                LoadCustomLayout("Assets/CinematicTemplate/Layouts/CinematicToolboxAnimation.wlt");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Lighting"))
                LoadCustomLayout("Assets/CinematicTemplate/Layouts/CinematicToolboxLighting.wlt");
            if (GUILayout.Button("Audio"))
                LoadCustomLayout("Assets/CinematicTemplate/Layouts/CinematicToolboxAudio.wlt");
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Visibility");
            GUILayout.BeginVertical();
            frameOverlayToggle = ToggleObject(frameOverlayToggle, "CinematicOverlay", "FrameOverlay");

            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }

        public static void LoadCustomLayout(string layoutPath)
        {
            if (System.IO.File.Exists(layoutPath))
            {
                EditorUtility.LoadWindowLayout(layoutPath);
            }
            else
            {
                Debug.LogWarning("Layout not loaded. Layout file missing at: " + layoutPath);
            }
        }

        private bool ToggleObject(bool toggleValue, string searchString, string toggleName)
        {
            GameObject foundObject = null;
            for (int i = 0; i < SceneManager.sceneCount; ++i)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.isLoaded)
                {
                    GameObject[] rootGameObjects = scene.GetRootGameObjects();
                    foreach (GameObject go in rootGameObjects)
                    {
                        if (string.Equals(go.name, searchString))
                        {
                            foundObject = go;
                            break;
                        }
                    }

                    if (foundObject != null)
                        break;
                }
            }

            if (foundObject)
            {
                toggleValue = foundObject.activeSelf;
                toggleValue = GUILayout.Toggle(toggleValue, toggleName);
                foundObject.SetActive(toggleValue);
            }

            return toggleValue;
        }
    }
}