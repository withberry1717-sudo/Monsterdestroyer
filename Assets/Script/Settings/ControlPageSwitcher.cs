using UnityEngine;
using UnityEngine.UI;

public class ControlPageSwitcher : MonoBehaviour
{
    [Header("Control Pages")]
    [SerializeField] private GameObject[] controlPages;

    [Header("Arrow Buttons")]
    [SerializeField] private Button leftArrowButton;
    [SerializeField] private Button rightArrowButton;

    [Header("Settings")]
    [SerializeField] private bool loopPages = true;

    private int currentPageIndex = 0;

    private void OnEnable()
    {
        RegisterButtons();

        currentPageIndex = 0;
        UpdatePage();
    }

    private void OnDisable()
    {
        UnregisterButtons();
    }

    private void RegisterButtons()
    {
        if (leftArrowButton != null)
        {
            leftArrowButton.onClick.RemoveListener(ShowPreviousPage);
            leftArrowButton.onClick.AddListener(ShowPreviousPage);
        }

        if (rightArrowButton != null)
        {
            rightArrowButton.onClick.RemoveListener(ShowNextPage);
            rightArrowButton.onClick.AddListener(ShowNextPage);
        }
    }

    private void UnregisterButtons()
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
        Debug.Log("Next Page Button Pressed");

        if (controlPages == null || controlPages.Length == 0)
        {
            Debug.LogWarning("Control Pages ‚ª‹ó‚Å‚·");
            return;
        }

        currentPageIndex++;

        if (currentPageIndex >= controlPages.Length)
        {
            currentPageIndex = loopPages ? 0 : controlPages.Length - 1;
        }

        UpdatePage();
    }

    public void ShowPreviousPage()
    {
        Debug.Log("Previous Page Button Pressed");

        if (controlPages == null || controlPages.Length == 0)
        {
            Debug.LogWarning("Control Pages ‚ª‹ó‚Å‚·");
            return;
        }

        currentPageIndex--;

        if (currentPageIndex < 0)
        {
            currentPageIndex = loopPages ? controlPages.Length - 1 : 0;
        }

        UpdatePage();
    }

    private void UpdatePage()
    {
        if (controlPages == null || controlPages.Length == 0) return;

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
        else
        {
            if (leftArrowButton != null)
            {
                leftArrowButton.interactable = true;
            }

            if (rightArrowButton != null)
            {
                rightArrowButton.interactable = true;
            }
        }

        Debug.Log("Control Page Index: " + currentPageIndex);
    }
}