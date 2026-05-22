using UnityEngine;
using UnityEngine.SceneManagement; 

public class TitleManager : MonoBehaviour
{

    public void OnStartButton()
    {
        SceneManager.LoadScene(1);
    }
}