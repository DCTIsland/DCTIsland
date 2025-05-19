using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class CamFindIsland : MonoBehaviour
{
    [SerializeField] RectTransform popup;
    Camera cam;
    bool isIslandFound;
    GameObject currentIslandAnchor;
    GameObject previousIslandAnchor;

    void Start()
    {
        cam = gameObject.GetComponent<Camera>();
        popup.gameObject.SetActive(false);
    }

    void Update()
    {
        DetectCenterIsland();

        // If the island is not found, hide the popup
        if (!isIslandFound)
        {
            if (previousIslandAnchor != null)
            {
                popup.GetComponent<RawImage>().DOFade(0, 0.15f);
                popup.DOAnchorPosY(popup.anchoredPosition.y - 20, 0.15f).SetEase(Ease.OutBack).OnComplete(() =>
                {
                    popup.gameObject.SetActive(false);
                    previousIslandAnchor = null;
                });
            }
            else
            {
                popup.gameObject.SetActive(false);
                previousIslandAnchor = null;
            }

            return;
        }

        Vector3 screenPos = cam.WorldToScreenPoint(currentIslandAnchor.transform.position + new Vector3(0, 0.6f, 0));
        if (screenPos.z > 0)
        {
            RectTransform canvasRect = popup.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, null, out Vector2 popUpLocalPos);

            // appear animation
            if (previousIslandAnchor == null || previousIslandAnchor != currentIslandAnchor)
            {
                popup.anchoredPosition = new Vector2(popUpLocalPos.x, popUpLocalPos.y - 20);
                popup.DOAnchorPosY(popUpLocalPos.y, 0.15f).SetEase(Ease.OutBack);
                popup.GetComponent<RawImage>().DOFade(1, 0.15f);
            }
            else
            {
                popup.anchoredPosition = popUpLocalPos;
            }

            // update previous anchor
            previousIslandAnchor = currentIslandAnchor;

            // update popup info
            Island island = currentIslandAnchor.GetComponent<Island>();
            popup.GetComponent<PopUp>().SetUpPop(island.thread_id, island.topics);
            popup.gameObject.SetActive(true);
        }
        else
        {
            popup.GetComponent<RawImage>().DOFade(0, 0.15f);
            popup.DOAnchorPosY(popup.anchoredPosition.y - 20, 0.15f).SetEase(Ease.OutBack).OnComplete(() =>
            {
                popup.gameObject.SetActive(false);
            });
        }
    }

    void DetectCenterIsland()
    {
        Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        float angle = 15f;
        Vector3 rotatedDirection = Quaternion.AngleAxis(angle, cam.transform.right) * ray.direction;
        Ray downwardRay = new(ray.origin, rotatedDirection);

        Debug.DrawRay(downwardRay.origin, downwardRay.direction, Color.green);
        LayerMask layer = LayerMask.GetMask("Island");
        if (Physics.Raycast(downwardRay, out RaycastHit hitInfo, 1f, layer))
        {
            isIslandFound = true;
            currentIslandAnchor = hitInfo.collider.gameObject;
        }
        else
        {
            isIslandFound = false;
        }
    }
}