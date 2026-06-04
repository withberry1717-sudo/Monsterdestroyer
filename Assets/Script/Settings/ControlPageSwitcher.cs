using UnityEngine;
using UnityEngine.UI;

public class ControlPageSwitcher : MonoBehaviour
{
    [Header("Control Pages")]
    [Tooltip("操作説明ページを順番に入れてください。Page1, Page2...")]
    [SerializeField] private GameObject[] controlPages;

    [Header("Arrow Buttons")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;

    [Header("Settings")]
    [Tooltip("ONなら最後のページの次に1ページ目へ戻ります。")]
    [SerializeField] private bool loopPages = true;

    private int currentPageIndex = 0;

    private void Awake()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.AddListener(ShowPreviousPage);
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.AddListener(ShowNextPage);
        }
    }

    private void OnEnable()
    {
        currentPageIndex = 0;
        UpdatePage();
    }

    private void OnDestroy()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveListener(ShowPreviousPage);
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveListener(ShowNextPage);
        }
    }

    public void ShowNextPage()
    {
        if (controlPages == null || controlPages.Length == 0) return;

        currentPageIndex++;

        if (currentPageIndex >= controlPages.Length)
        {
            currentPageIndex = loopPages ? 0 : controlPages.Length - 1;
        }

        UpdatePage();
    }

    public void ShowPreviousPage()
    {
        if (controlPages == null || controlPages.Length == 0) return;

        currentPageIndex--;

        if (currentPageIndex < 0)
        {
            currentPageIndex = loopPages ? controlPages.Length - 1 : 0;
        }

        UpdatePage();
    }

    private void UpdatePage()
    {
        if (controlPages == null) return;

        for (int i = 0; i < controlPages.Length; i++)
        {
            if (controlPages[i] != null)
            {
                controlPages[i].SetActive(i == currentPageIndex);
            }
        }

        if (!loopPages)
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.interactable = currentPageIndex > 0;
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.interactable = currentPageIndex < controlPages.Length - 1;
            }
        }
    }
}