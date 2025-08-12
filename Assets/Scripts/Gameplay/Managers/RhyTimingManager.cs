using System;
using UnityEngine;

namespace Gameplay.Managers
{
    public class RhyTimingManager : MonoBehaviour
    {
        public static RhyTimingManager Instance{get; private set;}

        private void Awake()
        {
            Instance = this;
        }

        public float velocity = 30f;
        public int bpm = 120;

        public float CalculatePositionByTiming(int timing)
        {
            return CalculatePositionByTimingAndStart(RhyGameplayManager.Instance.ChartTiming, timing);
        }
        public float CalculatePositionByTimingAndStart(int pivotTiming, int targetTiming)
        {
            int deltaTime = targetTiming - pivotTiming;
            return deltaTime * velocity;
        }
        
    }
}