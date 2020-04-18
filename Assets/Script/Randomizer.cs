using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class Randomizer : MonoBehaviour
{
    [SerializeField] GameObject[] _objects = null;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene("Stress");
    }

    void Update()
    {
        var count = Random.Range(1, 4);
        for (var i = 0; i < count; i++)
        {
            var index = Random.Range(0, _objects.Length);
            _objects[index].SetActive(!_objects[index].activeSelf);
        }
    }
}
