using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractPageController : MonoBehaviour
{
    [SerializeField] GameObject BG;
    [SerializeField] GameObject MainPage;
    [SerializeField] GameObject InputPage;
    [SerializeField] GameObject ErrorPage;
    [SerializeField] GameObject WalkPage;
    [SerializeField] GameObject ThanksPage;

    [SerializeField] TMP_InputField input;

    [SerializeField] VCamManager VCManager;
    [SerializeField] IslandManage islandManage;

    public void ShowMainPage()
    {
        CloseAllPage();
        BG.SetActive(true);
        MainPage.SetActive(true);
    }

    public void ShowInputPage()
    {
        CloseAllPage();
        BG.SetActive(true);
        InputPage.SetActive(true);
    }

    public void ShowErrorPage()
    {
        CloseAllPage();
        BG.SetActive(true);
        ErrorPage.SetActive(true);
    }

    public void ShowWalkPage()
    {
        CloseAllPage();
        WalkPage.SetActive(true);
    }

    public void ShowThanksPage()
    {
        CloseAllPage();
        BG.SetActive(true);
        ThanksPage.SetActive(true);
    }

    void CloseAllPage()
    {
        BG.SetActive(false);
        MainPage.SetActive(false);
        InputPage.SetActive(false);
        ErrorPage.SetActive(false);
        WalkPage.SetActive(false);
        ThanksPage.SetActive(false);
    }

    public void Search()
    {
        string text = input.text;

        // check if the input is empty
        if (!Regex.IsMatch(text, @"^[a-zA-Z0-9._]+$"))
            return;

        //search match island
        int searchSuccess = 0;
        foreach (GameObject island in islandManage.islandInWorldQueue)
        {
            Island islandScript = island.GetComponent<Island>();
            if (islandScript.thread_id == text)
            {
                searchSuccess = 1;
                ShowWalkPage();
                VCManager.ToFocusCamera(island);
                break;
            }
        }

        // if not match
        if (searchSuccess == 0)
        {
            ShowErrorPage();
        }
    }

    public void RndToOtherIsland()
    {
        int queneN = islandManage.islandInWorldQueue.Count;
        GameObject island;

        if (queneN <= 0)
        {
            island = null;
        }
        else
        {
            int cnt = queneN > 10 ? 10 : queneN;
            int rnd = Random.Range(0, cnt);
            island = islandManage.islandInWorldQueue.ElementAt(rnd);
        }

        ShowWalkPage();
        VCManager.ToFocusCamera(island);
    }
}
