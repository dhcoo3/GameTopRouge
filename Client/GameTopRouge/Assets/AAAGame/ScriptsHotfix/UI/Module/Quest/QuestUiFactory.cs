using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AAAGame.ScriptsHotfix.UI.Module.Quest
{
    /// <summary>统一创建任务模块运行时使用的 uGUI 控件。</summary>
    internal static class QuestUiFactory
    {
        /// <summary>创建并按锚点定位矩形变换。</summary>
        /// <param name="name">层级节点名称。</param>
        /// <param name="parent">新节点的父级。</param>
        /// <param name="size">矩形宽高。</param>
        /// <param name="anchor">矩形锚点。</param>
        /// <param name="pivot">矩形轴心。</param>
        /// <param name="position">相对锚点的位置。</param>
        /// <returns>新节点的矩形变换。</returns>
        public static RectTransform Rect(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 position)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        /// <summary>创建带指定颜色的 uGUI 图像。</summary>
        /// <param name="name">层级节点名称。</param>
        /// <param name="parent">新节点的父级。</param>
        /// <param name="size">图像宽高。</param>
        /// <param name="anchor">矩形锚点。</param>
        /// <param name="pivot">矩形轴心。</param>
        /// <param name="position">相对锚点的位置。</param>
        /// <param name="color">图像颜色。</param>
        /// <returns>新建的图像组件。</returns>
        public static Image Image(string name, Transform parent, Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 position, Color color)
        {
            var rect = Rect(name, parent, size, anchor, pivot, position);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>创建任务界面使用的可换行 TextMeshPro 文本。</summary>
        /// <param name="name">层级节点名称。</param>
        /// <param name="parent">新节点的父级。</param>
        /// <param name="value">初始显示文本。</param>
        /// <param name="fontSize">文字字号。</param>
        /// <param name="color">文字颜色。</param>
        /// <param name="size">文本区域宽高。</param>
        /// <param name="anchor">矩形锚点。</param>
        /// <param name="pivot">矩形轴心。</param>
        /// <param name="position">相对锚点的位置。</param>
        /// <param name="alignment">文字对齐方式。</param>
        /// <returns>新建的文本组件。</returns>
        public static TextMeshProUGUI Text(string name, Transform parent, string value, float fontSize, Color color,
            Vector2 size, Vector2 anchor, Vector2 pivot, Vector2 position, TextAlignmentOptions alignment = TextAlignmentOptions.TopLeft)
        {
            var rect = Rect(name, parent, size, anchor, pivot, position);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = TMP_Settings.defaultFontAsset;
            text.fontSize = fontSize;
            text.color = color;
            text.text = value;
            text.alignment = alignment;
            text.enableWordWrapping = true;
            text.richText = false;
            return text;
        }

        /// <summary>创建具有统一配色和文本子节点的按钮。</summary>
        /// <param name="name">层级节点名称。</param>
        /// <param name="parent">新节点的父级。</param>
        /// <param name="label">按钮文字。</param>
        /// <param name="fontSize">按钮文字字号。</param>
        /// <param name="size">按钮宽高。</param>
        /// <param name="anchor">矩形锚点。</param>
        /// <param name="pivot">矩形轴心。</param>
        /// <param name="position">相对锚点的位置。</param>
        /// <param name="action">按钮点击操作；允许为空。</param>
        /// <returns>新建的按钮组件。</returns>
        public static Button Button(string name, Transform parent, string label, float fontSize, Vector2 size,
            Vector2 anchor, Vector2 pivot, Vector2 position, System.Action action)
        {
            var image = Image(name, parent, size, anchor, pivot, position, new Color32(43, 37, 29, 245));
            var button = image.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color32(72, 59, 42, 255);
            colors.highlightedColor = new Color32(137, 106, 57, 255);
            colors.selectedColor = new Color32(137, 106, 57, 255);
            colors.pressedColor = new Color32(166, 129, 63, 255);
            button.colors = colors;
            if (action != null) button.onClick.AddListener(() => action());
            Text(name + "Label", image.transform, label, fontSize, new Color32(232, 219, 190, 255), size - new Vector2(20, 12),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, TextAlignmentOptions.Center);
            return button;
        }

        /// <summary>按本地化键读取文案，并在缺失时回显键以便定位配置问题。</summary>
        /// <param name="key">本地化表中的稳定文案键。</param>
        /// <returns>本地化文案或缺失键。</returns>
        public static string Localize(string key)
        {
            return GF.Localization.GetText(key);
        }

        /// <summary>销毁指定容器的所有子节点。</summary>
        /// <param name="parent">需要清空的节点容器。</param>
        public static void ClearChildren(Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
                Object.Destroy(parent.GetChild(i).gameObject);
        }
    }
}
