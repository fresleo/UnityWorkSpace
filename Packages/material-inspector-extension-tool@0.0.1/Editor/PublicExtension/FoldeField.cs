using UnityEditor;
using UnityEngine;

namespace MaterialInspectorExtensionTool.Editor.PublicExtension
{
    public class FoldeField : GUIBase
    {
        protected override void OnDispose()
        {
        }

        protected string mPath;

        public string Path => mPath;

        protected string mFolder;
        
        public string Folder
        {
            set
            {
                if (value == "Asset")
                {
                    mFolder = Application.dataPath;
                }
                else
                {
                    mFolder = value;
                }
            }
            get => mFolder;
        }

        public bool IsGetPath;
        
        protected string mTitle;
        protected string mDefaultName;

        public FoldeField(string path = "可以将文件夹拖到这里", string folder = "Asset", string title = "请选择文件夹", string defaultName = "")
        {
            mPath = path;
            Folder = folder;
            mTitle = title;
            mDefaultName = defaultName;
        }

        public override void OnGUI(Rect position)
        {
            var rects = position.HorizontalSplit(position.width - 30, 5);

            Rect leftRect = rects[0];
            Rect rightRect = rects[1];
            leftRect.xMin += 2;
            rightRect.xMax -= 2;

            var lastGUIEnabled = GUI.enabled;
            {
                GUI.enabled = false;
                mPath = GUI.TextField(leftRect, mPath);
            }
            GUI.enabled = lastGUIEnabled;

            if (GUI.Button(rightRect, GUIContents.Folder))
            {
                // 打开1个文件夹选择对话框
                var path = EditorUtility.OpenFolderPanel(mTitle, mFolder, mDefaultName);
                // 将全路径截取为工程内路径
                if (!string.IsNullOrEmpty(path))
                {
                    mPath = path.TryToAssetPath(out IsGetPath);
                }
                else
                {
                    mPath = null;
                    IsGetPath = false;
                }
            }

            // 拖入按钮 识别路径
            var info = DragAndDropTool.Drag(leftRect);
            if (info.EnterArea && !info.Dragging && info.Complete)
            {
                if (info.Paths[0].IsDirectory())
                {
                    mPath = info.Paths[0];
                    IsGetPath = true;
                }
                else
                {
                    mPath = "只能拖入文件夹";
                    IsGetPath = false;
                }
            }
        }
    }
}