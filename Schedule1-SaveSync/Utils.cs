using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MelonLoader;
using UnityEngine;

namespace Schedule1_SaveSync
{
    internal class Utils
    {
        // util method to dump the UI hierarchy for debugging
        public static void DumpUIHierarchy(Transform root, int depth = 0)
        {
            string indent = new string(' ', depth * 2);
            MelonLogger.Msg($"{indent}- {root.name}");

            for (int i = 0; i < root.childCount; i++)
                DumpUIHierarchy(root.GetChild(i), depth + 1);
        }

    }
}
