using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using GameFramework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class UGF_Text : Text, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    private int _InstanceID = 0;

    private int m_InstanceId
    {
        get
        {
            if (_InstanceID == 0)
            {
                _InstanceID = GetInstanceID();
            }
            
            return _InstanceID;
        }
    }
    
    /// <summary>
    /// 原材质球
    /// </summary>
    private Material m_CacheOriginalMaterial;
    
    /// <summary>
    /// 缓存材质名
    /// </summary>
    private StringBuilder m_CacheMaterialAssetName;

    /// <summary>
    /// 同步主字体
    /// </summary>
    [HideInInspector]
    public bool SynFont;

    /// <summary>
    /// 自动本地化文字
    /// </summary>
    [HideInInspector]
    public bool UseLocalization;

    /// <summary>
    /// 本地化Key
    /// </summary>
    [HideInInspector]
    public string LocalizationKey = "";

    /// <summary>
    /// 使用图文混排功能
    /// </summary>
    [HideInInspector]
    public bool UseLinkImage;

    /// <summary>
    /// 使用文字超链接功能
    /// </summary>
    [HideInInspector]
    public bool UseLinkText;
    
    /// <summary>
    /// 单行居中，多行左对齐
    /// </summary>
    [HideInInspector]
    public bool UseAutoLayout;
    
    private WaitForEndOfFrame m_WaitForEndOfFrame;
    
    UGF_UIHelper _mUgfUIHelper;

    public UGF_UIHelper UIHelper
    {
        get
        {
            if (_mUgfUIHelper == null)
            {
                _mUgfUIHelper = UGF_UIHelper.Create(this.gameObject);
            }
            return _mUgfUIHelper;
        }
    }
    
    protected override void Awake()
    {
        if (Application.isPlaying && UseLocalization && !string.IsNullOrEmpty(LocalizationKey))
        {
            SetLocalizationText(LocalizationKey);
        }      
        
        #if UNITY_EDITOR
            CheckLocalization();
        #endif
    }

    protected override void OnDestroy()
    {
        //m_CallBackListener.Release();
        ReleaseBindAsset();
        base.OnDestroy();
    }

    /// <summary>
    /// 设置文字
    /// </summary>
    /// <param name="newVal"></param>
    public void SetText(string newVal)
    {
        if (UseLinkImage || UseLinkText)
        {
            if (string.IsNullOrEmpty(newVal))
            {
                if (string.IsNullOrEmpty(text))
                {
                    return;
                }

                originText = String.Empty;
                _mTextDirty = true;
                SetVerticesDirty();
                text = _outputText;
            }
            else
            {
                if (originText == newVal)
                {
                    return;
                }

                originText = newVal;
                _mTextDirty = true;
                SetVerticesDirty();
                text = _outputText;
            }
        }
        else
        {
            text = newVal;
        }

        if (UseAutoLayout)
        {
            // 获取TextGenerator
            TextGenerator generator = new TextGenerator();
            TextGenerationSettings settings = GetGenerationSettings(new Vector2(rectTransform.rect.width, 1000));
            // 计算文本布局
            generator.Populate(newVal, settings);
            if (generator.lineCount == 1)
            {
                alignment = TextAnchor.UpperCenter;
            }
            else
            {
                alignment = TextAnchor.UpperLeft;
            }
        }
    }

    /// <summary>
    /// 通过本地化Key,加载本地化文字
    /// </summary>
    /// <param name="key">多语言ID</param>
    public void SetLocalizationText(string key)
    {
        if (GF.Localization == null)
        {
           return;
        }   
        
        string str = GF.Localization.GetString(key);
        if (string.IsNullOrEmpty(str))
        {
            return;
        }
        
        //转表时，会将@用于标记为转行符
       // str = str.Replace("@", "\n");
        SetText(str);
    }


    /// <summary>
    /// 获取文字
    /// </summary>
    /// <returns></returns>
    public string GetText()
    {
        return text;
    }

    /// <summary>
    /// 设置文字大小
    /// </summary>
    /// <param name="size"></param>
    public void SetTextSize(int size)
    {
        fontSize = size;
    }

    public void SetAlignment(int type)
    {
        alignment = (TextAnchor)type;
    }

    /// <summary>
    /// 设置字体
    /// </summary>
    /// <param name="fontName"></param>
    public void SetFont(string fontName)
    {
        GF.AssetBridge.LoadFont(fontName, font =>
        {
            this.font = font;
        });
    }

    /// <summary>
    /// 跳动变化数字
    /// </summary>
    /// <param name="startNum">开始值</param>
    /// <param name="endNum">结束值</param>
    /// <param name="duration">持续时间</param>
    /// <param name="decimalPlace">小数位传0，变化时，不再计算小数位</param>
    /// <param name="saveIntCount">整数位</param>
    /// <param name="endCall">回调</param>
    /// <param name="updateCall">回调</param>
    public void JumpToNumber(float startNum, float endNum, float duration, int decimalPlace = 0, int saveIntCount = 0, GameFrameworkAction endCall = null, GameFrameworkAction<float> updateCall = null)
    {
        m_WaitForEndOfFrame ??= new WaitForEndOfFrame();
        StopAllCoroutines();
        StartCoroutine(SmoothValue(startNum, endNum, duration, decimalPlace, saveIntCount, endCall, updateCall));
    }

    private IEnumerator SmoothValue(float startNum, float endNum, float duration, int decimalPlace = 0, int saveIntCount = 0, GameFrameworkAction endCall = null, GameFrameworkAction<float> updateCall = null)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            double val = Mathf.Lerp(startNum, endNum, time / duration);

            //double temp = Math.Round(val, decimalPlace);
            string tempStr = Math.Round(val, decimalPlace).ToString(CultureInfo.InvariantCulture);

            if (saveIntCount != 0)
            {
                if (tempStr.Length < saveIntCount)
                {
                    int addLen = saveIntCount - tempStr.Length;

                    for (int i = 1; i <= addLen; i++)
                    {
                        tempStr = string.Format("{0}{1}", "0", tempStr);
                    }
                }
            }

            text = tempStr;
           // updateCall?.Call(tempStr);
            yield return new WaitForEndOfFrame();
        }

        text = Math.Round(endNum, decimalPlace).ToString(CultureInfo.InvariantCulture);
       // endCall?.Call();
    }


    public void SetTextAlignMent(int alignment)
    {
        this.alignment = (TextAnchor)alignment;
    }    
    
    /// <summary>
    /// 设置描边颜色
    /// </summary>
    /// <param name="color"></param>
    public void SetColorOutline(Color color)
    {
        Shadow[] outline = GetComponents<Shadow>();
        if (outline ==  null)
        {
            return;
        }
        foreach (var item in outline)
        {
            item.effectColor = color;
        }
    }
    
    /// <summary>
    /// 设置描边颜色
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="a"></param>
    public void SetColorOutline(float r, float g, float b, float a)
    {
        SetColorOutline(new Color(r, g, b, a));
    }
    
    /// <summary>
    /// 设置描边颜色string
    /// </summary>
    /// <param name="colorStr"></param>
    public void SetColorOutline(string colorStr)
    {
        ColorUtility.TryParseHtmlString(colorStr, out var nowColor);
        SetColorOutline(nowColor);
    }

    /// <summary>
    /// 设置文字颜色
    /// </summary>
    /// <param name="color"></param>
    public void SetColor(Color color)
    {
        this.color = color;
    }


      /// <summary>
      /// 设置文字颜色
      /// </summary>
      /// <param name="r"></param>
      /// <param name="g"></param>
      /// <param name="b"></param>
      /// <param name="a"></param>
    public void SetColor(float r, float g, float b, float a)
    {
        color = new Color(r, g, b, a);
    }

    /// <summary>
    /// 设置文字颜色
    /// </summary>
    /// <param name="colorStr"></param>
    public Color SetColor(string colorStr)
    {
        ColorUtility.TryParseHtmlString(colorStr, out var nowColor);
        color = nowColor;
        return nowColor;
    }

    /// <summary>
    /// 获取颜色的代码
    /// </summary>
    /// <returns></returns>
    public string GetColorString()
    {
        return GetColorString(color);
    }

    /// <summary>
    /// 获取颜色的代码
    /// </summary>
    /// <returns></returns>
    public string GetColorString(Color color)
    {
        return ColorUtility.ToHtmlStringRGB(color);
    }

    /// <summary>
    /// 获取字符串在文本的坐标
    /// </summary>
    public Vector3 GetPosAtText(string strFragment)
    {
        string newStr = GetInputText(text);
        if (newStr != null)
        {
            int strFragmentIndex = newStr.IndexOf(strFragment, StringComparison.Ordinal);//-1表示不包含strFragment
            Vector3 stringPos;
            Canvas canvas = GetComponentInParent<Canvas>();
            if (strFragmentIndex > -1)
            {
                Vector3 firstPos = GetPosAtText(canvas, this, strFragmentIndex + 1);
                Vector3 lastPos = GetPosAtText(canvas, this, strFragmentIndex + strFragment.Length);
                stringPos = (firstPos + lastPos) * 0.5f;
            }
            else
            {
                stringPos = GetPosAtText(canvas, this, strFragmentIndex);
            }
            return stringPos;
        }
        
        return Vector3.zero;
    }

    /// <summary>
    /// 剔除文本颜色字号等
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    private string GetInputText(string str)
    {
        char[] chars = new char[2];
        chars[0] = '<';
        chars[1] = '>';
        string[] newStrs = str.Split(chars);
        StringBuilder stringBuilder = new StringBuilder();
        foreach (var t in newStrs)
        {
            if (t != "")
            {
                char[] newChars = t.ToCharArray();
                //size
                if (newChars[0] == '/')
                {
                    if (newChars[1] == 's' && newChars.Length >= 5 && t.Substring(0, 5) == "/size")
                    {
                        continue;
                    }
                    if (newChars[1] == 'c' && newChars.Length >= 6 && t.Substring(0, 6) == "/color")
                    {
                        continue;
                    }
                }
                if (newChars[0] == 'c')
                {
                    if (newChars.Length > 6 && t.Substring(0, 6) == "color=")
                    {
                        continue;
                    }
                }
                if (newChars[0] == 's')
                {
                    if (newChars.Length > 5 && t.Substring(0, 5) == "size=")
                    {
                        continue;
                    }
                }
                stringBuilder.Append(t);
            }
        }
        return stringBuilder.ToString();
    }

    /// <summary>
    /// 得到Text中字符的位置；canvas:所在的Canvas，text:需要定位的Text,charIndex:Text中的字符位置
    /// </summary>
    private Vector3 GetPosAtText(Canvas canvas, Text text, int charIndex)
    {
        string textStr = text.text;
        Vector3 charPos = Vector3.zero;
        if (charIndex <= textStr.Length && charIndex > 0)
        {
            TextGenerator textGen = new TextGenerator(textStr.Length);
            Vector2 extents = text.gameObject.GetComponent<RectTransform>().rect.size;
            textGen.Populate(textStr, text.GetGenerationSettings(extents));

            int newLine = textStr.Substring(0, charIndex).Split('\n').Length - 1;              
            int indexOfTextQuad = (charIndex * 4) + (newLine * 4) - 4;
            if (indexOfTextQuad < textGen.vertexCount)
            {
                charPos = (textGen.verts[indexOfTextQuad].position +
                    textGen.verts[indexOfTextQuad + 1].position +
                    textGen.verts[indexOfTextQuad + 2].position +
                    textGen.verts[indexOfTextQuad + 3].position) / 4f;


            }
        }
        charPos /= canvas.scaleFactor;//适应不同分辨率的屏幕
        charPos = text.transform.TransformPoint(charPos);//转换为世界坐标
        return charPos;
    }

    /// <summary>
    /// 设置材质
    /// </summary>
    /// <param name="materialsAsset"></param>
    /// <param name="callBack"></param>
    /// <param name="isLocalized"></param>
    public void SetMaterial(string materialsAsset, GameFrameworkAction callback = null)
    {
        if (m_CacheOriginalMaterial == null && material != null)
        {
            m_CacheOriginalMaterial = material;
        }

        m_CacheMaterialAssetName ??= new StringBuilder(0);
        
        if (string.IsNullOrEmpty(materialsAsset))
        {
            material = null;
            m_CacheMaterialAssetName.Clear();
            return;
        }
        
        m_CacheMaterialAssetName.Clear();
        m_CacheMaterialAssetName.Append(materialsAsset);

        if (GF.AssetBridge != null)
            GF.AssetBridge.LoadNewMaterial(GetInstanceID(),this.gameObject,assetPath: materialsAsset,
                callBack: assetCache =>
            {
                if (!m_CacheMaterialAssetName.Equals(assetCache.Name))
                {
                    return;
                }

                material = assetCache.Asset;
                callback?.Invoke();
            });
    }

    /// <summary>
    /// 还原初始材质
    /// </summary>
    public void RestoreMaterial()
    {
        m_CacheOriginalMaterial ??= material;
        m_CacheMaterialAssetName?.Clear();
        material = m_CacheOriginalMaterial;
    }

    /// <summary>
    /// 复制文本到剪切板
    /// </summary>
    public void CopyString()
    {
        //Utils.CopyToClipboard(text);
    }
    
    /// <summary>
    /// 检测本地化文本是否正确使用
    /// </summary>
    public void CheckLocalization()
    {
        if(string.IsNullOrEmpty(text)) return;
        bool isChinese = ContainsChinese(text);
        if (isChinese)
        {
            if(name.Contains("m_")) return;
            if (!UseLocalization)
            {
                Debug.LogWarning("文本使用中文，但没有使用代码赋值也没有使用自动本地化功能,本地化文字会失败，请点击这里调整:"+this.name,this.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 是否包含中文
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    public bool ContainsChinese(string input)
    {
        return Regex.IsMatch(input, @"[\u4e00-\u9fa5]");
    }
    
    /// <summary>
    /// 统一设置列表对象点击回调，也可单独设置
    /// </summary>
    /// <param name="luaFunction">回调</param>
    public void SetLuaClickListener(GameFrameworkAction callBack)
    {
        //m_CallBackListener.AddLua(CallBackType.TextClickCall,m_InstanceId,luaFunction);
    }
    
    /// <summary>
    /// 释放该组件加载的资源对象
    /// </summary>
    private void ReleaseBindAsset()
    {
        if (GF.AssetBridge != null)
        {
            if(m_CacheMaterialAssetName != null && m_CacheMaterialAssetName.Length != 0)
            {
                GF.AssetBridge.RemoveNewMaterial(GetInstanceID(), m_CacheMaterialAssetName.ToString());
            }
        }
    }

    #region LinkImageText   
    //文字超链接、文字图文混排 
    [HideInInspector]
    [TextArea(3, 10), SerializeField] protected string originText;

    [HideInInspector]
    public string customLinkColor = "blue";

    public void SetLinkColor(string c)
    {
        customLinkColor = c;
        _mTextDirty = false;
    }

    /// <summary>
    /// 解析完最终的文本
    /// </summary>
    private string _outputText;

    /// <summary>
    /// 对应的顶点输出的文本
    /// </summary>
    private string _vertexText;

    /// <summary>
    /// 是否需要解析
    /// </summary>
    private bool _mTextDirty = true;

    /// <summary>
    /// 图片池
    /// </summary>
    private List<UGF_Image> _mImagesPool;

    /// <summary>
    /// 图片的最后一个顶点的索引
    /// </summary>
    private List<int> _mImagesVertexIndex ;

    /// <summary>
    /// 超链接信息列表
    /// </summary>
    private List<LinkInfo> _mLinkInfos;

    /// <summary>
    /// 正则取出所需要的属性
    /// </summary>
    private static readonly Regex ImageRegex =
        new Regex(@"<quad name=(.+?) width=(\d*\.?\d+%?) height=(\d*\.?\d+%?) />", RegexOptions.Singleline);

    /// <summary>
    /// 超链接正则
    /// </summary>
    private static readonly Regex LinkRegex =
        new Regex(@"<a link=([^>\n\s]+)>(.*?)(</a>)", RegexOptions.Singleline);


    public override void SetVerticesDirty()
    {
        if(gameObject == null) return;
        base.SetVerticesDirty();
        _mTextDirty = true;
        UpdateQuadImage();
    }

    private void UpdateQuadImage()
    {
        if (string.IsNullOrEmpty(originText))
        {
            _outputText = "";
            return;
        }
        
        _mImagesVertexIndex ??= new List<int>();
        _mImagesPool ??= new List<UGF_Image>();
        _mLinkInfos ??= new List<LinkInfo>();

        if (_mTextDirty)
        {
            _outputText = GetOutputText(originText);
        }

        _mImagesVertexIndex.Clear();
        int startSearchIndex = 0;
        var matches = ImageRegex.Matches(originText);
        for (var i = 0; i < matches.Count; i++)
        {
            Match match = matches[i];
            int index = _vertexText.IndexOf('&', startSearchIndex);

            var firstIndex = index * 4;
            startSearchIndex = index + 1;

            _mImagesVertexIndex.Add(firstIndex);

            _mImagesPool.RemoveAll(image => image == null);
            if (_mImagesPool.Count == 0)
            {
                GetComponentsInChildren(_mImagesPool);
            }

            if (_mImagesVertexIndex.Count > _mImagesPool.Count)
            {
                var go = new GameObject();
                go.layer = gameObject.layer;
                var rt = go.transform;
                if (rt)
                {
                    rt.SetParent(rectTransform, false);
                    rt.localPosition = Vector3.zero;
                    rt.localRotation = Quaternion.identity;
                    rt.localScale = Vector3.one;
                }

                _mImagesPool.Add(go.GetOrAddComponent<UGF_Image>());
            }

            var spriteName = match.Groups[1].Value;
            var img = _mImagesPool[i];
            if (img.sprite == null || img.sprite.name != spriteName)
            {
                // AtlasAssetPath|AssetName|AssetPath
                string[] str = spriteName.Split('|');

                if (string.IsNullOrEmpty(str[0]))
                {
                    img.SetImageCs(str[2]);
                }
                else
                {
                    img.SetImageCs(str[0],str[1]);
                }
            }

            var imgRectTransform = img.GetComponent<RectTransform>();
            if (Int32.TryParse(match.Groups[2].Value, out int width) && Int32.TryParse(match.Groups[3].Value, out int height))
            {
                imgRectTransform.sizeDelta = new Vector2(width, height);
            }
            else
            {
                Debug.LogWarning("无法正常解析大小");
                imgRectTransform.sizeDelta = new Vector2(16f, 16f);
            }

            img.enabled = true;
        }

        for (var i = _mImagesVertexIndex.Count; i < _mImagesPool.Count; i++)
        {
            if (_mImagesPool[i])
            {
                _mImagesPool[i].enabled = false;
            }
        }
    }

    protected override void OnPopulateMesh(VertexHelper toFill)
    {
        m_DisableFontTextureRebuiltCallback = true;
        base.OnPopulateMesh(toFill);
      
        if (_mImagesVertexIndex !=null && UseLinkImage)
        {
            UIVertex vert = UIVertex.simpleVert;
            
            for (var i = 0; i < _mImagesVertexIndex.Count; i++)
            {
                var index = _mImagesVertexIndex[i];
                var rt = _mImagesPool[i].rectTransform;
                var size = rt.sizeDelta;
                if (index < toFill.currentVertCount)
                {
                    toFill.PopulateUIVertex(ref vert, index);
                    Vector3 pos1 = vert.position;
                    
                    toFill.PopulateUIVertex(ref vert, index + 2); //28  -18
                    Vector3 pos2 = vert.position;
                    
                    rt.anchoredPosition = new Vector2(vert.position.x + size.x / 2, (pos1 + pos2).y*0.5f);
                
                    toFill.PopulateUIVertex(ref vert, index);
                    for (int j = index + 3, m = index; j > m; j--)
                    {
                        toFill.SetUIVertex(vert, j);
                    }
                }
            }
        }

        if (_mLinkInfos!=null && UseLinkText)
        {
            UIVertex vert = UIVertex.simpleVert;
            
            // 处理超链接包围框
            foreach (var info in _mLinkInfos)
            {
                info.Boxes.Clear();
                if (info.StartIndex >= toFill.currentVertCount)
                {
                    continue;
                }

                // 将超链接里面的文本顶点索引坐标加入到包围框
                toFill.PopulateUIVertex(ref vert, info.StartIndex);
                var pos = vert.position;
                var bounds = new Bounds(pos, Vector3.zero);
                for (int i = info.StartIndex, m = info.EndIndex; i < m; i++)
                {
                    if (i >= toFill.currentVertCount)
                    {
                        break;
                    }

                    toFill.PopulateUIVertex(ref vert, i);
                    pos = vert.position;
                    if (pos.x < bounds.min.x) // 换行重新添加包围框
                    {
                        info.Boxes.Add(new Rect(bounds.min, bounds.size));
                        bounds = new Bounds(pos, Vector3.zero);
                    }
                    else
                    {
                        bounds.Encapsulate(pos); // 扩展包围框
                    }
                }

                info.Boxes.Add(new Rect(bounds.min, bounds.size));
            }
            
            DrawUnderLine(toFill);
        }

        m_DisableFontTextureRebuiltCallback = false;
    }


    /*public void SetOnLinkClick(LuaFunction luaFunction)
    {
        m_CallBackListener.AddLua(CallBackType.TextLinkClickCall,m_InstanceId,luaFunction);
    }
    
    public void SetOffLinkClick()
    {
        m_CallBackListener.RemoveLua(CallBackType.TextLinkClickCall);
    }
    
    public void SetOnLinkDown(LuaFunction luaFunction)
    {
        m_CallBackListener.AddLua(CallBackType.TextLinkDownCall,m_InstanceId,luaFunction);
    }*/

    /// <summary>
    /// 获取超链接解析后的最后输出文本 
    /// </summary>
    /// <returns></returns>
    protected virtual string GetOutputText(string outputText)
    {
        _mLinkInfos.Clear();
        if (string.IsNullOrEmpty(outputText))
            return "";
        
        string tempOutputText = outputText;
        _vertexText = outputText;
        _vertexText = Regex.Replace(_vertexText, "<color.*?>", "");
        _vertexText = Regex.Replace(_vertexText, "</color>", "");
        _vertexText = ImageRegex.Replace(_vertexText, "&");
        _vertexText = _vertexText.Replace("\n", "");
        _vertexText = Regex.Replace(_vertexText, @"(?<!a)\s(?!link)", "");
        foreach (Match match in LinkRegex.Matches(_vertexText))
        {
            var group = match.Groups[1];
            _vertexText = _vertexText.Replace(match.Value, match.Groups[2].Value);
            int startNum = _vertexText.IndexOf(match.Groups[2].Value, match.Index, StringComparison.Ordinal);
            if (startNum < 0)
            {
                Debug.LogError("超链接顶点解析错误");
            }

            var info = new LinkInfo
            {
                StartIndex = startNum * 4, // 超链接里的文本起始顶点索引

                EndIndex = startNum * 4 + (match.Groups[2].Length - 1) * 4 + 3,
                Name = group.Value
            };
            _mLinkInfos.Add(info);
        }


        foreach (Match match in LinkRegex.Matches(outputText))
        {
            tempOutputText = tempOutputText.Replace(match.Value,
                $"<color={customLinkColor}>" + match.Groups[2].Value + "</color>");
        }


        _mTextDirty = false;

        return tempOutputText;
    }

    /// <summary>
    /// 点击事件检测是否点击到超链接文本
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerClick(PointerEventData eventData)
    {
        //m_CallBackListener.CallLua(CallBackType.TextClickCall,m_InstanceId,this);
        
        if (_mLinkInfos == null)
        {
            return;
        }
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out var lp);

        foreach (var info in _mLinkInfos)
        {
            var boxes = info.Boxes;
            foreach (var t in boxes)
            {
                if (!t.Contains(lp)) continue;
                //m_CallBackListener.CallLua(CallBackType.TextLinkClickCall,m_InstanceId,info.Name);
                return;
            }
        }
    }

    /// <summary>
    /// 点击事件检测是否点击到超链接文本
    /// </summary>
    /// <param name="eventData"></param>
    public void OnPointerDown(PointerEventData eventData)
    {
        if (_mLinkInfos == null)
        {
            return;
        }
        
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, eventData.position, eventData.pressEventCamera, out var lp);

        foreach (var info in _mLinkInfos)
        {
            var boxes = info.Boxes;
            foreach (var t in boxes)
            {
                if (!t.Contains(lp)) continue;
               // m_CallBackListener.CallLua(CallBackType.TextLinkDownCall,m_InstanceId,info.Name);
                return;
            }
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {

    }

    /// <summary>
    /// 超链接信息类
    /// </summary>
    private class LinkInfo
    {
        public int StartIndex;

        public int EndIndex;

        public string Name;

        public readonly List<Rect> Boxes = new List<Rect>();
    }
    
    readonly UIVertex[] _tempVerts = new UIVertex[4];
    private Vector2 underlineOffset = new Vector2(0, -4);
    
    void DrawUnderLine(VertexHelper toFill)
    {
        Vector2 extents = rectTransform.rect.size;
        var settings = GetGenerationSettings(extents);
        cachedTextGenerator.Populate("_", settings);
        IList<UIVertex> uList = cachedTextGenerator.verts;
        float h = uList[2].position.y - uList[1].position.y;
        Vector3[] temVecs = new Vector3[4];

        for (int i = 0; i < _mLinkInfos.Count; i++)
        {
            var info = _mLinkInfos[i];
          
            for (int j = 0; j < info.Boxes.Count; j++)
            {
                if (info.Boxes[j].width <= 0 || info.Boxes[j].height <= 0)
                    continue;
                temVecs[0] = info.Boxes[j].min + underlineOffset;
                temVecs[1] = temVecs[0] + new Vector3(info.Boxes[j].width, 0);
                temVecs[2] = temVecs[0] + new Vector3(info.Boxes[j].width, -h);
                temVecs[3] = temVecs[0] + new Vector3(0, -h);

                for (int k = 0; k < 4; k++)
                {
                    _tempVerts[k] = uList[k];
                    _tempVerts[k].color = Color.blue;
                    _tempVerts[k].position = temVecs[k];
                }

                toFill.AddUIVertexQuad(_tempVerts);
            }
        }

    }
    #endregion
    
}

