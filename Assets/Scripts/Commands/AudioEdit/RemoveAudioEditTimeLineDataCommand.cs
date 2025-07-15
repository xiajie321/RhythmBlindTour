using Qf.ClassDatas.AudioEdit;
using Qf.Events;
using Qf.Models.AudioEdit;
using QFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Qf.Commands.AudioEdit
{
    /// <summary>
    /// 删除对应时间轴下的信息
    /// </summary>
    public class RemoveAudioEditTimeLineDataCommand : AbstractCommand
    {
        float time;
        int index = -1;
        AudioEditModel model;
        public RemoveAudioEditTimeLineDataCommand(float time)
        {
            this.time = time;
        }//清除时间轴上的所有信息
        public RemoveAudioEditTimeLineDataCommand(float time, int index)
        {
            this.time = time;
            this.index = index;
        }//清除时间轴上的指定信息
        protected override void OnExecute()
        {
            model = this.GetModel<AudioEditModel>();
            if (model.EditAudioClip == null || model.Mode.Equals(SystemModeData.PlayMode)) return;
            if (time == -1)
            {
                model.TimeLineData.Clear();
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
                return;
            }
            if (index == -1)
            {
                if (model.TimeLineData.ContainsKey(time))
                    model.TimeLineData.Remove(time);
                this.SendEvent<OnUpdateAudioEditDrumsUI>();
                return;
            }
            // 只做一次删除和清理key！
            if (model.TimeLineData.ContainsKey(time))
            {
                if (model.TimeLineData[time].Count > index)
                    model.TimeLineData[time].RemoveAt(index); // 更安全写法

                // 如果该 time 下鼓点已全部删除，则清理 key
                if (model.TimeLineData[time].Count == 0)
                    model.TimeLineData.Remove(time);
            }
            this.SendEvent<OnUpdateAudioEditDrumsUI>();
        }

    }
}
