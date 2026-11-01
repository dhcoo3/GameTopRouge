using TMPro;

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using AAAGame.ScriptsHotfix.UI.Module.Dialogue;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

/// <summary>在独立演示运行期间执行会话回归检查，不修改配置文件。</summary>
public static class DialogueVerification
{
    /// <summary>仅用于测试访问组件内部状态。</summary>
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    /// <summary>检查通过项数量。</summary>
    private static int _checks;
    /// <summary>启动可重复的演示回归检查。</summary>
    [MenuItem("Tools/Dialogue/Verify Running Demo")]
    public static void Run() => RunAsync().Forget(exception =>
    {
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "dialogue-verification.txt"), exception.ToString());
        Debug.LogException(exception);
    });
    /// <summary>检验分支、取消、历史、分页与并发入口。</summary>
    /// <returns>测试完成任务。</returns>
    private static async UniTask RunAsync()
    {
        _checks = 0;
        Application.runInBackground = true;
        var demo = UnityEngine.Object.FindObjectOfType<DialogueDemo>();
        Check(demo != null, "demo exists");
        for (int branch = 0; branch < 3; branch++)
        {
            demo.StartButton.onClick.Invoke();
            await UniTask.NextFrame();
            var panel = UnityEngine.Object.FindObjectOfType<DialoguePanel>();
            Check(panel != null, "panel opened");
            Check(panel.PanelUIContainer.Get<TMP_Text>("m_Body").text.Contains(branch == 0 ? "封闭" : "知道入口"), "flag opening");
            var busy = await DialogueView.Instance.PlayAsync(1);
            Check(busy.Status == DialogueStatus.Busy, "busy rejected");
            Complete(panel);
            panel.PanelUIContainer.Get<UGF_Button>("m_ContinueButton").OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            Complete(panel);
            Check(Enumerable.Range(0, 4).Select(i => panel.PanelUIContainer.Get<UGF_Button>("m_OptionButtons" + i)).ToArray().Take(3).All(x => x.gameObject.activeSelf), "three options visible");
            Enumerable.Range(0, 4).Select(i => panel.PanelUIContainer.Get<UGF_Button>("m_OptionButtons" + i)).ToArray()[branch].OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            Check(panel.PanelUIContainer.Get<TMP_Text>("m_Body").text.Contains("你想了解"), "same-frame option rejected");
            await UniTask.NextFrame();
            Enumerable.Range(0, 4).Select(i => panel.PanelUIContainer.Get<UGF_Button>("m_OptionButtons" + i)).ToArray()[branch].OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            panel.PanelUIContainer.Get<UGF_Button>("m_HistoryButton").OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            Check(panel.PanelUIContainer.Get<Transform>("m_HistoryRoot").gameObject.activeSelf && panel.PanelUIContainer.Get<TMP_Text>("m_HistoryText").text.Contains("> "), "history contains choice");
            int visible = panel.PanelUIContainer.Get<TMP_Text>("m_Body").maxVisibleCharacters;
            await UniTask.NextFrame();
            Check(panel.PanelUIContainer.Get<TMP_Text>("m_Body").maxVisibleCharacters == visible, "history pauses typing");
            panel.PanelUIContainer.Get<UGF_Button>("m_HistoryBackButton").OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
            for (int step = 0; step < 5 && DialogueModel.Instance.IsPlaying; step++)
            {
                Complete(panel);
                panel.PanelUIContainer.Get<UGF_Button>("m_ContinueButton").OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
                await UniTask.NextFrame();
            }
            Check(!DialogueModel.Instance.IsPlaying, "branch completed");
            await UniTask.NextFrame();
            Check(UnityEngine.Object.FindObjectsOfType<DialoguePanel>().Length == 0, "panel released");
        }
        demo.ResetButton.onClick.Invoke();
        demo.StartButton.onClick.Invoke();
        await UniTask.NextFrame();
        var pagePanel = UnityEngine.Object.FindObjectOfType<DialoguePanel>();
        Check(pagePanel.PanelUIContainer.Get<TMP_Text>("m_Body").text.Contains("封闭"), "reset restores opening");
        // 构造旁白节点，验证无需在 NPC 表中配置特殊说话者。
        var narrationBytes = new Luban.ByteBuf();
        narrationBytes.WriteInt(500);
        narrationBytes.WriteInt(1);
        narrationBytes.WriteInt((int)cfg.Dialogue.DialogueNodeType.Line);
        narrationBytes.WriteInt(0);
        narrationBytes.WriteString("Dialogue.Guard.Opening");
        narrationBytes.WriteInt(199);
        narrationBytes.WriteString(string.Empty);
        narrationBytes.WriteBool(false);
        narrationBytes.WriteInt(0);
        narrationBytes.WriteInt(0);
        typeof(DialoguePanel).GetMethod("ShowNode", Private).Invoke(pagePanel, new object[] { new cfg.Dialogue.DialogueNode(narrationBytes) });
        Check(!pagePanel.PanelUIContainer.Get<UGF_Image>("m_Portrait").gameObject.activeSelf && !pagePanel.PanelUIContainer.Get<TMP_Text>("m_Speaker").gameObject.activeSelf, "narration hides NPC presentation");
        Check(pagePanel.PanelUIContainer.Get<TMP_Text>("m_Body").rectTransform.offsetMin.x == 40, "narration expands text area");
        pagePanel.PanelUIContainer.Get<TMP_Text>("m_Body").text = string.Concat(Enumerable.Repeat(pagePanel.PanelUIContainer.Get<TMP_Text>("m_Body").text + "\n", 24));
        pagePanel.PanelUIContainer.Get<TMP_Text>("m_Body").maxVisibleCharacters = int.MaxValue;
        pagePanel.PanelUIContainer.Get<TMP_Text>("m_Body").ForceMeshUpdate();
        typeof(DialoguePanel).GetMethod("BeginPage", Private).Invoke(pagePanel, null);
        Check(pagePanel.PanelUIContainer.Get<TMP_Text>("m_Body").textInfo.pageCount > 1, "long text paginated");
        Check(pagePanel.PanelUIContainer.Get<TMP_Text>("m_Body").textInfo.pageInfo[0].lastCharacterIndex < pagePanel.PanelUIContainer.Get<TMP_Text>("m_Body").textInfo.characterCount - 1, "first page bounded");
        Complete(pagePanel);
        pagePanel.PanelUIContainer.Get<UGF_Button>("m_ContinueButton").OnPointerClick(new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current));
        Check(pagePanel.PanelUIContainer.Get<TMP_Text>("m_Body").pageToDisplay == 2, "next page remains in same node");
        pagePanel.gameObject.SetActive(false);
        await UniTask.NextFrame();
        await UniTask.NextFrame();
        Check(!DialogueModel.Instance.IsPlaying, "external disable cancels");
        var open = (Func<UniTask<DialoguePanel>>)Delegate.CreateDelegate(typeof(Func<UniTask<DialoguePanel>>), demo, typeof(DialogueDemo).GetMethod("OpenPanelAsync", Private));
        var load = (Func<UniTask<DialogueData>>)Delegate.CreateDelegate(typeof(Func<UniTask<DialogueData>>), demo, typeof(DialogueDemo).GetMethod("LoadDataAsync", Private));
        var play = typeof(DialogueController).GetMethod("PlayAsync", Private);
        using (var cancellation = new CancellationTokenSource())
        {
            var pending = (UniTask<DialogueResult>)play.Invoke(DialogueController.Instance, new object[] { 1, open, load, cancellation.Token });
            cancellation.Cancel();
            var result = await pending;
            Check(result.Status == DialogueStatus.Cancelled, "token cancels");
        }
        await UniTask.NextFrame();
        Check(!DialogueModel.Instance.IsPlaying, "cancel unlocks entry");
        string report = $"PASS: {_checks} dialogue runtime checks.";
        File.WriteAllText(Path.Combine(Path.GetTempPath(), "dialogue-verification.txt"), report);
        Debug.Log(report);
    }
    /// <summary>补全当前页并执行一次组件更新。</summary>
    /// <param name="panel">当前活动对话表单。</param>
    private static void Complete(DialoguePanel panel)
    {
        int last = (int)typeof(DialoguePanel).GetField("_lastCharacter", Private).GetValue(panel);
        typeof(DialoguePanel).GetField("_visible", Private).SetValue(panel, (float)last);
        panel.PanelUIContainer.Get<TMP_Text>("m_Body").maxVisibleCharacters = last;
        typeof(DialoguePanel).GetMethod("Update", Private).Invoke(panel, null);
    }
    /// <summary>断言业务行为，失败时保留可定位的原因。</summary>
    /// <param name="condition">检查条件。</param>
    /// <param name="message">检查含义。</param>
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("Dialogue verification failed: " + message);
        _checks++;
    }
}
