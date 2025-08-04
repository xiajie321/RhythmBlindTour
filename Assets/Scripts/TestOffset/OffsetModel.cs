using System;
using System.Collections;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

namespace TestOffset
{
    [Serializable]
    public class OffsetModel : AbstractModel
    {
        public double offset;
        protected override void OnInit()
        {
            offset = 0d;
        }
    }
}
