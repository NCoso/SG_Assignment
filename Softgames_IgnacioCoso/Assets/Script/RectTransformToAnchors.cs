using UnityEngine;

public class RectTransformToAnchors : MonoBehaviour
{
    [ContextMenu("Convert postions and size to anchors")]
    public void ConvertToAnchors()
    {
        ResetAnchors();
        RectTransform transform = GetComponent<RectTransform>();
        ConvertPostionAndSizeToAnchors(transform);
    }

    [ContextMenu("Set Default anchors")]
    public void ResetAnchors()
    {
        RectTransform transform = GetComponent<RectTransform>();
        SetDefaultAnchorsAndPivot(transform);
    }

    // keep everything but change pivots and anchors to 0.5
    public void SetDefaultAnchorsAndPivot(RectTransform transform)
    {
        Vector3 originalPosition = transform.position;
        Vector2 originalPivot = transform.pivot;
        Vector2 originalSize = transform.rect.size;

        transform.anchorMax = Vector2.one * 0.5f;
        transform.anchorMin = Vector2.one * 0.5f;
        transform.pivot = Vector2.one * 0.5f;
        transform.position = originalPosition;
        transform.localPosition += new Vector3((0.5f - originalPivot.x) * transform.rect.width, (0.5f - originalPivot.y) * transform.rect.height, 0);

        transform.ForceUpdateRectTransforms();

        transform.sizeDelta = originalSize;

    }

    // Change postion and size to anchors
    public void ConvertPostionAndSizeToAnchors(RectTransform transform)
    {
        Vector3 originalLocalPosition = transform.localPosition;
        RectTransform parent = (transform.parent as RectTransform);

        float diffX = (transform.rect.width / parent.rect.width);
        float anchorMinX = 0.5f + (originalLocalPosition.x / parent.rect.width) - diffX * 0.5f;
        float anchorMaxX = anchorMinX + diffX;

        float diffY = (transform.rect.height / parent.rect.height);
        float anchorMinY = 0.5f + (originalLocalPosition.y / parent.rect.height) - diffY*0.5f;
        float anchorMaxY = anchorMinY + diffY;

        transform.anchorMin = new Vector2(anchorMinX, anchorMinY);
        transform.anchorMax = new Vector2(anchorMaxX, anchorMaxY);

        transform.ForceUpdateRectTransforms();

        transform.offsetMax = new Vector2(0, 0);
        transform.offsetMin = new Vector2(0, 0);

        transform.localPosition = new Vector2(originalLocalPosition.x, originalLocalPosition.y);
    }
}
