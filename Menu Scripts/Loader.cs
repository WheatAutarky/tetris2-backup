using UnityEngine;
using UnityEngine.SceneManagement;


//this and loadercallback will only trigger in the LoadingScene, hecne the Update() in the other file.
public static class Loader
{

    public enum Scene
    {
        MainMenuScene,
        FortyLinesScene,
        LoadingScene
    }
    private static Scene targetScene;


    public static void Load(Scene targetScene)
    {
        Loader.targetScene = targetScene;
        SceneManager.LoadScene(Scene.LoadingScene.ToString()); //loads the loading scene, then triggers the update in loadercallback
    }

    public static void LoaderCallback()
    {
        SceneManager.LoadScene(targetScene.ToString());
    }
}
