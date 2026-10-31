
using System.Text;
using Codice.Utils.Buffers;
using UnityEngine;


public class UIContainerAutoCreate
{
    private string tmp = "public class <Name> : UGF_Panel" +
                         "{      " +
                         "  #region Auto Create" +
                         "  #endregion" +
                         "  protected override void OnInit(object userData)" +
                         "  {    " +
                         "    #region Auto Bind" +
                         "    #endregion" +                   
                         "    #region Auto Event" +
                         "    #endregion" +                   
                         "    base.OnInit(userData);" +
                         "  }" +
                         "  #region Auto Write Event" +
                         "  #endregion" +
                         "}";
    
    
    public static void AutoCreate(GameObject obj)
    {
        
    }
    
    public static string GetDefineText(UIContainer uiContainer,bool isReplace = false)
    {
        StringBuilder builder = new StringBuilder();
        if (isReplace)
        {
            builder.Append("#region Auto Create\n");
        }
       
        foreach (var data in uiContainer.UIContainerDict)
        {
            string str = string.Format("    {0} {1} {2};\n", data.Value.Domain, data.Value.NodeType, data.Value.NodeName);
            builder.Append(str);
        }
        builder.Append("    #endregion");
        return builder.ToString();
    }

    public static string GetBindText(UIContainer uiContainer, string mgrName, bool isReplace = false)
    {
        StringBuilder builder = new StringBuilder();
        if (isReplace)
        {
            builder.Append("#region Auto Bind\n");
        }
        else
        {
            
        }
   
        foreach (var data in uiContainer.UIContainerDict)
        {
            string str = string.Format("    {0} = {1}.Get<{2}>(\"{3}\");\n",data.Value.NodeName,mgrName,data.Value.NodeType,data.Value.NodeName);
            builder.Append(str);
        }
        builder.Append("    #endregion");
        return builder.ToString();
    }
}