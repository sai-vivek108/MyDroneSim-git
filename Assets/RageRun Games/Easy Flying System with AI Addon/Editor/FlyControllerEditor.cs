using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RageRunGames.EasyFlyingSystem
{
    [CustomEditor(typeof(DroneController))]
    public class FlyControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            DroneController droneController = (DroneController)target;

            GUI.color = Color.white;

            EditorGUILayout.LabelField("Input Settings:", EditorStyles.boldLabel, GUILayout.Width(140));

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("AI")) // <-- Added AI button
            {
                Debug.Log("AI button added");
                droneController.AddAIInputs(); // <-- Calls AddAIInputs method
                if (!Application.isPlaying)
                {
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                }
                //SaveSceneIfNotPlaying();
            }

            if (GUILayout.Button("Keyboard"))
            {
                droneController.AddKeyboardInputs();

                if (!Application.isPlaying)
                {
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                }
            }

            if (GUILayout.Button("Mobile"))
            {
                droneController.AddMobileInputs();

                if (!Application.isPlaying)
                {
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                }
            }

            if (GUILayout.Button("Mouse"))
            {
                droneController.AddMouseInputs();

                if (!Application.isPlaying)
                {
                    EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Current Input Selected:", EditorStyles.boldLabel, GUILayout.Width(140));
            GUI.color = Color.green;
            EditorGUILayout.LabelField($"{droneController.GetInputType()}", EditorStyles.boldLabel, GUILayout.Width(70));
            EditorGUILayout.EndHorizontal();
        }

        /*[MenuItem("Tools/Easy Flying System/Toggle AI & Keyboard Input %&t")]
        private static void ToggleAIKeyboardInput()
        {
            DroneController droneController = FindFirstObjectByType<DroneController>();
            Debug.Log("Drone Controller name in FlyEditor : "+droneController.name);
            if (droneController == null)
            {
                Debug.LogWarning("No DroneController found in the scene!");
                return;
            }

            if (droneController.GetInputType() == InputType.AI)
            {
                droneController.AddKeyboardInputs();
                Debug.Log("Switched to Keyboard Input");
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
            }
            else
            {
                droneController.AddAIInputs();
                Debug.Log("Switched to AI Input");
                if (!Application.isPlaying)
                {
                    EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
                }
            }
        }*/
        [MenuItem("Tools/Easy Flying System/Toggle AI & Keyboard Input %&t")]
        private static void ToggleAIKeyboardInput()
        {
            // Get the currently selected GameObject in the editor
            GameObject selectedObject = Selection.activeGameObject;

            if (selectedObject == null)
            {
                Debug.LogWarning("No object selected in the scene!");
                return;
            }

            // Attempt to get the DroneController component from the selected object
            DroneController droneController = selectedObject.GetComponent<DroneController>();

            if (droneController == null)
            {
                Debug.LogWarning("Selected object does not have a DroneController!");
                return;
            }

            // Log the name of the selected DroneController
            Debug.Log("Drone Controller name in FlyEditor: " + droneController.name);

            // Check the current input type and switch accordingly
            if (droneController.GetInputType() == InputType.AI)
            {
                droneController.AddKeyboardInputs();
                Debug.Log("Switched to Keyboard Input");
            }
            else
            {
                droneController.AddAIInputs();
                Debug.Log("Switched to AI Input");
            }

            // If not in play mode, mark the scene as dirty to save changes
            if (!Application.isPlaying)
            {
                EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            }
        }
    }
}
