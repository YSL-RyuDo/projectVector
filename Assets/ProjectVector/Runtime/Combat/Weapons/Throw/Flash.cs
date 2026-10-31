using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Flash : MonoBehaviour
{
    public Image flashImage;

    public void flash()
    {
        StartCoroutine(flashRoutine());
    }

    IEnumerator flashRoutine()
    {
        Color c = flashImage.color;

        c.a = 1f;
        flashImage.color = c;

        yield return new WaitForSeconds(0.2f);

        while (c.a > 0)
        {
            c.a -= Time.deltaTime;
            flashImage.color = c;

            yield return null;
        }
    }
}
