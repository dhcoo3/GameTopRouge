using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AAAGame.ScriptsHotfix.GamePlay.Logic.Job
{
    public struct CalcDistanceRequest
    {
        public int UnitId;
        public fix2 UnitPos;
        public fix Distance;
    }
    
    public struct CalcDistancePosData
    {
        public bool Found;
        public int UnitId;
        public int Length;
    }
    
    [BurstCompile]
    public struct CalcDistanceJob : IJobParallelFor
    {
        [ReadOnly] public int MaxTargetCount;
        // 共享的只读数据
        [ReadOnly] public NativeArray<CalcDistanceRequest> needCalcDistanceUnit;
        // 共享的只读数据
        [ReadOnly] public NativeArray<CalcDistanceRequest> allUnitPos;
        // 批量输出：所有寻路结果
        [WriteOnly] public NativeArray<CalcDistancePosData> Results;
        
        [NativeDisableParallelForRestriction]
        public NativeArray<int> idDatas;

        public void Execute(int index)
        {
            var request = needCalcDistanceUnit[index];
            
            var result = new CalcDistancePosData
            {
                Found = false,
                UnitId = request.UnitId,
                Length = 0
            };

            NativeList<int> ids = new NativeList<int>(allUnitPos.Length, Allocator.TempJob);

            for (int i = 0; i < allUnitPos.Length; i++)
            {
                if (fixMath.distance(request.UnitPos, allUnitPos[i].UnitPos) >= request.Distance)
                {
                    ids.Add(allUnitPos[i].UnitId);
                }
            }

            if (ids.Length > 0)
            {
                result.Found = true;
                result.Length = ids.Length;
                
                int baseIndex = index * MaxTargetCount;
                
                for (int i = 0; i < math.min(ids.Length, MaxTargetCount); i++)
                {
                    idDatas[baseIndex + i] = ids[i];
                }
            }
            
            Results[index] = result;
        }
    }
}