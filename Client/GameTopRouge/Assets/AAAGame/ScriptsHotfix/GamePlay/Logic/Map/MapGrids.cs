//地图的每个单元格的信息
public struct GridInfo{
    ///<summary>
    ///位于resources/prefabs文件夹下的的位置
    ///比如是resources/prefabs/terrain/grass，这个值就是"terrain/grass"
    ///</summary>
    public string prefabPath;

    ///<summary>
    ///地面移动是否可以通过
    ///</summary>
    public bool groundCanPass;

    ///<summary>
    ///飞行是否可以通过
    ///</summary>
    public bool flyCanPass;

    public GridInfo(string prefabPath, bool characterCanPass = true, bool bulletCanPass = true){
        this.prefabPath = prefabPath;
        this.groundCanPass = characterCanPass;
        this.flyCanPass = bulletCanPass;
    }

    ///<summary>
    ///场景外的格子，无意义的单元格
    ///</summary>
    public static GridInfo VoidGrid{get;} = new GridInfo("", false, false);
}

