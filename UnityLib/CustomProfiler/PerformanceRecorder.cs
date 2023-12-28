using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Profiling.Memory.Experimental;

namespace CustomProfiler
{
    [Serializable]
    internal struct PerformanceRecordData
    {
        public float time;
        public int frameCount;
        public string value;
        public PerformanceRecordData(long recordValue)
        {
            time = Time.time;
            frameCount = Time.frameCount;
            value = recordValue.ToString();
        }
        
        public PerformanceRecordData(string recordValue)
        {
            time = Time.time;
            frameCount = Time.frameCount;
            value = recordValue.ToString();
        }
    }

    [Serializable]
    internal class PerformanceCaptureSetting
    {
        public string category;
        public string key;
        public string des;
        [NonSerialized] 
        public Action<PerformanceCaptureSetting> sampleFunction;
        public int internalFrameTime;
        public int lastCaptureFrameTime;
        public long maxValue;
        public Queue<PerformanceRecordData> results = new Queue<PerformanceRecordData>();
        public EUnitConversion unitConversion;
    }

    public enum EUnitConversion
    {
        UnitByte = 0,
        UnitKb = 1,
        Unit = 2,
        UnitK = 3,
    }

    [Serializable]
    internal struct OffLineData
    {
        public string category;
        public string name;
        public string des;
        public string maxValue;
        public string value;
        // 0=bytes  1=kb  2=mb 3=count
        public EUnitConversion unitConversion;

        public override string ToString()
        {
            return $"{this.category},{this.name},{this.des},{(int) this.unitConversion},{this.maxValue},{this.value}";
        }
    }

    public class PerformanceRecorder : MonoBehaviour
    {
        public bool EnableAutoSample = false;
        public static readonly int MaxCacheCountPer = 100;
        public const int MBToBytes = 1024 * 1024;
        public const int MBToKbs = 1024;
        public const float BytesToMB = 1.0f / (1024 * 1024);
        public const float BytesToKB = 1.0f / 1024;
        private List<PerformanceCaptureSetting> m_performanceDatas = new List<PerformanceCaptureSetting>();

        private static PerformanceRecorder s_instance;

        public static PerformanceRecorder Get()
        {
            if (s_instance == null)
            {
                s_instance = new GameObject("PerformanceRecorder").AddComponent<PerformanceRecorder>();
                s_instance.Start();
                DontDestroyOnLoad(s_instance.gameObject);
            }

            return s_instance;
        }

        private bool m_init = false;
        private ProfilerRecorder drawCallsRecorder;
        private ProfilerRecorder verticesRecorder;
        private ProfilerRecorder trianglesRecorder;
        private ProfilerRecorder setPassRecorder;

        private void Start()
        {
            if (m_init)
            {
                return;
            }

            m_init = true;
            RegisterPerformanceRecorder();
            drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
            verticesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
            trianglesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
        }

        private void LateUpdate()
        {
            if (EnableAutoSample)
            {
                Profiler.BeginSample("PerformanceRecorder LateUpdate");
                for (int i = 0; i < m_performanceDatas.Count; i++)
                {
                    var item = m_performanceDatas[i];
                    if ((Time.frameCount - item.lastCaptureFrameTime) >= item.internalFrameTime)
                    {
                        item.sampleFunction.Invoke(item);
                        if (item.results.Count > MaxCacheCountPer)
                        {
                            item.results.Dequeue();
                        }

                        item.lastCaptureFrameTime = Time.frameCount;
                    }
                }

                Profiler.EndSample();
            }
        }

        private void OnDestroy()
        {
            s_instance = null;
        }

        [ContextMenu("Peek Performance")]
        public void Peek()
        {
            Sample();
            Debug.Log(Newtonsoft.Json.JsonConvert.SerializeObject(m_performanceDatas, Formatting.Indented));
        }

        private void Sample()
        {
            for (int i = 0; i < m_performanceDatas.Count; i++)
            {
                var item = m_performanceDatas[i];
                item.sampleFunction.Invoke(item);
                item.lastCaptureFrameTime = Time.frameCount;
            }
        }

        [ContextMenu("Test Save")]
        public void TestSave()
        {
            SampleAndSave("Test");
        }

        public void SampleAndSave(string saveTag)
        {
            Sample();
            UpOffLineLoadMaxData(saveTag);
        }


        private string GetDescription()
        {
            return "Nothing";
        }

        public string SampleFrameData(string saveTag)
        {
            Sample();
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("---[SystemInfo]");
            sb.AppendLine(GetSystem());
            sb.AppendLine("Des:" + GetDescription());
            sb.AppendLine("Tag:" + saveTag);
            sb.AppendLine("---[Data]");
            StringBuilder top = new StringBuilder();
            foreach (var captureItem in m_performanceDatas)
            {
                string max = "0";
                while (captureItem.results.TryDequeue(out var item))
                {
                    if (captureItem.results.Count == 0)
                    {
                        max = item.value;
                    }
                }
                OffLineData offLineData = new OffLineData();
                offLineData.unitConversion = captureItem.unitConversion;
                offLineData.category = captureItem.category;
                offLineData.name = captureItem.key;
                offLineData.maxValue = captureItem.maxValue.ToString();
                offLineData.des = captureItem.des;
                offLineData.value = max;
                if (captureItem.category == "Top")
                {
                    top.AppendLine(offLineData.ToString());
                }
                else
                {
                    sb.AppendLine(offLineData.ToString());
                }
            }
            sb.AppendLine("---[Top]");
            sb.AppendLine(top.ToString());
            
            // snap 内存
            // MemoryProfiler.TakeSnapshot($"{saveTag}.snap", (str, suc) =>
            // {
            //     Debug.Log(str);
            // });
            return sb.ToString();
        }

        /// <summary>
        ///  写数据
        /// </summary>
        /// <param name="captureTag">标签</param>
        private void UpOffLineLoadMaxData(string captureTag)
        {
            using (var fs = SafeGet(captureTag))
            {
                using (StreamWriter streamWriter = new StreamWriter(fs, Encoding.UTF8))
                {
                    string v = SampleFrameData(captureTag);
                    streamWriter.Write(v);
                    // streamWriter.WriteLine("---[SystemInfo]");
                    // streamWriter.Write(GetSystem());
                    // streamWriter.WriteLine("Des:" + GetDescription());
                    // streamWriter.WriteLine("TestTag:" + captureTag);
                    // streamWriter.WriteLine("---[Data]");
                    // foreach (var captureItem in m_performanceDatas)
                    // {
                    //     string max = "0";
                    //     while (captureItem.results.TryDequeue(out var item))
                    //     {
                    //         if (captureItem.results.Count == 0)
                    //         {
                    //             max = item.value;
                    //         }
                    //     }
                    //     OffLineData offLineData = new OffLineData();
                    //     offLineData.unitConversion = captureItem.unitConversion;
                    //     offLineData.category = captureItem.category;
                    //     offLineData.name = captureItem.key;
                    //     offLineData.maxValue = captureItem.maxValue.ToString();
                    //     offLineData.des = captureItem.des;
                    //     offLineData.value = max.ToString();
                    //     WriteToStream(streamWriter, offLineData);
                    // }
                }
            }
        }
        //
        // private void WriteToStream(StreamWriter streamWriter, OffLineData offLineData)
        // {
        //     streamWriter.WriteLine($"{offLineData.category},{offLineData.name},{offLineData.des},{(int) offLineData.unitConversion},{offLineData.maxValue},{offLineData.value}");
        // }

        private FileStream SafeGet(string captureTag)
        {
#if UNITY_EDITOR
            return new FileStream(Path.Combine(Application.dataPath, "..", "Temp", $"{captureTag}.txt"), FileMode.Create, FileAccess.ReadWrite);
#endif
            return new FileStream(Path.Combine(Application.persistentDataPath, $"{captureTag}.txt"), FileMode.Create, FileAccess.ReadWrite);
        }

        private void RegisterPerformanceRecorder()
        {
            RegisterTotalMemory();
            RegisterGPU();
            RegisterUnityObjectMemory();
            RegisterUnityObjectCount();
            RegisterUnityObjectTop();
        }

        public string GetSystem()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine(SystemInfo.deviceName);
            stringBuilder.AppendLine(SystemInfo.operatingSystem + " | " + SystemInfo.graphicsDeviceVersion);
            return stringBuilder.ToString();
        }

        private void RegisterTotalMemory()
        {
            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Memory",
                key = "total-pss",
                des = "android total-pss",
                internalFrameTime = 30,
                maxValue = 1000 * MBToKbs,
                unitConversion = EUnitConversion.UnitKb,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.GetProcessMemory()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Memory",
                key = "ReservedMemory",
                des = "Total Reserved memory by Unity",
                internalFrameTime = 30,
                maxValue = 350 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(Profiler.GetTotalReservedMemoryLong()));
                }
            });
            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Memory",
                key = "AllocatedMemory",
                des = "Allocated memory by Unity",
                internalFrameTime = 30,
                maxValue = 350 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(Profiler.GetTotalAllocatedMemoryLong()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Memory",
                key = "UnusedReservedMemory",
                des = "Reserved but not allocated",
                internalFrameTime = 30,
                maxValue = 200 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(Profiler.GetTotalUnusedReservedMemoryLong()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Memory",
                key = "MonoHeap",
                des = "Allocated Mono heap size",
                internalFrameTime = 100,
                maxValue = 130 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(Profiler.GetMonoHeapSizeLong()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Memory",
                key = "MonoUsed",
                des = "Mono used size",
                internalFrameTime = 100,
                maxValue = 120 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(Profiler.GetMonoUsedSizeLong()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Memory",
                key = "GraphicsDriver",
                des = "AllocatedMemoryForGraphicsDriver",
                internalFrameTime = 100,
                maxValue = 100 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(Profiler.GetAllocatedMemoryForGraphicsDriver()));
                }
            });


            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Memory",
                key = "LuaMemory",
                des = "LuaMemory",
                internalFrameTime = 100,
                maxValue = 100 * MBToKbs,
                unitConversion = EUnitConversion.UnitKb,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.GetLuaMemory()));
                }
            });
        }

        private void RegisterUnityObjectMemory()
        {
            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectMemory",
                key = "RenderTexture",
                des = "Unity RT Size",
                internalFrameTime = 30,
                maxValue = 70 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSize<RenderTexture>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectMemory",
                key = "Shader",
                des = "Unity Shader Size",
                internalFrameTime = 30,
                maxValue = 50 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSize<Shader>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectMemory",
                key = "Mesh",
                des = "Unity Mesh Size",
                internalFrameTime = 30,
                maxValue = 30 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSize<Mesh>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectMemory",
                key = "Font",
                des = "Unity Font Size",
                internalFrameTime = 30,
                maxValue = 16 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSize<Font>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectMemory",
                key = "Texture2D",
                des = "Unity Texture2D Size",
                internalFrameTime = 30,
                maxValue = 75 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSize<Texture2D>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectMemory",
                key = "Texture2DArray",
                des = "Unity Texture2DArray Size",
                internalFrameTime = 30,
                maxValue = 40 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSize<Texture2DArray>()));
                }
            });
            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectMemory",
                key = "ParticleSystem",
                des = "Unity ParticleSystem Size",
                internalFrameTime = 30,
                maxValue = 10 * MBToBytes,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSize<ParticleSystem>()));
                }
            });
        }
        private void RegisterUnityObjectCount()
        {
            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectCount",
                key = "RenderTexture",
                des = "Unity RT Count",
                internalFrameTime = 30,
                unitConversion = EUnitConversion.Unit,
                maxValue = 30,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsCount<RenderTexture>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectCount",
                key = "Shader",
                des = "Unity Shader Count",
                unitConversion = EUnitConversion.Unit,
                internalFrameTime = 30,
                maxValue = 100,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsCount<Shader>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectCount",
                key = "Mesh",
                des = "Unity Mesh Count",
                unitConversion = EUnitConversion.Unit,
                internalFrameTime = 30,
                maxValue = 400,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsCount<Mesh>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectCount",
                key = "Font",
                des = "Unity Font Count",
                unitConversion = EUnitConversion.Unit,
                internalFrameTime = 30,
                maxValue = 10,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsCount<Font>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectCount",
                key = "Texture2D",
                des = "Unity Texture2D Count",
                internalFrameTime = 30,
                unitConversion = EUnitConversion.Unit,
                maxValue = 600,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsCount<Texture2D>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectCount",
                key = "Texture2DArray",
                des = "Unity Texture2DArray Count",
                internalFrameTime = 30,
                maxValue = 100,
                unitConversion = EUnitConversion.Unit,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsCount<Texture2DArray>()));
                }
            });
            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectCount",
                key = "ParticleSystem",
                des = "Unity ParticleSystem Count",
                unitConversion = EUnitConversion.Unit,
                internalFrameTime = 30,
                maxValue = 300,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsCount<ParticleSystem>()));
                }
            });
            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "UnityObjectCount",
                key = "GameObject",
                des = "Unity GameObject Count",
                unitConversion = EUnitConversion.Unit,
                internalFrameTime = 30,
                maxValue = 3000,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsCount<GameObject>()));
                }
            });
        }
        
        private void RegisterUnityObjectTop()
        {
            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Top",
                key = "RenderTexture",
                des = "Unity RT Count",
                internalFrameTime = 30,
                maxValue = 30,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSizeTop<RenderTexture>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Top",
                key = "Shader",
                des = "Unity Shader Count",
                internalFrameTime = 30,
                maxValue = 100,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSizeTop<Shader>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Top",
                key = "Mesh",
                des = "Unity Mesh Count",
                internalFrameTime = 30,
                maxValue = 100,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSizeTop<Mesh>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Top",
                key = "Font",
                des = "Unity Font Count",
                internalFrameTime = 30,
                maxValue = 100,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSizeTop<Font>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Top",
                key = "Texture2D",
                des = "Unity Texture2D Count",
                internalFrameTime = 30,
                maxValue = 100,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSizeTop<Texture2D>()));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Top",
                key = "Texture2DArray",
                des = "Unity Texture2DArray Count",
                internalFrameTime = 30,
                maxValue = 100,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsSizeTop<Texture2DArray>()));
                }
            });
        }
        private void RegisterGPU()
        {
            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "GPU",
                key = "Drawcall",
                des = "Drawcall",
                internalFrameTime = 30,
                maxValue = 300,
                unitConversion = EUnitConversion.Unit,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(drawCallsRecorder.LastValue));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "GPU",
                key = "SetPass",
                des = "SetPass",
                internalFrameTime = 30,
                maxValue = 300,
                unitConversion = EUnitConversion.Unit,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(setPassRecorder.LastValue));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "GPU",
                key = "Triangles",
                des = "Triangles",
                internalFrameTime = 30,
                maxValue = 30000,
                unitConversion = EUnitConversion.UnitK,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(trianglesRecorder.LastValue));
                }
            });

            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "GPU",
                key = "VerticesCount",
                des = "顶点数量",
                internalFrameTime = 30,
                maxValue = 100000,
                unitConversion = EUnitConversion.UnitK,
                sampleFunction = (item) =>
                {
                    item.results.Enqueue(new PerformanceRecordData(verticesRecorder.LastValue));
                }
            });

            //
            // m_performanceDatas.Add(new PerformanceCaptureSetting()
            // {
            //     category = "GPU",
            //     key = "ShaderCount",
            //     des = "Shader数量",
            //     internalFrameTime = 30,
            //     maxValue = 300,
            //     unitConversion = EUnitConversion.Unit,
            //     sampleFunction = (item) =>
            //     {
            //         item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsCount<Shader>()));
            //     }
            // });
            //
            // m_performanceDatas.Add(new PerformanceCaptureSetting()
            // {
            //     category = "GPU",
            //     key = "MeshCount",
            //     des = "Mesh数量",
            //     internalFrameTime = 30,
            //     maxValue = 300,
            //     unitConversion = EUnitConversion.Unit,
            //     sampleFunction = (item) =>
            //     {
            //         item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.FindObjectsCount<Mesh>()));
            //     }
            // });
        }

        private void RegisterParticle()
        {
            m_performanceDatas.Add(new PerformanceCaptureSetting()
            {
                category = "Effect",
                key = "EffectCount",
                des = "战斗特效数量",
                internalFrameTime = 1,
                maxValue = 150,
                sampleFunction = (item) =>
                {
                    item.maxValue = PerformanceUtil.GetMaxEffectCount();
                    item.results.Enqueue(new PerformanceRecordData(PerformanceUtil.GetEffectCount()));
                },
                unitConversion = EUnitConversion.Unit,
            });
        }
    }
}
