using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static ErrorMessage;

namespace Need_for_Sleep
{
    internal class Util
    {

        public static float MapTo01range(int value, int min, int max)
        {
            float fl;
            int oldRange = max - min;

            if (oldRange == 0)
                fl = 0f;
            else
                fl = ((float)value - (float)min) / (float)oldRange;

            return Mathf.Clamp01(fl);
        }

        public static float MapTo01range(float value, float min, float max)
        {
            float fl;
            float oldRange = max - min;

            if (oldRange == 0)
                fl = 0f;
            else
                fl = (value - min) / oldRange;

            return Mathf.Clamp01(fl);
        }

        public static IEnumerator Spawn(TechType techType, IOut<GameObject> result, Vector3 pos = default, Vector3 rot = default)
        {
            //AddDebug("Spawn " + techType);
            GameObject prefab;
            TaskResult<GameObject> result_ = new TaskResult<GameObject>();
            yield return CraftData.GetPrefabForTechTypeAsync(techType, false, result_);
            prefab = result_.Get();
            GameObject go = prefab == null ? Utils.CreateGenericLoot(techType) : Utils.SpawnFromPrefab(prefab, null);
            if (go != null)
            {
                result.Set(go);
                if (pos == default)
                {
                    Transform camTr = MainCamera.camera.transform;
                    go.transform.position = camTr.position + camTr.forward * 3f;
                }
                else
                    go.transform.position = pos;

                if (rot == default)
                    go.transform.rotation = Quaternion.identity;
                else
                    go.transform.rotation = Quaternion.Euler(rot.x, rot.y, rot.z);

                //AddDebug($"Spawn {techType} {pos} {rot}");
                //CrafterLogic.NotifyCraftEnd(go, techType);
            }
        }

        public static float MapToRange(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            float oldRange = oldMax - oldMin;
            float newValue;

            if (oldRange == 0)
                newValue = newMin;
            else
            {
                float newRange = newMax - newMin;
                newValue = ((value - oldMin) * newRange) / oldRange + newMin;
            }
            return newValue;
        }


    }
}
