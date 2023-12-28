using System;
using System.Collections.Generic;
using System.Text;

using CNC;
using ULOD.Runtime;
using UnityEngine;
using UnityEngine.Profiling;

namespace CustomProfiler
{
    internal static class PerformanceUtil
    {
        public static long FindObjectsSize<T>() where T : UnityEngine.Object
        {
            T[] results = Resources.FindObjectsOfTypeAll<T>();
            long size = 0;
            for (int i = 0; i < results.Length; i++)
            {
                long  objSize = Profiler.GetRuntimeMemorySizeLong(results[i]);
                size += objSize;
            }
            return size;
        }
        
        internal struct Sample
        {
            public string name;
            public long size;
        }
        
        private static List<Sample> s_Samples = new List<Sample>();
        public static string FindObjectsSizeTop<T>(int count = 10) where T : UnityEngine.Object
        {
            s_Samples.Clear();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[");
            T[] results = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < results.Length; i++)
            {
                var t = results[i];
                long objSize = Profiler.GetRuntimeMemorySizeLong(t);
                s_Samples.Add(new Sample()
                {
                    name = t.name,
                    size = objSize
                });
            }
            s_Samples.Sort(SampleComparer);
            for (int i = 0; i < s_Samples.Count && i< count; i++)
            {
                var s = s_Samples[i];
                stringBuilder.Append(s.name);
                stringBuilder.Append("=");
                stringBuilder.Append(s.size);
                stringBuilder.Append("$");
            }
            stringBuilder.Append("]");
            s_Samples.Clear();
            return stringBuilder.ToString();
        }
        
        
        private static int SampleComparer(Sample a, Sample b)
        {
            return b.size.CompareTo(a.size);;
        }
        
        public static int FindObjectsCount<T>() where T : UnityEngine.Object
        {
            T[] results = Resources.FindObjectsOfTypeAll<T>();
            return results.Length;
        }
        
        public static int GetMaxEffectCount()
        {
            return UEffectManager.MaxEffectCount;
        }
        
        public static int GetEffectCount()
        {
            if (UEffectManager.Instance!=null)
            {
                return UEffectManager.Instance.GetEffectCount();
            }
            return 0;
        }

        public static int GetLuaMemory()
        {
            if (GameManager.LuaMgr!=null)
            {
                return GameManager.LuaMgr.GetLuaMemory();
            }
            return 0;
        }

        public static long GetProcessMemory()
        {
#if UNITY_EDITOR
            return 0;
#elif  UNITY_ANDROID
            return GetAndroidPssMemory();
#endif
            return 0;
        }

        public static long GetAndroidPssMemory()
        {
#if UNITY_ANDROID
            try
            {
                using (var jo = new AndroidJavaObject("android.os.Debug"))
                {
                    using ( var obj = new AndroidJavaObject("android.os.Debug$MemoryInfo"))
                    {
                        jo.CallStatic("getMemoryInfo",obj);
                        var pss = obj.Call<string>("getMemoryStat", "summary.total-pss");
                        Debug.Log(pss);
                        if (long.TryParse(pss,out var mem))
                        {
                            return mem;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
#endif
            return 0;
        }
    }
}
