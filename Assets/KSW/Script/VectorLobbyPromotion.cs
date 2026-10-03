using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VectorLobbyPromotion : MonoBehaviour
{
    public NavigationPanelController navigation;
    public VectorEventPass pass;
    public Image art;
    public TMP_Text title;
    public Sprite[] banners;
    public string[] titles;
    public Image[] indicators;
    public CanvasGroup bannerVisibility, entryVisibility;
    public VectorShopNavigation shop;
    public int[] shopPages;
    private int current;
    private float elapsed;
    private void OnEnable(){elapsed=0; Show(0);}
    private void Update()
    {
        bool visible=navigation.currentLobbyMainMenu==LobbyMainMenu.Lobby&&!pass.gameObject.activeSelf;
        SetVisible(bannerVisibility,visible);SetVisible(entryVisibility,visible);
        if(!visible)return;
        elapsed+=Time.unscaledDeltaTime;if(elapsed>=5){elapsed=0;Show(current+1);}
    }
    public void Show(int index){if(banners.Length==0)return;current=index%banners.Length;art.sprite=banners[current];title.text=titles[current];for(int i=0;i<indicators.Length;i++)indicators[i].color=i==current?Color.yellow:Color.white;}
    static void SetVisible(CanvasGroup group,bool visible){if(group!=null){group.alpha=visible?1:0;group.interactable=group.blocksRaycasts=visible;}}
    public void OpenShop(){navigation.OnClickStoreMainButton();if(shop!=null && shopPages!=null && current<shopPages.Length)shop.SelectPage(shopPages[current]);}
    public void OpenPass()=>pass.Open();
}
