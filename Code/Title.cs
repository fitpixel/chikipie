using UnityEngine;
using UnityEngine.Assertions;

public class Title : MonoBehaviour {

    public void LoadScene(int level)
    {
        Application.LoadLevel(level);
    }
}
