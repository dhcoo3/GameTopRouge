#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace UGF.EditorTools
{
    public static partial class DataTableUpdater
    {
        static IList<string> tableFileChangedList;
        static bool isInitialized = false;
        static AppConfigs appConfigs = null;
        [InitializeOnLoadMethod]
        private static void Init()
        {
            if (isInitialized) return;
            InitGlobalCulture();
            tableFileChangedList = new List<string>();
            EditorApplication.update -= OnUpdate;
            EditorApplication.update += OnUpdate;
            var tbWatcher = new FileSystemWatcher(ConstEditor.DataTableExcelPath, "*.xlsx")
            {
                IncludeSubdirectories = true,

                NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };
            var fileChangedCb = new FileSystemEventHandler(OnDataTableChanged);
            tbWatcher.Changed -= fileChangedCb;
            tbWatcher.Changed += fileChangedCb;
            tbWatcher.Deleted -= fileChangedCb;
            tbWatcher.Deleted += fileChangedCb;
            appConfigs = AppConfigs.GetInstanceEditor();
            isInitialized = true;
        }
        static void InitGlobalCulture()
        {
            CultureInfo.CurrentCulture = CultureInfo.CreateSpecificCulture("en-GB");
        }

        private static void OnUpdate()
        {
            if (!isInitialized) return;

            if (tableFileChangedList.Count > 0)
            {
                var changedFiles = GetMainExcelFiles(GameDataType.DataTable, appConfigs.DataTables, tableFileChangedList);
                GameDataGenerator.RefreshAllDataTable(changedFiles);
                if (changedFiles.Contains(ConstEditor.UITableExcelFullPath))
                {
                    GameDataGenerator.GenerateUIFormNamesScript();
                }
                if (changedFiles.Contains(ConstEditor.EntityGroupTableExcelFullPath) ||
                        changedFiles.Contains(ConstEditor.SoundGroupTableExcelFullPath) ||
                        changedFiles.Contains(ConstEditor.UIGroupTableExcelFullPath) ||
                        changedFiles.Contains(ConstEditor.EntityGroupTableExcelFullPath))
                {
                    GameDataGenerator.GenerateGroupEnumScript();
                }
                foreach (var item in changedFiles)
                {
                    GFBuiltin.Log($"-----------------自动刷新DataTable:{item}-----------------");
                }
                tableFileChangedList.Clear();
            }
        }
        /// <summary>
        /// 根据改变的Excel列表获取所有对应的主文件列表
        /// </summary>
        /// <param name="tp"></param>
        /// <param name="relativeMainFiles"></param>
        /// <param name="changedFiles"></param>
        /// <returns></returns>
        private static IList<string> GetMainExcelFiles(GameDataType tp, IList<string> relativeMainFiles, IList<string> changedFiles)
        {
            IList<string> result = new List<string>();
            foreach (var changedFile in changedFiles)
            {
                var relativePathNoExt = GameDataGenerator.GetGameDataExcelRelativePath(tp, changedFile);
                foreach (var mainName in relativeMainFiles)
                {
                    if (relativePathNoExt.CompareTo(mainName) == 0 || relativePathNoExt.StartsWith(mainName + ConstBuiltin.AB_TEST_TAG))
                    {
                        var mainExcelFullPath = GameDataGenerator.GameDataExcelRelative2FullPath(tp, mainName);
                        if (!result.Contains(mainExcelFullPath))
                        {
                            result.Add(mainExcelFullPath);
                        }
                    }
                }
            }
            return result;
        }
   
        private static void OnDataTableChanged(object sender, FileSystemEventArgs e)
        {
            var fName = Path.GetFileNameWithoutExtension(e.Name);
            if (!fName.StartsWith("~$"))
            {
                tableFileChangedList.Add(e.FullPath);
            }
        }
    }

}
#endif
