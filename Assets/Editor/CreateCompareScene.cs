using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using VoidConverter.View;

namespace VoidConverter.EditorTools
{
    /// <summary>Creates Assets/Scenes/EffectCompare.unity, the before/after scene for recording the play clip.</summary>
    public static class CreateCompareScene
    {
        const string Path = "Assets/Scenes/EffectCompare.unity";

        [MenuItem("Void Converter/Create Effect Compare Scene")]
        public static void Create()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Effect Compare").AddComponent<EffectCompare>();
            EditorSceneManager.SaveScene(scene, Path);
            Debug.Log("Created " + Path + " (not added to the build settings).");
        }
    }
}
