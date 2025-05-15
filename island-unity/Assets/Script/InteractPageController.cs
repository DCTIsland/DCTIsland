using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InteractPageController : MonoBehaviour
{
    [SerializeField] GameObject MainPage;
    [SerializeField] GameObject InputPage;
    [SerializeField] GameObject ErrorPage;
    [SerializeField] GameObject BG;

    [SerializeField] TMP_InputField input;

    [SerializeField] VCamManager VCManager;
    [SerializeField] IslandManage islandManage;

    public void ShowMainPage()
    {
        MainPage.SetActive(true);
        InputPage.SetActive(false);
        ErrorPage.SetActive(false);
    }

    public void ShowInputPage()
    {
        MainPage.SetActive(false);
        InputPage.SetActive(true);
        ErrorPage.SetActive(false);
    }

    public void ShowErrorPage()
    {
        MainPage.SetActive(false);
        InputPage.SetActive(false);
        ErrorPage.SetActive(true);
    }

    void CloseAllPage()
    {
        MainPage.SetActive(false);
        InputPage.SetActive(false);
        ErrorPage.SetActive(false);
        BG.SetActive(false);
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
                CloseAllPage();
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
        if(queneN == 0)
        {
            return;
        }

        int cnt = queneN > 10 ? 10 : queneN;
        int rnd = Random.Range(0, cnt);
        GameObject island = islandManage.islandInWorldQueue.ElementAt(rnd);

        CloseAllPage();
        VCManager.ToFocusCamera(island);
    }
}
