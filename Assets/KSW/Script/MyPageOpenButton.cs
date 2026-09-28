using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public sealed class MyPageOpenButton : MonoBehaviour
{
    [SerializeField] private MyPagePanelController panel;

    public void Configure(MyPagePanelController value) => panel = value;

    private void Awake()
    {
        Button button = GetComponent<Button>();
        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.RemoveListener(Open);
            button.onClick.AddListener(Open);
        }
    }

    public void Open()
    {
        if (panel == null)
        {
            panel = Resources.FindObjectsOfTypeAll<MyPagePanelController>()
                .FirstOrDefault(value => value != null && value.gameObject.scene.IsValid());
        }
        if (panel == null)
        {
            Debug.LogError("[MY PAGE] MyPagePanelController was not found in the Lobby scene.");
            return;
        }

        Debug.Log("[MY PAGE] PlayerIcon clicked. Opening profile panel.");
        panel.gameObject.SetActive(true);
        panel.OpenSelf();
    }
}
