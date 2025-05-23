using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using TMPro;
using DG.Tweening;
using UnityEngine.UI;

public class InteractPageController : MonoBehaviour
{
    [SerializeField] GameObject BG;
    [SerializeField] GameObject MainPage;
    [SerializeField] GameObject InputPage;
    [SerializeField] GameObject ErrorPage;
    [SerializeField] GameObject AutoTipsPage;
    [SerializeField] GameObject WalkPage;
    [SerializeField] GameObject ThanksPage;

    [SerializeField] TMP_InputField input;
    [SerializeField] Image[] tipsImages;

    [SerializeField] VCamManager VCManager;
    [SerializeField] FocusCamController focusCamController;
    [SerializeField] IslandManage islandManage;

    [SerializeField] GameObject defaultIsland;
    GameObject island;

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

    public void ShowAutoTipsPage(System.Action callback = null)
    {
        CloseAllPage();
        BG.SetActive(true);
        AutoTipsPage.SetActive(true);

        Sequence tipsAnim = DOTween.Sequence();
        tipsAnim.Append(tipsImages[0].DOFade(1, 1f));
        tipsAnim.Append(tipsImages[1].DOFade(1, 1f));
        tipsAnim.Append(tipsImages[2].DOFade(1, 1f));
        tipsAnim.AppendInterval(0.8f);
        tipsAnim.OnComplete(() =>
        {
            tipsImages[0].color = new Color(1, 1, 1, 0);
            tipsImages[1].color = new Color(1, 1, 1, 0);
            tipsImages[2].color = new Color(1, 1, 1, 0);
            ShowWalkPage();
            callback?.Invoke();
        });
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
        VCManager.ToWholeCamera();
    }

    void CloseAllPage()
    {
        BG.SetActive(false);
        MainPage.SetActive(false);
        InputPage.SetActive(false);
        ErrorPage.SetActive(false);
        AutoTipsPage.SetActive(false);
        WalkPage.SetActive(false);
        ThanksPage.SetActive(false);
    }

    public void OnInputEndEdit()
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            Search();
        }
    }

    public void Search()
    {
        string text = input.text.ToLower();

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
                this.island = island;
                ShowAutoTipsPage(() => VCManager.ToFocusCamera(island));
                break;
            }
        }

        // if not match
        if (searchSuccess == 0)
        {
            ShowErrorPage();
        }

        input.text = "";
    }

    public void RndToOtherIsland()
    {
        int queneN = islandManage.islandInWorldQueue.Count;

        if (queneN <= 0)
        {
            island = defaultIsland;
        }
        else
        {
            int cnt = queneN > 10 ? 10 : queneN;
            int rnd = Random.Range(0, cnt);
            island = islandManage.islandInWorldQueue.ElementAt(rnd);
        }

        ShowAutoTipsPage(() => VCManager.ToFocusCamera(island));
    }

    public void ResetPosition()
    {
        focusCamController.StartReturnPos(island);
    }
}
