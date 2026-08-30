using UnityEngine;


//Called by being attached to the active loadercallbackObject upon loading the LoadingScene
public class LoaderCallback : MonoBehaviour 
{
    private bool isFirstUpdate = true;

    private void Update()
    {
        if (isFirstUpdate)
        {
            isFirstUpdate = false;

            Loader.LoaderCallback();
        }
    }
}
