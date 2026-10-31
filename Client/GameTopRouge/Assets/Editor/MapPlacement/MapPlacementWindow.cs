using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using AAAGame.ScriptsHotfix.GamePlay.Render.Map;
using OfficeOpenXml;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public partial class MapPlacementWindow : EditorWindow
{
    class Map { public int id; public string path,title,npcs,portals,objects; public MapPlacementGridPreview grid; }
    readonly List<Map> maps=new List<Map>();
    readonly List<KeyValuePair<int,string>> npcs=new List<KeyValuePair<int,string>>();
    readonly Dictionary<int,string> npcNames=new Dictionary<int,string>();
    int index,npcIndex;
    GameObject root;
    string baseline="";
    bool ready;
    bool showScenePanel = true;
    bool showGrid;
    Vector2 windowScroll,sceneScroll;
    Rect scenePanel = new Rect(12, 48, 330, 390);
    static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
    [MenuItem("Tools/地图/NPC 与传送点编辑器")]
    static void Open()
    {
        var window=GetWindow<MapPlacementWindow>("地图对象编辑");
        window.Show();
        window.showScenePanel=true;
        SceneView.RepaintAll();
    }
    void OnEnable(){SceneView.duringSceneGui+=DrawScenePanel; Undo.undoRedoPerformed+=Repaint; EditorApplication.playModeStateChanged+=PlayChanged; Read();}
    void PlayChanged(PlayModeStateChange state){if(state==PlayModeStateChange.ExitingEditMode){DestroyPreview();ready=false;}}
    void OnDisable(){SceneView.duringSceneGui-=DrawScenePanel; Undo.undoRedoPerformed-=Repaint;EditorApplication.playModeStateChanged-=PlayChanged;DestroyPreview();}
    void DestroyPreview(){if(root!=null)DestroyImmediate(root);root=null;}
    void Read()
    {
        try {
            maps.Clear();npcs.Clear();npcNames.Clear();
            using(var p=MapPlacementWorkbook.Open("Map/#MapData.xlsx")) { var s=p.Workbook.Worksheets[0]; for(int r=5;r<=s.Dimension.End.Row;r++){if(!int.TryParse(s.Cells[r,2].Text,out int id))continue;maps.Add(new Map{id=id,path=s.Cells[r,MapPlacementWorkbook.Column(s,"name")].Text.Replace('\\','/'),title=s.Cells[r,MapPlacementWorkbook.Column(s,"title")].Text,npcs=s.Cells[r,MapPlacementWorkbook.Column(s,"Npc")].Text,portals=s.Cells[r,MapPlacementWorkbook.Column(s,"Portal")].Text,objects=s.Cells[r,MapPlacementWorkbook.Column(s,"Objects")].Text,grid=MapPlacementGridPreview.Read(s,r)});} }
            using(var p=MapPlacementWorkbook.Open("Map/#Npc.xlsx")){var s=p.Workbook.Worksheets[0];for(int r=5;r<=s.Dimension.End.Row;r++)if(int.TryParse(s.Cells[r,MapPlacementWorkbook.Column(s,"id")].Text,out int id)){npcs.Add(new KeyValuePair<int,string>(id,s.Cells[r,MapPlacementWorkbook.Column(s,"res")].Text));npcNames[id]=s.Cells[r,MapPlacementWorkbook.Column(s,"name")].Text;}}
            ReadFixedObjects();
            ResolveNpcNames();
            index=Mathf.Clamp(index,0,Math.Max(0,maps.Count-1));
        } catch(Exception e){Debug.LogException(e);ShowNotification(new GUIContent(e.Message));}
    }
    /// <summary>从同一份本地化表解析 NPC 名称，保持地图编辑器显示可读中文。</summary>
    void ResolveNpcNames()
    {
        var names = new Dictionary<string, string>();
        using (var package = MapPlacementWorkbook.Open("#Localization.xlsx"))
        {
            var sheet = package.Workbook.Worksheets[0];
            int keyColumn = MapPlacementWorkbook.Column(sheet, "id");
            int textColumn = MapPlacementWorkbook.Column(sheet, "ChineseSimplified");
            for (int row = 5; row <= sheet.Dimension.End.Row; row++)
                names[sheet.Cells[row, keyColumn].Text] = sheet.Cells[row, textColumn].Text;
        }
        foreach (int id in npcNames.Keys.ToArray())
            if (names.TryGetValue(npcNames[id], out string text)) npcNames[id] = text;
    }
    IEnumerable<MapPlacementMarker> Markers()=>root==null?Enumerable.Empty<MapPlacementMarker>():root.GetComponentsInChildren<MapPlacementMarker>(true);
    string Snapshot()=>string.Join(";",Markers().Select(m=>m.GetInstanceID()+":"+m.npcId+":"+m.transform.position.ToString("R")+":"+m.radius.ToString("R",Inv)+":"+m.targetMap))+FixedSnapshot();
    bool Leave()=>!ready || Snapshot()==baseline || EditorUtility.DisplayDialog("未保存配置","放弃当前尚未保存的地图对象修改？","放弃","取消");
    void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        showScenePanel=EditorGUILayout.ToggleLeft("在 Scene 视图显示操作面板",showScenePanel);
        if(EditorGUI.EndChangeCheck())SceneView.RepaintAll();
        windowScroll=EditorGUILayout.BeginScrollView(windowScroll);
        DrawControls();
        DrawSelectedFixedObject();
        EditorGUILayout.EndScrollView();
    }
    void DrawScenePanel(SceneView view)
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode)return;
        if(ready && root!=null && showGrid)maps[index].grid.Draw();
        if(!showScenePanel)return;
        scenePanel.x=Mathf.Clamp(scenePanel.x,0,Mathf.Max(0,view.position.width-scenePanel.width));
        scenePanel.y=Mathf.Clamp(scenePanel.y,20,Mathf.Max(20,view.position.height-scenePanel.height));
        Handles.BeginGUI();
        scenePanel=GUILayout.Window(GetInstanceID(),scenePanel,id=>
        {
            float oldWidth=EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth=90;
            sceneScroll=EditorGUILayout.BeginScrollView(sceneScroll,GUILayout.Height(Mathf.Clamp(view.position.height-145,220,620)));
            try
            {
                DrawControls();
                DrawSelectedFixedObject();
                var selected=Selection.activeGameObject==null?null:Selection.activeGameObject.GetComponent<MapPlacementMarker>();
                if(selected!=null && root!=null && selected.transform.IsChildOf(root.transform))
                {
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField(selected.portal?"选中的传送点":"选中的 NPC "+selected.npcId,EditorStyles.boldLabel);
                    EditorGUI.BeginChangeCheck();
                    string target=selected.targetMap;
                    float radius=selected.radius;
                    if(selected.portal)target=EditorGUILayout.TextField("目标地图",target);
                    radius=EditorGUILayout.FloatField("触发半径",radius);
                    if(EditorGUI.EndChangeCheck())
                    {
                        Undo.RecordObject(selected,"修改地图对象属性");
                        selected.targetMap=target;selected.radius=radius;
                        EditorUtility.SetDirty(selected);view.Repaint();
                    }
                    if(GUILayout.Button("删除选中对象"))Undo.DestroyObjectImmediate(selected.gameObject);
                }
            }
            finally{EditorGUILayout.EndScrollView();EditorGUIUtility.labelWidth=oldWidth;}
            GUI.DragWindow(new Rect(0,0,330,20));
        },"地图 NPC / 传送点",GUILayout.Width(330),GUILayout.Height(0));
        Handles.EndGUI();
        // Consume only mouse events inside the panel; leave Scene selection and handles usable outside.
        var e=Event.current;
        if(scenePanel.Contains(e.mousePosition) && e.type==EventType.Layout)
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
    }
    void ClosePanelAndScene()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode || !Leave())return;
        var scene=maps.Count>0 && index>=0 && index<maps.Count
            ? UnityEngine.SceneManagement.SceneManager.GetSceneByPath(maps[index].path)
            : default(UnityEngine.SceneManagement.Scene);
        if(scene.IsValid() && scene.isLoaded)
        {
            if(!EditorSceneManager.SaveModifiedScenesIfUserWantsTo(new[]{scene}))return;
            // Keep one empty scene open when closing the final loaded scene.
            if(UnityEngine.SceneManagement.SceneManager.sceneCount==1)
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            if(!EditorSceneManager.CloseScene(scene,true))return;
        }
        ready=false;
        DestroyPreview();
        showScenePanel=false;
        SceneView.RepaintAll();
        Close();
    }
    void DrawControls()
    {
        if(EditorApplication.isPlayingOrWillChangePlaymode){EditorGUILayout.HelpBox("请在非运行状态编辑地图。",MessageType.Info);return;}
        if(GUILayout.Button("关闭面板并关闭场景"))
        {
            // Finish the current IMGUI layout before destroying its window and scene.
            EditorApplication.delayCall+=()=>{if(this!=null)ClosePanelAndScene();};
        }
        if(maps.Count==0){if(GUILayout.Button("读取配置"))Read();return;}
        int next=EditorGUILayout.Popup("地图",index,maps.Select(m=>m.id+" "+m.title).ToArray());
        if(next!=index && Leave()){DestroyPreview();ready=false;index=next;}
        if(GUILayout.Button("打开场景并读取摆放") && Leave())LoadPreview(true);
        EditorGUI.BeginChangeCheck();
        showGrid=EditorGUILayout.ToggleLeft("显示网格区域（绿：行走 / 红：碰撞）",showGrid);
        if(EditorGUI.EndChangeCheck()){SceneView.RepaintAll();Repaint();}
        if(!ready)return;
        DrawFixedObjectControls();
        if(npcs.Count>0){npcIndex=EditorGUILayout.Popup("NPC",Mathf.Clamp(npcIndex,0,npcs.Count-1),npcs.Select(n=>n.Key+" "+(npcNames.TryGetValue(n.Key,out var name)&&!string.IsNullOrWhiteSpace(name)?name:Path.GetFileNameWithoutExtension(n.Value))).ToArray());if(GUILayout.Button("添加 NPC"))Add(npcs[npcIndex].Value,npcs[npcIndex].Key,false,Vector3.zero,1,"");}
        if(GUILayout.Button("添加传送点"))Add(MapPlacementMarker.PortalPrefab,0,true,Vector3.zero,1,"");
        EditorGUILayout.HelpBox("在 Scene 中移动对象；选中对象在 Inspector 编辑半径或目标地图。预览不会保存到场景。",MessageType.Info);
        if(GUILayout.Button("重新读取配置") && Leave())LoadPreview(false);
        if(GUILayout.Button("保存当前地图配置"))Save();
        if(GUILayout.Button("导出 Luban 配置（客户端与服务端）"))Export();
    }
    void LoadPreview(bool open)
    {
        try {
            if(open){if(!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())return;if(AssetDatabase.LoadAssetAtPath<SceneAsset>(maps[index].path)==null)throw new Exception("找不到地图场景："+maps[index].path);EditorSceneManager.OpenScene(maps[index].path);}
            int id=maps[index].id;Read();index=maps.FindIndex(m=>m.id==id);
            DestroyPreview();root=new GameObject("地图对象预览（不保存）");root.hideFlags=HideFlags.DontSave;
            var map=maps[index];
            foreach(var entry in map.npcs.Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries)){var v=entry.Split(',');int npc=int.Parse(v[0],Inv);var path=npcs.FirstOrDefault(n=>n.Key==npc).Value;if(path==null)throw new Exception("NPC 不存在："+npc);Add(path,npc,false,Position(v,1),float.Parse(v[4],Inv),"",false);}
            foreach(var entry in map.portals.Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries)){var v=entry.Split(',');Add(MapPlacementMarker.PortalPrefab,0,true,Position(v,0),v.Length>4?float.Parse(v[4],Inv):1,v[3],false);}
            LoadFixedObjects(map.objects);
            ready=true;baseline=Snapshot();
        }catch(Exception e){DestroyPreview();ready=false;EditorUtility.DisplayDialog("读取失败",e.Message,"确定");}
    }
    static Vector3 Position(string[] v,int offset)=>new Vector3(float.Parse(v[offset],Inv),float.Parse(v[offset+1],Inv),float.Parse(v[offset+2],Inv));
    void Add(string path,int id,bool portal,Vector3 pos,float radius,string target,bool undo=true)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if(prefab==null){throw new Exception("资源缺失："+path);}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);go.hideFlags=HideFlags.DontSave;go.transform.position=pos;
        var m=go.AddComponent<MapPlacementMarker>();m.npcId=id;m.portal=portal;m.radius=radius;m.targetMap=target;
        if(undo){Undo.RegisterCreatedObjectUndo(go,"添加地图对象");Selection.activeGameObject=go;}
    }
    int Resolve(string input)
    {
        input=(input??"").Trim();
        var found=maps.Where(m=>int.TryParse(input,out int id)?m.id==id:m.title==input||m.path==input.Replace('\\','/')||Path.GetFileNameWithoutExtension(m.path)==input).ToArray();
        if(found.Length!=1)throw new Exception("目标地图不存在或名称重复："+input);return found[0].id;
    }
    void Save()
    {
        try {
            var ns=new List<string>();var ps=new List<string>();
            foreach(var m in Markers()){
                var p=m.transform.position;if(!Finite(p.x)||!Finite(p.y)||!Finite(p.z))throw new Exception("坐标必须为有限数值");
                if(!Finite(m.radius)||m.radius<=0)throw new Exception("触发半径必须为有限正数");
                string xyz=string.Join(",",p.x.ToString("R",Inv),p.y.ToString("R",Inv),p.z.ToString("R",Inv));
                if(m.portal){if(AssetDatabase.LoadAssetAtPath<GameObject>(MapPlacementMarker.PortalPrefab)==null)throw new Exception("传送点资源缺失");ps.Add(xyz+","+Resolve(m.targetMap)+","+m.radius.ToString("R",Inv));}
                else {var npc=npcs.FirstOrDefault(n=>n.Key==m.npcId);if(npc.Value==null||AssetDatabase.LoadAssetAtPath<GameObject>(npc.Value)==null||!Finite(m.radius)||m.radius<=0)throw new Exception("NPC 或半径无效："+m.npcId);ns.Add(m.npcId+","+xyz+","+m.radius.ToString("R",Inv));}
            }
            string fixedData=SerializeFixedObjects();
            string file=Path.Combine(MapPlacementWorkbook.Root,"Map/#MapData.xlsx");
            using(var locked=new FileStream(file,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            using(var p=new ExcelPackage(locked)){
                var s=p.Workbook.Worksheets[0];int row=0;for(int r=5;r<=s.Dimension.End.Row;r++)if(s.Cells[r,2].Text==maps[index].id.ToString(Inv))row=r;
                if(row==0)throw new Exception("地图记录已被删除");
                s.Cells[row,MapPlacementWorkbook.Column(s,"Npc")].Value=string.Join(";",ns);s.Cells[row,MapPlacementWorkbook.Column(s,"Portal")].Value=string.Join(";",ps);s.Cells[row,MapPlacementWorkbook.Column(s,"Objects")].Value=fixedData;var bytes=p.GetAsByteArray();locked.Position=0;locked.SetLength(0);locked.Write(bytes,0,bytes.Length);locked.Flush();
            }
            baseline=Snapshot();ShowNotification(new GUIContent("配置已保存；请导出 Luban 后运行"));
        }catch(Exception e){EditorUtility.DisplayDialog("保存失败",e.Message,"确定");}
    }
    static bool Finite(float v)=>!float.IsNaN(v)&&!float.IsInfinity(v);
    static void Export()
    {
        try {string config=Path.GetFullPath(Path.Combine(MapPlacementWorkbook.Root,"../.."));var start=new System.Diagnostics.ProcessStartInfo{FileName=Application.platform==RuntimePlatform.WindowsEditor?"cmd.exe":"/bin/bash",Arguments=Application.platform==RuntimePlatform.WindowsEditor?"/c gen.bat":"gen.sh",WorkingDirectory=config,UseShellExecute=false,RedirectStandardOutput=true,RedirectStandardError=true};using(var process=System.Diagnostics.Process.Start(start)){var output=process.StandardOutput.ReadToEndAsync();var error=process.StandardError.ReadToEndAsync();process.WaitForExit();if(process.ExitCode!=0)throw new Exception(output.Result+error.Result);Debug.Log(output.Result);}AssetDatabase.Refresh();}catch(Exception e){EditorUtility.DisplayDialog("导出失败",e.Message,"确定");}
    }
}

[CustomEditor(typeof(MapPlacementMarker))]
public class MapPlacementMarkerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();var m=(MapPlacementMarker)target;
        EditorGUILayout.LabelField(m.portal?"传送点":"NPC "+m.npcId);
        if(m.portal)EditorGUILayout.PropertyField(serializedObject.FindProperty("targetMap"),new GUIContent("目标地图 ID / 标题 / 场景名"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("radius"),new GUIContent("触发半径"));serializedObject.ApplyModifiedProperties();
        if(GUILayout.Button("删除对象"))Undo.DestroyObjectImmediate(m.gameObject);
    }
    void OnSceneGUI()
    {
        var m=(MapPlacementMarker)target;
        EditorGUI.BeginChangeCheck();float radius=Handles.RadiusHandle(Quaternion.identity,m.transform.position,m.radius);
        if(EditorGUI.EndChangeCheck()){Undo.RecordObject(m,"调整触发范围");m.radius=Mathf.Max(0.01f,radius);}
    }
}
