using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FirstScene : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(LoadFirstScene());
    }

    IEnumerator LoadFirstScene()
    {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene("Menu");
    }
}
