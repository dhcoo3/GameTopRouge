
using GameFramework;
using UnityEngine;

public class UGF_UIHelper:IReference
{
    public static UGF_UIHelper Create(GameObject go)
    {
        UGF_UIHelper helper = ReferencePool.Acquire<UGF_UIHelper>();
        helper.m_GameObject = go;
        return helper;
    }

    private GameObject m_GameObject;
    public GameObject UGameObject
    {
        get
        {            
            return m_GameObject;
        }
    }

    private Transform m_Transform;
    public Transform UTransform
    {
        get
        {             
            return m_Transform;
        }
    }

    private RectTransform m_RectTransform;
    public RectTransform URectTransform
    {
        get
        {
            RectTransform rectTransform = UGameObject.GetOrAddComponent<RectTransform>();
            m_RectTransform = rectTransform;
            return m_RectTransform;
        }
    }

    private Animator m_Animator;
    public Animator UAnimator
    {
        get
        {             
            return m_Animator ??= UGameObject.GetComponent<Animator>();
        }
    }

    public AnimatorHelper UAnimatorHelper;

    public Vector3 GetPosition()
    {
        if (UTransform == null) return Vector3.zero;
        return UTransform.position;
    }

    public void SetPosition(float x, float y, float z)
    {
        if (UTransform == null) return;
        UTransform.position = new Vector3(x, y, z);
    }

    public Vector3 GetLocalPosition()
    {
        if (UTransform == null) return Vector3.zero;
        return UTransform.localPosition;
    }

    public void SetLocalPosition(float x, float y, float z)
    {
        if (UTransform == null) return;
        UTransform.localPosition = new Vector3(x, y, z);
    }

    public Vector3 GetRotation()
    {
        if (UTransform == null) return Vector3.zero;
        return UTransform.eulerAngles;
    }

    public void SetRoation(float x, float y, float z)
    {
        if (UTransform == null) return;
        UTransform.rotation = Quaternion.Euler(new Vector3(x, y, z));
    }

    public void SetActive(bool isActive)
    {
        if (UGameObject == null) return;
        UGameObject.SetActive(isActive);
    }

    public float GetAnchoredPositionX()
    {
        if (URectTransform == null) return 0;
        return URectTransform.anchoredPosition.x;
    }

    public float GetAnchoredPositionY()
    {
        if (URectTransform == null) return 0;
        return URectTransform.anchoredPosition.y;
    }

    public void SetAnchoredPositionX(float val)
    {
        if (URectTransform == null) return;
        URectTransform.anchoredPosition = new Vector2(val,URectTransform.anchoredPosition.y);
    }

    public void SetAnchoredPositionY(float val)
    {
        if (URectTransform == null) return;
        URectTransform.anchoredPosition = new Vector2(URectTransform.anchoredPosition.x,val);
    }

    public void SetAnchoredPositionXY(float xVal, float yVal)
    {
        if (URectTransform == null) return;
        URectTransform.anchoredPosition = new Vector2(xVal, yVal);
    }
    
    public void SetAnchoredPositionXYZ(float xVal, float yVal, float zVal)
    {
        if (URectTransform == null) return;
        URectTransform.anchoredPosition3D = new Vector3(xVal, yVal,zVal);
    }

    public void SetAnchor(float anchorMinX, float anchorMinY, float anchorMaxX, float anchorMaxY)
    {
        if (URectTransform == null) return;
        URectTransform.anchorMin = new Vector2(anchorMinX, anchorMinY);
        URectTransform.anchorMax = new Vector2(anchorMaxX, anchorMaxY);
    }

    public void SetPivot(float x, float y)
    {           
        if (URectTransform == null) return;
        URectTransform.pivot = new Vector2(x, y);
    }

    public void SetOffset(float offsetMaxX, float offsetMaxY, float offsetMinX, float offsetMinY)
    {
        if (URectTransform == null) return;
        URectTransform.offsetMax = new Vector2(offsetMaxX, offsetMaxY);
        URectTransform.offsetMin = new Vector2(offsetMinX, offsetMinY);
    }

    public void SetInsetAndSizeFromParentEdge(int edge, float inset, float size)
    {
        if (URectTransform == null) return;
        URectTransform.SetInsetAndSizeFromParentEdge((RectTransform.Edge)edge, inset, size);
    }

    public void SetHeight(float height)
    {
        if (URectTransform == null) return;
        URectTransform.sizeDelta = new Vector2(URectTransform.rect.width, height);
    }

    public float GetHeight()
    {
        if (URectTransform == null) return 0 ;
        return URectTransform.rect.height;
    }

    public void SetWidth(float width)
    {
        if (URectTransform == null) return ;
        URectTransform.sizeDelta = new Vector2(width, URectTransform.rect.height);
    }

    public float GetWidth()
    {
        if (URectTransform == null) return 0 ;
        return URectTransform.rect.width;
    }

    public void SetSize(float width, float height)
    {
        if (URectTransform == null) return ;
        URectTransform.sizeDelta = new Vector2(width, height);
    }

    public void SetScale(float x, float y, float z)
    {
        if(UTransform == null) return;
        UTransform.localScale = new Vector3(x, y, z);
    }

    public Vector3 GetScale()
    {
        if (UTransform == null) return Vector3.zero;
        return UTransform.localScale;
    }

    public Vector3 GetLocalRoation()
    {
        if (UTransform == null) return Vector3.zero;
        return UTransform.localEulerAngles;
    }

    public void SetLocalRoation(float x, float y, float z)
    {
        if (UTransform == null) return;
        UTransform.localRotation = Quaternion.Euler(new Vector3(x, y, z));
    }

    public GameObject GetChild(int index)
    {
        if (UTransform == null) return null;
        if (UTransform.childCount <= index || index < 0)
        {
            return null;
        }
        return UTransform.GetChild(index).gameObject;
    }

    public GameObject GetParent()
    {
        if (UTransform == null) return null;
        if (UTransform.parent == null)
        {
            return null;
        }
        return UTransform.parent.gameObject;
    }

    public int GetChildCount()
    {
        if (UTransform == null) return 0;
        return UTransform.childCount;
    }

    /// <summary>
    /// 设置父物体
    /// </summary>
    /// <param name="tran"></param>
    public void SetParent(Transform tran)
    {
        if (UTransform == null) return;
        UTransform.SetParent(tran);
    }
    
    public void Clear()
    {
        m_GameObject = null;
    }
}
