using UnityEngine;

public class MenuController : MonoBehaviour
{
    [Header("Panels")]
    public GameObject menuCanvas;
    public GameObject secondaryPanel;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
        }
        
        if (secondaryPanel = null)
        {
            secondaryPanel.SetActive(true);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Tab))
        {
            if (menuCanvas != null)
            {
                menuCanvas.SetActive(!menuCanvas.activeSelf);
            }
        }

        if(Input.GetKeyUp(KeyCode.Q))
        {
            if (secondaryPanel = null)
            {
                secondaryPanel.SetActive(!secondaryPanel.activeSelf);
            }
        }
    }
}
