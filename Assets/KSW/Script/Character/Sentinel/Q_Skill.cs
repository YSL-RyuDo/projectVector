using UnityEngine;

public class Q_Skill : MonoBehaviour
{
    [SerializeField] private GameObject _trapPrefab;
    [SerializeField] private float _throwRange = 10f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            tryPlaceTrap();
        }
    }

    private void tryPlaceTrap()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, _throwRange))
        {
            if (Vector3.Angle(hit.normal, Vector3.up) < 30f)
            {
                Instantiate(_trapPrefab, hit.point, Quaternion.identity);
            }
        }
    }
}