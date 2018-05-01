using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace ASE_to_Unity {
    public static class AseUtils {

        /// <summary>
        /// Copy component original and paste its values to a new component in the destination GameObject
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="original"></param>
        /// <param name="destination"></param>
        /// <returns></returns>
        public static T CopyComponent<T>(T original, GameObject destination) where T : Component {
            Type type = original.GetType();
            if (original is Transform) return null;
            Component copy = destination.AddComponent(type);
            FieldInfo[] fields = type.GetFields();
            foreach (FieldInfo field in fields) {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        /// <summary>
        /// Get the file's name, without any path related elements
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public static string StripPath(string option) {
            string s = option.Replace("\\", "/");
            return s.Contains("/") ? s.Substring(s.LastIndexOf("/") + 1) : s;
        }

        /// <summary>
        /// put the first character of string to uppercase
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string UppercaseFirst(string s) {
            if (string.IsNullOrEmpty(s)) {
                return string.Empty;
            }
            char[] a = s.ToCharArray();
            a[0] = char.ToUpper(a[0]);
            return new string(a);
        }
    }
}
