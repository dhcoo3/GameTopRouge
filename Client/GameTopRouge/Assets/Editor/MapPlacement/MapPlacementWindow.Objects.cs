using System;
using System.Collections.Generic;
using System.Linq;
using AAAGame.ScriptsHotfix.GamePlay.Render.Map;
using UnityEditor;
using UnityEngine;
using cfg.Map;

public partial class MapPlacementWindow
{
    sealed class ObjectDefinition { public int id;public string name,res;public ObjectType type; }
    readonly List<ObjectDefinition> objectDefinitions=new List<ObjectDefinition>();
    int objectFilter,objectIndex;
    IEnumerable<MapObjectMarker> FixedMarkers()=>root==null?Enumerable.Empty<MapObjectMarker>():root.GetComponentsInChildren<MapObjectMarker>(true);
    void ReadFixedObjects()
    {
        objectDefinitions.Clear();
        using(var p=MapPlacementWorkbook.Open("Map/#MapObject.xlsx")){
            var s=p.Workbook.Worksheets[0];
            for(int r=5;r<=s.Dimension.End.Row;r++){
                if(!int.TryParse(s.Cells[r,MapPlacementWorkbook.Column(s,"id")].Text,out int id))continue;
                string type=s.Cells[r,MapPlacementWorkbook.Column(s,"type")].Text;
                var kind=int.TryParse(type,out int number)?(ObjectType)number:(ObjectType)Enum.Parse(typeof(ObjectType),type);
                objectDefinitions.Add(new ObjectDefinition{id=id,name=s.Cells[r,MapPlacementWorkbook.Column(s,"name")].Text,res=s.Cells[r,MapPlacementWorkbook.Column(s,"res")].Text,type=kind});
            }
        }
    }
    int NextObjectId()=>checked(FixedMarkers().Select(m=>m.instanceId).DefaultIfEmpty(0).Max()+1);
    string FixedSnapshot()=>"|Objects:"+string.Join(";",FixedMarkers().Select(m=>m.GetInstanceID()+":"+m.instanceId+":"+m.objectId+":"+m.objectiveId+":"+m.transform.position.ToString("R")+":"+m.transform.eulerAngles.ToString("R")+":"+m.transform.localScale.ToString("R")+":"+m.radius.ToString("R",Inv)));
    void DrawFixedObjectControls()
    {
        EditorGUILayout.Space();EditorGUILayout.LabelField("固定物件",EditorStyles.boldLabel);
        int filter=EditorGUILayout.Popup("类型",objectFilter,new[]{"全部","任务标识","场景道具"});
        if(filter!=objectFilter){objectFilter=filter;objectIndex=0;}
        var available=objectDefinitions.Where(d=>objectFilter==0||(int)d.type==objectFilter).ToArray();
        if(available.Length==0){EditorGUILayout.HelpBox("请在 Map/#MapObject.xlsx 配置物件后重新读取。",MessageType.Info);return;}
        objectIndex=EditorGUILayout.Popup("物件",Mathf.Clamp(objectIndex,0,available.Length-1),available.Select(d=>d.id+" "+d.name).ToArray());
        if(GUILayout.Button("添加固定物件")){
            try{AddFixedObject(available[objectIndex],NextObjectId(),Vector3.zero,null,null,1,true);}
            catch(Exception e){EditorUtility.DisplayDialog("添加失败",e.Message,"确定");}
        }
    }
    MapObjectMarker AddFixedObject(ObjectDefinition def,int instance,Vector3 pos,float? rotation,Vector3? scale,float radius,bool undo,int objectiveId=0)
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(def.res);
        if(prefab==null)throw new Exception("物件资源缺失："+def.res);
        var go=(GameObject)PrefabUtility.InstantiatePrefab(prefab,root.transform);
        go.hideFlags=HideFlags.DontSave;go.transform.position=pos;
        go.transform.rotation=Quaternion.Euler(0,0,rotation??prefab.transform.eulerAngles.z);
        go.transform.localScale=scale??prefab.transform.localScale;
        var marker=go.GetComponent<MapObjectMarker>();if(marker==null)marker=go.AddComponent<MapObjectMarker>();
        marker.objectId=def.id;marker.instanceId=instance;marker.objectType=def.type;marker.radius=radius;marker.objectiveId=objectiveId;
        if(undo){Undo.RegisterCreatedObjectUndo(go,"添加固定物件");Selection.activeGameObject=go;}
        return marker;
    }
    void LoadFixedObjects(string data)
    {
        foreach(var entry in (data??"").Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries)){
            var v=entry.Split(',');if(v.Length!=10&&v.Length!=11)throw new Exception("固定物件参数数量错误");
            int id=int.Parse(v[1],Inv);var def=objectDefinitions.FirstOrDefault(d=>d.id==id);
            if(def==null)throw new Exception("找不到固定物件："+id);
            int objectiveId=v.Length==11?int.Parse(v[10],Inv):0;
            AddFixedObject(def,int.Parse(v[0],Inv),Position(v,2),float.Parse(v[5],Inv),Position(v,6),float.Parse(v[9],Inv),false,objectiveId);
        }
    }
    string SerializeFixedObjects()
    {
        var entries=new List<string>();var ids=new HashSet<int>();
        foreach(var m in FixedMarkers()){
            var def=objectDefinitions.FirstOrDefault(d=>d.id==m.objectId);
            if(def==null || ((int)def.type!=1&&(int)def.type!=2)||AssetDatabase.LoadAssetAtPath<GameObject>(def.res)==null)throw new Exception("固定物件配置无效："+m.objectId);
            if(m.instanceId<=0||!ids.Add(m.instanceId))throw new Exception("固定物件实例编号重复或无效："+m.instanceId);
            ValidateObjectiveBinding(m,def);
            var p=m.transform.position;var scale=m.transform.localScale;
            float[] values={p.x,p.y,p.z,m.transform.eulerAngles.z,scale.x,scale.y,scale.z,m.radius};
            if(values.Any(v=>!Finite(v))||scale.x<=0||scale.y<=0||scale.z<=0||m.radius<=0)throw new Exception("固定物件变换或范围无效："+m.instanceId);
            entries.Add(m.instanceId+","+m.objectId+","+string.Join(",",values.Select(v=>v.ToString("R",Inv)))+","+m.objectiveId);
        }
        return string.Join(";",entries);
    }
    public void DuplicateFixedObject(MapObjectMarker source)
    {
        if(root==null||!source.transform.IsChildOf(root.transform))return;
        var def=objectDefinitions.First(d=>d.id==source.objectId);
        AddFixedObject(def,NextObjectId(),source.transform.position,source.transform.eulerAngles.z,source.transform.localScale,source.radius,true,
            def.type==ObjectType.TaskMarker?0:source.objectiveId);
    }

    void ValidateObjectiveBinding(MapObjectMarker marker,ObjectDefinition definition)
    {
        if(definition.type==ObjectType.SceneProp)
        {
            if(marker.objectiveId!=0)throw new Exception("场景道具不能绑定任务目标："+marker.instanceId);
            return;
        }
        if(definition.type!=ObjectType.TaskMarker||marker.objectiveId<=0)throw new Exception("任务标识必须绑定有效目标："+marker.instanceId);
        if(mapsStaticCurrentId<=0)throw new Exception("无法识别当前地图");
        using(var package=MapPlacementWorkbook.Open("Quest/#QuestObjective.xlsx"))
        {
            var sheet=package.Workbook.Worksheets[0];
            int idCol=MapPlacementWorkbook.Column(sheet,"id"),typeCol=MapPlacementWorkbook.Column(sheet,"type");
            int mapCol=MapPlacementWorkbook.Column(sheet,"sourceMapId"),instanceCol=MapPlacementWorkbook.Column(sheet,"sourceInstanceId");
            int row=0;
            for(int r=5;r<=sheet.Dimension.End.Row;r++)if(int.TryParse(sheet.Cells[r,idCol].Text,out int value)&&value==marker.objectiveId){row=r;break;}
            if(row==0)throw new Exception("找不到任务目标："+marker.objectiveId);
            string type=sheet.Cells[row,typeCol].Text;
            bool supported=type=="InspectObject"||type=="CollectItem"||
                (int.TryParse(type,out int typeId)&&(typeId==1||typeId==4));
            if(!supported||sheet.Cells[row,mapCol].Text!=mapsStaticCurrentId.ToString(Inv)||
                sheet.Cells[row,instanceCol].Text!=marker.instanceId.ToString(Inv))
                throw new Exception("任务目标的地图、实例或类型与当前摆放不匹配："+marker.objectiveId);
        }
    }
    int mapsStaticCurrentId=>maps!=null&&index>=0&&index<maps.Count?maps[index].id:0;

    public static void PlaceConfiguredFixedObject(int mapId,int objectId,int instanceId,Vector3 position,int objectiveId,float radius=1.5f)
    {
        var window=GetWindow<MapPlacementWindow>("地图对象编辑");
        window.Read();
        int mapIndex=window.maps.FindIndex(value=>value.id==mapId);
        if(mapIndex<0)throw new Exception("地图不存在："+mapId);
        window.index=mapIndex;
        window.DestroyPreview();window.ready=false;
        window.LoadPreview(false);
        if(!window.ready||window.root==null)throw new Exception("地图摆放预览未能载入");
        ObjectDefinition definition=window.objectDefinitions.FirstOrDefault(value=>value.id==objectId);
        if(definition==null)throw new Exception("固定物件不存在："+objectId);
        MapObjectMarker marker=window.FixedMarkers().FirstOrDefault(value=>value.instanceId==instanceId);
        if(marker!=null)
        {
            if(marker.objectId!=objectId)throw new Exception("摆放实例已绑定到其他物件："+instanceId);
            Undo.RecordObjects(new UnityEngine.Object[]{marker,marker.transform},"绑定任务目标");
            marker.objectiveId=objectiveId;marker.radius=radius;marker.transform.position=position;
        }
        else marker=window.AddFixedObject(definition,instanceId,position,0,null,radius,true,objectiveId);
        window.ValidateObjectiveBinding(marker,definition);
        window.Save();
        using(var package=MapPlacementWorkbook.Open("Map/#MapData.xlsx"))
        {
            var sheet=package.Workbook.Worksheets[0];
            int idColumn=MapPlacementWorkbook.Column(sheet,"id"),objectsColumn=MapPlacementWorkbook.Column(sheet,"Objects");
            for(int row=5;row<=sheet.Dimension.End.Row;row++)
                if(sheet.Cells[row,idColumn].Text==mapId.ToString(Inv)&&
                   sheet.Cells[row,objectsColumn].Text.Split(';').Any(entry=>entry.StartsWith(instanceId+","+objectId+",")&&entry.EndsWith(","+objectiveId)))return;
        }
        throw new Exception("地图对象编辑器未能保存任务标识，请查看保存错误提示");
    }
    public static string VerifyFixedObjects()
    {
        string mapPath=System.IO.Path.Combine(MapPlacementWorkbook.Root,"Map/#MapData.xlsx");
        byte[] original=System.IO.File.ReadAllBytes(mapPath);
        var window=CreateInstance<MapPlacementWindow>();
        try {
            window.root=new GameObject("Fixed object verification"){hideFlags=HideFlags.DontSave};
            window.index=0;window.ready=true;
            var definition=new ObjectDefinition{id=990001,name="Verification",res=MapPlacementMarker.PortalPrefab,type=ObjectType.SceneProp};
            window.objectDefinitions.Add(definition);
            var first=window.AddFixedObject(definition,window.NextObjectId(),new Vector3(1,2,3),35,new Vector3(2,3,4),2.5f,false);
            window.DuplicateFixedObject(first);
            var markers=window.FixedMarkers().ToArray();
            if(markers.Length!=2||markers[0].instanceId==markers[1].instanceId)throw new Exception("Duplicate instance ID");
            string serialized=window.SerializeFixedObjects();
            window.Save();
            using(var p=MapPlacementWorkbook.Open("Map/#MapData.xlsx")){
                var s=p.Workbook.Worksheets[0];int row=0;
                for(int r=5;r<=s.Dimension.End.Row;r++)if(s.Cells[r,2].Text==window.maps[0].id.ToString(Inv))row=r;
                if(s.Cells[row,MapPlacementWorkbook.Column(s,"Objects")].Text!=serialized)throw new Exception("Workbook roundtrip failed");
            }
            window.DestroyPreview();window.root=new GameObject("Fixed object readback"){hideFlags=HideFlags.DontSave};
            window.LoadFixedObjects(serialized);
            if(window.SerializeFixedObjects()!=serialized)throw new Exception("Transform roundtrip failed");
            var test=window.FixedMarkers().First();test.radius=float.NaN;
            bool rejected=false;try{window.SerializeFixedObjects();}catch{rejected=true;}
            if(!rejected)throw new Exception("Invalid radius accepted");test.radius=2.5f;
            Undo.RecordObject(test,"Verify radius Undo");test.radius=7;Undo.FlushUndoRecordObjects();Undo.PerformUndo();
            if(test.radius!=2.5f)throw new Exception("Undo failed");
            return "PASS duplicate IDs, workbook and transform roundtrip, invalid radius, Undo";
        }finally{System.IO.File.WriteAllBytes(mapPath,original);DestroyImmediate(window);}
    }
    void DrawSelectedFixedObject()
    {
        var marker=Selection.activeGameObject==null?null:Selection.activeGameObject.GetComponent<MapObjectMarker>();
        if(marker==null||root==null||!marker.transform.IsChildOf(root.transform))return;
        MapObjectMarkerEditor.DrawProperties(marker);
        if(GUILayout.Button("复制固定物件"))DuplicateFixedObject(marker);
        if(GUILayout.Button("删除固定物件"))Undo.DestroyObjectImmediate(marker.gameObject);
    }
}

[CustomEditor(typeof(MapObjectMarker))]
public sealed class MapObjectMarkerEditor : Editor
{
    public static void DrawProperties(MapObjectMarker marker)
    {
        EditorGUILayout.LabelField("实例编号",marker.instanceId.ToString());
        EditorGUILayout.LabelField("物件 ID",marker.objectId.ToString());
        EditorGUILayout.LabelField("类型",(int)marker.objectType==1?"任务标识":"场景道具");
        EditorGUI.BeginChangeCheck();
        var position=EditorGUILayout.Vector3Field("世界位置",marker.transform.position);
        float rotation=EditorGUILayout.FloatField("Z 轴朝向",marker.transform.eulerAngles.z);
        var scale=EditorGUILayout.Vector3Field("缩放",marker.transform.localScale);
        float radius=EditorGUILayout.FloatField("触发半径",marker.radius);
        int objectiveId=marker.objectiveId;
        if(marker.objectType==ObjectType.TaskMarker)objectiveId=EditorGUILayout.IntField("绑定目标 ID",objectiveId);
        if(EditorGUI.EndChangeCheck()){
            Undo.RecordObjects(new UnityEngine.Object[]{marker,marker.transform},"修改固定物件");
            marker.transform.position=position;marker.transform.rotation=Quaternion.Euler(0,0,rotation);marker.transform.localScale=scale;marker.radius=radius;marker.objectiveId=objectiveId;
            EditorUtility.SetDirty(marker);SceneView.RepaintAll();
        }
    }
    public override void OnInspectorGUI()
    {
        var marker=(MapObjectMarker)target;DrawProperties(marker);
        if(GUILayout.Button("复制固定物件")){
            var window=Resources.FindObjectsOfTypeAll<MapPlacementWindow>().FirstOrDefault();
            if(window!=null)window.DuplicateFixedObject(marker);
        }
        if(GUILayout.Button("删除固定物件"))Undo.DestroyObjectImmediate(marker.gameObject);
    }
    void OnSceneGUI()
    {
        var m=(MapObjectMarker)target;
        EditorGUI.BeginChangeCheck();float radius=Handles.RadiusHandle(Quaternion.identity,m.transform.position,m.radius);
        if(EditorGUI.EndChangeCheck()){Undo.RecordObject(m,"调整固定物件范围");m.radius=Mathf.Max(0.01f,radius);}
    }
}
