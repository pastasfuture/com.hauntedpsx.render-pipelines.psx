using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.Rendering;

namespace HauntedPSX.RenderPipelines.PSX.Runtime
{
    public class PSXMSAAHandleSystem : IDisposable
    {
        bool m_DisposedValue = false;
        int timestampCurrent = 0;
        
        RTHandleSystem[] rtHandleSystems;
        int[] rtHandleSystemTimestamps;

        private static readonly int rtHandleSystemDisposeDelayTicks = 4;

        public void Dispose()
        {
            Dispose(true);
        }

        void Dispose(bool disposing)
        {
            if (!m_DisposedValue)
            {
                if (disposing)
                {
                    ReleaseAll();
                }

                m_DisposedValue = true;
                timestampCurrent = 0;
            }
        }
        
        void ReleaseAll()
        {
            if (rtHandleSystems != null)
            {
                for (int i = 0; i < rtHandleSystems.Length; ++i)
                {
                    if (rtHandleSystems[i] != null)
                    {
                        rtHandleSystems[i].Dispose();
                        rtHandleSystems[i] = null;
                        rtHandleSystemTimestamps[i] = -1;
                    }
                }

                rtHandleSystems = null;
                rtHandleSystemTimestamps = null;
            }
        }

        public void Tick()
        {
            ++timestampCurrent;
            MaybeReleaseUnusedSystems();
        }

        public RTHandleSystem Ensure(MSAASamples msaaSamples, int width, int height)
        {
            if (rtHandleSystems == null)
            {
                rtHandleSystems = new RTHandleSystem[GetSystemCount()];
                rtHandleSystemTimestamps = new int[GetSystemCount()];
                for (int i = 0; i < GetSystemCount(); ++i)
                {
                    rtHandleSystems[i] = null;
                    rtHandleSystemTimestamps[i] = -1;
                }
            }

            int index = GetIndexFromMSAASamples(msaaSamples);
            if (rtHandleSystems[index] == null)
            {
                rtHandleSystems[index] = new RTHandleSystem();
                rtHandleSystems[index].Initialize(width, height
#if !UNITY_2021_2_OR_NEWER
                    , scaledRTsupportsMSAA: index > 0,
                    msaaSamples
#endif
                );
            }

            rtHandleSystemTimestamps[index] = timestampCurrent;
            return rtHandleSystems[index];
        }

        public bool TryGet(out RTHandleSystem res, MSAASamples msaaSamples)
        {
            res = null;
            int index = GetIndexFromMSAASamples(msaaSamples);
            if (rtHandleSystems == null || rtHandleSystems[index] == null)
            {
                return false;
            }

            res = rtHandleSystems[index];
            return true;
        }
        
        private void MaybeReleaseUnusedSystems()
        {
            if (rtHandleSystems == null) { return; }

            for (int i = 0; i < rtHandleSystems.Length; ++i)
            {
                if (rtHandleSystems[i] != null)
                {
                    if ((rtHandleSystemTimestamps[i] + rtHandleSystemDisposeDelayTicks) <= timestampCurrent)
                    {
                        rtHandleSystems[i].Dispose();
                        rtHandleSystems[i] = null;
                        rtHandleSystemTimestamps[i] = -1;
                    }
                }
                
            }
        }
        
        private static int GetIndexFromMSAASamples(MSAASamples msaaSamples)
        {
            switch (msaaSamples)
            {
                case MSAASamples.None: return 0;
                case MSAASamples.MSAA2x: return 1;
                case MSAASamples.MSAA4x: return 2;
                case MSAASamples.MSAA8x: return 3;
                default:
                {
                    Debug.Assert(false);
                    return -1;
                }
            }
        }
        
        private static int GetSystemCount()
        {
            return 4;
        }
    }
}
