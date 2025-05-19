using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PopUp : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI id;
    [SerializeField] TextMeshProUGUI keywords;

    public void SetUpPop(string idText, string[] topics)
    {
        id.text = "@" + idText;
        keywords.text = $"#{topics[0]}\n#{topics[1]}\n#{topics[2]}";
    }
}
