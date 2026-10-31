using System;
using System.IO;
using OfficeOpenXml;
using UnityEngine;

public static class MapPlacementWorkbook
{
    public static string Root => Path.GetFullPath(Path.Combine(Application.dataPath, "../../../Config/DataTables/Datas"));
    public static ExcelPackage Open(string name) { ExcelPackage.LicenseContext = LicenseContext.NonCommercial; return new ExcelPackage(new FileInfo(Path.Combine(Root,name))); }
    public static int Column(ExcelWorksheet s,string name) { for(int c=2;c<=s.Dimension.End.Column;c++) if(s.Cells[1,c].Text==name)return c; return 0; }
    public static void Migrate()
    {
        using(var maps=Open("Map/#MapData.xlsx"))
        using(var npcs=Open("Map/#Npc.xlsx"))
        using(var beans=Open("__beans__.xlsx"))
        {
            var m=maps.Workbook.Worksheets[0]; var n=npcs.Workbook.Worksheets[0]; var b=beans.Workbook.Worksheets[0];
            int nc=Column(m,"Npc");
            if(m.Cells[2,nc].Text=="int,list")
            {
                for(int r=5;r<=m.Dimension.End.Row;r++)
                {
                    var entries=new System.Collections.Generic.List<string>();
                    foreach(var id in m.Cells[r,nc].Text.Split(new[]{',',';','|'},StringSplitOptions.RemoveEmptyEntries))
                    {
                        int nr=0; for(int j=5;j<=n.Dimension.End.Row;j++) if(n.Cells[j,Column(n,"id")].Text==id.Trim())nr=j;
                        if(nr==0)throw new Exception("Unknown legacy NPC "+id);
                        var xy=n.Cells[nr,Column(n,"mapPos")].Text.Split(',');
                        float x=float.Parse(xy[0],System.Globalization.CultureInfo.InvariantCulture), y=float.Parse(xy[1],System.Globalization.CultureInfo.InvariantCulture);
                        float ox=float.Parse(m.Cells[r,Column(m,"originX")].Text,System.Globalization.CultureInfo.InvariantCulture), oy=float.Parse(m.Cells[r,Column(m,"originY")].Text,System.Globalization.CultureInfo.InvariantCulture);
                        float sx=float.Parse(m.Cells[r,Column(m,"cellSizeX")].Text,System.Globalization.CultureInfo.InvariantCulture), sy=float.Parse(m.Cells[r,Column(m,"cellSizeY")].Text,System.Globalization.CultureInfo.InvariantCulture);
                        bool iso=m.Cells[r,Column(m,"isometric")].Text=="1";
                        entries.Add(string.Join(",",id.Trim(),(ox+(iso?(x-y)*sx/2:x*sx)).ToString(System.Globalization.CultureInfo.InvariantCulture),(oy+(iso?(x+y)*sy/2:y*sy)).ToString(System.Globalization.CultureInfo.InvariantCulture),"0","1"));
                    }
                    m.Cells[r,nc].Value=string.Join(";",entries);
                }
                m.Cells[2,nc].Value="(list#sep=;),Map.NpcPlacement";
            }
            if(Column(m,"Portal")==0) { int c=m.Dimension.End.Column+1; m.Cells[1,c].Value="Portal"; m.Cells[2,c].Value="(list#sep=;),Map.Portal"; m.Cells[3,c].Value="c"; m.Cells[4,c].Value="地图传送点列表"; }
            AddBean(b,"Map.NpcPlacement",new[]{"npcId","x","y","z","radius"},new[]{"int","float","float","float","float"});
            AddBean(b,"Map.Portal",new[]{"x","y","z","targetMapId","radius"},new[]{"float","float","float","int","float"});
            int col=Column(n,"mapPos"); if(col>0)n.DeleteColumn(col); col=Column(n,"mapId"); if(col>0)n.DeleteColumn(col);
            // Save MapData first so a repeated migration cannot lose legacy coordinates.
            maps.Save(); beans.Save(); npcs.Save();
        }
    }
    public static void MigratePortalRadius()
    {
        using(var maps=Open("Map/#MapData.xlsx"))
        using(var beans=Open("__beans__.xlsx"))
        {
            var b=beans.Workbook.Worksheets[0]; int start=0;
            for(int r=4;r<=b.Dimension.End.Row;r++)if(b.Cells[r,2].Text=="Map.Portal"){start=r;break;}
            if(start==0)throw new Exception("Map.Portal schema missing");
            int end=start+1;while(end<=b.Dimension.End.Row && string.IsNullOrEmpty(b.Cells[end,2].Text))end++;
            bool exists=false;for(int r=start;r<end;r++)if(b.Cells[r,10].Text=="radius")exists=true;
            if(!exists){b.InsertRow(end,1);b.Cells[end,10].Value="radius";b.Cells[end,12].Value="float";}
            var m=maps.Workbook.Worksheets[0];int col=Column(m,"Portal");
            for(int r=5;r<=m.Dimension.End.Row;r++){
                var entries=m.Cells[r,col].Text.Split(new[]{';'},StringSplitOptions.RemoveEmptyEntries);
                for(int j=0;j<entries.Length;j++)if(entries[j].Split(',').Length==4)entries[j]+=",1";
                m.Cells[r,col].Value=string.Join(";",entries);
            }
            maps.Save();beans.Save();
        }
    }
    public static void AddFixedObjectSchema()
    {
        string path=Path.Combine(Root,"Map/#MapObject.xlsx");
        ExcelPackage.LicenseContext=LicenseContext.NonCommercial;
        if(!File.Exists(path))using(var p=new ExcelPackage()){
            var s=p.Workbook.Worksheets.Add("MapObject");
            string[] names={"id","name","res","type"},types={"int","string","string","Map.ObjectType"},comments={"物件ID","显示名称","预制体路径","1任务标识，2场景道具"};
            s.Cells[1,1].Value="##var";s.Cells[2,1].Value="##type";s.Cells[3,1].Value="##group";s.Cells[4,1].Value="##";
            for(int c=0;c<4;c++){s.Cells[1,c+2].Value=names[c];s.Cells[2,c+2].Value=types[c];s.Cells[3,c+2].Value="c,s";s.Cells[4,c+2].Value=comments[c];}
            p.SaveAs(new FileInfo(path));
        }
        using(var p=Open("__tables__.xlsx")){
            var s=p.Workbook.Worksheets[0];bool found=false;for(int r=4;r<=s.Dimension.End.Row;r++)if(s.Cells[r,2].Text=="Map.TbMapObject")found=true;
            if(found){for(int r=s.Dimension.End.Row;r>=4;r--)if(s.Cells[r,2].Text=="Map.TbMapObject")s.DeleteRow(r);p.Save();}
            // # workbooks are registered by the project's existing wildcard table rule.
        }
        using(var p=Open("Map/#MapData.xlsx")){
            var s=p.Workbook.Worksheets[0];if(Column(s,"Objects")==0){int c=s.Dimension.End.Column+1;s.Cells[1,c].Value="Objects";s.Cells[2,c].Value="(list#sep=;),Map.ObjectPlacement";s.Cells[3,c].Value="c,s";s.Cells[4,c].Value="固定物件实例列表";p.Save();}
        }
    }
    static void AddBean(ExcelWorksheet s,string name,string[] fields,string[] types)
    {
        for(int r=4;r<=s.Dimension.End.Row;r++)if(s.Cells[r,2].Text==name)return;
        int row=s.Dimension.End.Row+1;
        for(int i=0;i<fields.Length;i++){if(i==0){s.Cells[row+i,2].Value=name;s.Cells[row+i,5].Value=",";}s.Cells[row+i,10].Value=fields[i];s.Cells[row+i,12].Value=types[i];}
    }
}
