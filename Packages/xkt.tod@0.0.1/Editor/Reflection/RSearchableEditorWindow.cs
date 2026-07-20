// Created By: WangYu  Date: 2025-03-12

using System;
using System.Reflection;
using UnityEditor;

namespace XKT.TOD
{
    public class RSearchableEditorWindow
    {
        private static Type s_type = typeof(SearchableEditorWindow);

        public static void SearchForReferencesToInstanceID(int instanceID)
        {
            var flags = BindingFlags.NonPublic | BindingFlags.Static;
            
            MethodInfo mi = s_type.GetMethod("SearchForReferencesToInstanceID", flags,
                null, 
                new Type[] { typeof(int) }, 
                null);
            if (mi == null) return;
            
            mi.Invoke(null, new object[] { instanceID });
        }
        
    }
}