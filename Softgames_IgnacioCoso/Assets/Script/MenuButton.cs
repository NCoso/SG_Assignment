using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
#endif

public class ChangeSceneButton : Button
{
    [SerializeField] protected int sceneIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        onClick.AddListener(ChangeScene);
    }
    
    private void ChangeScene()
    {
        if (Application.CanStreamedLevelBeLoaded(sceneIndex))
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.LogError($"Scene index {sceneIndex} is not valid or not added to build settings!");
        }
    }
}





#if UNITY_EDITOR
[CustomEditor(typeof(ChangeSceneButton), true)]
[CanEditMultipleObjects]
public class ChangeSceneButtonEditor : ButtonEditor
{
    private SerializedProperty sceneIndexProperty;

    protected override void OnEnable()
    {
        base.OnEnable();
        sceneIndexProperty = serializedObject.FindProperty("sceneIndex");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(sceneIndexProperty);
        EditorGUILayout.Space();
        serializedObject.ApplyModifiedProperties();
        base.OnInspectorGUI();
    }
}
#endif