using System;
using System.Collections.Generic;
using System.Globalization;
using OfficeOpenXml;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Passive Scene overlay from the selected MapData row, independent of play mode.</summary>
public sealed class MapPlacementGridPreview
{
    public int width,height;
    public float originX,originY,cellX,cellY;
    public bool isometric;
    public readonly HashSet<Vector2Int> blocked=new HashSet<Vector2Int>();
    readonly Vector3[] vertices=new Vector3[4];
    public static MapPlacementGridPreview Read(ExcelWorksheet sheet,int row)
    {
        Func<string,string> value=name=>sheet.Cells[row,MapPlacementWorkbook.Column(sheet,name)].Text;
        Func<string,float> number=name=>float.Parse(value(name),CultureInfo.InvariantCulture);
        var grid=new MapPlacementGridPreview{width=int.Parse(value("width")),height=int.Parse(value("height")),originX=number("originX"),originY=number("originY"),cellX=number("cellSizeX"),cellY=number("cellSizeY"),isometric=value("isometric")=="1"||value("isometric").Equals("true",StringComparison.OrdinalIgnoreCase)};
        foreach(var entry in value("blockedCells").Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries)){
            var xy=entry.Split(',');if(xy.Length!=2)throw new FormatException("Invalid blocked cell: "+entry);
            grid.blocked.Add(new Vector2Int(int.Parse(xy[0]),int.Parse(xy[1])));
        }
        return grid;
    }
    public Vector3[] CellVertices(int x,int y)
    {
        float cx=originX+(isometric?(x-y)*cellX/2:x*cellX);
        float cy=originY+(isometric?(x+y)*cellY/2:y*cellY);
        float hx=cellX/2,hy=cellY/2;
        if(isometric){vertices[0]=new Vector3(cx,cy+hy,0);vertices[1]=new Vector3(cx+hx,cy,0);vertices[2]=new Vector3(cx,cy-hy,0);vertices[3]=new Vector3(cx-hx,cy,0);}
        else{vertices[0]=new Vector3(cx-hx,cy-hy,0);vertices[1]=new Vector3(cx+hx,cy-hy,0);vertices[2]=new Vector3(cx+hx,cy+hy,0);vertices[3]=new Vector3(cx-hx,cy+hy,0);}
        return vertices;
    }
    public void Draw()
    {
        if(Event.current.type!=EventType.Repaint)return;
        var color=Handles.color;var ztest=Handles.zTest;
        try {
            Handles.zTest=CompareFunction.Always;
            for(int x=0;x<width;x++)for(int y=0;y<height;y++){
                bool collision=blocked.Contains(new Vector2Int(x,y));
                Handles.DrawSolidRectangleWithOutline(CellVertices(x,y),collision?new Color(1,0,0,0.28f):new Color(0,1,0,0.22f),collision?new Color(1,0,0,0.9f):new Color(0,1,0,0.9f));
            }
        }finally{Handles.color=color;Handles.zTest=ztest;}
    }
}
